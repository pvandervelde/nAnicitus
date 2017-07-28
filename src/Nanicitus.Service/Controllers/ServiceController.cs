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
using Microsoft.Owin;
using Microsoft.Web.Http;
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
    public sealed class ServiceController : ApiController
    {
        private readonly IConfiguration _configuration;
        private readonly SystemDiagnostics _diagnostics;
        private readonly IServiceDiscovery _serviceDiscovery;
        private readonly IServiceInfo _serviceInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceController"/> class.
        /// </summary>
        /// <param name="configuration">The object that provides the configuration values for the application.</param>
        /// <param name="serviceDiscovery">An object that handles interaction with the service discovery system.</param>
        /// <param name="serviceInfo">Info object</param>
        /// <param name="diagnostics">The logging object</param>
        public ServiceController(
            IConfiguration configuration,
            IServiceDiscovery serviceDiscovery,
            IServiceInfo serviceInfo,
            SystemDiagnostics diagnostics)
        {
            _configuration = configuration ?? throw new ArgumentNullException("configuration");
            _serviceDiscovery = serviceDiscovery ?? throw new ArgumentNullException("serviceDiscovery");
            _serviceInfo = serviceInfo ?? throw new ArgumentNullException("serviceInfo");
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
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

            _serviceInfo.IsActive = true;
            _serviceInfo.IsEnabled = true;
            _serviceInfo.IsStandby = false;

            try
            {
                var activeTag = _configuration.Value(ServiceConfigurationKeys.ServiceDiscoveryActiveTag);
                _serviceDiscovery.UpdateTags(activeTag);

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

            _serviceInfo.IsActive = false;
            _serviceInfo.IsEnabled = false;
            _serviceInfo.IsStandby = false;

            try
            {
                _serviceDiscovery.UpdateTags();

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

            var metricsIsUp = IsWebServiceUp(_serviceDiscovery.MetricsServer);

            var responseContent = new
            {
                MetricsIsUp = metricsIsUp,
            };
            var responseMessage = new HttpResponseMessage();
            responseMessage.StatusCode = HttpStatusCode.OK;

            responseMessage.Content = new StringContent(JsonConvert.SerializeObject(responseContent), System.Text.Encoding.UTF8, "application/json");

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

            var responseContent = new
            {
                ReportAsOf = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                LastReceivedTime = default(DateTimeOffset).Equals(_serviceInfo.LastReceivedTime) ? "None" : _serviceInfo.LastReceivedTime.ToString("o", CultureInfo.InvariantCulture),
                IsActive = _serviceInfo.IsActive,
                IsEnabled = _serviceInfo.IsEnabled,
                IsStandBy = _serviceInfo.IsStandby,
            };

            var responseCode = _serviceInfo.IsEnabled ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;
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

            var responseContent = new
            {
                IsActive = _serviceInfo.IsActive,
                IsEnabled = _serviceInfo.IsEnabled,
                IsStandBy = _serviceInfo.IsStandby,
            };

            var responseCode = _serviceInfo.IsActive ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;
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

            _serviceInfo.IsActive = false;
            _serviceInfo.IsEnabled = true;
            _serviceInfo.IsStandby = true;

            try
            {
                _serviceDiscovery.UpdateTags();

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

            var responseContent = new
            {
                ServiceId = _serviceInfo.ServiceId,
                CurrentTime = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                MachineName = _serviceInfo.MachineName,
                OSName = _serviceInfo.OSName,
                Version = _serviceInfo.AssemblyVersion.ToString(),
                GitSHA = _serviceInfo.GitSHA,
                BuildTime = _serviceInfo.BuildTime,
                UpDuration = (DateTimeOffset.UtcNow - _serviceInfo.StartTime).ToString(),
                UpSince = _serviceInfo.StartTime.ToString("o", CultureInfo.InvariantCulture),
                IsEnabled = _serviceInfo.IsEnabled,
                IsStandBy = _serviceInfo.IsStandby,
                IsActive = _serviceInfo.IsActive,
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

                _diagnostics.Log(
                    LevelToLog.Error,
                    "{0}",
                    ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// Logs the IP address, method and URI of an incoming request
        /// </summary>
        /// <param name="request">The trace record to log</param>
        private void LogRequestDetails(HttpRequestMessage request)
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
    }
}
