//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace Nanicitus.Core
{
    /// <summary>
    /// Defines the methods for describing the result of an symbol indexing action.
    /// </summary>
    public sealed class IndexReport
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexReport"/> class.
        /// </summary>
        /// <param name="packageId">The ID of the package for which the indexing is described in the report.</param>
        /// <param name="packageVersion">The version of the package for which the indexing is described in the report.</param>
        /// <param name="status">The status of the indexing process.</param>
        /// <param name="messages">The indexing messages.</param>
        public IndexReport(
            string packageId,
            string packageVersion,
            IndexStatus status,
            string[] messages)
        {
            PackageId = packageId;
            PackageVersion = packageVersion;
            Status = status;
            Messages = messages;
        }

        /// <summary>
        /// Gets the messages that describe the result of the indexing process for the
        /// given package.
        /// </summary>
        public IEnumerable<string> Messages { get; }

        /// <summary>
        /// Gets the ID of the package which is decribed in the report.
        /// </summary>
        public string PackageId { get; }

        /// <summary>
        /// Gets the version of the package which is described in the report.
        /// </summary>
        public string PackageVersion { get; }

        /// <summary>
        /// Gets the status of the indexing process for the given package.
        /// </summary>
        public IndexStatus Status { get; }
    }
}
