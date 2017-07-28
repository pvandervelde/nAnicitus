//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Net;

namespace Nanicitus.Service
{
    /// <summary>
    /// Defines the interface for interacting with a service discovery system.
    /// </summary>
    public interface IServiceDiscovery
    {
        /// <summary>
        /// Deregisters the service from the service discovery system.
        /// </summary>
        void Deregister();

        /// <summary>
        /// Gets the environment from the service discovery system.
        /// </summary>
        /// <returns>The current environment</returns>
        string Environment { get; }

        /// <summary>
        /// Gets the metrics server uri, with port
        /// </summary>
        /// <returns>String of metrics uri and port</returns>
        string MetricsServer { get; }

        /// <summary>
        /// Registers the service with the service discovery system.
        /// </summary>
        /// <param name="localIP">The IP address to register.</param>
        void Register(IPAddress localIP);

        /// <summary>
        /// Updates the tags for the current service with the service discovery system.
        /// </summary>
        /// <param name="additionalTags">The collection of tags additional to the ones specified in the configuration.</param>
        void UpdateTags(params string[] additionalTags);
    }
}
