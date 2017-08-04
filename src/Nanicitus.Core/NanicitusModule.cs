//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
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
            builder.Register(
                (c, p) =>
                {
                    var ctx = c.Resolve<IComponentContext>();
                    Func<IQueueSymbolPackages, IConfiguration, IIndexSymbols> b =
                        (queue, config) => new SymbolIndexer(
                            queue,
                            config,
                            ctx.Resolve<IMetricsCollector>(),
                            ctx.Resolve<SystemDiagnostics>());
                    return b;
                })
                .As<Func<IQueueSymbolPackages, IConfiguration, IIndexSymbols>>()
                .SingleInstance();

            builder.Register(
                c =>
                {
                    Func<IQueueSymbolPackages> b = () => new PackageQueue();
                    return b;
                })
                .As<Func<IQueueSymbolPackages>>()
                .SingleInstance();

            builder.Register(c => new SymbolProcessor(
                    c.Resolve<Func<IQueueSymbolPackages, IConfiguration, IIndexSymbols>>(),
                    c.Resolve<Func<IQueueSymbolPackages>>(),
                    c.Resolve<IConfiguration>(),
                    c.Resolve<IMetricsCollector>(),
                    c.Resolve<SystemDiagnostics>()))
                .As<ISymbolProcessor>()
                .SingleInstance();
        }
    }
}
