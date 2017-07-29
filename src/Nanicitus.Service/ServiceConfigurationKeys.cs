//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Nanicitus.Service.Properties;
using Nuclei;
using Nuclei.Configuration;

namespace Nanicitus.Service
{
    internal static class ServiceConfigurationKeys
    {
        /// <summary>
        /// The configuration key that is used to retrieve path for the directory in
        /// which the log files will be placed.
        /// </summary>
        public static readonly ConfigurationKey<string> LogPath
            = new ConfigurationKey<string>("LogPath");

        /// <summary>
        /// The configuration key that is used to retrieve the name of the time series
        /// database into which the metrics for the application are stored.
        /// </summary>
        public static readonly ConfigurationKey<string> MetricsDatabaseName
            = new ConfigurationKey<string>("MetricsDatabaseName");

        /// <summary>
        /// The configuration key that is used to retrieve the service discovery name
        /// of the metrics service.
        /// </summary>
        public static readonly ConfigurationKey<string> MetricsServiceName
            = new ConfigurationKey<string>("MetricsServiceName");

        /// <summary>
        /// The configuration key that is used to retrieve the service discovery tag
        /// of the metrics service.
        /// </summary>
        public static readonly ConfigurationKey<string> MetricsServiceTag
            = new ConfigurationKey<string>("MetricsServiceTag");

        /// <summary>
        /// The configuration key that is used to retrieve the interval on which the health check
        /// should take place.
        /// </summary>
        public static readonly ConfigurationKey<int> ServiceDiscoveryHealthCheckIntervalInSeconds
            = new ConfigurationKey<int>("ServiceDiscoveryHealthCheckIntervalInSeconds");

        /// <summary>
        /// The configuration key that is used to retrieve the name of the service.
        /// </summary>
        public static readonly ConfigurationKey<string> ServiceDiscoveryName
            = new ConfigurationKey<string>("ServiceDiscoveryName");

        /// <summary>
        /// The configuration key that is used to retrieve the service discovery tag
        /// which is used to indicate that the service is active.
        /// </summary>
        public static readonly ConfigurationKey<string> ServiceDiscoveryActiveTag
            = new ConfigurationKey<string>("ServiceDiscoveryActiveTag");

        /// <summary>
        /// The configuration key that is used to retrieve the tags for the service.
        /// </summary>
        public static readonly ConfigurationKey<string[]> ServiceDiscoveryTags
            = new ConfigurationKey<string[]>("ServiceDiscoveryTags");

        /// <summary>
        /// The configuration key that is used to retrieve the port which should be
        /// used by the service.
        /// </summary>
        public static readonly ConfigurationKey<int> ServicePort
            = new ConfigurationKey<int>("ServicePort");

        /// <summary>
        /// The configuration key that is used to retrieve a flag that indicates if the
        /// service should register with the service discovery system.
        /// </summary>
        public static readonly ConfigurationKey<bool> ShouldRegisterForDiscovery
            = new ConfigurationKey<bool>("ShouldRegisterForDiscovery");

        /// <summary>
        /// The configuration key that is used to retrieve the temporary path location.
        /// </summary>
        public static readonly ConfigurationKey<string> TempPath
            = new ConfigurationKey<string>("TempPath");

        /// <summary>
        /// Returns a collection containing all the configuration keys for the application and
        /// their default values.
        /// </summary>
        /// <returns>A collection containing all the configuration keys and their default values.</returns>
        public static IDictionary<ConfigurationKeyBase, object> ToDefault()
        {
            return new Dictionary<ConfigurationKeyBase, object>
            {
                { ConfigurationKeys.ConsulAddress, "http://localhost:8500" },
                { LogPath, FileConstants.LogPath() },
                { MetricsDatabaseName, "symbols" },
                { MetricsServiceName, "metrics" },
                { MetricsServiceTag, "active" },
                { ServiceDiscoveryHealthCheckIntervalInSeconds, 300 },
                { ServiceDiscoveryName, Resources.Service_ServiceName },
                { ServiceDiscoveryActiveTag, "active" },
                { ServiceDiscoveryTags, new string[0] },
                { ServicePort, 5050 },
                { ShouldRegisterForDiscovery, true },
                { TempPath, Path.Combine(Assembly.GetExecutingAssembly().LocalDirectoryPath(), "temp") },
            };
        }
    }
}
