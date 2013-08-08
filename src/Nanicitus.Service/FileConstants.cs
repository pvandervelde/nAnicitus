//-----------------------------------------------------------------------
// <copyright company="NAnicitus">
//     Copyright 2013 NAnicitus. Licensed under the Apache License, Version 2.0.
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
    internal sealed class FileConstants
    {
        /// <summary>
        /// The object that stores constant values for the application.
        /// </summary>
        private readonly ApplicationConstants m_Constants;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileConstants"/> class.
        /// </summary>
        /// <param name="constants">The object that stores constant values for the application.</param>
        public FileConstants(ApplicationConstants constants)
        {
            {
                Lokad.Enforce.Argument(() => constants);
            }

            m_Constants = constants;
        }

        /// <summary>
        /// Returns the path for the directory in the AppData directory which contains
        /// all the product directories for the current company.
        /// </summary>
        /// <returns>
        /// The full path for the AppData directory for the current company.
        /// </returns>
        public string CompanyCommonPath()
        {
            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var companyDirectory = Path.Combine(appDataDir, m_Constants.CompanyName);

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
        public string ProductSettingsCommonPath()
        {
            var companyDirectory = CompanyCommonPath();
            var productDirectory = Path.Combine(companyDirectory, m_Constants.ApplicationName);
            var versionDirectory = Path.Combine(productDirectory, m_Constants.ApplicationCompatibilityVersion.ToString(2));

            return versionDirectory;
        }

        /// <summary>
        /// Returns the path for the directory where the log files are
        /// written to.
        /// </summary>
        /// <returns>
        /// The full path for the directory where the log files are written to.
        /// </returns>
        public string LogPath()
        {
            var versionDirectory = ProductSettingsCommonPath();
            var logDirectory = Path.Combine(versionDirectory, "logs");

            return logDirectory;
        }
    }
}
