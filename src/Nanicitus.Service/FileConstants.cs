//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;

namespace Nanicitus.Service
{
    /// <summary>
    /// Defines a set of values related to files and file paths.
    /// </summary>
    [Serializable]
    internal static class FileConstants
    {
        /// <summary>
        /// Returns the path for the directory in the AppData directory which contains
        /// all the product directories for the current company.
        /// </summary>
        /// <returns>
        /// The full path for the AppData directory for the current company.
        /// </returns>
        public static string CompanyCommonPath()
        {
            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var companyDirectory = Path.Combine(appDataDir, ApplicationConstants.CompanyName);

            return companyDirectory;
        }

        /// <summary>
        /// Returns the path for the directory where the global
        /// settings for the product are written to.
        /// </summary>
        /// <returns>
        /// The full path for the directory where the global settings
        /// for the product are written to.
        /// </returns>
        public static string ProductSettingsCommonPath()
        {
            var companyDirectory = CompanyCommonPath();
            var productDirectory = Path.Combine(companyDirectory, ApplicationConstants.ApplicationName);
            var versionDirectory = Path.Combine(productDirectory, ApplicationConstants.ApplicationCompatibilityVersion.ToString(2));

            return versionDirectory;
        }

        /// <summary>
        /// Returns the path for the directory where the log files are
        /// written to.
        /// </summary>
        /// <returns>
        /// The full path for the directory where the log files are written to.
        /// </returns>
        public static string LogPath()
        {
            var versionDirectory = ProductSettingsCommonPath();
            var logDirectory = Path.Combine(versionDirectory, "logs");

            return logDirectory;
        }
    }
}
