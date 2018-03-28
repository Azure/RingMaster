// <copyright file="IMetric2D.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation
{
    /// <summary>
    /// A Metric with two dimensions.
    /// </summary>
    public interface IMetric2D
    {
        /// <summary>
        /// Logs a value to MDM
        /// </summary>
        /// <param name="value">Value to be logged in non-negative long integer</param>
        /// <param name="dimensionValue1">First dimension in string</param>
        /// <param name="dimensionValue2">Second dimension in string</param>
        void LogValue(
            long value,
            string dimensionValue1,
            string dimensionValue2);
    }
}