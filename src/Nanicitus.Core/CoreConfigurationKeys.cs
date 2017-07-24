//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Nuclei.Configuration;

namespace Nanicitus.Core
{
    /// <summary>
    /// Defines all the configuration keys.
    /// </summary>
    public static class CoreConfigurationKeys
    {
        /// <summary>
        /// The configuration key that is used to retrieve directory path where the symbol server
        /// tools are installed.
        /// </summary>
        internal static readonly ConfigurationKeyBase _debuggingToolsDirectory
            = new ConfigurationKey<string>("DebuggingToolsDirectory");

        /// <summary>
        /// The configuration key that is used to retrieve UNC path for the source index directory.
        /// </summary>
        internal static readonly ConfigurationKeyBase _sourceIndexUncPath
            = new ConfigurationKey<string>("SourceIndexUncPath");

        /// <summary>
        /// The configuration key that is used to retrieve UNC path for the symbols directory.
        /// </summary>
        internal static readonly ConfigurationKeyBase _symbolsIndexUncPath
            = new ConfigurationKey<string>("SymbolsIndexUncPath");

        /// <summary>
        /// The configuration key that is used to retrieve path for the directory in
        /// which the processed symbol packages will be placed.
        /// </summary>
        internal static readonly ConfigurationKeyBase _processedPackagesPath
            = new ConfigurationKey<string>("ProcessedPackagesPath");

        /// <summary>
        /// The configuration key that is used to retrieve path for the directory in
        /// which the uploads will be placed.
        /// </summary>
        internal static readonly ConfigurationKeyBase _uploadPath
            = new ConfigurationKey<string>("UploadPath");

        /// <summary>
        /// Returns a collection containing all the configuration keys for the application.
        /// </summary>
        /// <returns>A collection containing all the configuration keys for the application.</returns>
        public static IEnumerable<ConfigurationKeyBase> ToCollection()
        {
            return new List<ConfigurationKeyBase>
                {
                    _debuggingToolsDirectory,
                    _sourceIndexUncPath,
                    _symbolsIndexUncPath,
                    _processedPackagesPath,
                    _uploadPath
                };
        }
    }
}
