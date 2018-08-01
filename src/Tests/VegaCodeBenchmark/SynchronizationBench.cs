// <copyright file="SynchronizationBench.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Synchronization primitives benchmark
    /// </summary>
    internal sealed class SynchronizationBench : IBench
    {
        private const int WarmupMilliSeconds = 10_000;
        private const int MeasureMilliSeconds = 10_000;

        /// <inheritdoc />
        public void Run(Action<string> log)
        {
            log($"## Synchronization Primitives Benchmark (no contention and high contention)\n");

            var monitor = new object();
            SynchronizationTestInternal(log, 1, "Monitor", () => Monitor.Enter(monitor), () => Monitor.Exit(monitor));
            SynchronizationTestInternal(log, 100, "Monitor", () => Monitor.Enter(monitor), () => Monitor.Exit(monitor));

            var sema = new Semaphore(1, 1);
            SynchronizationTestInternal(log, 1, "Semaphore", () => sema.WaitOne(), () => sema.Release());
            SynchronizationTestInternal(log, 100, "Semaphore", () => sema.WaitOne(), () => sema.Release());

            var rwl = new ReaderWriterLock();
            SynchronizationTestInternal(log, 1, "ReaderWriterLock", () => rwl.AcquireWriterLock(1000), () => rwl.ReleaseWriterLock());
            SynchronizationTestInternal(log, 100, "ReaderWriterLock", () => rwl.AcquireWriterLock(1000), () => rwl.ReleaseWriterLock());

            var semaSlim = new SemaphoreSlim(1, 1);
            SynchronizationTestInternal(log, 1, "SemaphoreSlim", () => semaSlim.Wait(), () => semaSlim.Release());
            SynchronizationTestInternal(log, 100, "SemaphoreSlim", () => semaSlim.Wait(), () => semaSlim.Release());

            var rwlSlim = new ReaderWriterLockSlim();
            SynchronizationTestInternal(log, 1, "ReaderWriterLockSlim", () => rwlSlim.EnterWriteLock(), () => rwlSlim.ExitWriteLock());
            SynchronizationTestInternal(log, 100, "ReaderWriterLockSlim", () => rwlSlim.EnterWriteLock(), () => rwlSlim.ExitWriteLock());

            var evtA = new ManualResetEvent(false);
            var evtB = new ManualResetEvent(false);
            SyncPingPongInternal(
                log,
                "ManualResetEvent",
                () =>
                {
                    evtB.Reset();
                    evtA.WaitOne();
                },
                () => evtA.Set(),
                () =>
                {
                    evtB.WaitOne();
                    evtA.WaitOne();
                },
                () => evtB.Set());

            var evtSlimA = new ManualResetEventSlim(false);
            var evtSlimB = new ManualResetEventSlim(false);
            SyncPingPongInternal(
                log,
                "ManualResetEventSlim",
                () =>
                {
                    evtB.Reset();
                    evtA.WaitOne();
                },
                () => evtA.Set(),
                () =>
                {
                    evtB.WaitOne();
                    evtA.WaitOne();
                },
                () => evtB.Set());
        }

        private static void SynchronizationTestInternal(Action<string> log, int concurrency, string testCaseName, Action before, Action after)
        {
            log($"Starting {testCaseName} with concurrency level = {concurrency}...");

            var count = 0;
            var stop = false;
            var threads = Enumerable.Range(0, concurrency).Select(
                _ => new Thread(() =>
                {
                    while (!stop)
                    {
                        before();
                        count++;
                        after();
                    }
                }))
                .ToArray();
            Parallel.ForEach(threads, t => t.Start());

            log($"    Warming up for {WarmupMilliSeconds / 1000} seconds");
            Thread.Sleep(WarmupMilliSeconds);

            log($"    Measuring for {MeasureMilliSeconds / 1000} seconds");

            var sw = new Stopwatch();
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            count = 0;
            sw.Start();

            Thread.Sleep(MeasureMilliSeconds);
            sw.Stop();
            stop = true;

            Parallel.ForEach(threads, t => t.Join());

            var totalCount = count;
            var rate = totalCount / sw.Elapsed.TotalSeconds;

            log($"{testCaseName} concurrency={concurrency} count={totalCount} in {sw.Elapsed}. QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }

        private static void SyncPingPongInternal(Action<string> log, string testCaseName, Action beforeA, Action beforeB, Action afterA, Action afterB)
        {
            log($"Starting {testCaseName} ...");

            var count = 0;
            var stop = false;
            var threadA = new Thread(() =>
            {
                while (!stop)
                {
                    beforeA();
                    count++;
                    afterA();
                }
            });
            var threadB = new Thread(() =>
            {
                while (!stop)
                {
                    beforeB();
                    afterB();
                }
            });
            threadA.Start();
            threadB.Start();

            log($"    Warming up for {WarmupMilliSeconds / 1000} seconds");
            Thread.Sleep(WarmupMilliSeconds);

            log($"    Measuring for {MeasureMilliSeconds / 1000} seconds");

            var sw = new Stopwatch();
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            count = 0;
            sw.Start();

            Thread.Sleep(MeasureMilliSeconds);
            sw.Stop();
            stop = true;

            threadA.Join();
            threadB.Join();

            var totalCount = count;
            var rate = totalCount / sw.Elapsed.TotalSeconds;

            log($"{testCaseName} count={totalCount} in {sw.Elapsed}. QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }
    }
}
