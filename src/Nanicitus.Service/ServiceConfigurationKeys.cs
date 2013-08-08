//-----------------------------------------------------------------------
// <copyright company="NAnicitus">
//     Copyright 2013 NAnicitus. Licensed under the Apache License, Version 2.0.
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
        public static readonly ConfigurationKey LogPath
            = new ConfigurationKey("LogPath", typeof(string));

        /// <summary>
        /// Returns a collection containing all the configuration keys for the application.
        /// </summary>
        /// <returns>A collection containing all the configuration keys for the application.</returns>
        public static IEnumerable<ConfigurationKey> ToCollection()
        {
            return new List<ConfigurationKey>
                {
                    LogPath,
                };
        }
    }
}
