//-----------------------------------------------------------------------
// <copyright company="NAnicitus">
//     Copyright 2013 NAnicitus. Licensed under the Apache License, Version 2.0.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Nuclei.Configuration;

namespace Nanicitus.Core
{
    /// <summary>
    /// Defines all the configuration keys.
    /// </summary>
    internal static class ConfigurationKeys
    {
        /// <summary>
        /// The configuration key that is used to retrieve directory path where the symbol server
        /// tools are installed.
        /// </summary>
        public static readonly ConfigurationKey DebuggingToolsDirectory
            = new ConfigurationKey("DebuggingToolsDirectory", typeof(string));

        /// <summary>
        /// The configuration key that is used to retrieve UNC path for the source index directory.
        /// </summary>
        public static readonly ConfigurationKey SourceIndexUncPath
            = new ConfigurationKey("SourceIndexUncPath", typeof(string));

        /// <summary>
        /// The configuration key that is used to retrieve UNC path for the symbols directory.
        /// </summary>
        public static readonly ConfigurationKey SymbolsIndexUncPath
            = new ConfigurationKey("SymbolsIndexUncPath", typeof(string));

        /// <summary>
        /// The configuration key that is used to retrieve path for the directory in 
        /// which the uploads will be placed.
        /// </summary>
        public static readonly ConfigurationKey UploadPath
            = new ConfigurationKey("UploadPath", typeof(string));

        /// <summary>
        /// Returns a collection containing all the configuration keys for the application.
        /// </summary>
        /// <returns>A collection containing all the configuration keys for the application.</returns>
        public static IEnumerable<ConfigurationKey> ToCollection()
        {
            return new List<ConfigurationKey>
                {
                    DebuggingToolsDirectory,
                    SourceIndexUncPath,
                    SymbolsIndexUncPath,
                    UploadPath
                };
        }
    }
}
