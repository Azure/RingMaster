// <copyright file="CollectionsBench.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// .NET collections benchmark
    /// </summary>
    internal sealed class CollectionsBench : IBench
    {
        /// <inheritdoc />
        public void Run(Action<string> log)
        {
            log($"## Collections Benchmark\n");

            StackBench(log);
            DictionaryBench(log);
            ConcurrentDictionaryBench(log, 1);
            ConcurrentDictionaryBench(log, 10);
            ConcurrentQueueBench(log, 2);
            ConcurrentQueueBench(log, 100);
        }

        private static void StackBench(Action<string> log)
        {
            var stack = new Stack<string>();
            var sw = new Stopwatch();
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);

            var totalCount = 20_000_000;
            sw.Start();

            for (int i = 0; i < totalCount; i++)
            {
                stack.Push(i.ToString());
            }

            while (stack.TryPop(out var s))
            {
            }

            sw.Stop();

            var rate = totalCount / sw.Elapsed.TotalSeconds;
            log($"{nameof(StackBench)} {totalCount} Push/Pop in {sw.Elapsed}. QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }

        private static void DictionaryBench(Action<string> log)
        {
            var dict = new Dictionary<long, string>();
            var sw = new Stopwatch();
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);

            var totalCount = 20_000_000;
            sw.Start();

            for (int i = 0; i < totalCount; i++)
            {
                dict.Add(i, i.ToString());
            }

            foreach (var kvp in dict)
            {
                if (kvp.Key.ToString() != kvp.Value)
                {
                    throw new Exception();
                }
            }

            for (int i = 0; i < totalCount; i++)
            {
                dict.Remove(i);
            }

            sw.Stop();

            var rate = totalCount / sw.Elapsed.TotalSeconds;
            log($"{nameof(DictionaryBench)} {totalCount}Add/Query/Remove in {sw.Elapsed}. QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }

        private static void ConcurrentDictionaryBench(Action<string> log, int concurrency)
        {
            var dict = new ConcurrentDictionary<int, string>();
            var sw = new Stopwatch();
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);

            var totalCount = 20_000_000 / concurrency;
            sw.Start();

            var threads = Enumerable.Range(0, concurrency).Select(n => new Thread(() =>
            {
                var begin = totalCount * n;
                var end = begin + totalCount;

                for (int i = begin; i < end; i++)
                {
                    dict.TryAdd(i, i.ToString());
                }

                foreach (var kvp in dict)
                {
                    if (kvp.Key.ToString() != kvp.Value)
                    {
                        throw new Exception();
                    }
                }

                for (int i = begin; i < end; i++)
                {
                    dict.Remove(i, out var s);
                }
            })).ToArray();

            Parallel.ForEach(threads, t => t.Start());
            Parallel.ForEach(threads, t => t.Join());

            sw.Stop();

            var rate = totalCount / sw.Elapsed.TotalSeconds;
            log($"{nameof(ConcurrentDictionaryBench)} {totalCount}Add/Query/Remove in {sw.Elapsed}. QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }

        private static void ConcurrentQueueBench(Action<string> log, int concurrency)
        {
            log($"{nameof(ConcurrentQueueBench)} starting with {concurrency} threads...");

            var queue = new ConcurrentQueue<string>();
            var sw = new Stopwatch();
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);

            var count = 0;
            var stop = false;

            var threads = Enumerable.Range(0, concurrency).Select(n => new Thread(() =>
            {
                if ((n & 0x01) == 0)
                {
                    var rand = new Random();
                    while (!stop)
                    {
                        queue.Enqueue(rand.Next().ToString());
                    }
                }
                else
                {
                    while (!stop)
                    {
                        if (queue.TryDequeue(out var s))
                        {
                            Interlocked.Increment(ref count);
                        }
                    }
                }
            })).ToArray();

            Parallel.ForEach(threads, t => t.Start());

            log($"    warming up for 10 seconds");
            Thread.Sleep(10_000);

            sw.Start();
            Interlocked.Exchange(ref count, 0);
            log($"    Measuring for 10 seconds");
            Thread.Sleep(10_000);

            sw.Stop();
            stop = true;
            Parallel.ForEach(threads, t => t.Join());

            var rate = count / sw.Elapsed.TotalSeconds;
            log($"{nameof(ConcurrentQueueBench)} {count} enqueue/dequeue in {sw.Elapsed}. QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }
    }
}
