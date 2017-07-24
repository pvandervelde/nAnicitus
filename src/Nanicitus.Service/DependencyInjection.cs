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
using Autofac;
using Nanicitus.Core;
using NLog;
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

        private static void RegisterLoggers(ContainerBuilder builder)
        {
            builder.Register(
                    c =>
                    {
                        var ctx = c.Resolve<IComponentContext>();
                        var config = ctx.Resolve<IConfiguration>();
                        var configPath = string.Empty;
                        if (config.HasValueFor(ServiceConfigurationKeys.LogPath))
                        {
                            configPath = config.Value<string>(ServiceConfigurationKeys.LogPath);
                        }

                        var path = Path.Combine(
                            !string.IsNullOrWhiteSpace(configPath)
                                ? configPath
                                : FileConstants.LogPath(),
                            DefaultInfoFileName);
                        return LoggerBuilder.ForFile(
                            "file",
                            path);
                    })
                .As<ILogger>()
                .SingleInstance();

            builder.Register(c => LoggerBuilder.ForEventLog(
                    "eventlog",
                    Assembly.GetExecutingAssembly().GetName().Name))
                .As<ILogger>()
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

        /// <summary>
        /// Creates the DI container for the application.
        /// </summary>
        /// <returns>The DI container.</returns>
        public static IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();
            {
                builder.Register(c => new ApplicationConfiguration(
                        ServiceConfigurationKeys.ToCollection()
                            .Append(CoreConfigurationKeys.ToCollection())
                            .Append(DiagnosticsConfigurationKeys.ToCollection())
                            .ToList(),
                        Constants.ConfigurationSectionApplicationSettings))
                    .As<IConfiguration>()
                    .SingleInstance();

                builder.RegisterModule(new NanicitusModule());

                RegisterLoggers(builder);
                RegisterDiagnostics(builder);
            }

            return builder.Build();
        }
    }
}
