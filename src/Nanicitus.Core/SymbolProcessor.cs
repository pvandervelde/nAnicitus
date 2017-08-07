//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nanicitus.Core.Monitoring;
using Nanicitus.Core.Properties;
using Nuclei.Configuration;
using Nuclei.Diagnostics;
using Nuclei.Diagnostics.Logging;

namespace Nanicitus.Core
{
    /// <summary>
    /// Provides the global entrypoint for the symbol processing.
    /// </summary>
    internal sealed class SymbolProcessor : ISymbolProcessor
    {
        private readonly IConfiguration _configuration;
        private readonly SystemDiagnostics _diagnostics;

        private readonly Func<IQueueSymbolPackages, IConfiguration, IIndexSymbols> _indexerBuilder;

        private readonly object _lock = new object();

        private readonly IMetricsCollector _metrics;

        private readonly Func<IQueueSymbolPackages> _queueBuilder;

        private IIndexSymbols _indexer;
        private IQueueSymbolPackages _indexerQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolProcessor"/> class.
        /// </summary>
        /// <param name="indexerBuilder">The function that generates objects that index the PDB files from the symbol packages.</param>
        /// <param name="queueBuilder">The the function that generates objects which queues symbol packages to be processed.</param>
        /// <param name="configuration">The object that provides the configuration for the application.</param>
        /// <param name="metrics">The objet that provides the metrics collection methods.</param>
        /// <param name="diagnostics">The object that provides the diagnostics methods for the application.</param>
        public SymbolProcessor(
            Func<IQueueSymbolPackages, IConfiguration, IIndexSymbols> indexerBuilder,
            Func<IQueueSymbolPackages> queueBuilder,
            IConfiguration configuration,
            IMetricsCollector metrics,
            SystemDiagnostics diagnostics)
        {
            _configuration = configuration ?? throw new ArgumentNullException("configuration");
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
            _indexerBuilder = indexerBuilder ?? throw new ArgumentNullException("indexerBuilder");
            _metrics = metrics ?? throw new ArgumentNullException("metrics");
            _queueBuilder = queueBuilder ?? throw new ArgumentNullException("queueBuilder");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_indexer != null)
            {
                _indexer.Dispose();
            }

            _indexer = null;
            _indexerQueue = null;
        }

        private void EnsureIndexingInstances()
        {
            lock (_lock)
            {
                if (_indexerQueue == null)
                {
                    _indexerQueue = _queueBuilder();
                }

                if (_indexer == null)
                {
                    _indexer = _indexerBuilder(_indexerQueue, _configuration);
                }
            }
        }

        /// <summary>
        /// Indexes the symbols in the package at the given path.
        /// </summary>
        /// <param name="paths">A collection containing the full paths to the symbol packages that should be indexed.</param>
        /// <returns>The status reports of the indexing process.</returns>
        public IEnumerable<IndexReport> Index(IEnumerable<string> paths)
        {
            ValidateIndexingInstances();
            _metrics.Increment("Symbols.Uploaded");

            if (paths == null)
            {
                return new[]
                {
                    new IndexReport(
                        "Unknown",
                        "Unknown",
                        IndexStatus.Failed,
                        new[] { "No symbol package paths provided." })
                };
            }

            var result = new Dictionary<string, Task<IndexReport>>();
            foreach (var path in paths)
            {
                var addTask = new TaskCompletionSource<IndexReport>();
                Action<IndexReport> action = r => addTask.SetResult(r);

                _indexerQueue.Enqueue(path, action);
                _diagnostics.Log(
                    LevelToLog.Info,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_SymbolProcessor_FileToIndex_WithFilePath,
                        path));

                result.Add(path, addTask.Task);
            }

            Task.WaitAll(result.Values.ToArray(), TimeSpan.FromSeconds(60));
            return result
                .Select(
                    pair =>
                    {
                        var path = pair.Key;
                        var t = pair.Value;
                        if (t.IsCompleted)
                        {
                            return t.Result;
                        }

                        var id = PackageUtilities.GetPackageIdentity(path);
                        return new IndexReport(
                            id.Id,
                            id.Version.ToString(),
                            IndexStatus.Failed,
                            new[] { "Failed to process symbols within timespan of 60 seconds." });
                    });
        }

