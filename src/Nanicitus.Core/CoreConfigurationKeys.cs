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
using System.Linq;
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
        public static readonly ConfigurationKey<string> DebuggingToolsDirectory
            = new ConfigurationKey<string>("DebuggingToolsDirectory");

        private static string DefaultSymbolServerToolsDirectory()
        {
            var possibleLocations = new[]
            {
                @"C:\Program Files (x86)\Windows Kits\10\Debuggers",
                @"C:\Program Files (x86)\Windows Kits\8.1\Debuggers",
                @"C:\Program Files (x86)\Windows Kits\8.0\Debuggers",
                @"C:\Program Files (x86)\Windows Kits\7.1\Debuggers",
                @"C:\Program Files (x86)\Windows Kits\7.0\Debuggers",
            };

            // Find the suitable windows kit
            var path = possibleLocations
                .Where(p => Directory.Exists(p))
                .FirstOrDefault();

            var bitness = Environment.Is64BitOperatingSystem
                ? "x64"
                : "x86";

            if (!string.IsNullOrWhiteSpace(path))
            {
                return Path.Combine(path, bitness);
            }
            else
            {
                return Assembly.GetExecutingAssembly().LocalDirectoryPath();
            }
        }

        /// <summary>
        /// The configuration key that is used to retrieve path for the directory in
        /// which the processed symbol packages will be placed.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "ConfigurationKey objects are immutable.")]
        public static readonly ConfigurationKey<string> ProcessedPackagesPath
            = new ConfigurationKey<string>("ProcessedPackagesPath");

        /// <summary>
        /// The configuration key that is used to retrieve UNC path for the source index directory.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "ConfigurationKey objects are immutable.")]
        public static readonly ConfigurationKey<string> ProcessedSourcePath
            = new ConfigurationKey<string>("ProcessedSourcePath");

        /// <summary>
        /// The configuration key that is used to retrieve UNC path for the symbols directory.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "ConfigurationKey objects are immutable.")]
        public static readonly ConfigurationKey<string> ProcessedSymbolsPath
            = new ConfigurationKey<string>("ProcessedSymbolsPath");

        /// <summary>
        /// The configuration key that is used to retrieve the URL of the source server
        /// as it will be used by the developers.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "ConfigurationKey objects are immutable.")]
        public static readonly ConfigurationKey<string> SourceServerUrl
            = new ConfigurationKey<string>("SourceServerUrl");

        /// <summary>
        /// The configuration key that is used to retrieve the URL of the symbol server
        /// as it will be used by the developers.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "ConfigurationKey objects are immutable.")]
        public static readonly ConfigurationKey<string> SymbolServerUrl
            = new ConfigurationKey<string>("SymbolServerUrl");

        /// <summary>
        /// The configuration key that is used to retrieve path for the directory in
        /// which the uploads will be placed.
        /// </summary>
        [SuppressMessage(
            "Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes",
            Justification = "ConfigurationKey objects are immutable.")]
        public static readonly ConfigurationKey<string> UploadPath
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
                    { ProcessedPackagesPath, Path.Combine(Assembly.GetExecutingAssembly().LocalDirectoryPath(), "processed") },
                    { ProcessedSourcePath, Path.Combine(Assembly.GetExecutingAssembly().LocalDirectoryPath(), "sources") },
                    { ProcessedSymbolsPath, Path.Combine(Assembly.GetExecutingAssembly().LocalDirectoryPath(), "symbols") },
                    { SourceServerUrl, "http://example.com/sources" },
                    { SymbolServerUrl, "http://example.com/symbols" },
                    { UploadPath, Path.Combine(Assembly.GetExecutingAssembly().LocalDirectoryPath(), "uploads") },
                };
        }
    }
}
