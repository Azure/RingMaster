// <copyright file="PreciseTime.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.DistributedTest
{
    using System;
    using System.Diagnostics;

    /// <summary>
    ///     High precision system time provider for retrieving sub-millisecond elapsed time
    /// </summary>
    public static class PreciseTime
    {
        private static readonly Stopwatch Wallclock = Stopwatch.StartNew();

        /// <summary>
        ///     Gets the elapsed time since the class is initialized
        /// </summary>
        public static TimeSpan Uptime
        {
            get
            {
                return Wallclock.Elapsed;
            }
        }

        /// <summary>
        ///     Returns the total milliseconds since the specified starting time
        /// </summary>
        /// <param name="previousTime">Previous time</param>
        /// <returns>Time span in millisecond</returns>
        public static double MillisecondsSince(TimeSpan previousTime)
        {
            return (Uptime - previousTime).TotalMilliseconds;
        }
    }
}
