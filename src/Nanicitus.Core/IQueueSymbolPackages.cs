//-----------------------------------------------------------------------
// <copyright company="NAnicitus">
//     Copyright 2013 NAnicitus. Licensed under the Apache License, Version 2.0.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Nanicitus.Core
{
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
