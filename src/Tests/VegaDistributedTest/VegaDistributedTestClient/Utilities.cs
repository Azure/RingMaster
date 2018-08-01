// <copyright file="Utilities.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Linq;

    /// <summary>
    /// Provides utility functions for statistics report.
    /// </summary>
    internal static class Utilities
    {
        /// <summary>
        /// Percentiles the specified sequence.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <param name="excelPercentile">The excel percentile.</param>
        /// <returns>the percentile</returns>
        internal static double Percentile(double[] sequence, double excelPercentile)
        {
            int len = sequence.Length;
            double n = ((len - 1) * excelPercentile) + 1;
            if (n == 1d)
            {
                return sequence[0];
            }
            else if (n == len)
            {
                return sequence[len - 1];
            }
            else
            {
                int k = (int)n;
                double d = n - k;
                return sequence[k - 1] + (d * (sequence[k] - sequence[k - 1]));
            }
        }

        /// <summary>
        /// Gets the report.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <returns>the report in a string format.</returns>
        internal static string GetReport(double[] sequence)
        {
            Array.Sort(sequence);
            return string.Format("#{0} Min/Max/Average: {1,6:F1},{2,7:F1},{3,7:F1}, P90: {4,7:F1}, P99: {5,7:F1}", sequence.Length, sequence.Min(), sequence.Max(), sequence.Average(), Percentile(sequence, 0.9), Percentile(sequence, 0.99));
        }
    }
}
