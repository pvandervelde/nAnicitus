﻿//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using Autofac;
using Nanicitus.Core.Monitoring;
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
                    c.Resolve<IMetricsCollector>(),
                    c.Resolve<SystemDiagnostics>()))
                .As<IIndexSymbols>()
                .SingleInstance();

            builder.Register(c => new PackageQueue())
                .As<IQueueSymbolPackages>()
                .SingleInstance();

            builder.Register(c => new SymbolProcessor(
                    c.Resolve<IIndexSymbols>(),
                    c.Resolve<IQueueSymbolPackages>(),
                    c.Resolve<IConfiguration>(),
                    c.Resolve<IMetricsCollector>(),
                    c.Resolve<SystemDiagnostics>()))
                .As<ISymbolProcessor>()
                .SingleInstance();
        }
    }
}
