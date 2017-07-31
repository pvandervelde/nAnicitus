//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using InfluxDB.Collector;
using InfluxDB.Collector.Diagnostics;
using Nanicitus.Core.Monitoring;
using Nuclei.Configuration;
using Nuclei.Diagnostics;
using Nuclei.Diagnostics.Logging;

namespace Nanicitus.Service.Monitoring
{
    /// <summary>
    /// Class for pushing metrics to InfluxDB
    /// </summary>
    public sealed class InfluxMetricsCollector : IMetricsCollector
    {
        private readonly IConfiguration _configuration;
        private readonly SystemDiagnostics _diagnostics;
        private readonly IServiceDiscovery _serviceDiscovery;

        private MetricsCollector _metricsCollector;

        /// <summary>
        /// Initializes a new instance of the <see cref="InfluxMetricsCollector"/> class.
        /// </summary>
        /// <param name="configuration">Wrapper for obtaining app config settings</param>
        /// <param name="diagnostics">The logger object</param>
        /// <param name="serviceDiscovery">The object which handles consul interaction</param>
        public InfluxMetricsCollector(
            IConfiguration configuration,
            IServiceDiscovery serviceDiscovery,
            SystemDiagnostics diagnostics)
        {
            _configuration = configuration ?? throw new ArgumentNullException("configuration");
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
            _serviceDiscovery = serviceDiscovery ?? throw new ArgumentNullException("serviceDiscovery");

            InitialiseMetricsCollector();
        }

        /// <summary>
        /// Increments the count for the specific measurement.
        /// </summary>
        /// <param name="measurement">The name of the measurement.</param>
        /// <param name="tags">The tags for the measurement, e.g. the HTTP method for a HTTP request.</param>
        public void Increment(string measurement, IReadOnlyDictionary<string, string> tags = null)
        {
            _metricsCollector?.Increment(
                measurement,
                tags: tags);
        }

        /// <summary>
        /// Adds a value to a measurement.
        /// </summary>
        /// <param name="measurement">The name of the measurment.</param>
        /// <param name="type">The sub-type of the measurement, e.g. the HTTP method for a HTTP request.</param>
        /// <param name="value">The current value of the measurement.</param>
        public void Measure(string measurement, string type, object value)
        {
            _metricsCollector?.Measure(
                measurement,
                value,
                tags: new Dictionary<string, string>
                {
                    { "typeTag", type }
                });
        }

        /// <summary>
        /// Writes a request metric to InfluxDB
        /// </summary>
        /// <param name="measurement">The name of the measurement.</param>
        /// <param name="fields">The field values for the measurement which should be written.</param>
        /// <param name="tags">The tags for the measurement.</param>
        public void Write(
            string measurement,
            IReadOnlyDictionary<string, object> fields,
            IReadOnlyDictionary<string, string> tags)
        {
            _metricsCollector?.Write(
                measurement,
                fields,
                tags);
        }

        private void InitialiseMetricsCollector()
        {
            var metricsServer = _serviceDiscovery.MetricsServer;
            if (metricsServer != null)
            {
                var databaseName = _configuration.Value(ServiceConfigurationKeys.MetricsDatabaseName);
                var consulEnvironment = _serviceDiscovery.Environment;

                _metricsCollector = new CollectorConfiguration()
                    .Tag.With("host", Environment.GetEnvironmentVariable("COMPUTERNAME"))
                    .Tag.With("environment", consulEnvironment)
                    .Tag.With("stage", "Receiver")
                    .Batch.AtInterval(TimeSpan.FromSeconds(2))
                    .WriteTo.InfluxDB(metricsServer, databaseName)
                    .CreateCollector();

                // Output _metricsCollector errors to _logger
                CollectorLog.RegisterErrorHandler(
                    (m, e) => _diagnostics.Log(
                        LevelToLog.Error,
                        "{0} - {1}",
                        m,
                        e));
            }
        }
    }
}
