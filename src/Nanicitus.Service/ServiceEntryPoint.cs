//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using Autofac;
using log4net.Core;
using Nanicitus.Core;
using Nanicitus.Service.Properties;
using Nuclei.Diagnostics;
using Nuclei.Diagnostics.Logging;

namespace Nanicitus.Service
{
    /// <summary>
    /// Defines the entry point for the service.
    /// </summary>
    internal sealed class ServiceEntryPoint
    {
        /// <summary>
        /// The object used to lock on.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// The IOC container.
        /// </summary>
        private IContainer _container;

        /// <summary>
        /// The object that handles the indexing of source files.
        /// </summary>
        private IIndexSymbols _indexer;

        /// <summary>
        /// The object that handles tracking of the new uploads.
        /// </summary>
        private IUploadPackages _uploader;

        /// <summary>
        /// The object that provides the diagnostics methods for the application.
        /// </summary>
        private SystemDiagnostics _diagnostics;

        /// <summary>
        /// When implemented in a derived class, executes when a Start command is sent
        /// to the service by the Service Control Manager (SCM) or when the operating
        /// system starts (for a service that starts automatically). Specifies actions
        /// to take when the service starts.
        /// </summary>
        public void OnStart()
        {
            lock (_lock)
            {
                if ((_uploader != null) || (_indexer != null) || (_container != null))
                {
                    OnStop();
                }

                _container = DependencyInjection.CreateContainer();
                _uploader = _container.Resolve<IUploadPackages>();
                _indexer = _container.Resolve<IIndexSymbols>();
                _diagnostics = _container.Resolve<SystemDiagnostics>();

                _diagnostics.Log(
                    LevelToLog.Info,
                    Resources.Log_Messages_ServiceEntryPoint_StartingService);

                _indexer.Start();
                _uploader.EnableUpload();
            }
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Stop command is sent
        /// to the service by the Service Control Manager (SCM). Specifies actions to
        /// take when a service stops running.
        /// </summary>
        public void OnStop()
        {
            lock (_lock)
            {
                if (_diagnostics != null)
                {
                    _diagnostics.Log(
                        LevelToLog.Info,
                        Resources.Log_Messages_ServiceEntryPoint_StoppingService);
                }

                if (_uploader != null)
                {
                    _uploader.DisableUpload();
                    _uploader = null;
                }

                if (_indexer != null)
                {
                    var clearingTask = _indexer.Stop(true);
                    clearingTask.Wait();
                    _indexer = null;
                }

                if (_container != null)
                {
                    _container.Dispose();
                    _container = null;
                }

                if (_diagnostics != null)
                {
                    _diagnostics.Log(
                        LevelToLog.Info,
                        Resources.Log_Messages_ServiceEntryPoint_ServiceStopped);
                }
            }
        }
    }
}
