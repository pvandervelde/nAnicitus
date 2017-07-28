//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using NuGet;

namespace Nanicitus.Core
{
    /// <summary>
    /// Provides utility methods for dealing with nuget symbol packages.
    /// </summary>
    public static class PackageUtilities
    {
        /// <summary>
        /// The HResult value that indicates that the file is locked by the operating system.
        /// </summary>
        private const uint FileLocked = 0x80070020;

        /// <summary>
        /// Indicates the maximum number of times that the process will wait for a locked file before
        /// giving up and moving on.
        /// </summary>
        private const int MaximumNumberOfTimesWaitingForPackageFileLock = 3;

        /// <summary>
        /// The amount of time the process sleeps when it encounters a file that is locked by the
        /// operating system.
        /// </summary>
        private const int PackageFileLockSleepTimeInMilliSeconds = 5000;

        /// <summary>
        /// The HResult value that indicates that a portion of the file is locked by the operating
        /// system.
        /// </summary>
        private const uint PortionOfFileLocked = 0x80070021;

        /// <summary>
        /// Returns a value indicating if the file is locked for reading.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <returns>
        ///     <see langword="true"/> if the file is locked for reading; otherwise <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// Original code here: http://stackoverflow.com/a/14132721/539846.
        /// </remarks>
        public static bool IsAvailableForReading(string path)
        {
            try
            {
                using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    // Do nothing. Just want to know if the file is locked.
                }

                return true;
            }
            catch (IOException e)
            {
                var errorCode = (uint)Marshal.GetHRForException(e);
                if (errorCode != FileLocked && errorCode != PortionOfFileLocked)
                {
                    throw;
                }

                return false;
            }
        }

        /// <summary>
        /// Loads NuGet package information for a given file.
        /// </summary>
        /// <param name="packageFile">The full path to the file that contains the package information.</param>
        /// <param name="onLoadFailure">The action that should be execute if the package fails to load.</param>
        /// <returns>The package information.</returns>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Do not want the application to crash if there is an error loading symbols.")]
        public static ZipPackage LoadSymbolPackage(string packageFile, Action<string, Exception> onLoadFailure)
        {
            try
            {
                int waitCount = 0;
                while ((!IsAvailableForReading(packageFile)) && (waitCount < MaximumNumberOfTimesWaitingForPackageFileLock))
                {
                    waitCount++;
                    Thread.Sleep(PackageFileLockSleepTimeInMilliSeconds);
                }

                return new ZipPackage(packageFile);
            }
            catch (Exception e)
            {
                onLoadFailure?.Invoke(packageFile, e);
                return null;
            }
        }
    }
}
