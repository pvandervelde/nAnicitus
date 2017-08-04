//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
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
        /// <param name="path">The full path to the symbol package.</param>
        void Index(string path);

        /// <summary>
        /// Returns a value indicating whether or not the symbol package at the given
        /// location is a valid symbol package.
        /// </summary>
        /// <param name="path">The full path to the symbol package.</param>
        /// <returns>
        ///     <see langword="true" /> if the symbol package at the given location is valid,
        ///     otherwise <see langword="false" />.
        /// </returns>
        bool IsValid(string path);

        /// <summary>
        /// Re-Indexes all the previously processed symbols.
        /// </summary>
        void RebuildIndex();

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
