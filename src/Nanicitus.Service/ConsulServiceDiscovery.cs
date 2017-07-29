//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using Consul;
using Nuclei.Configuration;
using Nuclei.Diagnostics;
using Nuclei.Diagnostics.Logging;

namespace Nanicitus.Service
{
    /// <summary>
    /// Defines a <see cref="IServiceDiscovery"/> instance that connects to a consul cluster.
    /// </summary>
    internal sealed class ConsulServiceDiscovery : IServiceDiscovery
    {
        private static AgentServiceCheck[] CreateChecks(int port, int healthIntervalInSeconds)
        {
            return new[]
                {
                    new AgentServiceCheck
                    {
                        HTTP = string.Format(
                            CultureInfo.InvariantCulture,
                            "http://localhost:{0}/api/v1/service/dependencies",
                            port),
                        Interval = new TimeSpan(0, 0, healthIntervalInSeconds)
                    },
                    new AgentServiceCheck
                    {
                        HTTP = string.Format(
                            CultureInfo.InvariantCulture,
                            "http://localhost:{0}/api/v1/service/healthcheck",
                            port),
                        Interval = new TimeSpan(0, 0, healthIntervalInSeconds)
                    },
                    new AgentServiceCheck
                    {
                        HTTP = string.Format(
                            CultureInfo.InvariantCulture,
                            "http://localhost:{0}/api/v1/service/isactive",
                            port),
                        Interval = new TimeSpan(0, 0, healthIntervalInSeconds)
                    },
                };
        }

