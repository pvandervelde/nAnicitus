//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using NuGet.Packaging;
using NuGet.Packaging.Core;

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
        public const int MaximumNumberOfTimesWaitingForPackageFileLock = 3;

        /// <summary>
        /// The amount of time the process sleeps when it encounters a file that is locked by the
        /// operating system.
        /// </summary>
        public const int PackageFileLockSleepTimeInMilliSeconds = 5000;

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
        /// Returns the identity of a symbol package.
        /// </summary>
        /// <param name="packageFile">The full path to the file that contains the symbol package information.</param>
        /// <returns>The identity of the package, or <see langword="null" /> if the file could not be loaded.</returns>
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Do not want the application to crash if there is an error loading symbols.")]
        public static PackageIdentity GetPackageIdentity(string packageFile)
        {
            try
            {
                int waitCount = 0;
                while ((!IsAvailableForReading(packageFile)) && (waitCount < MaximumNumberOfTimesWaitingForPackageFileLock))
                {
                    waitCount++;
                    Thread.Sleep(PackageFileLockSleepTimeInMilliSeconds);
                }

                using (var reader = new PackageArchiveReader(new ZipArchive(new FileStream(packageFile, FileMode.Open, FileAccess.Read))))
                {
                    return reader.GetIdentity();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