#pragma warning disable SA1008 // Opening parenthesis must be spaced correctly
        /// <summary>
        /// Returns a value indicating whether or not the symbol package at the given
        /// location is a valid symbol package.
        /// </summary>
        /// <param name="paths">A collection containing the full paths to the symbol packages.</param>
        /// <returns>
        ///     A tuple containing the overall result of the operation and the status reports
        ///     for the indexing process.
        /// </returns>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "For some reason we can't always delete the directory we just created")]
        public (bool result, IEnumerable<IndexReport> messages) IsValid(IEnumerable<string> paths)
#pragma warning restore SA1008 // Opening parenthesis must be spaced correctly
        {
            if (paths == null)
            {
                return (
                    false,
                    new[]
                    {
                        new IndexReport(
                            "Unknown",
                            "Unknown",
                            IndexStatus.Failed,
                            new[] { "No symbol package paths provided." })
                    });
            }

            // process the symbols on a separate queue in a separate location
            var validationConfiguration = new ConstantConfiguration(
                new Dictionary<ConfigurationKeyBase, object>
                {
                    {
                        CoreConfigurationKeys.DebuggingToolsDirectory,
                        _configuration.Value(CoreConfigurationKeys.DebuggingToolsDirectory)
                    },
                    {
                        CoreConfigurationKeys.SourceServerUrl,
                        _configuration.Value(CoreConfigurationKeys.SourceServerUrl)
                    },
                    {
                        CoreConfigurationKeys.ProcessedPackagesPath,
                        Path.Combine(
                            _configuration.Value(CoreConfigurationKeys.TempPath),
                            Guid.NewGuid().ToString())
                    },
                    {
                        CoreConfigurationKeys.ProcessedSourcePath,
                        Path.Combine(
                            _configuration.Value(CoreConfigurationKeys.TempPath),
                            Guid.NewGuid().ToString())
                    },
                    {
                        CoreConfigurationKeys.ProcessedSymbolsPath,
                        Path.Combine(
                            _configuration.Value(CoreConfigurationKeys.TempPath),
                            Guid.NewGuid().ToString())
                    },
                });

            var result = new Dictionary<string, Task<IndexReport>>();

            try
            {
                var validationQueue = _queueBuilder();
                using (var indexer = _indexerBuilder(validationQueue, validationConfiguration))
                {
                    indexer.Start();

                    foreach (var path in paths)
                    {
                        var addTask = new TaskCompletionSource<IndexReport>();
                        Action<IndexReport> action = r => addTask.SetResult(r);

                        validationQueue.Enqueue(path, action);
                        result.Add(path, addTask.Task);
                    }

                    var indexingTask = indexer.Stop(true);
                    indexingTask.Wait();
                }

                Task.WaitAll(result.Values.ToArray(), TimeSpan.FromSeconds(300));
                var reports = result
                    .Select(
                        pair =>
                        {
                            var path = pair.Key;
                            var t = pair.Value;
                            if (t.IsCompleted)
                            {
                                return t.Result;
                            }

                            var id = PackageUtilities.GetPackageIdentity(path);
                            return new IndexReport(
                                id.Id,
                                id.Version.ToString(),
                                IndexStatus.Failed,
                                new[] { "Failed to process symbols within timespan of 60 seconds." });
                        });

                return (!reports.Any(r => r.Status != IndexStatus.Succeeded), reports);
            }
            finally
            {
                var pathsToDelete = new[]
                {
                    validationConfiguration.Value(CoreConfigurationKeys.ProcessedPackagesPath),
                    validationConfiguration.Value(CoreConfigurationKeys.ProcessedSourcePath),
                    validationConfiguration.Value(CoreConfigurationKeys.ProcessedSymbolsPath),
                };

                foreach (var path in pathsToDelete)
                {
                    try
                    {
                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                        }
                    }
                    catch (IOException)
                    {
                        _diagnostics.Log(
                            LevelToLog.Warn,
                            "Failed to delete validation directory at: {0}",
                            path);
                    }
                    catch (SystemException)
                    {
                        _diagnostics.Log(
                            LevelToLog.Warn,
                            "Failed to delete validation directory at: {0}",
                            path);
                    }
                }
            }
        }

        /// <summary>
        /// Re-Indexes all the previously processed symbols.
        /// </summary>
        /// <returns>The status reports of the indexing process.</returns>
        public IEnumerable<IndexReport> RebuildIndex()
        {
            ValidateIndexingInstances();
            _metrics.Increment("Symbols.RebuildIndex");

            var stoppingTask = _indexer.Stop(false);
            stoppingTask.Wait();

            var uploadDirectory = _configuration.Value(CoreConfigurationKeys.UploadPath);
            var processedFilePath = _configuration.Value(CoreConfigurationKeys.ProcessedPackagesPath);

            var result = new Dictionary<string, Task<IndexReport>>();
            foreach (var file in Directory.EnumerateFiles(processedFilePath))
            {
                var addTask = new TaskCompletionSource<IndexReport>();
                Action<IndexReport> action = r => addTask.SetResult(r);

                var target = Path.Combine(
                    uploadDirectory,
                    Path.GetFileName(file));
                File.Move(file, target);

                _indexerQueue.Enqueue(target, action);
                _diagnostics.Log(
                    LevelToLog.Info,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_SymbolProcessor_FileToIndex_WithFilePath,
                        target));

                result.Add(target, addTask.Task);
            }

            var symbolPath = _configuration.Value(CoreConfigurationKeys.ProcessedSymbolsPath);
            foreach (var file in Directory.EnumerateFiles(symbolPath))
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                    // Ignore it for now
                }
                catch (UnauthorizedAccessException)
                {
                    // Ignore it for now
                }
            }

            foreach (var directory in Directory.EnumerateDirectories(symbolPath))
            {
                try
                {
                    Directory.Delete(directory, true);
                }
                catch (IOException)
                {
                    // Ignore it for now
                }
                catch (UnauthorizedAccessException)
                {
                    // Ignore it for now
                }
            }

            var sourcePath = _configuration.Value(CoreConfigurationKeys.ProcessedSourcePath);
            foreach (var directory in Directory.EnumerateDirectories(sourcePath))
            {
                try
                {
                    Directory.Delete(directory, true);
                }
                catch (IOException)
                {
                    // Ignore it for now
                }
                catch (UnauthorizedAccessException)
                {
                    // Ignore it for now
                }
            }

            _indexer.Start();

            Task.WaitAll(result.Values.ToArray(), TimeSpan.FromSeconds(60));
            return result
                .Select(
                    pair =>
                    {
                        var path = pair.Key;
                        var t = pair.Value;
                        if (t.IsCompleted)
                        {
                            return t.Result;
                        }

                        var id = PackageUtilities.GetPackageIdentity(path);
                        return new IndexReport(
                            id.Id,
                            id.Version.ToString(),
                            IndexStatus.Failed,
                            new[] { "Failed to process symbols within timespan of 60 seconds." });
                    });
        }

        private void RemoveIndexingInstances()
        {
            lock (_lock)
            {
                if (_indexer != null)
                {
                    _indexer.Stop(false).Wait();

                    var disposable = _indexer as IDisposable;
                    disposable.Dispose();
                    _indexer = null;
                }

                if (_indexerQueue != null)
                {
                    _indexerQueue = null;
                }
            }
        }

        /// <summary>
        /// Starts the symbol indexing process.
        /// </summary>
        public void Start()
        {
            _diagnostics.Log(
                LevelToLog.Info,
                Resources.Log_Messages_SymbolProcessor_PackageIndexing_Enabled);

            EnsureIndexingInstances();
            _indexer.Start();
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
            _diagnostics.Log(
                    LevelToLog.Info,
                    Resources.Log_Messages_SymbolProcessor_PackageIndexing_Disabled);

            try
            {
                return _indexer.Stop(clearCurrentQueue);
            }
            finally
            {
                RemoveIndexingInstances();
            }
        }

        private void ValidateIndexingInstances()
        {
            lock (_lock)
            {
                if ((_indexerQueue == null) || (_indexer == null))
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
