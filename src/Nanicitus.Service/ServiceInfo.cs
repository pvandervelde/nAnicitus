//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Reflection;
using Nuclei.Build;

namespace Nanicitus.Service
{
    /// <summary>
    /// Stores information about the current service.
    /// </summary>
    internal sealed class ServiceInfo : IServiceInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceInfo"/> class.
        /// </summary>
        public ServiceInfo()
        {
            StartTime = DateTimeOffset.UtcNow;
            ServiceId = Assembly.GetExecutingAssembly().GetName().Name;
            MachineName = Environment.MachineName;
            OSName = Environment.OSVersion.ToString();
            AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            GitSHA = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyBuildInformationAttribute>().VersionControlInformation;
            BuildTime = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyBuildTimeAttribute>().BuildTime;
        }

        /// <summary>
        /// Gets the version of the service assembly
        /// </summary>
        public Version AssemblyVersion
        {
            get;
        }

        /// <summary>
        /// Gets the time when this application was built
        /// </summary>
        public DateTimeOffset BuildTime
        {
            get;
        }

        /// <summary>
        /// Gets the GitSHA used to build this version of the application
        /// </summary>
        public string GitSHA
        {
            get;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the current instance is ready to
        /// process symbols.
        /// </summary>
        public bool IsActive
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the current instance can be activated.
        /// </summary>
        public bool IsEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the current instance is in stand-by mode.
        /// </summary>
        public bool IsStandby
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets gets the time when the last event was received
        /// </summary>
        public DateTimeOffset LastReceivedTime
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the name of the machine which is executing the application
        /// </summary>
        public string MachineName
        {
            get;
        }

        /// <summary>
        /// Gets the name of the OS which is executing the application
        /// </summary>
        public string OSName
        {
            get;
        }

        /// <summary>
        /// Gets the name of the assembly.
        /// </summary>
        public string ServiceId
        {
            get;
        }

        /// <summary>
        /// Gets the system time when the service was started
        /// </summary>
        public DateTimeOffset StartTime
        {
            get;
        }
    }
}
