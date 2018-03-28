// <copyright file="IMetric4D.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation
{
    /// <summary>
    /// A Metric with four dimensions.
    /// </summary>
    public interface IMetric4D
    {
        /// <summary>
        /// Logs a value to MDM
        /// </summary>
        /// <param name="value">Value to be logged</param>
        /// <param name="dimensionValue1">First dimension in string</param>
        /// <param name="dimensionValue2">Second dimension in string</param>
        /// <param name="dimensionValue3">Third dimension in string</param>
        /// <param name="dimensionValue4">Fourth dimension in string</param>
        void LogValue(
            long value,
            string dimensionValue1,
            string dimensionValue2,
            string dimensionValue3,
            string dimensionValue4);
    }
}