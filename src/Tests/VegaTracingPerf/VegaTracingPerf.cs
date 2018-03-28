// <copyright file = "VegaTracingPerf.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Azure.Networking.Infrastructure.RingMaster;

    /// <summary>
    /// Measures the tracing performance
    /// </summary>
    internal class VegaTracingPerf
    {
        /// <summary>
        /// Location of logging directory
        /// </summary>
        private string loggingDirectory;

        /// <summary>
        /// Number of worker threads to start
        /// </summary>
        private int numberOfThreads;

        /// <summary>
        /// If the work threads are still running
        /// </summary>
        private volatile bool running;

        /// <summary>
        /// Performance statistics from all threads
        /// </summary>
        private ConcurrentBag<DurationPerThread> durationAllThreads;

        /// <summary>
        /// Calculates first 800 digits of Pi, for the sake of consuming CPU time
        /// </summary>
        /// <returns>Pi result in string</returns>
        private static string Pi800Digits()
        {
            var sb = new StringBuilder();
            int[] r = new int[2800 + 1];
            int i, k;
            int b, d;
            int c = 0;

            for (i = 0; i < 2800; i++)
            {
                r[i] = 2000;
            }

            for (k = 2800; k > 0; k -= 14)
            {
                d = 0;

                i = k;
                for (; ;)
                {
                    d += r[i] * 10000;
                    b = (2 * i) - 1;

                    r[i] = d % b;
                    d /= b;
                    i--;

                    if (i == 0)
                    {
                        break;
                    }

                    d *= i;
                }

                sb.AppendFormat("{0:d4}", c + (d / 10000));
                c = d % 10000;
            }

            var pi = sb.ToString();
            if (!pi.StartsWith("31415926535897932384626433") || !pi.EndsWith("60963185"))
            {
                throw new PerfTestException();
            }

            return pi;
        }

        /// <summary>
        /// Main program entry
        /// </summary>
        private static void Main()
        {
            MeasureThreadStartup();

            foreach (var threadCount in new[] { 2, 8, 64, 128, 512, 2048, })
            {
                var stopWatch = Stopwatch.StartNew();

                int count = new VegaTracingPerf
                {
                    numberOfThreads = threadCount,
                }.Run();

                stopWatch.Stop();
                Console.WriteLine(
                    "Total execution time: {0} ms, throughput: {1:F3} count/sec. Received: {2} Dropped: {3}\n",
                    stopWatch.ElapsedMilliseconds,
                    count * 1000.0 / stopWatch.ElapsedMilliseconds,
                    LogFileEventTracing.ReceivedTraceCount,
                    LogFileEventTracing.DroppedTraceCount);

                Thread.Sleep(1000 * 2);
            }

            LogFileEventTracing.Stop();
        }

        /// <summary>
        /// Measures the thread startup cost
        /// </summary>
        private static void MeasureThreadStartup()
        {
            var loggingDirectory = Path.Combine(Path.GetTempPath(), "TracePerfTest");

            LogFileEventTracing.Start(loggingDirectory);

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 100000; i++)
            {
                var thread = new Thread(() =>
                {
                    LogFileEventTracing.Trace($"Thread {i}");
                });

                thread.Start();
                thread.Join();
            }

            stopwatch.Stop();
            Console.WriteLine(
                "MeasureThreadStartup: {0} ms  Workingset: {1}",
                stopwatch.ElapsedMilliseconds,
                Process.GetCurrentProcess().WorkingSet64);
        }

        /// <summary>
        /// Worker thread of the test
        /// </summary>
        private void WorkerThread()
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var durations = new List<long>();
            var count = 0;
            var stopWatch = new Stopwatch();

            StreamWriter localTracer = null;
            Action<string> trace;

            try
            {
                ////localTracer = new StreamWriter(string.Format("{0}\\LocalLog{1}.log", this.loggingDirectory, threadId), false, Encoding.UTF8);
                ////trace = s => localTracer.Write(s);
                trace = LogFileEventTracing.Trace;

                while (this.running)
                {
                    // One unit of work
                    stopWatch.Restart();

                    for (int i = 0; i < 1000; i++)
                    {
                        trace(string.Concat("Thread ", threadId.ToString(), "is running at ", DateTime.Now.ToString(), new string('x', i)));
                    }

                    trace(Pi800Digits());

                    stopWatch.Stop();

                    durations.Add(stopWatch.ElapsedMilliseconds);
                    count++;
                }

                this.durationAllThreads.Add(new DurationPerThread
                {
                    Count = count,
                    Avg = durations.Average(),
                    Max = durations.Max(),
                    Min = durations.Min(),
                });
            }
            finally
            {
                if (localTracer != null)
                {
                    localTracer.Dispose();
                }
            }
        }

        /// <summary>
        /// Runs the performance test
        /// </summary>
        /// <returns>Number of work unit completed across all threads</returns>
        private int Run()
        {
            this.loggingDirectory = Path.Combine(Path.GetTempPath(), "TracePerfTest");

            // Remove all files in the logging directory
            try
            {
                Array.ForEach(Directory.GetFiles(this.loggingDirectory), File.Delete);
            }
            catch (IOException)
            {
            }

            this.durationAllThreads = new ConcurrentBag<DurationPerThread>();

            Console.WriteLine("Starting {0} threads...", this.numberOfThreads);

            this.running = true;

            var threads = new List<Thread>(this.numberOfThreads);
            for (int i = 0; i < this.numberOfThreads; i++)
            {
                var thread = new Thread(this.WorkerThread);
                thread.Name = "worker" + i;
                thread.Start();
                threads.Add(thread);
            }

            var stopWatch = Stopwatch.StartNew();

            Console.WriteLine("All threads are started, press Esc to stop...");

            while (true)
            {
                if (stopWatch.Elapsed > TimeSpan.FromSeconds(60))
                {
                    Console.WriteLine("Execution timeout");
                    break;
                }

                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("Test is terminated");
                    break;
                }
            }

            this.running = false;

            foreach (var thread in threads)
            {
                thread.Join();
            }

            Console.WriteLine(
                "Average duration: {0:F3} ms, Max: {1} ms, Min: {2} ms",
                this.durationAllThreads.Select(d => d.Avg).Average(),
                this.durationAllThreads.Select(d => d.Max).Max(),
                this.durationAllThreads.Select(d => d.Min).Min());

            int totalCount = this.durationAllThreads.Select(d => d.Count).Sum();
            Console.WriteLine("Count {0} in {1} threads.", totalCount, this.numberOfThreads);

            return totalCount;
        }

        /// <summary>
        /// Performance statistics for each thread
        /// </summary>
        private struct DurationPerThread
        {
            /// <summary>
            /// Number of work unit completed
            /// </summary>
            public int Count;

            /// <summary>
            /// Average duration to complete one work unit
            /// </summary>
            public double Avg;

            /// <summary>
            /// Maximum duration to complete one work unit
            /// </summary>
            public double Max;

            /// <summary>
            /// Minimum duration to complete one work unit
            /// </summary>
            public double Min;
        }

        /// <summary>
        /// Exception in the performance test
        /// </summary>
        [Serializable]
        private sealed class PerfTestException : Exception
        {
        }
    }
}
