//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using Nanicitus.Core.Properties;
using Nuclei.Configuration;
using Nuclei.Diagnostics;
using Nuclei.Diagnostics.Logging;

namespace Nanicitus.Core
{
    internal sealed class FileWatcherBasedPackageUploader : IUploadPackages
    {
        /// <summary>
        /// The object that watches the file system for newly added packages.
        /// </summary>
        private readonly FileSystemWatcher _watcher;

        /// <summary>
        /// The queue that stores the location of the non-processed packages.
        /// </summary>
        private readonly IQueueSymbolPackages _queue;

        /// <summary>
        /// The object that provides the diagnostics methods for the application.
        /// </summary>
        private readonly SystemDiagnostics _diagnostics;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileWatcherBasedPackageUploader"/> class.
        /// </summary>
        /// <param name="packageQueue">The object that queues packages that need to be processed.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="diagnostics">The object providing the diagnostics methods for the application.</param>
        internal FileWatcherBasedPackageUploader(
            IQueueSymbolPackages packageQueue,
            IConfiguration configuration,
            SystemDiagnostics diagnostics)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _queue = packageQueue ?? throw new ArgumentNullException("packageQueue");
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");

            var uploadPath = configuration.Value(CoreConfigurationKeys.UploadPath);
            _watcher = new FileSystemWatcher
            {
                Path = uploadPath,
                Filter = "*.symbols.nupkg",
                EnableRaisingEvents = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            };

            _watcher.Created += HandleFileCreated;
        }

        private void HandleFileCreated(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                _queue.Enqueue(e.FullPath);
                _diagnostics.Log(
                    LevelToLog.Info,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_FileWatcherBasedPackageUploader_DiscoveredFile_WithFilePath,
                        e.FullPath));
            }
        }

        /// <summary>
        /// Enables the uploading of packages.
        /// </summary>
        public void EnableUpload()
        {
            _diagnostics.Log(
                LevelToLog.Info,
                Resources.Log_Messages_FileWatcherBasedPackageUploader_PackageDiscovery_Enabled);

            EnqueueExistingFiles();
            _watcher.EnableRaisingEvents = true;
        }

        private void EnqueueExistingFiles()
        {
            foreach (var file in Directory.GetFiles(_watcher.Path, _watcher.Filter, SearchOption.TopDirectoryOnly))
            {
                _queue.Enqueue(file);
                _diagnostics.Log(
                    LevelToLog.Info,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources.Log_Messages_FileWatcherBasedPackageUploader_DiscoveredFile_WithFilePath,
                        file));
            }
        }

        /// <summary>
        /// Disables the uploading of packages.
        /// </summary>
        public void DisableUpload()
        {
            _watcher.EnableRaisingEvents = false;
            _diagnostics.Log(
                    LevelToLog.Info,
                    Resources.Log_Messages_FileWatcherBasedPackageUploader_PackageDiscovery_Disabled);
        }
    }
}
