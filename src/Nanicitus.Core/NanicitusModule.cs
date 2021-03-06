﻿//-----------------------------------------------------------------------
// <copyright company="NAnicitus">
//     Copyright 2013 NAnicitus. Licensed under the Apache License, Version 2.0.
// </copyright>
//-----------------------------------------------------------------------

using Autofac;
using Nuclei.Configuration;
using Nuclei.Diagnostics;

namespace Nanicitus.Core
{
    /// <summary>
    /// Provides the component registrations for the core layer.
    /// </summary>
    public sealed class NanicitusModule : Module
    {
        /// <summary>
        /// Override to add registrations to the container.
        /// </summary>
        /// <param name="builder">The builder through which components can be registered.</param>
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new SymbolIndexer(
                    c.Resolve<IQueueSymbolPackages>(),
                    c.Resolve<IConfiguration>(),
                    c.Resolve<SystemDiagnostics>()))
                .As<IIndexSymbols>()
                .SingleInstance();

            builder.Register(c => new PackageQueue())
                .As<IQueueSymbolPackages>()
                .SingleInstance();

            builder.Register(c => new FileWatcherBasedPackageUploader(
                    c.Resolve<IQueueSymbolPackages>(),
                    c.Resolve<IConfiguration>(),
                    c.Resolve<SystemDiagnostics>()))
                .As<IUploadPackages>()
                .SingleInstance();
        }
    }
}
