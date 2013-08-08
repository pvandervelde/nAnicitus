//-----------------------------------------------------------------------
// <copyright company="NAnicitus">
//     Copyright 2013 NAnicitus. Licensed under the Apache License, Version 2.0.
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
using Nuclei.Diagnostics.Profiling;
using Nuclei.Diagnostics.Profiling.Reporting;

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
                                : c.Resolve<FileConstants>().LogPath(), 
                            DefaultInfoFileName);
                        return LoggerBuilder.ForFile(
                            path,
                            new DebugLogTemplate(
                                c.Resolve<IConfiguration>(),
                                () => DateTimeOffset.Now));
                    })
                .As<ILogger>()
                .SingleInstance();

            builder.Register(c => LoggerBuilder.ForEventLog(
                    Assembly.GetExecutingAssembly().GetName().Name,
                    new DebugLogTemplate(
                        c.Resolve<IConfiguration>(),
                        () => DateTimeOffset.Now)))
                .As<ILogger>()
                .SingleInstance();
        }

        private static void RegisterProfiler(ContainerBuilder builder)
        {
            if (ConfigurationHelpers.ShouldBeProfiling())
            {
                builder.Register((c, p) => new TextReporter(p.TypedAs<Func<Stream>>()))
                        .As<TextReporter>()
                        .As<ITransformReports>();

                builder.Register(c => new TimingStorage())
                    .OnRelease(
                        storage =>
                        {
                            // Write all the profiling results out to disk. Do this the ugly way 
                            // because we don't know if any of the other items in the container have
                            // been removed yet.
                            Func<Stream> factory =
                                () => new FileStream(
                                    Path.Combine(new FileConstants(new ApplicationConstants()).LogPath(), DefaultProfilerFileName),
                                    FileMode.Append,
                                    FileAccess.Write,
                                    FileShare.Read);
                            var reporter = new TextReporter(factory);
                            reporter.Transform(storage.FromStartTillEnd());
                        })
                    .As<IStoreIntervals>()
                    .As<IGenerateTimingReports>()
                    .SingleInstance();

                builder.Register(c => new Profiler(
                        c.Resolve<IStoreIntervals>()))
                    .SingleInstance();
            }
        }

        private static void RegisterDiagnostics(ContainerBuilder builder)
        {
            builder.Register(
                c =>
                {
                    var loggers = c.Resolve<IEnumerable<ILogger>>();
                    Action<LevelToLog, string> action = (p, s) =>
                    {
                        var msg = new LogMessage(p, s);
                        foreach (var logger in loggers)
                        {
                            try
                            {
                                logger.Log(msg);
                            }
                            catch (NLogRuntimeException)
                            {
                                // Ignore it and move on to the next logger.
                            }
                        }
                    };

                    Profiler profiler = null;
                    if (c.IsRegistered<Profiler>())
                    {
                        profiler = c.Resolve<Profiler>();
                    }

                    return new SystemDiagnostics(action, profiler);
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
                builder.Register(c => new ApplicationConstants())
                   .As<ApplicationConstants>();

                builder.Register(c => new FileConstants(c.Resolve<ApplicationConstants>()))
                    .As<FileConstants>();

                builder.Register(c => new XmlConfiguration(
                        ServiceConfigurationKeys.ToCollection()
                            .Append(CoreConfigurationKeys.ToCollection())
                            .Append(DiagnosticsConfigurationKeys.ToCollection())
                            .ToList(),
                        Constants.ConfigurationSectionApplicationSettings))
                    .As<IConfiguration>()
                    .SingleInstance();

                builder.RegisterModule(new NanicitusModule());
                
                RegisterLoggers(builder);
                RegisterProfiler(builder);
                RegisterDiagnostics(builder);
            }

            return builder.Build();
        }
    }
}
