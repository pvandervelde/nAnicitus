//-----------------------------------------------------------------------
// <copyright company="nAnicitus">
// Copyright (c) nAnicitus. All rights reserved.
// Licensed under the Apache License, Version 2.0 license. See LICENCE.md file in the project root for full license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace Nanicitus.Core.Monitoring
{
    /// <summary>
    /// An object which pushes metrics to an time series database instance
    /// </summary>
    public interface IMetricsCollector
    {
        /// <summary>
        /// Increments the count for the specific measurement.
        /// </summary>
        /// <param name="measurement">The name of the measurement.</param>
        /// <param name="type">The sub-type of the measurement, e.g. the HTTP method for a HTTP request.</param>
        void Increment(string measurement, string type = null);

        /// <summary>
        /// Adds a value to a measurement.
        /// </summary>
        /// <param name="measurement">The name of the measurment.</param>
        /// <param name="type">The sub-type of the measurement, e.g. the HTTP method for a HTTP request.</param>
        /// <param name="value">The current value of the measurement.</param>
        void Measure(string measurement, string type, object value);

        /// <summary>
        /// Writes a request metric to InfluxDB
        /// </summary>
        /// <param name="measurement">The name of the measurement.</param>
        /// <param name="fields">The field values for the measurement which should be written.</param>
        /// <param name="tags">The tags for the measurement.</param>
        void Write(
            string measurement,
            IReadOnlyDictionary<string, object> fields,
            IReadOnlyDictionary<string, string> tags);
    }
}
