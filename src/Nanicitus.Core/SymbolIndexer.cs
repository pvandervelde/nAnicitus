﻿//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zip;
using Nanicitus.Core.Properties;
using Nuclei.Configuration;
using Nuclei.Diagnostics;
using Nuclei.Diagnostics.Logging;
using NuGet;

namespace Nanicitus.Core
{
    /// <summary>
    /// Handles the indexing of symbols and sources.
    /// </summary>
    internal sealed class SymbolIndexer : IIndexSymbols, IDisposable
    {
        /// <summary>
        /// The HResult value that indicates that the file is locked by the operating system.
        /// </summary>
        private const uint FileLocked = 0x80070020;

        /// <summary>
        /// The HResult value that indicates that a portion of the file is locked by the operating
        /// system.
        /// </summary>
        private const uint PortionOfFileLocked = 0x80070021;

        /// <summary>
        /// Indicates the maximum number of times the indexer will try to process a package
        /// before giving up.
        /// </summary>
        private const int MaximumNumberOfTimesPackageCanBeProcessed = 3;

        /// <summary>
        /// Indicates the maximum number of times that the process will wait for a locked file before
        /// giving up and moving on.
        /// </summary>
        private const int MaximumNumberOfTimesWaitingForPackageFileLock = 3;

        /// <summary>
        /// The amount of time the process sleeps when it encounters a file that is locked by the
        /// operating system.
        /// </summary>
        private const int PackageFileLockSleepTimeInMilliSeconds = 5000;

        /// <summary>
        /// Returns a value indicating if the file is locked for reading.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <returns>
        ///     <see langword="true"/> if the file is locked for reading; otherwise <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// Original code here: http://stackoverflow.com/a/14132721/539846.
        /// </remarks>
        private static bool IsAvailableForReading(string path)
        {
            try
            {
                using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    // Do nothing. Just want to know if the file is locked.
                }

                return true;
            }
            catch (IOException e)
            {
                var errorCode = (uint)Marshal.GetHRForException(e);
                if (errorCode != FileLocked && errorCode != PortionOfFileLocked)
                {
                    throw;
                }

                return false;
            }
        }

        private static string CreateUnzipDirectory()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var attributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            Debug.Assert(attributes.Length == 1, "There should only be 1 AssemblyProductAttribute");

