//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;

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
        private readonly ConcurrentQueue<string> _queue
            = new ConcurrentQueue<string>();

        /// <summary>
        /// Adds the given package to the queue for processing.
        /// </summary>
        /// <param name="fileName">The full path of the package.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="fileName"/> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when <paramref name="fileName"/> is an empty string.
        /// </exception>
        public void Enqueue(string fileName)
        {
            {
                Lokad.Enforce.Argument(() => fileName);
                Lokad.Enforce.Argument(() => fileName, Lokad.Rules.StringIs.NotEmpty);
            }

            _queue.Enqueue(fileName);
            RaiseOnEnqueue();
        }

        /// <summary>
        /// Removes a package from the queue for processing.
        /// </summary>
        /// <returns>A reference to the package.</returns>
        public string Dequeue()
        {
            var path = string.Empty;
            _queue.TryDequeue(out path);

            return path;
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
