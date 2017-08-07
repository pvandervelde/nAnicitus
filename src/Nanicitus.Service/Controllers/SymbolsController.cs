//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
        /// Verifies that the symbol packages which are being pushed are valid.
        /// </summary>
        /// <returns>
        /// A HTTP response message indicating whether or not the symbol packages are
        /// valid or not.
        /// </returns>
        [HttpPut]
        public HttpResponseMessage IsValid()
        {
            LogRequestDetails(Request);
            StoreRequestMetrics(Request);

            if (!ServiceInfo.IsActive)
            {
                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable);
            }

            var tempPath = ServiceConfiguration.Value(CoreConfigurationKeys.TempPath);
            var uploadPath = Path.Combine(tempPath, Guid.NewGuid().ToString());

            var isValid = false;
            var reports = new List<IndexReport>();
            try
            {
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var symbolPackages = new List<string>();
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

                    isValid = true;
                    foreach (var fileData in streamProvider.FileData)
                    {
                        // Turn the file into a nuget package and then move it to the
                        // storage location
                        var packageIdentity = PackageUtilities.GetPackageIdentity(fileData.LocalFileName);
                        if (packageIdentity != null)
                        {
                            var fileName = string.Format(
                                CultureInfo.InvariantCulture,
                                "{0}.{1}.symbols.nupkg",
                                packageIdentity.Id,
                                packageIdentity.Version);

                            var symbolPackage = Path.Combine(uploadPath, fileName);
                            File.Move(fileData.LocalFileName, symbolPackage);
                            symbolPackages.Add(symbolPackage);
                        }
                        else
                        {
                            isValid = false;
                        }
                    }
                }

                var result = _symbolProcessor.IsValid(symbolPackages);
                isValid = isValid && result.result;
                reports.AddRange(result.messages);
            }
            finally
            {
                if (Directory.Exists(uploadPath))
                {
                    try
                    {
                        Directory.Delete(uploadPath);
                    }
                    catch (IOException)
                    {
                        // Ignore it
                    }
                }
            }

            var responseCode = isValid ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            return Request.CreateResponse(
                responseCode,
                reports.ToArray());
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

            var tempPath = ServiceConfiguration.Value(CoreConfigurationKeys.TempPath);
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

                var symbolPackages = new List<string>();
                var reports = new List<IndexReport>();
                foreach (var fileData in streamProvider.FileData)
                {
                    // Turn the file into a nuget package and then move it to the
                    // storage location
                    var packageIdentity = PackageUtilities.GetPackageIdentity(fileData.LocalFileName);
                    if (packageIdentity != null)
                    {
                        var fileName = string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}.{1}.symbols.nupkg",
                            packageIdentity.Id,
                            packageIdentity.Version);

                        var symbolPackage = Path.Combine(uploadPath, fileName);
                        File.Move(fileData.LocalFileName, symbolPackage);
                        symbolPackages.Add(symbolPackage);
                    }
                    else
                    {
                        reports.Add(
                            new IndexReport(
                                fileData.LocalFileName,
                                "Unknown",
                                IndexStatus.Failed,
                                new[]
                                {
                                    "Could not load file."
                                }));
                    }
                }

                reports.AddRange(_symbolProcessor.Index(symbolPackages));
                if (reports.Any(r => r.Status != IndexStatus.Succeeded))
                {
                    return Request.CreateResponse(
                        HttpStatusCode.Accepted);
                }
                else
                {
                    return Request.CreateResponse(
                        HttpStatusCode.NotAcceptable,
                        reports.ToArray());
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