            var product = ((AssemblyProductAttribute)attributes[0]).Product;
            var path = Path.Combine(
                Path.GetTempPath(),
                product,
                Guid.NewGuid().ToString("D"));

            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (IOException)
                {
                }
            }

            return path;
        }

        /// <summary>
        /// Execute a command and return the output.
        /// </summary>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="args">Arguments to the command.</param>
        /// <returns>The output of the command.</returns>
        private static string Execute(string cmd, string args)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo(cmd, args)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            return output;
        }

        private static string[] GetPdbFiles(string unpackLocation)
        {
            var pdbFiles = Directory.GetFiles(unpackLocation, "*.pdb", SearchOption.AllDirectories);
            return pdbFiles;
        }

        private static int NumberOfMatchingCharactersStartingFromTheEnd(string first, string second)
        {
            var longer = (first.Length > second.Length) ? first : second;
            var shorter = (longer == first) ? second : first;

            var longerLastCharacterIndex = longer.Length - 1;
            var shorterLastCharacterIndex = shorter.Length - 1;

            var counter = 0;
            for (int i = 0; i < shorter.Length; i++)
            {
                if (longer[longerLastCharacterIndex - i] != shorter[shorterLastCharacterIndex - i])
                {
                    break;
                }

                counter++;
            }

            return counter;
        }

        private static Dictionary<string, string> CalculateRelativePaths(string sourceLocation, IEnumerable<string> files)
        {
            var result = new Dictionary<string, string>();

            var sourceFiles = Directory.GetFiles(sourceLocation, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);

                // The relative path should be the file path relative to the sourceLocation
                var matchingFiles = sourceFiles
                    .Where(f => f.Contains(fileName))
                    .Select(f => f.Substring(sourceLocation.Length).TrimStart(Path.DirectorySeparatorChar))
                    .OrderByDescending(f => NumberOfMatchingCharactersStartingFromTheEnd(file, f))
                    .ToList();

                if (matchingFiles.Count == 0)
                {
                    continue;
                }

                var matchedFile = matchingFiles.First();
                result.Add(file, matchedFile);
            }

            return result;
        }

        private static string BuildSourcePath(string project, string version)
        {
            // The string format template for the layout of the source indexing directory.
            const string sourceStoragePathTemplate = @"{0}\{1}\";

            return string.Format(
                CultureInfo.InvariantCulture,
                sourceStoragePathTemplate,
                project,
                version);
        }

        private static string FormatVersion(Version version)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}.{1}.{2}.{3}",
                version.Major,
                version.Minor,
                version.Build,
                version.Revision);
        }

        /// <summary>
        /// The object used to lock on.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// The collection that holds information about packages that could not be processed because
        /// the package file was locked when the indexer first tried to access it
        /// (see https://github.com/pvandervelde/nAnicitus/issues/1).
        /// </summary>
        private readonly Queue<Tuple<string, int>> _lockedPackages
            = new Queue<Tuple<string, int>>();

        /// <summary>
        /// The queue that stores the location of the non-processed packages.
        /// </summary>
        private readonly IQueueSymbolPackages _queue;

        /// <summary>
        /// The object that provides the diagnostics methods for the application.
        /// </summary>
        private readonly SystemDiagnostics _diagnostics;

        /// <summary>
        /// The full path to the 'srctool.exe' application that is used
        /// to extract source file information from a PDB file.
        /// </summary>
        private readonly string _srcToolPath;

        /// <summary>
        /// The full path to the 'pdbstr.exe' application that is used
        /// to write source index information to a PDB file.
        /// </summary>
        private readonly string _pdbStrPath;

        /// <summary>
        /// The full path to the 'symstore.exe' application that is used
        /// to store the symbols.
        /// </summary>
        private readonly string _symStorePath;

        /// <summary>
        /// The UNC path to the location where the sources are indexed.
        /// </summary>
        private readonly string _sourceUncPath;

        /// <summary>
        /// The UNC path to the location where the symbols are indexed.
        /// </summary>
        private readonly string _symbolsUncPath;

        /// <summary>
        /// The full path to the location where the processed packages are stored
        /// in case they are needed at a later stage.
        /// </summary>
        private readonly string _processedPackagesPath;

        /// <summary>
        /// The directory in which the packages will be unzipped.
        /// </summary>
        private readonly string _unpackDirectory = CreateUnzipDirectory();

        /// <summary>
        /// The task that handles the actual symbol indexing process.
        /// </summary>
        private Task _worker;

        /// <summary>
        /// The cancellation source that is used to cancel the worker task.
        /// </summary>
        private CancellationTokenSource _cancellationSource;

        /// <summary>
        /// A flag indicating if the processing of symbols has started or not.
        /// </summary>
        private bool _isStarted;

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolIndexer"/> class.
        /// </summary>
        /// <param name="packageQueue">The object that queues packages that need to be processed.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="diagnostics">The object that provides the diagnostics methods for the application.</param>
        public SymbolIndexer(
            IQueueSymbolPackages packageQueue,
            IConfiguration configuration,
            SystemDiagnostics diagnostics)
        {
            {
                Lokad.Enforce.Argument(() => packageQueue);
                Lokad.Enforce.Argument(() => configuration);
                Lokad.Enforce.Argument(() => diagnostics);

                Lokad.Enforce.With<ArgumentException>(
                    configuration.HasValueFor(CoreConfigurationKeys.SourceIndexUncPath),
                    Resources.Exceptions_Messages_MissingConfigurationValue_WithKey,
                    CoreConfigurationKeys.SourceIndexUncPath);
                Lokad.Enforce.With<ArgumentException>(
                    configuration.HasValueFor(CoreConfigurationKeys.SymbolsIndexUncPath),
                    Resources.Exceptions_Messages_MissingConfigurationValue_WithKey,
                    CoreConfigurationKeys.SymbolsIndexUncPath);
                Lokad.Enforce.With<ArgumentException>(
                    configuration.HasValueFor(CoreConfigurationKeys.ProcessedPackagesPath),
                    Resources.Exceptions_Messages_MissingConfigurationValue_WithKey,
                    CoreConfigurationKeys.ProcessedPackagesPath);
            }

            _diagnostics = diagnostics;
            _queue = packageQueue;
            _queue.OnEnqueue += HandleOnEnqueue;

            var debuggingToolsDirectory = configuration.Value<string>(CoreConfigurationKeys.DebuggingToolsDirectory);
            _symStorePath = Path.Combine(debuggingToolsDirectory, "symstore.exe");
            _srcToolPath = Path.Combine(debuggingToolsDirectory, "srcsrv", "srctool.exe");
            _pdbStrPath = Path.Combine(debuggingToolsDirectory, "srcsrv", "pdbstr.exe");
            _sourceUncPath = configuration.Value<string>(CoreConfigurationKeys.SourceIndexUncPath);
            _symbolsUncPath = configuration.Value<string>(CoreConfigurationKeys.SymbolsIndexUncPath);
            _processedPackagesPath = configuration.Value<string>(CoreConfigurationKeys.ProcessedPackagesPath);
        }

        private void HandleOnEnqueue(object sender, EventArgs e)
        {
            lock (_lock)
            {
                if (!_isStarted)
                {
                    _diagnostics.Log(
                        LevelToLog.Trace,
                        Resources.Log_Messages_SymbolIndexer_NewItemInQueue_ProcessingNotStarted);

                    return;
                }

                if (_worker != null)
                {
                    _diagnostics.Log(
                        LevelToLog.Trace,
                        Resources.Log_Messages_SymbolIndexer_NewItemInQueue_WorkerAlreadyExists);

                    return;
                }

                _diagnostics.Log(
                    LevelToLog.Trace,
                    Resources.Log_Messages_SymbolIndexer_NewItemInQueue_StartingThread);

                _cancellationSource = new CancellationTokenSource();
                _worker = Task.Factory.StartNew(
                    () => ProcessSymbols(_cancellationSource.Token),
                    _cancellationSource.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }
        }

        /// <summary>
        /// Starts the symbol indexing process.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                _isStarted = true;
            }
        }

        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Do not want the application to crash because the processor dies.")]
        private void ProcessSymbols(CancellationToken token)
        {
            try
            {
                _diagnostics.Log(LevelToLog.Trace, Resources.Log_Messages_SymbolIndexer_StartingSymbolProcessing);

                while (!token.IsCancellationRequested)
                {
                    if (_queue.IsEmpty && (_lockedPackages.Count == 0))
                    {
                        _diagnostics.Log(LevelToLog.Trace, Resources.Log_Messages_SymbolIndexer_QueueEmpty);
                        break;
                    }

                    string packageFile;
                    ZipPackage package;
                    if (!LoadPackage(out packageFile, out package))
                    {
                        continue;
                    }

                    var project = package.Id;
                    var version = package.Version.Version;

                    _diagnostics.Log(
                        LevelToLog.Info,
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.Log_Messages_SymbolIndexer_ProcessingPackage_WithIdAndVersion,
                            project,
                            version));

                    // Unpack package file in temp location
                    var processingWasSuccessful = true;
                    var unpackLocation = Unpack(packageFile, project, version);
                    try
                    {
                        IndexSymbols(unpackLocation, project, version);
                        UploadSources(unpackLocation, project, version);
                        UploadSymbols(unpackLocation, project, version);
                    }
                    catch (Exception e)
                    {
                        processingWasSuccessful = false;

                        _diagnostics.Log(
                            LevelToLog.Error,
                            string.Format(
                                CultureInfo.InvariantCulture,
                                Resources.Log_Messages_SymbolIndexer_ProcessingFailed_WithExceptionAndPackageDetails,
                                e,
                                project,
                                version));
                    }
                    finally
                    {
                        try
                        {
                            Directory.Delete(unpackLocation, true);
                        }
                        catch (IOException e)
                        {
                            _diagnostics.Log(
                                LevelToLog.Error,
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    Resources.Log_Messages_SymbolIndexer_PackageDeleteFailed_WithExceptionAndPackageDetails,
                                    e,
                                    project,
                                    version));
                        }

                        if (processingWasSuccessful)
                        {
                            MarkSymbolsAsProcessed(packageFile, project, version);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _diagnostics.Log(
                    LevelToLog.Error,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_SymbolIndexer_ProcessSymbolsFailed_WithException,
                        e));
            }
            finally
            {
                CleanUpWorkerTask();
            }
        }

        private bool LoadPackage(out string packageFile, out ZipPackage package)
        {
            packageFile = null;
            package = null;

            int processCount = 0;
            if (!_queue.IsEmpty)
            {
                packageFile = _queue.Dequeue();
            }
            else
            {
                if (_lockedPackages.Count > 0)
                {
                    var pair = _lockedPackages.Dequeue();
                    packageFile = pair.Item1;
                    processCount = pair.Item2;
                }
            }

            if (packageFile == null)
            {
                _diagnostics.Log(LevelToLog.Trace, Resources.Log_Messages_SymbolIndexer_PackageNotDefined);
                return false;
            }

            if (processCount > MaximumNumberOfTimesPackageCanBeProcessed)
            {
                _diagnostics.Log(
                    LevelToLog.Warn,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_PackageLoadCountBeyondMaximum_WithPackagePath,
                        packageFile));
                return false;
            }

            package = LoadSymbolPackage(packageFile, processCount);
            return package != null;
        }

        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Do not want the application to crash if there is an error loading symbols.")]
        private ZipPackage LoadSymbolPackage(string packageFile, int processCount)
        {
            try
            {
                int waitCount = 0;
                while ((!IsAvailableForReading(packageFile)) && (waitCount < MaximumNumberOfTimesWaitingForPackageFileLock))
                {
                    waitCount++;
                    Thread.Sleep(PackageFileLockSleepTimeInMilliSeconds);
                }

                return new ZipPackage(packageFile);
            }
            catch (Exception e)
            {
                _diagnostics.Log(
                    LevelToLog.Error,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_SymbolIndexer_PackageLoadingFailed_WithException,
                        e));

                _lockedPackages.Enqueue(new Tuple<string, int>(packageFile, ++processCount));

                return null;
            }
        }

        private string Unpack(string packageFile, string project, Version version)
        {
            var destinationDirectory = Path.Combine(
                _unpackDirectory,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.{1}",
                    project,
                    FormatVersion(version)));
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            using (var zipFile = ZipFile.Read(packageFile))
            {
                foreach (var entry in zipFile)
                {
                    entry.Extract(destinationDirectory, ExtractExistingFileAction.OverwriteSilently);
                }
            }

            return destinationDirectory;
        }

        /// <summary>
        /// Indexes the PDB files that were stored in the package.
        /// </summary>
        /// <remarks>
        /// Code for the indexing of the symbols was taken from here: http://www.jayway.com/2011/06/19/hosting-your-own-source-symbol-server/.
        /// </remarks>
        /// <param name="unpackLocation">The directory in which the package was extracted.</param>
        /// <param name="project">The name of the project for which the PDBs are published.</param>
        /// <param name="version">The version of the project for which the PDBs are published.</param>
        private void IndexSymbols(string unpackLocation, string project, Version version)
        {
            var sourceLocation = Path.Combine(unpackLocation, "src");

            var pdbFiles = GetPdbFiles(unpackLocation);
            foreach (var pdbFile in pdbFiles)
            {
                _diagnostics.Log(
                    LevelToLog.Info,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_SymbolIndexer_IndexingSymbol_WithPackageDetails,
                        pdbFile,
                        project,
                        version));

                var indexingFile = new FileInfo(pdbFile + ".stream");
                using (var writer = indexingFile.CreateText())
                {
                    writer.WriteLine("SRCSRV: ini ------------------------------------------------");
                    writer.WriteLine("VERSION=2");
                    writer.WriteLine("INDEXVERSION=2");
                    writer.WriteLine("VERCTRL=http");
                    writer.WriteLine("SRCSRV: variables ------------------------------------------");
                    writer.WriteLine("SRCSRVVERCTRL=http");
                    writer.WriteLine("UNCROOT=" + _sourceUncPath);
                    writer.WriteLine("HTTP_EXTRACT_TARGET=%UNCROOT%\\" + BuildSourcePath("%var2%", "%var3%") + "%var4%");
                    writer.WriteLine("SRCSRVTRG=%http_extract_target%");
                    writer.WriteLine("SRCSRVCMD=");
                    writer.WriteLine("SRCSRV: source files ---------------------------------------");

                    // Run SrcTool to list all files in a pdb
                    var filesAsText = Execute(_srcToolPath, "-r " + pdbFile);
                    var files = filesAsText
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(l => !l.Contains("source files are indexed"))
                        .ToArray();
                    if (files.Length == 0)
                    {
                        continue;
                    }

                    // Find the base path
                    var relativePathMap = CalculateRelativePaths(sourceLocation, files);
                    if (relativePathMap.Count == 0)
                    {
                        continue;
                    }

                    // Iterate files. Add path to file in SRCSRV stream
                    foreach (var file in files)
                    {
                        if (relativePathMap.ContainsKey(file))
                        {
                            var relativeFile = relativePathMap[file];

                            writer.Write(file);
                            writer.Write("*");
                            writer.Write(project);
                            writer.Write("*");
                            writer.Write(FormatVersion(version));
                            writer.Write("*");
                            writer.Write(relativeFile);
                            writer.WriteLine();
                        }
                    }

                    // Write SRCSRV footer
                    writer.WriteLine("SRCSRV: end------------------------------------------------");
                    writer.WriteLine();
                    writer.WriteLine();
                }

                // Write SRCSRV stream to pdb with PDBStr
                var pdbstrArguments = string.Format(
                    CultureInfo.InvariantCulture,
                    "-w -p:{0} -i:{1} -s:srcsrv",
                    pdbFile,
                    indexingFile.FullName);
                var output = Execute(_pdbStrPath, pdbstrArguments);

                _diagnostics.Log(
                    LevelToLog.Trace,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_SymbolIndexer_SymbolIndexingComplete_PdbStrOutput,
                        !string.IsNullOrWhiteSpace(output) ? output : Resources.Log_Messages_ExternalTool_NoResponse));

                _diagnostics.Log(
                    LevelToLog.Info,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_SymbolIndexer_SymbolIndexingComplete_WithPackageDetails,
                        pdbFile,
                        project,
                        version));
            }
        }

        private void UploadSources(string unpackLocation, string project, Version version)
        {
            var destination = Path.Combine(_sourceUncPath, BuildSourcePath(project, FormatVersion(version)));

            var sourceLocation = Path.Combine(unpackLocation, "src");
            var srcFiles = Directory.GetFiles(sourceLocation, "*.*", SearchOption.AllDirectories);
            foreach (var sourceFile in srcFiles)
            {
                var destinationFile = sourceFile.Replace(sourceLocation, destination);
                var directory = Path.GetDirectoryName(destinationFile);

                _diagnostics.Log(
                    LevelToLog.Info,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_SymbolIndexer_UploadingSources_WithPackageDetails,
                        sourceFile,
                        destinationFile,
                        project,
                        version));

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(sourceFile, destinationFile);
                File.SetAttributes(destinationFile, FileAttributes.ReadOnly);

                _diagnostics.Log(
                    LevelToLog.Info,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_SymbolIndexer_SourceUploadComplete_WithPackageDetails,
                        sourceFile,
                        destinationFile,
                        project,
                        version));
            }
        }

        private void UploadSymbols(string unpackLocation, string project, Version version)
        {
            var pdbFiles = GetPdbFiles(unpackLocation);
            foreach (var path in pdbFiles)
            {
                _diagnostics.Log(
                    LevelToLog.Info,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_SymbolIndexer_UploadingSymbols_WithPackageDetails,
                        path,
                        project,
                        version));

                var symStoreArguments = string.Format(
                    CultureInfo.InvariantCulture,
                    "add /f \"{0}\" /s \"{1}\" /t \"{2}\" /v {3}",
                    path,
                    _symbolsUncPath,
                    project,
                    version);
                var output = Execute(_symStorePath, symStoreArguments);

                _diagnostics.Log(
                    LevelToLog.Trace,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_SymbolIndexer_UploadingSymbols_SymStoreOutput,
                        !string.IsNullOrWhiteSpace(output) ? output : Resources.Log_Messages_ExternalTool_NoResponse));

                _diagnostics.Log(
                    LevelToLog.Info,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_SymbolIndexer_SymbolUploadComplete_WithPackageDetails,
                        path,
                        project,
                        version));
            }
        }

        private void MarkSymbolsAsProcessed(string packageFile, string project, Version version)
        {
            {
                Debug.Assert(packageFile != null, "The package file path should not be a null reference.");
            }

            try
            {
                if (!Directory.Exists(_processedPackagesPath))
                {
                    Directory.CreateDirectory(_processedPackagesPath);
                }

                var newPath = Path.Combine(_processedPackagesPath, Path.GetFileName(packageFile));
                File.Move(packageFile, newPath);
            }
            catch (IOException e)
            {
                _diagnostics.Log(
                    LevelToLog.Error,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_SymbolIndexer_PackageMoveFailed_WithExceptionAndPackageDetails,
                        e,
                        project,
                        version));
            }
        }

        /// <summary>
        /// Stops the symbol indexing process.
        /// </summary>
        /// <param name="clearCurrentQueue">
        /// Indicates if the elements currently in the queue need to be processed before stopping or not.
        /// </param>
        /// <returns>A task that completes when the indexer has stopped.</returns>
        public Task Stop(bool clearCurrentQueue)
        {
            _isStarted = false;

            var result = Task.Factory.StartNew(
                () =>
                {
                    _diagnostics.Log(
                        LevelToLog.Info,
                        Resources.Log_Messages_SymbolIndexer_StoppingProcessing);

                    if (!clearCurrentQueue && !_queue.IsEmpty)
                    {
                        lock (_lock)
                        {
                            if (_cancellationSource != null)
                            {
                                _cancellationSource.Cancel();
                            }
                        }
                    }

                    Task worker;
                    lock (_lock)
                    {
                        worker = _worker;
                    }

                    if (worker != null)
                    {
                        worker.Wait();
                    }

                    CleanUpWorkerTask();
                });

            return result;
        }

        private void CleanUpWorkerTask()
        {
            lock (_lock)
            {
                _diagnostics.Log(
                    LevelToLog.Trace,
                    Resources.Log_Messages_SymbolIndexer_CleaningUpWorker);

                _cancellationSource = null;
                _worker = null;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            var task = Stop(false);
            task.Wait();

            if (_cancellationSource != null)
            {
                _cancellationSource.Dispose();
            }

            try
            {
                Directory.Delete(_unpackDirectory, true);
            }
            catch (IOException)
            {
            }
        }
    }
}
