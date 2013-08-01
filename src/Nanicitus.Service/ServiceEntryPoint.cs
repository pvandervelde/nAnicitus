//-----------------------------------------------------------------------
// <copyright company="NAnicitus">
//     Copyright 2013 NAnicitus. Licensed under the Apache License, Version 2.0.
// </copyright>
//-----------------------------------------------------------------------

using Autofac;
using Nanicitus.Core;

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
        private readonly object m_Lock = new object();

        /// <summary>
        /// The IOC container.
        /// </summary>
        private IContainer m_Container;

        /// <summary>
        /// The object that handles the indexing of source files.
        /// </summary>
        private IIndexSymbols m_Indexer;

        /// <summary>
        /// The object that handles tracking of the new uploads.
        /// </summary>
        private IUploadPackages m_Uploader;

        /// <summary>
        /// When implemented in a derived class, executes when a Start command is sent
        /// to the service by the Service Control Manager (SCM) or when the operating
        /// system starts (for a service that starts automatically). Specifies actions
        /// to take when the service starts.
        /// </summary>
        public void OnStart()
        {
            lock (m_Lock)
            {
                if ((m_Uploader != null) || (m_Indexer != null) || (m_Container != null))
                {
                    OnStop();
                }

                m_Container = DependencyInjection.CreateContainer();
                m_Uploader = m_Container.Resolve<IUploadPackages>();
                m_Indexer = m_Container.Resolve<IIndexSymbols>();

                m_Indexer.Start();
                m_Uploader.EnableUpload();
            }
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Stop command is sent
        /// to the service by the Service Control Manager (SCM). Specifies actions to
        /// take when a service stops running.
        /// </summary>
        public void OnStop()
        {
            lock (m_Lock)
            {
                if (m_Uploader != null)
                {
                    m_Uploader.DisableUpload();
                    m_Uploader = null;
                }

                if (m_Indexer != null)
                {
                    var clearingTask = m_Indexer.Stop(true);
                    clearingTask.Wait();
                    m_Indexer = null;
                }

                if (m_Container != null)
                {
                    m_Container.Dispose();
                    m_Container = null;
                }
            }
        }
    }
}
