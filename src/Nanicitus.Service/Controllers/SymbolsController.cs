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
using System.Web.Http;
using Microsoft.Web.Http;
using Nanicitus.Core;
using Nanicitus.Core.Monitoring;
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
    public sealed class SymbolsController : ServiceBaseApiController
    {
        private readonly ISymbolProcessor _symbolProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolsController"/> class.
        /// </summary>
        /// <param name="symbolProcessor">The object that processes the symbol packages.</param>
        /// <param name="configuration">The object that provides the configuration values for the application.</param>
        /// <param name="serviceDiscovery">An object that handles interaction with the service discovery system.</param>
        /// <param name="serviceInfo">Info object</param>
        /// <param name="metrics">The metrics object.</param>
        /// <param name="diagnostics">The logging object</param>
        public SymbolsController(
            ISymbolProcessor symbolProcessor,
            IConfiguration configuration,
            IServiceDiscovery serviceDiscovery,
            IServiceInfo serviceInfo,
            IMetricsCollector metrics,
            SystemDiagnostics diagnostics)
            : base(configuration, serviceDiscovery, serviceInfo, metrics, diagnostics)
        {
            _symbolProcessor = symbolProcessor ?? throw new ArgumentNullException("symbolProcessor");
        }

        /// <summary>
        /// Reindexes all the symbol packages and rebuilds the symbol and source stores.
        /// </summary>
        /// <returns>The http resonse message with the return code of the operation.</returns>
        [HttpPut]
        public HttpResponseMessage RebuildIndex()
        {
            LogRequestDetails(Request);
            StoreRequestMetrics(Request);

            if (!ServiceInfo.IsActive)
            {
                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable);
            }

            _symbolProcessor.RebuildIndex();

            return Request.CreateResponse(HttpStatusCode.Accepted);
        }

        /// <summary>
        /// Stores the provided file and prepares it for symbol processing.
        /// </summary>
        /// <returns>The http response message with the return code of the operation.</returns>
        [HttpPut]
        public HttpResponseMessage Upload()
        {
            LogRequestDetails(Request);
            StoreRequestMetrics(Request);

            if (!ServiceInfo.IsActive)
            {
                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable);
            }

            var tempPath = ServiceConfiguration.Value(ServiceConfigurationKeys.TempPath);
            var uploadPath = ServiceConfiguration.Value(CoreConfigurationKeys.UploadPath);
            if (Request.Content.IsMimeMultipartContent())
            {
                var streamProvider = new ProvidedFileNameMultipartFormDataStreamProvider(tempPath);

                try
                {
                    Request.Content.ReadAsMultipartAsync(streamProvider).Wait();
                }
                catch (AggregateException e)
                {
                    Diagnostics.Log(LevelToLog.Error, e.ToString());
                }

                var receivedAllFiles = true;
                foreach (var fileData in streamProvider.FileData)
                {
                    // Turn the file into a nuget package and then move it to the
                    // storage location
                    var package = PackageUtilities.LoadSymbolPackage(
                        fileData.LocalFileName,
                        (file, e) =>
                        {
                            Diagnostics.Log(
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

                        var symbolPackage = Path.Combine(uploadPath, fileName);
                        File.Move(fileData.LocalFileName, symbolPackage);
                        _symbolProcessor.Index(symbolPackage);
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
