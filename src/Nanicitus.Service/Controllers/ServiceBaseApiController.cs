//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using Microsoft.Owin;
using Nanicitus.Core.Monitoring;
using Nuclei.Configuration;
using Nuclei.Diagnostics;
using Nuclei.Diagnostics.Logging;

namespace Nanicitus.Service.Controllers
{
    /// <summary>
    /// Defines a set of standard controller methods.
    /// </summary>
    public abstract class ServiceBaseApiController : ApiController
    {
        private readonly IConfiguration _configuration;
        private readonly SystemDiagnostics _diagnostics;
        private readonly IMetricsCollector _metrics;
        private readonly IServiceDiscovery _serviceDiscovery;
        private readonly IServiceInfo _serviceInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBaseApiController"/> class.
        /// </summary>
        /// <param name="configuration">The object that provides the configuration values for the application.</param>
        /// <param name="serviceDiscovery">An object that handles interaction with the service discovery system.</param>
        /// <param name="serviceInfo">Info object</param>
        /// <param name="metrics">The metrics object.</param>
        /// <param name="diagnostics">The logging object</param>
        protected ServiceBaseApiController(
            IConfiguration configuration,
            IServiceDiscovery serviceDiscovery,
            IServiceInfo serviceInfo,
            IMetricsCollector metrics,
            SystemDiagnostics diagnostics)
        {
            _configuration = configuration ?? throw new ArgumentNullException("configuration");
            _serviceDiscovery = serviceDiscovery ?? throw new ArgumentNullException("serviceDiscovery");
            _serviceInfo = serviceInfo ?? throw new ArgumentNullException("serviceInfo");
            _metrics = metrics ?? throw new ArgumentNullException("metrics");
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
        }

        /// <summary>
        /// Gets the diagnostics object.
        /// </summary>
        protected SystemDiagnostics Diagnostics
        {
            get => _diagnostics;
        }

        /// <summary>
        /// Logs the IP address, method and URI of an incoming request
        /// </summary>
        /// <param name="request">The trace record to log</param>
        protected void LogRequestDetails(HttpRequestMessage request)
        {
            if (request == null)
            {
                return;
            }

            var messageBuilder = new StringBuilder();

            // Get the ip address of the request
            if (request.Properties.ContainsKey("MS_OwinContext"))
            {
                var context = request.Properties["MS_OwinContext"] as OwinContext;
                if (context != null)
                {
                    var ip = context.Request.RemoteIpAddress;
                    messageBuilder.AppendFormat(CultureInfo.InvariantCulture, "Request from {0} ", ip);
                }
            }

            messageBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0} {1}", request.Method.Method, request.RequestUri);

            _diagnostics.Log(
                LevelToLog.Trace,
                messageBuilder.ToString());
        }

        /// <summary>
        /// Gets the metrics object.
        /// </summary>
        protected IMetricsCollector Metrics
        {
            get => _metrics;
        }

        /// <summary>
        /// Gets the configuration object.
        /// </summary>
        protected IConfiguration ServiceConfiguration
        {
            get => _configuration;
        }

        /// <summary>
        /// Gets the service discovery object.
        /// </summary>
        protected IServiceDiscovery ServiceDiscovery
        {
            get => _serviceDiscovery;
        }

        /// <summary>
        /// Gets the service info object.
        /// </summary>
        protected IServiceInfo ServiceInfo
        {
            get => _serviceInfo;
        }

        /// <summary>
        /// Stores metrics information for the given request.
        /// </summary>
        /// <param name="request">The request</param>
        protected void StoreRequestMetrics(HttpRequestMessage request)
        {
            if (request == null)
            {
                return;
            }

            _metrics.Write(
                "Symbols.HttpRequest",
                new Dictionary<string, object>
                {
                    { "path", request.RequestUri.PathAndQuery },
                },
                new Dictionary<string, string>
                {
                    { "method", request.Method.ToString() }
                });
        }
    }
}
