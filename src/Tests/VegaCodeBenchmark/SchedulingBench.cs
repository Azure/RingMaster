// <copyright file="SchedulingBench.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;

    /// <summary>
    /// Work item scheduling benchmark
    /// </summary>
    internal sealed class SchedulingBench : IBench
    {
        private const int TotalCount = 10_000_000;
        private const int ThreadCount = 50_000;

        /// <inheritdoc />
        public void Run(Action<string> log)
        {
            log($"## Scheduling Benchmark (incrementing shared integer to {TotalCount})\n");

            SequentialRun(log);
            ParallelRun(log);
            AwaitTaskRun(log).GetAwaiter().GetResult();
            DotNetThreadPoolBench(log);
            QueuedWorkItemPoolBench(log);

            StartingMultiThread(log);
        }

        private static void SequentialRun(Action<string> log)
        {
            var sw = new Stopwatch();
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            sw.Start();

            int finishCount = 0;
            Action action = () => Interlocked.Increment(ref finishCount);
            for (int i = 0; i < TotalCount * 10; i++)
            {
                action();
            }

            sw.Stop();

            var rate = TotalCount * 10 / sw.Elapsed.TotalSeconds;
            log($"{nameof(SequentialRun)} 100 M actions in {sw.Elapsed}. QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }

        private static void ParallelRun(Action<string> log)
        {
            var sw = new Stopwatch();
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            sw.Start();

            int finishCount = 0;
            Parallel.For(0, TotalCount, _ => { Interlocked.Increment(ref finishCount); });
            sw.Stop();

            var rate = TotalCount / sw.Elapsed.TotalSeconds;
            log($"{nameof(ParallelRun)} 10 M actions in {sw.Elapsed}. QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }

        private static async Task AwaitTaskRun(Action<string> log)
        {
            var sw = new Stopwatch();
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            sw.Start();

            int finishCount = 0;
            var tasks = Enumerable.Range(0, TotalCount).Select(_ => Task.Run(() => { Interlocked.Increment(ref finishCount); }));
            await Task.WhenAll(tasks);
            sw.Stop();

            var rate = TotalCount / sw.Elapsed.TotalSeconds;
            log($"{nameof(AwaitTaskRun)} 10 M actions in {sw.Elapsed}. QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }

        private static void DotNetThreadPoolBench(Action<string> log)
        {
            var sw = new Stopwatch();
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            sw.Start();

            int finishCount = 0;
            Parallel.For(0, TotalCount, _ => ThreadPool.QueueUserWorkItem(obj => Interlocked.Increment(ref finishCount), null));

            SpinWait.SpinUntil(() => finishCount >= TotalCount);

            sw.Stop();

            var rate = TotalCount / sw.Elapsed.TotalSeconds;
            log($"{nameof(DotNetThreadPoolBench)} 10 M actions in {sw.Elapsed}. QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }

        private static void QueuedWorkItemPoolBench(Action<string> log)
        {
            QueuedWorkItemPool.Default.Initialize(Environment.ProcessorCount, CancellationToken.None);

            var sw = new Stopwatch();
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            sw.Start();

            int finishCount = 0;
            Parallel.For(0, TotalCount, _ => QueuedWorkItemPool.Default.Queue(() => Interlocked.Increment(ref finishCount)));

            SpinWait.SpinUntil(() => finishCount >= TotalCount);

            sw.Stop();

            var rate = TotalCount / sw.Elapsed.TotalSeconds;
            log($"{nameof(QueuedWorkItemPoolBench)} 10 M actions in {sw.Elapsed}. QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }

        private static void StartingMultiThread(Action<string> log)
        {
            var sw = new Stopwatch();
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            sw.Start();

            int finishCount = 0;
            var threads = Enumerable.Range(0, ThreadCount).Select(_ => new Thread(() => Interlocked.Increment(ref finishCount))).ToArray();
            Parallel.ForEach(threads, t => t.Start());
            Parallel.ForEach(threads, t => t.Join());

            sw.Stop();

            var rate = TotalCount / sw.Elapsed.TotalSeconds;
            log($"{nameof(StartingMultiThread)} {ThreadCount} threads in {sw.Elapsed}. QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }
    }
}
