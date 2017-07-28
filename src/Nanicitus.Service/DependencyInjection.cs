//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Http.Tracing;
using Autofac;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using Nanicitus.Core;
using Nuclei.Configuration;
using Nuclei.Diagnostics;
using Nuclei.Diagnostics.Logging;
using Nuclei.Diagnostics.Logging.NLog;
using Nuclei.Diagnostics.Metrics;
using ILogger = Nuclei.Diagnostics.Logging.ILogger;

namespace Nanicitus.Service
{
    /// <summary>
    /// Creates the dependency injection container with all the required references.
    /// </summary>
    internal static class DependencyInjection
    {
        /// <summary>
        /// The default name for the error log.
        /// </summary>
        private const string DefaultInfoFileName = "nanicitus.info.log";

        /// <summary>
        /// The default name for the profiler log.
        /// </summary>
        private const string DefaultProfilerFileName = "nanicitus.profile";

        /// <summary>
        /// The DI container.
        /// </summary>
        private static IContainer _container;

        /// <summary>
        /// Gets the DI container.
        /// </summary>
        public static IContainer Container
        {
            get => _container;
        }

        /// <summary>
        /// Creates the DI container for the application.
        /// </summary>
        public static void CreateContainer()
        {
            var builder = new ContainerBuilder();
            {
                RegisterWebApiControllers(builder);
                RegisterMvcControllers(builder);

                RegisterConfiguration(builder);
                RegisterLoggers(builder);
                RegisterDiagnostics(builder);

                builder.RegisterModule(new NanicitusModule());

                RegisterServiceDiscovery(builder);
                RegisterServiceInfo(builder);
                RegisterEntryPoint(builder);
            }

            _container = builder.Build();
        }

        private static void RegisterConfiguration(ContainerBuilder builder)
        {
            var constantConfiguration = new ConstantConfiguration(
                    new[]
                    {
                        CoreConfigurationKeys.ToDefault(),
                        ServiceConfigurationKeys.ToDefault()
                    }
                    .SelectMany(dict => dict)
                    .ToDictionary(pair => pair.Key, pair => pair.Value));

            var knownKeys = ServiceConfigurationKeys.ToDefault().Keys
                .Concat(CoreConfigurationKeys.ToDefault().Keys)
                .Concat(DiagnosticsConfigurationKeys.ToCollection())
                .ToList();
            var applicationConfiguration = new ApplicationConfiguration(
                knownKeys,
                Constants.ConfigurationSectionApplicationSettings);

            var baseConfiguration = new HierarchicalConfiguration(
                new IConfiguration[]
                {
                    applicationConfiguration,
                    constantConfiguration
                });

            var consulConfiguration = new ConsulConfiguration(
                knownKeys,
                baseConfiguration);

            builder.Register(c => new HierarchicalConfiguration(
                    new IConfiguration[]
                    {
                        consulConfiguration,
                        applicationConfiguration,
                        constantConfiguration
                    }))
                .As<IConfiguration>()
                .SingleInstance();
        }

        private static void RegisterDiagnostics(ContainerBuilder builder)
        {
            builder.Register(
                c =>
                {
                    var loggers = c.Resolve<IEnumerable<ILogger>>();
                    IMetricsCollector profiler = null;
                    return new SystemDiagnostics(new DistributedLogger(loggers), profiler);
                })
                .As<SystemDiagnostics>()
                .SingleInstance();
        }

        private static void RegisterEntryPoint(ContainerBuilder builder)
        {
            builder.RegisterType<WebService>();
        }

        private static void RegisterLoggers(ContainerBuilder builder)
        {
            builder.Register(
                    c =>
                    {
                        return LoggerBuilder.FromConfiguration("file");
                    })
                .As<ILogger>()
                .SingleInstance();

            builder.Register(c => new NucleiBasedTraceWriter(
                        c.Resolve<SystemDiagnostics>()))
                    .As<ITraceWriter>()
                    .SingleInstance();
        }

        private static void RegisterMvcControllers(ContainerBuilder builder)
        {
            builder.RegisterControllers(Assembly.GetExecutingAssembly());
            builder.RegisterModelBinders(Assembly.GetExecutingAssembly());
            builder.RegisterModelBinderProvider();
        }

        private static void RegisterServiceDiscovery(ContainerBuilder builder)
        {
            builder.RegisterType(typeof(ConsulServiceDiscovery))
                .As<IServiceDiscovery>()
                .SingleInstance();
        }

        private static void RegisterServiceInfo(ContainerBuilder builder)
        {
            builder.RegisterType(typeof(ServiceInfo))
                .As<IServiceInfo>()
                .SingleInstance();
        }

        private static void RegisterWebApiControllers(ContainerBuilder builder)
        {
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
        }
    }
}
