//-----------------------------------------------------------------------
// <copyright company="NAnicitus">
//     Copyright 2013 NAnicitus. Licensed under the Apache License, Version 2.0.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using Nanicitus.Core.Properties;
using Nuclei.Configuration;

namespace Nanicitus.Core
{
    internal sealed class FileWatcherBasedPackageUploader : IUploadPackages
    {
        /// <summary>
        /// The object that watches the file system for newly added packages.
        /// </summary>
        private readonly FileSystemWatcher m_Watcher;

        /// <summary>
        /// The queue that stores the location of the non-processed packages.
        /// </summary>
        private readonly IQueueSymbolPackages m_Queue;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileWatcherBasedPackageUploader"/> class.
        /// </summary>
        /// <param name="packageQueue">The object that queues packages that need to be processed.</param>
        /// <param name="configuration">The configuration.</param>
        internal FileWatcherBasedPackageUploader(
            IQueueSymbolPackages packageQueue,
            IConfiguration configuration)
        {
            {
                Lokad.Enforce.Argument(() => packageQueue);
                Lokad.Enforce.Argument(() => configuration);

                Lokad.Enforce.With<ArgumentException>(
                    configuration.HasValueFor(ConfigurationKeys.UploadPath),
                    Resources.Exceptions_Messages_MissingConfigurationValue_WithKey,
                    ConfigurationKeys.UploadPath);
            }

            m_Queue = packageQueue;

            var uploadPath = configuration.Value<string>(ConfigurationKeys.UploadPath);
            m_Watcher = new FileSystemWatcher
            {
                Path = uploadPath,
                Filter = "*.symbols.nupkg",
                EnableRaisingEvents = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            };

            m_Watcher.Created += HandleFileCreated;
        }

        private void HandleFileCreated(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                m_Queue.Enqueue(e.FullPath);
            }
        }

        /// <summary>
        /// Enables the uploading of packages.
        /// </summary>
        public void EnableUpload()
        {
            EnqueueExistingFiles();
            m_Watcher.EnableRaisingEvents = true;
        }

        private void EnqueueExistingFiles()
        {
            foreach (var file in Directory.GetFiles(m_Watcher.Path, m_Watcher.Filter, SearchOption.TopDirectoryOnly))
            {
                m_Queue.Enqueue(file);
            }
        }

        /// <summary>
        /// Disables the uploading of packages.
        /// </summary>
        public void DisableUpload()
        {
            m_Watcher.EnableRaisingEvents = false;
        }
    }
}