        private static string GetServiceId(string serviceName)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1}-{2}",
                serviceName,
                System.Environment.MachineName,
                Process.GetCurrentProcess().Id);
        }

        private readonly IConfiguration _configuration;
        private readonly ConsulClient _consulClient;
        private readonly SystemDiagnostics _diagnostics;

        private string _consulAddress = null;
        private string _consulDomain = null;
        private string _consulEnvironment = null;
        private string _metricsServer = null;

        public ConsulServiceDiscovery(IConfiguration configuration, SystemDiagnostics diagnostics)
        {
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
            _configuration = configuration ?? throw new ArgumentNullException("configuration");
            _consulClient = GetConsulClient();
        }

        private string ConsulAddress
        {
            get
            {
                if (_consulAddress == null)
                {
                    _consulAddress = _configuration.Value(ConfigurationKeys.ConsulAddress);
                }

                return _consulAddress;
            }
        }

        private string ConsulDomain
        {
            get
            {
                if (_consulDomain == null)
                {
                    var configRequestResponse = _consulClient.Agent.Self().Result.Response;
                    _consulDomain = configRequestResponse["Config"]["Domain"].ToString();
                }

                return _consulDomain;
            }
        }

        /// <summary>
        /// Deregisters the service from the service discovery system.
        /// </summary>
        public void Deregister()
        {
            var serviceName = _configuration.Value(ServiceConfigurationKeys.ServiceDiscoveryName);
            var res = _consulClient.Agent.ServiceDeregister(serviceName).Result;

            if (res.StatusCode == HttpStatusCode.OK)
            {
                _diagnostics.Log(
                    LevelToLog.Info,
                    "{0}",
                    "Successfully deregistered from Consul");
            }
            else
            {
                _diagnostics.Log(
                    LevelToLog.Error,
                    "{0}",
                    "Failed to deregister from Consul");
            }
        }

        /// <summary>
        /// Gets the environment from the service discovery system.
        /// </summary>
        /// <returns>The current environment</returns>
        public string Environment
        {
            get
            {
                if (_consulEnvironment == null)
                {
                    var configRequestResponse = _consulClient.Agent.Self().Result.Response;
                    _consulEnvironment = configRequestResponse["Config"]["Datacenter"].ToString();
                }

                return _consulEnvironment;
            }
        }

        private ConsulClient GetConsulClient()
        {
            var config = new ConsulClientConfiguration();
            config.Address = new Uri(ConsulAddress);
            var client = new ConsulClient(config);

            return client;
        }

        /// <summary>
        /// Gets the metrics server uri, with port
        /// </summary>
        /// <returns>String of metrics uri and port</returns>
        public string MetricsServer
        {
            get
            {
                if (_metricsServer == null)
                {
                    // We assume that potential multiple instances of the metrics server are hidden behind a load balancer
                    // Therefore we expect zero or one response here
                    var metricsServiceName = _configuration.Value(ServiceConfigurationKeys.MetricsServiceName);
                    var metricsTag = _configuration.Value(ServiceConfigurationKeys.MetricsServiceTag);
                    var metricsRequestResponse = _consulClient.Catalog.Node(metricsServiceName).Result.Response;
                    var metricsServices = metricsRequestResponse.Services.Where(s => s.Value.Service.Equals(metricsTag));

                    if (metricsServices.Any())
                    {
                        var metricsService = metricsServices.First();
                        _metricsServer = string.Format(
                            CultureInfo.InvariantCulture,
                            "http://{0}.{1}.service.{2}:{3}",
                            metricsTag,
                            metricsServiceName,
                            ConsulDomain,
                            metricsService.Value.Port == 0
                                ? 8086
                                : metricsService.Value.Port);
                    }
                }

                return _metricsServer;
            }
        }

        /// <summary>
        /// Registers the service with the service discovery system.
        /// </summary>
        /// <param name="localIP">The IP address to register.</param>
        public void Register(IPAddress localIP)
        {
            if (localIP == null)
            {
                _diagnostics.Log(
                    LevelToLog.Error,
                    "{0}",
                    "Failed to register to Consul because the local ip could not be found");
                throw new ServiceDiscoveryException("IP address was not specified");
            }

            var ipAddress = localIP.ToString();
            var serviceName = _configuration.Value(ServiceConfigurationKeys.ServiceDiscoveryName);
            var servicePort = _configuration.Value(ServiceConfigurationKeys.ServicePort);
            var healthIntervalInSeconds = _configuration.Value(ServiceConfigurationKeys.ServiceDiscoveryHealthCheckIntervalInSeconds);
            var tags = _configuration.Value(ServiceConfigurationKeys.ServiceDiscoveryTags)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToArray();

            var registration = new AgentServiceRegistration()
            {
                Address = ipAddress,

                Checks = CreateChecks(servicePort, healthIntervalInSeconds),

                EnableTagOverride = false,
                ID = GetServiceId(serviceName),
                Name = serviceName,
                Port = servicePort,

                Tags = tags,
            };

            var res = _consulClient.Agent.ServiceRegister(registration).Result;

            if (res.StatusCode == HttpStatusCode.OK)
            {
                _diagnostics.Log(
                    LevelToLog.Info,
                    "{0}",
                    "Successfully Registered on Consul");
            }
            else
            {
                _diagnostics.Log(
                    LevelToLog.Error,
                    "{0}",
                    "Failed to register to Consul");
                throw new ConsulConfigurationException();
            }
        }

        /// <summary>
        /// Updates the tags for the current service with the service discovery system.
        /// </summary>
        /// <param name="additionalTags">The collection of tags additional to the ones specified in the configuration.</param>
        public void UpdateTags(params string[] additionalTags)
        {
            var serviceName = _configuration.Value(ServiceConfigurationKeys.ServiceDiscoveryName);
            var serviceId = GetServiceId(serviceName);
            var tags = _configuration.Value(ServiceConfigurationKeys.ServiceDiscoveryTags)
                .Concat(additionalTags)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToArray();

            var services = _consulClient.Agent.Services().Result;
            if (!services.Response.ContainsKey(serviceId))
            {
                return;
            }

            var service = services.Response[serviceId];
            var registration = new AgentServiceRegistration()
            {
                Address = service.Address,

                EnableTagOverride = false,
                ID = service.ID,
                Name = serviceName,
                Port = service.Port,
                Tags = tags,
            };

            var res = _consulClient.Agent.ServiceRegister(registration).Result;

            if (res.StatusCode == HttpStatusCode.OK)
            {
                _diagnostics.Log(
                    LevelToLog.Info,
                    "{0}",
                    "Successfully updated service with Consul");
            }
            else
            {
                _diagnostics.Log(
                    LevelToLog.Error,
                    "{0}",
                    "Failed to update service with Consul");
                throw new ConsulConfigurationException();
            }
        }
    }
}
