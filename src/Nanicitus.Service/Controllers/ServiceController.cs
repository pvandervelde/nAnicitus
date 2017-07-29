//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using Consul;
using Microsoft.Web.Http;
using Nanicitus.Core.Monitoring;
using Newtonsoft.Json;
using Nuclei.Configuration;
using Nuclei.Diagnostics;
using Nuclei.Diagnostics.Logging;

namespace Nanicitus.Service.Controllers
{
    /// <summary>
    /// The api controller for the health endpoints
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/service/{action}")]
    public sealed class ServiceController : ServiceBaseApiController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceController"/> class.
        /// </summary>
        /// <param name="configuration">The object that provides the configuration values for the application.</param>
        /// <param name="serviceDiscovery">An object that handles interaction with the service discovery system.</param>
        /// <param name="serviceInfo">Info object</param>
        /// <param name="metrics">The metrics object.</param>
        /// <param name="diagnostics">The logging object</param>
        public ServiceController(
            IConfiguration configuration,
            IServiceDiscovery serviceDiscovery,
            IServiceInfo serviceInfo,
            IMetricsCollector metrics,
            SystemDiagnostics diagnostics)
            : base(configuration, serviceDiscovery, serviceInfo, metrics, diagnostics)
        {
        }

        /// <summary>
        /// Activates the services, enabling message processing and setting the service discovery
        /// tag to indicate that the service will accept requests.
        /// </summary>
        /// <returns>An response message containing the result of the operation.</returns>
        [HttpPut]
        public HttpResponseMessage Activate()
        {
            LogRequestDetails(Request);
            StoreRequestMetrics(Request);

            ServiceInfo.IsActive = true;
            ServiceInfo.IsEnabled = true;
            ServiceInfo.IsStandby = false;

            try
            {
                var activeTag = ServiceConfiguration.Value(ServiceConfigurationKeys.ServiceDiscoveryActiveTag);
                ServiceDiscovery.UpdateTags(activeTag);

                var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

                return responseMessage;
            }
            catch (ConsulConfigurationException)
            {
                var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);

                return responseMessage;
            }
        }

        /// <summary>
        /// Disables the services and message processing and setting the service discovery
        /// to indicate that the service is not available.
        /// </summary>
        /// <returns>An response message containing the result of the operation.</returns>
        [HttpPut]
        public HttpResponseMessage Disable()
        {
            LogRequestDetails(Request);
            StoreRequestMetrics(Request);

            ServiceInfo.IsActive = false;
            ServiceInfo.IsEnabled = false;
            ServiceInfo.IsStandby = false;

            try
            {
                ServiceDiscovery.UpdateTags();

                var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

                return responseMessage;
            }
            catch (ConsulConfigurationException)
            {
                var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);

                return responseMessage;
            }
        }

        /// <summary>
        /// Returns service's perception of the health of its dependencies on an HTTP GET request to the dependencies action
        /// </summary>
        /// <returns>
        /// An HttpResponseMessage with a JSON object containing the perceived health of dependencies
        /// </returns>
        /// <remarks>
        /// GET api/v1/Service/Dependencies
        /// </remarks>
        [HttpGet]
        public HttpResponseMessage Dependencies()
        {
            LogRequestDetails(Request);
            StoreRequestMetrics(Request);

            var metricsIsUp = IsWebServiceUp(ServiceDiscovery.MetricsServer);

            var responseContent = new
            {
                MetricsIsUp = metricsIsUp,
            };
            var responseMessage = new HttpResponseMessage();
            responseMessage.StatusCode = HttpStatusCode.OK;

            responseMessage.Content = new StringContent(
                JsonConvert.SerializeObject(responseContent),
                Encoding.UTF8,
                "application/json");

            return responseMessage;
        }

        /// <summary>
        /// Returns service health information on an HTTP GET request to the healthcheck action
        /// </summary>
        /// <returns>
        /// An HttpResponseMessage with a JSON object containing the health check response
        /// </returns>
        /// <remarks>
        /// GET api/v1/Service/HealthCheck
        /// </remarks>
        [HttpGet]
        public HttpResponseMessage HealthCheck()
        {
            LogRequestDetails(Request);
            StoreRequestMetrics(Request);

            var responseContent = new
            {
                ReportAsOf = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                LastReceivedTime = default(DateTimeOffset).Equals(ServiceInfo.LastReceivedTime) ? "None" : ServiceInfo.LastReceivedTime.ToString("o", CultureInfo.InvariantCulture),
                IsActive = ServiceInfo.IsActive,
                IsEnabled = ServiceInfo.IsEnabled,
                IsStandBy = ServiceInfo.IsStandby,
            };

            var responseCode = ServiceInfo.IsEnabled ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;
            var responseMessage = new HttpResponseMessage(responseCode);
            responseMessage.Content = new StringContent(
                JsonConvert.SerializeObject(responseContent),
                Encoding.UTF8,
                "application/json");

            return responseMessage;
        }

        /// <summary>
        /// Returns an indication on whether the service is active and able to process
        /// symbols or not.
        /// </summary>
        /// <returns>
        /// An HttpResponseMessage with a JSON object containing the active status response
        /// </returns>
        /// <remarks>
        /// GET api/v1/Service/IsActive
        /// </remarks>
        [HttpGet]
        public HttpResponseMessage IsActive()
        {
            LogRequestDetails(Request);
            StoreRequestMetrics(Request);

            var responseContent = new
            {
                IsActive = ServiceInfo.IsActive,
                IsEnabled = ServiceInfo.IsEnabled,
                IsStandBy = ServiceInfo.IsStandby,
            };

            var responseCode = ServiceInfo.IsActive ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;
            var responseMessage = new HttpResponseMessage(responseCode);
            responseMessage.Content = new StringContent(
                JsonConvert.SerializeObject(responseContent),
                Encoding.UTF8,
                "application/json");

            return responseMessage;
        }

        /// <summary>
        /// Activates the services, keeping message processing disabled and setting the service discovery
        /// tag to indicate that the service is healthy but will not accept requests.
        /// </summary>
        /// <returns>An response message containing the result of the operation.</returns>
        [HttpPut]
        public HttpResponseMessage Standby()
        {
            LogRequestDetails(Request);
            StoreRequestMetrics(Request);

            ServiceInfo.IsActive = false;
            ServiceInfo.IsEnabled = true;
            ServiceInfo.IsStandby = true;

            try
            {
                ServiceDiscovery.UpdateTags();

                var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

                return responseMessage;
            }
            catch (ConsulConfigurationException)
            {
                var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);

                return responseMessage;
            }
        }

        /// <summary>
        /// Returns service status information on an HTTP GET request to the status action
        /// </summary>
        /// <returns>
        /// An HttpResponseMessage with a JSON object containing the service status
        /// </returns>
        /// <remarks>
        /// GET api/v1/Service/Status
        /// </remarks>
        [HttpGet]
        public HttpResponseMessage Status()
        {
            LogRequestDetails(Request);
            StoreRequestMetrics(Request);

            var responseContent = new
            {
                ServiceId = ServiceInfo.ServiceId,
                CurrentTime = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                MachineName = ServiceInfo.MachineName,
                OSName = ServiceInfo.OSName,
                Version = ServiceInfo.AssemblyVersion.ToString(),
                GitSHA = ServiceInfo.GitSHA,
                BuildTime = ServiceInfo.BuildTime,
                UpDuration = (DateTimeOffset.UtcNow - ServiceInfo.StartTime).ToString(),
                UpSince = ServiceInfo.StartTime.ToString("o", CultureInfo.InvariantCulture),
                IsEnabled = ServiceInfo.IsEnabled,
                IsStandBy = ServiceInfo.IsStandby,
                IsActive = ServiceInfo.IsActive,
            };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            responseMessage.Content = new StringContent(
                JsonConvert.SerializeObject(responseContent),
                Encoding.UTF8,
                "application/json");

            return responseMessage;
        }

        private bool IsWebServiceUp(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            var request = (HttpWebRequest)WebRequest.Create(address);
            request.Timeout = 5000;

            try
            {
                request.GetResponse();
                return true;
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    return true;
                }

                Diagnostics.Log(
                    LevelToLog.Error,
                    "{0}",
                    ex.ToString());
                return false;
            }
        }
    }
}
