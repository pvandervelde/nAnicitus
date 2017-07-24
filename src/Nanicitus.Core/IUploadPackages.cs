//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

namespace Nanicitus.Core
{
    /// <summary>
    /// Defines the interface for objects that provide an ability to accept uploaded symbol packages.
    /// </summary>
    public interface IUploadPackages
    {
        /// <summary>
        /// Enables the uploading of packages.
        /// </summary>
        void EnableUpload();

        /// <summary>
        /// Disables the uploading of packages.
        /// </summary>
        void DisableUpload();
    }
}
