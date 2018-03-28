// <copyright file="IMetric10D.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation
{
    /// <summary>
    /// A Metric with ten dimensions.
    /// </summary>
    public interface IMetric10D
    {
        /// <summary>
        /// Logs a value to MDM
        /// </summary>
        /// <param name="value">Value to be logged</param>
        /// <param name="dimensionValue1">First parameter in string</param>
        /// <param name="dimensionValue2">Second parameter in string</param>
        /// <param name="dimensionValue3">Third parameter in string</param>
        /// <param name="dimensionValue4">Fourth parameter in string</param>
        /// <param name="dimensionValue5">Fifth parameter in string</param>
        /// <param name="dimensionValue6">Sixth parameter in string</param>
        /// <param name="dimensionValue7">Seventh parameter in string</param>
        /// <param name="dimensionValue8">Eighth parameter in string</param>
        /// <param name="dimensionValue9">Ninth parameter in string</param>
        /// <param name="dimensionValue10">Tenth parameter in string</param>
        void LogValue(
            long value,
            string dimensionValue1,
            string dimensionValue2,
            string dimensionValue3,
            string dimensionValue4,
            string dimensionValue5,
            string dimensionValue6,
            string dimensionValue7,
            string dimensionValue8,
            string dimensionValue9,
            string dimensionValue10);
    }
}
