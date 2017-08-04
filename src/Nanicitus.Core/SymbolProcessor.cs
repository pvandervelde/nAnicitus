//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
            _indexerBuilder = indexerBuilder ?? throw new ArgumentNullException("indexer");
            _metrics = metrics ?? throw new ArgumentNullException("metrics");
            _queueBuilder = queueBuilder ?? throw new ArgumentNullException("queue");
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
        /// <param name="path">The full path to the symbol package.</param>
        public void Index(string path)
        {
            ValidateIndexingInstances();
            _metrics.Increment("Symbols.Uploaded");

            _indexerQueue.Enqueue(path);
            _diagnostics.Log(
                LevelToLog.Info,
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.Log_Messages_SymbolProcessor_FileToIndex_WithFilePath,
                    path));
        }

        /// <summary>
        /// Returns a value indicating whether or not the symbol package at the given
        /// location is a valid symbol package.
        /// </summary>
        /// <param name="path">The full path to the symbol package.</param>
        /// <returns>
        ///     <see langword="true" /> if the symbol package at the given location is valid,
        ///     otherwise <see langword="false" />.
        /// </returns>
        public bool IsValid(string path)
        {
            // process the symbols on a separate queue in a separate location
            var validationConfiguration = new ConstantConfiguration(
                new Dictionary<ConfigurationKeyBase, object>
                { });
            var validationQueue = _queueBuilder();
            using (var indexer = _indexerBuilder(validationQueue, validationConfiguration))
            {
            }
        }

        /// <summary>
        /// Re-Indexes all the previously processed symbols.
        /// </summary>
        public void RebuildIndex()
        {
            ValidateIndexingInstances();
            _metrics.Increment("Symbols.RebuildIndex");

            var stoppingTask = _indexer.Stop(false);
            stoppingTask.Wait();

            var uploadDirectory = _configuration.Value(CoreConfigurationKeys.UploadPath);
            var processedFilePath = _configuration.Value(CoreConfigurationKeys.ProcessedPackagesPath);
            foreach (var file in Directory.EnumerateFiles(processedFilePath))
            {
                var target = Path.Combine(
                    uploadDirectory,
                    Path.GetFileName(file));
                File.Move(file, target);
                _indexerQueue.Enqueue(target);
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
