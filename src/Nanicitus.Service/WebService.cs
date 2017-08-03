//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Owin.Hosting;
using Nanicitus.Core;
using Nanicitus.Service.Properties;
using Nuclei.Configuration;
using Nuclei.Diagnostics;
using Nuclei.Diagnostics.Logging;

namespace Nanicitus.Service
{
    internal sealed class WebService
    {
        private static IPAddress GetLocalIPAddress()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    .GetIPProperties()
                    .UnicastAddresses
                    .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Address;
            }
            catch (NullReferenceException ex)
            {
                throw new ServiceException("Unable to find valid IP address", ex);
            }
        }

        private readonly IConfiguration _configuration;
        private readonly SystemDiagnostics _diagnostics;
        private readonly IServiceDiscovery _serviceDiscovery;
        private readonly IServiceInfo _serviceInfo;
        private readonly ISymbolProcessor _symbolProcessor;

        private IDisposable _app;
        private bool _isRegistered = false;

        [SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "Called by the Autofac DI framework")]
        public WebService(
            IConfiguration configuration,
            IServiceDiscovery serviceDiscovery,
            IServiceInfo serviceInfo,
            ISymbolProcessor processor,
            SystemDiagnostics diagnostics)
        {
            _configuration = configuration ?? throw new ArgumentNullException("configuration");
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
            _serviceDiscovery = serviceDiscovery ?? throw new ArgumentNullException("serviceDiscovery");
            _serviceInfo = serviceInfo ?? throw new ArgumentNullException("serviceInfo");
            _symbolProcessor = processor ?? throw new ArgumentNullException("processor");
        }

        public void Start()
        {
            _serviceInfo.IsActive = false;
            _serviceInfo.IsEnabled = true;
            _serviceInfo.IsStandby = true;

            var tempPath = _configuration.Value(ServiceConfigurationKeys.TempPath);
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            var uploadPath = _configuration.Value(CoreConfigurationKeys.UploadPath);
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var servicePort = _configuration.Value(ServiceConfigurationKeys.ServicePort);

            // The wildcard in the base address allows the service to run under any host name.
            // E.g. localhost or MyServer
            string baseAddress = "http://*:" + servicePort;
            _diagnostics.Log(LevelToLog.Info, "Web Server is running at " + baseAddress);
            _app = WebApp.Start<Startup>(url: baseAddress);

            if (_configuration.Value(ServiceConfigurationKeys.ShouldRegisterForDiscovery))
            {
                _serviceDiscovery.Register(GetLocalIPAddress());
                _isRegistered = true;
            }

            // Check the services dependencies before allowing the app to start. Fail fast.
            var address = string.Format(
                CultureInfo.InvariantCulture,
                "http://localhost:{0}/api/v1/service/dependencies",
                servicePort);
            var client = new HttpClient();
            var response = client.GetAsync(address).Result;

            if (!response.IsSuccessStatusCode)
            {
                Stop();
                throw new ServiceException("One or more dependencies were unavailable. \r\n" + response.Content.ReadAsStringAsync().Result);
            }

            _diagnostics.Log(
                LevelToLog.Info,
                Resources.Log_Messages_ServiceEntryPoint_StartingService);

            _symbolProcessor.Start();
        }

        public void Stop()
        {
            if (_isRegistered)
            {
                _serviceDiscovery.Deregister();
            }

            if (_symbolProcessor != null)
            {
                var clearingTask = _symbolProcessor.Stop(true);
                clearingTask.Wait();
            }

            _app.Dispose();
        }
    }
}
