//-----------------------------------------------------------------------
// <copyright company="NAnicitus">
//     Copyright 2013 NAnicitus. Licensed under the Apache License, Version 2.0.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zip;
using Nanicitus.Core.Properties;
using Nuclei.Configuration;
using NuGet;

namespace Nanicitus.Core
{
    /// <summary>
    /// Handles the indexing of symbols and sources.
    /// </summary>
    internal sealed class SymbolIndexer : IIndexSymbols, IDisposable
    {
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

        private static string DefaultSymbolServerToolsDirectory()
        {
            return Environment.Is64BitProcess
                ? @"C:\Program Files (x86)\Windows Kits\8.0\Debuggers\x64"
                : @"C:\Program Files (x86)\Windows Kits\8.0\Debuggers\x86";
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

        private static string DetermineBasePath(string sourceLocation, string[] files)
        {
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var matchingFiles = Directory.GetFiles(sourceLocation, fileName, SearchOption.AllDirectories);
                if (matchingFiles.Length != 1)
                {
                    continue;
                }

                var storedFile = matchingFiles[0];
                var relativePath = storedFile.Substring(sourceLocation.Length);
                relativePath = relativePath.TrimStart(Path.DirectorySeparatorChar);

                var baseDirectory = file.Substring(0, file.Length - relativePath.Length);
                return baseDirectory;
            }

            return null;
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
        private readonly object m_Lock = new object();

        /// <summary>
        /// The queue that stores the location of the non-processed packages.
        /// </summary>
        private readonly IQueueSymbolPackages m_Queue;

        /// <summary>
        /// The full path to the 'srctool.exe' application that is used
        /// to extract source file information from a PDB file.
        /// </summary>
        private readonly string m_SrcToolPath;

        /// <summary>
        /// The full path to the 'pdbstr.exe' application that is used
        /// to write source index information to a PDB file.
        /// </summary>
        private readonly string m_PdbStrPath;

        /// <summary>
        /// The full path to the 'symstore.exe' application that is used
        /// to store the symbols.
        /// </summary>
        private readonly string m_SymStorePath;

        /// <summary>
        /// The UNC path to the location where the sources are indexed.
        /// </summary>
        private readonly string m_SourceUncPath;

        /// <summary>
        /// The UNC path to the location where the symbols are indexed.
        /// </summary>
        private readonly string m_SymbolsUncPath;

        /// <summary>
        /// The full path to the location where the processed packages are stored
        /// in case they are needed at a later stage.
        /// </summary>
        private readonly string m_ProcessedPackagesPath;

        /// <summary>
        /// The directory in which the packages will be unzipped.
        /// </summary>
        private readonly string m_UnpackDirectory = CreateUnzipDirectory();

        /// <summary>
        /// The task that handles the actual symbol indexing process.
        /// </summary>
        private Task m_Worker;

        /// <summary>
        /// The cancellation source that is used to cancel the worker task.
        /// </summary>
        private CancellationTokenSource m_CancellationSource;

        /// <summary>
        /// A flag indicating if the processing of symbols has started or not.
        /// </summary>
        private bool m_IsStarted;

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolIndexer"/> class.
        /// </summary>
        /// <param name="packageQueue">The object that queues packages that need to be processed.</param>
        /// <param name="configuration">The configuration.</param>
        public SymbolIndexer(
            IQueueSymbolPackages packageQueue,
            IConfiguration configuration)
        {
            {
                Lokad.Enforce.Argument(() => packageQueue);
                Lokad.Enforce.Argument(() => configuration);

                Lokad.Enforce.With<ArgumentException>(
                    configuration.HasValueFor(ConfigurationKeys.SourceIndexUncPath),
                    Resources.Exceptions_Messages_MissingConfigurationValue_WithKey,
                    ConfigurationKeys.SourceIndexUncPath);
                Lokad.Enforce.With<ArgumentException>(
                    configuration.HasValueFor(ConfigurationKeys.SymbolsIndexUncPath),
                    Resources.Exceptions_Messages_MissingConfigurationValue_WithKey,
                    ConfigurationKeys.SymbolsIndexUncPath);
                Lokad.Enforce.With<ArgumentException>(
                    configuration.HasValueFor(ConfigurationKeys.ProcessedPackagesPath),
                    Resources.Exceptions_Messages_MissingConfigurationValue_WithKey,
                    ConfigurationKeys.ProcessedPackagesPath);
            }

            m_Queue = packageQueue;
            m_Queue.OnEnqueue += HandleOnEnqueue;

            var debuggingToolsDirectory = configuration.HasValueFor(ConfigurationKeys.DebuggingToolsDirectory)
                ? configuration.Value<string>(ConfigurationKeys.DebuggingToolsDirectory)
                : DefaultSymbolServerToolsDirectory();
            m_SymStorePath = Path.Combine(debuggingToolsDirectory, "symstore.exe");
            m_SrcToolPath = Path.Combine(debuggingToolsDirectory, "srcsrv", "srctool.exe");
            m_PdbStrPath = Path.Combine(debuggingToolsDirectory, "srcsrv", "pdbstr.exe");
            m_SourceUncPath = configuration.Value<string>(ConfigurationKeys.SourceIndexUncPath);
            m_SymbolsUncPath = configuration.Value<string>(ConfigurationKeys.SymbolsIndexUncPath);
            m_ProcessedPackagesPath = configuration.Value<string>(ConfigurationKeys.ProcessedPackagesPath);
        }

        private void HandleOnEnqueue(object sender, EventArgs e)
        {
            lock (m_Lock)
            {
                if (!m_IsStarted)
                {
                    return;
                }

                if (m_Worker != null)
                {
                    return;
                }

                m_CancellationSource = new CancellationTokenSource();
                m_Worker = Task.Factory.StartNew(
                    () => ProcessSymbols(m_CancellationSource.Token),
                    m_CancellationSource.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }
        }

        /// <summary>
        /// Starts the symbol indexing process.
        /// </summary>
        public void Start()
        {
            lock (m_Lock)
            {
                m_IsStarted = true;
            }
        }

        private void ProcessSymbols(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (m_Queue.IsEmpty)
                {
                    return;
                }

                var packageFile = m_Queue.Dequeue();
                if (packageFile == null)
                {
                    continue;
                }

                var package = new ZipPackage(packageFile);
                var project = package.Id;
                var version = package.Version.Version;

                // Unpack package file in temp location
                var unpackLocation = Unpack(packageFile, project, version);
                try
                {
                    IndexSymbols(unpackLocation, project, version);
                    UploadSources(unpackLocation, project, version);
                    UploadSymbols(unpackLocation, project, version);
                }
                catch (Exception)
                {
                    // Really just ignore it and move on
                    return;
                }
                finally
                {
                    try
                    {
                        Directory.Delete(unpackLocation, true);
                    }
                    catch (IOException)
                    {
                    }

                    try
                    {
                        if (!Directory.Exists(m_ProcessedPackagesPath))
                        {
                            try
                            {
                                Directory.CreateDirectory(m_ProcessedPackagesPath);
                            }
                            catch (IOException)
                            {
                            }
                        }

                        var newPath = Path.Combine(m_ProcessedPackagesPath, Path.GetFileName(packageFile));
                        File.Move(packageFile, newPath);
                    }
                    catch (IOException)
                    {
                    }
                }
            }
        }

        private string Unpack(string packageFile, string project, Version version)
        {
            var destinationDirectory = Path.Combine(
                m_UnpackDirectory,
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
                var indexingFile = new FileInfo(pdbFile + ".stream");
                using (var writer = indexingFile.CreateText())
                {
                    writer.WriteLine("SRCSRV: ini ------------------------------------------------");
                    writer.WriteLine("VERSION=2");
                    writer.WriteLine("INDEXVERSION=2");
                    writer.WriteLine("VERCTRL=http");
                    writer.WriteLine("SRCSRV: variables ------------------------------------------");
                    writer.WriteLine("SRCSRVVERCTRL=http");
                    writer.WriteLine("UNCROOT=" + m_SourceUncPath);
                    writer.WriteLine("HTTP_EXTRACT_TARGET=%UNCROOT%\\" + BuildSourcePath("%var2%", "%var3%") + "%var4%");
                    writer.WriteLine("SRCSRVTRG=%http_extract_target%");
                    writer.WriteLine("SRCSRVCMD=");
                    writer.WriteLine("SRCSRV: source files ---------------------------------------");

                    // Run SrcTool to list all files in a pdb
                    var filesAsText = Execute(m_SrcToolPath, "-r " + pdbFile);
                    var files = filesAsText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (files.Length == 0)
                    {
                        continue;
                    }

                    // Find the base path
                    var basePath = DetermineBasePath(sourceLocation, files);
                    if (basePath == null)
                    {
                        continue;
                    }

                    // Iterate files. Add path to file in SRCSRV stream
                    foreach (var file in files)
                    {
                        int index = -1;
                        if (file.StartsWith(basePath))
                        {
                            index = basePath.Length + 1;
                        }
                        else if (file.Contains(basePath))
                        {
                            index = file.IndexOf(basePath, StringComparison.InvariantCultureIgnoreCase) + basePath.Length + 1;
                        }

                        if (index >= 0)
                        {
                            var relativeFile = file.Replace(basePath, string.Empty);
                            relativeFile = relativeFile.TrimStart(Path.DirectorySeparatorChar);

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
                Execute(m_PdbStrPath, pdbstrArguments);
            }
        }

        private void UploadSources(string unpackLocation, string project, Version version)
        {
            var destination = Path.Combine(m_SourceUncPath, BuildSourcePath(project, FormatVersion(version)));

            var sourceLocation = Path.Combine(unpackLocation, "src");
            var srcFiles = Directory.GetFiles(sourceLocation, "*.*", SearchOption.AllDirectories);
            foreach (var sourceFile in srcFiles)
            {
                var destinationFile = sourceFile.Replace(sourceLocation, destination);
                var directory = Path.GetDirectoryName(destinationFile);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(sourceFile, destinationFile);
            }
        }

        private void UploadSymbols(string unpackLocation, string project, Version version)
        {
            var pdbFiles = GetPdbFiles(unpackLocation);
            foreach (var path in pdbFiles)
            {
                var symStoreArguments = string.Format(
                    CultureInfo.InvariantCulture,
                    "add /f \"{0}\" /s \"{1}\" /t \"{2}\" /v {3}",
                    path,
                    m_SymbolsUncPath,
                    project,
                    version);
                Execute(m_SymStorePath, symStoreArguments);
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
            m_IsStarted = false;

            var result = Task.Factory.StartNew(
                () =>
                {
                    if (!clearCurrentQueue && !m_Queue.IsEmpty)
                    {
                        lock (m_Lock)
                        {
                            if (m_CancellationSource != null)
                            {
                                m_CancellationSource.Cancel();
                            }
                        }
                    }

                    Task worker;
                    lock (m_Lock)
                    {
                        worker = m_Worker;
                    }

                    if (worker != null)
                    {
                        m_Worker.Wait();
                    }

                    m_CancellationSource = null;
                    m_Worker = null;
                });

            return result;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            var task = Stop(false);
            task.Wait();

            try
            {
                Directory.Delete(m_UnpackDirectory, true);
            }
            catch (IOException)
            {
            }
        }
    }
}
