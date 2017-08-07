//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using Nanicitus.Core.Properties;

namespace Nanicitus.Core
{
    /// <summary>
    /// Provides a thread-safe queue of package locations.
    /// </summary>
    internal sealed class PackageQueue : IQueueSymbolPackages
    {
        /// <summary>
        /// Stores the full path to the queued packages.
        /// </summary>
        private readonly ConcurrentQueue<ValueTuple<string, Action<IndexReport>>> _queue
            = new ConcurrentQueue<ValueTuple<string, Action<IndexReport>>>();

        /// <summary>
        /// Adds the given package to the queue for processing.
        /// </summary>
        /// <param name="fileName">The full path of the package.</param>
        /// <param name="reportSink">The function to which the final indexing report should be provided.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="fileName"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when <paramref name="fileName"/> is an empty string.
        /// </exception>
        public void Enqueue(string fileName, Action<IndexReport> reportSink = null)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(
                    Resources.Exceptions_Messages_ParameterShouldNotBeAnEmptyString,
                    "fileName");
            }

            _queue.Enqueue(ValueTuple.Create(fileName, reportSink));
            RaiseOnEnqueue();
        }

#pragma warning disable SA1008 // Opening parenthesis must be spaced correctly
        /// <summary>
        /// Removes a package from the queue for processing.
        /// </summary>
        /// <returns>
        ///     A tuple containing the references to the package and the function
        ///     which will process the indexing report.
        /// </returns>
        public (string path, Action<IndexReport> reportSink) Dequeue()
#pragma warning restore SA1008 // Opening parenthesis must be spaced correctly
        {
            _queue.TryDequeue(out var pair);

            return pair;
        }

        public event EventHandler OnEnqueue;

        private void RaiseOnEnqueue()
        {
            var local = OnEnqueue;
            if (local != null)
            {
                local(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the queue is currently empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return _queue.IsEmpty;
            }
        }
    }
}
