//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Nuclei.Configuration;

namespace Nanicitus.Service
{
    internal static class ServiceConfigurationKeys
    {
        /// <summary>
        /// The configuration key that is used to retrieve path for the directory in
        /// which the log files will be placed.
        /// </summary>
        public static readonly ConfigurationKeyBase LogPath
            = new ConfigurationKey<string>("LogPath");

        /// <summary>
        /// Returns a collection containing all the configuration keys for the application.
        /// </summary>
        /// <returns>A collection containing all the configuration keys for the application.</returns>
        public static IEnumerable<ConfigurationKeyBase> ToCollection()
        {
            return new List<ConfigurationKeyBase>
                {
                    LogPath,
                };
        }
    }
}
