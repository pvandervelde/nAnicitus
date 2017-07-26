//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
        private readonly IIndexSymbols _indexer;
        private readonly IServiceDiscovery _serviceDiscovery;
        private readonly IServiceInfo _serviceInfo;
        private readonly IUploadPackages _uploader;

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
            IIndexSymbols indexer,
            IUploadPackages uploader,
            SystemDiagnostics diagnostics)
        {
            _configuration = configuration ?? throw new ArgumentNullException("configuration");
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
            _indexer = indexer ?? throw new ArgumentNullException("indexer");
            _serviceDiscovery = serviceDiscovery ?? throw new ArgumentNullException("serviceDiscovery");
            _serviceInfo = serviceInfo ?? throw new ArgumentNullException("serviceInfo");
            _uploader = uploader ?? throw new ArgumentNullException("uploader");
        }

        public void Start()
        {
            _serviceInfo.IsActive = false;
            _serviceInfo.IsEnabled = true;
            _serviceInfo.IsStandby = true;

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

            _indexer.Start();
            _uploader.EnableUpload();
        }

        public void Stop()
        {
            if (_isRegistered)
            {
                _serviceDiscovery.Deregister();
            }

            if (_uploader != null)
            {
                _uploader.DisableUpload();
            }

            if (_indexer != null)
            {
                var clearingTask = _indexer.Stop(true);
                clearingTask.Wait();
            }

            _app.Dispose();
        }
    }
}
