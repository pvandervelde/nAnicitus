//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Nanicitus.Core
{
    /// <summary>
    /// Defines the interface for objects that queue symbol packages.
    /// </summary>
    internal interface IQueueSymbolPackages
    {
        /// <summary>
        /// Adds the given package to the queue for processing.
        /// </summary>
        /// <param name="fileName">The full path of the package.</param>
        void Enqueue(string fileName);

        /// <summary>
        /// Removes a package from the queue for processing.
        /// </summary>
        /// <returns>A reference to the package.</returns>
        string Dequeue();

        /// <summary>
        /// An event raised when a new package is enqueued.
        /// </summary>
        event EventHandler OnEnqueue;

        /// <summary>
        /// Gets a value indicating whether the queue is currently empty.
        /// </summary>
        bool IsEmpty
        {
            get;
        }
    }
}
