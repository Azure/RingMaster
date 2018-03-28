// <copyright file="IMetric1D.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation
{
    /// <summary>
    /// A Metric with one dimension.
    /// </summary>
    public interface IMetric1D
    {
        /// <summary>
        /// Logs a value to MDM
        /// </summary>
        /// <param name="value">Value to be logged</param>
        /// <param name="dimensionValue1">Dimension value</param>
        void LogValue(long value, string dimensionValue1);
    }
}