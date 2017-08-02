//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nanicitus.Core.Monitoring;
using Nanicitus.Core.Properties;
using Nuclei.Diagnostics;
using Nuclei.Diagnostics.Logging;

namespace Nanicitus.Core
{
    /// <summary>
    /// Provides the global entrypoint for the symbol processing.
    /// </summary>
    internal sealed class SymbolProcessor : ISymbolProcessor
    {
        private readonly SystemDiagnostics _diagnostics;
        private readonly IIndexSymbols _indexer;
        private readonly IMetricsCollector _metrics;
        private readonly IQueueSymbolPackages _queue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolProcessor"/> class.
        /// </summary>
        /// <param name="indexer">The object that indexes the PDB files from the symbol packages.</param>
        /// <param name="queue">The object which queues symbol packages to be processed.</param>
        /// <param name="metrics">The objet that provides the metrics collection methods.</param>
        /// <param name="diagnostics">The object that provides the diagnostics methods for the application.</param>
        public SymbolProcessor(
            IIndexSymbols indexer,
            IQueueSymbolPackages queue,
            IMetricsCollector metrics,
            SystemDiagnostics diagnostics)
        {
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
            _indexer = indexer ?? throw new ArgumentNullException("indexer");
            _metrics = metrics ?? throw new ArgumentNullException("metrics");
            _queue = queue ?? throw new ArgumentNullException("queue");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            var disposable = _indexer as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Indexes the symbols in the package at the given path.
        /// </summary>
        /// <param name="path">The full path to the symbol package.</param>
        public void Index(string path)
        {
            _queue.Enqueue(path);
            _diagnostics.Log(
                LevelToLog.Info,
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.Log_Messages_SymbolProcessor_FileToIndex_WithFilePath,
                    path));
        }

        /// <summary>
        /// Re-Indexes all the previously processed symbols.
        /// </summary>
        public void Reindex()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts the symbol indexing process.
        /// </summary>
        public void Start()
        {
            _diagnostics.Log(
                LevelToLog.Info,
                Resources.Log_Messages_SymbolProcessor_PackageIndexing_Enabled);

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

            return _indexer.Stop(clearCurrentQueue);
        }
    }
}
