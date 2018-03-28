﻿// <copyright file="IMetric6D.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation
{
    /// <summary>
    /// A Metric with six dimensions.
    /// </summary>
    public interface IMetric6D
    {
        /// <summary>
        /// Logs a value to MDM
        /// </summary>
        /// <param name="value">Value to be logged in non-negative long integer</param>
        /// <param name="dimensionValue1">First dimension in string</param>
        /// <param name="dimensionValue2">Second dimension in string</param>
        /// <param name="dimensionValue3">Third dimension in string</param>
        /// <param name="dimensionValue4">Fourth dimension in string</param>
        /// <param name="dimensionValue5">Fifth dimension in string</param>
        /// <param name="dimensionValue6">Sixth dimension in string</param>
        void LogValue(
            long value,
            string dimensionValue1,
            string dimensionValue2,
            string dimensionValue3,
            string dimensionValue4,
            string dimensionValue5,
            string dimensionValue6);
    }
}
