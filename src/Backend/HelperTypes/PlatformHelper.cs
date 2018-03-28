// <copyright file="PlatformHelper.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Class PlatformHelper.
    /// </summary>
    public static class PlatformHelper
    {
        /// <summary>
        /// The processor count refresh interval ms
        /// </summary>
        private const int ProcessorCountRefreshIntervalMs = 30000;

        /// <summary>
        /// The _s processor count
        /// </summary>
        private static volatile int sProcessorCount;

        /// <summary>
        /// The _s last processor count refresh ticks
        /// </summary>
        private static volatile int sLastProcessorCountRefreshTicks;

        /// <summary>
        /// Gets the processor count.
        /// </summary>
        /// <value>The processor count.</value>
        public static int ProcessorCount
        {
            get
            {
                int tickCount = Environment.TickCount;
                int num = sProcessorCount;
                if (num == 0 || tickCount - sLastProcessorCountRefreshTicks >= ProcessorCountRefreshIntervalMs)
                {
                    num = sProcessorCount = Environment.ProcessorCount;
                    sLastProcessorCountRefreshTicks = tickCount;
                }

                return num;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is single processor.
        /// </summary>
        /// <value><c>true</c> if this instance is single processor; otherwise, <c>false</c>.</value>
        public static bool IsSingleProcessor => ProcessorCount == 1;

        /// <summary>
        /// Gets the available physical memory in GB
        /// </summary>
        public static double MemoryGb
        {
            get
            {
                long phav = PerformanceInfo.GetPhysicalAvailableMemoryInMiB();

                double gb = 1.0 * phav / 1024;
                Trace.WriteLine(gb);
                return gb;
            }
        }

        private static class PerformanceInfo
        {
            [DllImport("psapi.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            [SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Unnecessary")]
            public static extern bool GetPerformanceInfo([Out] out PerformanceInformation performanceInformation, [In] int size);

            public static long GetPhysicalAvailableMemoryInMiB()
            {
                PerformanceInformation pi = default(PerformanceInformation);
                if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
                {
                    return Convert.ToInt64(pi.PhysicalAvailable.ToInt64() * pi.PageSize.ToInt64() / 1048576);
                }

                return -1;
            }

            public static long GetTotalMemoryInMiB()
            {
                PerformanceInformation pi = default(PerformanceInformation);
                if (GetPerformanceInfo(out pi, Marshal.SizeOf(pi)))
                {
                    return Convert.ToInt64(pi.PhysicalTotal.ToInt64() * pi.PageSize.ToInt64() / 1048576);
                }

                return -1;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct PerformanceInformation
            {
                public int Size;
                public IntPtr CommitTotal;
                public IntPtr CommitLimit;
                public IntPtr CommitPeak;
                public IntPtr PhysicalTotal;
                public IntPtr PhysicalAvailable;
                public IntPtr SystemCache;
                public IntPtr KernelTotal;
                public IntPtr KernelPaged;
                public IntPtr KernelNonPaged;
                public IntPtr PageSize;
                public int HandlesCount;
                public int ProcessCount;
                public int ThreadCount;
            }
        }
    }
}