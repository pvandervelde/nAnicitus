//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Nanicitus.Service
{
    /// <summary>
    /// Defines the interface for objects that store information about the current application.
    /// </summary>
    public interface IServiceInfo
    {
        /// <summary>
        /// Gets the version of the service assembly
        /// </summary>
        Version AssemblyVersion { get; }

        /// <summary>
        /// Gets the time when this application was built
        /// </summary>
        DateTimeOffset BuildTime { get; }

        /// <summary>
        /// Gets the GitSHA used to build this version of the application
        /// </summary>
        string GitSHA { get; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the current instance is ready to
        /// process symbols.
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the current instance can be activated.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the current instance is in stand-by mode.
        /// </summary>
        bool IsStandby { get; set; }

        /// <summary>
        /// Gets or sets gets the time when the last event was received
        /// </summary>
        DateTimeOffset LastReceivedTime { get; set; }

        /// <summary>
        /// Gets the name of the machine which is executing the application
        /// </summary>
        string MachineName { get; }

        /// <summary>
        /// Gets the name of the OS which is executing the application
        /// </summary>
        string OSName { get; }

        /// <summary>
        /// Gets the ServiceId of the assembly
        /// </summary>
        string ServiceId { get; }

        /// <summary>
        /// Gets the system time when the service was started
        /// </summary>
        DateTimeOffset StartTime { get; }
    }
}
