//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Nuclei;
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
        [SuppressMessage(
            "Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "ConfigurationKey objects are immutable.")]
        public static readonly ConfigurationKeyBase DebuggingToolsDirectory
            = new ConfigurationKey<string>("DebuggingToolsDirectory");

        private static string DefaultSymbolServerToolsDirectory()
        {
            return Environment.Is64BitProcess
                ? @"C:\Program Files (x86)\Windows Kits\8.0\Debuggers\x64"
                : @"C:\Program Files (x86)\Windows Kits\8.0\Debuggers\x86";
        }

        /// <summary>
        /// The configuration key that is used to retrieve UNC path for the source index directory.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "ConfigurationKey objects are immutable.")]
        public static readonly ConfigurationKeyBase SourceIndexUncPath
            = new ConfigurationKey<string>("SourceIndexUncPath");

        /// <summary>
        /// The configuration key that is used to retrieve UNC path for the symbols directory.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "ConfigurationKey objects are immutable.")]
        public static readonly ConfigurationKeyBase SymbolsIndexUncPath
            = new ConfigurationKey<string>("SymbolsIndexUncPath");

        /// <summary>
        /// The configuration key that is used to retrieve path for the directory in
        /// which the processed symbol packages will be placed.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "ConfigurationKey objects are immutable.")]
        public static readonly ConfigurationKeyBase ProcessedPackagesPath
            = new ConfigurationKey<string>("ProcessedPackagesPath");

        /// <summary>
        /// The configuration key that is used to retrieve path for the directory in
        /// which the uploads will be placed.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "ConfigurationKey objects are immutable.")]
        public static readonly ConfigurationKeyBase UploadPath
            = new ConfigurationKey<string>("UploadPath");

        /// <summary>
        /// Returns a collection containing all the configuration keys for the application.
        /// </summary>
        /// <returns>A collection containing all the configuration keys for the application.</returns>
        public static IDictionary<ConfigurationKeyBase, object> ToDefault()
        {
            return new Dictionary<ConfigurationKeyBase, object>
                {
                    { DebuggingToolsDirectory, DefaultSymbolServerToolsDirectory() },
                    { SourceIndexUncPath, Path.Combine(Assembly.GetExecutingAssembly().LocalDirectoryPath(), "sources") },
                    { SymbolsIndexUncPath, Path.Combine(Assembly.GetExecutingAssembly().LocalDirectoryPath(), "symbols") },
                    { ProcessedPackagesPath, Path.Combine(Assembly.GetExecutingAssembly().LocalDirectoryPath(), "processed") },
                    { UploadPath, Path.Combine(Assembly.GetExecutingAssembly().LocalDirectoryPath(), "uploads") },
                };
        }
    }
}
