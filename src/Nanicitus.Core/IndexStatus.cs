//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

namespace Nanicitus.Core
{
    /// <summary>
    /// Defines the diffent status codes for the indexing process.
    /// </summary>
    public enum IndexStatus
    {
        /// <summary>
        /// There is no status code.
        /// </summary>
        None,

        /// <summary>
        /// The indexing processed failed.
        /// </summary>
        Failed,

        /// <summary>
        /// The indexing process succeeded.
        /// </summary>
        Succeeded,
    }
}
