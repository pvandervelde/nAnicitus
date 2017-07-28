//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.Web.Http;
using Nanicitus.Core;
using Nuclei.Configuration;
using Nuclei.Diagnostics;
using Nuclei.Diagnostics.Logging;

namespace Nanicitus.Service.Controllers
{
    /// <summary>
    /// The api controller for the health endpoints
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/symbols/{action}")]
    public sealed class SymbolsController : ApiController
    {
        private readonly IConfiguration _configuration;
        private readonly SystemDiagnostics _diagnostics;
        private readonly IServiceInfo _serviceInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolsController"/> class.
        /// </summary>
        /// <param name="configuration">The object that provides the configuration values for the application.</param>
        /// <param name="serviceInfo">Info object</param>
        /// <param name="diagnostics">The logging object</param>
        public SymbolsController(
            IConfiguration configuration,
            IServiceInfo serviceInfo,
            SystemDiagnostics diagnostics)
        {
            _configuration = configuration ?? throw new ArgumentNullException("configuration");
            _serviceInfo = serviceInfo ?? throw new ArgumentNullException("serviceInfo");
            _diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
        }

        /// <summary>
        /// Stores the provided file and prepares it for symbol processing.
        /// </summary>
        /// <returns>A task that returns the http response message with the return code of the operation.</returns>
        [HttpPut]
        public HttpResponseMessage Upload()
        {
            LogRequestDetails(Request);

            if (!_serviceInfo.IsActive)
            {
                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable);
            }

            var tempPath = _configuration.Value(ServiceConfigurationKeys.TempPath);
            var uploadPath = _configuration.Value(CoreConfigurationKeys.UploadPath);
            if (Request.Content.IsMimeMultipartContent())
            {
                var streamProvider = new ProvidedFileNameMultipartFormDataStreamProvider(tempPath);

                try
                {
                    Request.Content.ReadAsMultipartAsync(streamProvider).Wait();
                }
                catch (AggregateException e)
                {
                    _diagnostics.Log(LevelToLog.Error, e.ToString());
                }

                var receivedAllFiles = true;
                foreach (MultipartFileData fileData in streamProvider.FileData)
                {
                    // Turn the file into a nuget package and then move it to the
                    // storage location
                    var package = PackageUtilities.LoadSymbolPackage(
                        fileData.LocalFileName,
                        (file, e) =>
                        {
                            _diagnostics.Log(
                                LevelToLog.Error,
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "Failed to load. Error was: {0}",
                                    e));
                        });
                    if (package != null)
                    {
                        var fileName = string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}.{1}.symbols.nupkg",
                            package.Id,
                            package.Version);

                        File.Move(fileData.LocalFileName, Path.Combine(uploadPath, fileName));
                    }
                    else
                    {
                        receivedAllFiles = false;
                    }
                }

                if (receivedAllFiles)
                {
                    return Request.CreateResponse(
                        HttpStatusCode.Accepted);
                }
                else
                {
                    return Request.CreateResponse(
                        HttpStatusCode.NotAcceptable,
                        "Not all files were valid NuGet symbol packages.");
                }
            }

            return Request.CreateResponse(
                HttpStatusCode.NotAcceptable,
                "Expected the request content to either be a stream.");
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

        private sealed class ProvidedFileNameMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
        {
            public ProvidedFileNameMultipartFormDataStreamProvider(string uploadPath)
                : base(uploadPath)
            {
            }

            public override string GetLocalFileName(HttpContentHeaders headers)
            {
                return Guid.NewGuid().ToString() + ".nupkg";
            }
        }
    }
}
