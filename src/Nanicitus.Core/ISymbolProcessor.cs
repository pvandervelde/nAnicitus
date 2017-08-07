﻿//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Nanicitus.Core
{
    /// <summary>
    /// Defines the interface for objects that control the symbol processing.
    /// </summary>
    public interface ISymbolProcessor : IDisposable
    {
        /// <summary>
        /// Indexes the symbols in the package at the given path.
        /// </summary>
        /// <param name="paths">A collection containing the full paths to the symbol packages that should be indexed.</param>
        /// <returns>The status reports of the indexing process.</returns>
        IEnumerable<IndexReport> Index(IEnumerable<string> paths);

#pragma warning disable SA1008 // Opening parenthesis must be spaced correctly
        /// <summary>
        /// Returns a value indicating whether or not the symbol package at the given
        /// location is a valid symbol package.
        /// </summary>
        /// <param name="paths">A collection containing the full paths to the symbol packages.</param>
        /// <returns>
        ///     A tuple containing the overall result of the operation and the status reports
        ///     for the indexing process.
        /// </returns>
        (bool result, IEnumerable<IndexReport> messages) IsValid(IEnumerable<string> paths);
#pragma warning restore SA1008 // Opening parenthesis must be spaced correctly

        /// <summary>
        /// Re-Indexes all the previously processed symbols.
        /// </summary>
        /// <returns>The status reports of the indexing process.</returns>
        IEnumerable<IndexReport> RebuildIndex();

        /// <summary>
        /// Starts the symbol indexing process.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the symbol indexing process.
        /// </summary>
        /// <param name="clearCurrentQueue">
        /// Indicates if the elements currently in the queue need to be processed before stopping or not.
        /// </param>
        /// <returns>A task that completes when the indexer has stopped.</returns>
        [SuppressMessage(
            "Microsoft.Naming",
            "CA1716:IdentifiersShouldNotMatchKeywords",
            MessageId = "Stop",
            Justification = "Stop is a sensible term to use.")]
        Task Stop(bool clearCurrentQueue);
    }
}