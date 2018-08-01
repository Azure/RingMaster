// <copyright file="BareBackendCoreBench.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.InMemory;

    /// <summary>
    /// Bare bone backend core benchmark
    /// </summary>
    internal sealed class BareBackendCoreBench : IBench
    {
        private const int TotalNodeCount = 1000_000;
        private const int PayloadLength = 1024;
        private const int NumberOfClients = 100;

        private static InMemoryFactory inMemoryFactory = new InMemoryFactory(true, null, CancellationToken.None);

        /// <inheritdoc />
        public void Run(Action<string> log)
        {
            log($"## Barebone Backend Core benchmark\n");
            log($"Running RingMasterBackendCore direct connection with {NumberOfClients} clients with {TotalNodeCount} nodes. Payload size is {PayloadLength}");

            var backend = CreateBackend();
            var clientSession = new ClientSession((requestCall, cs, responseAction) =>
            {
                backend.ProcessMessage(requestCall.Request, cs, responseAction);
            });

            backend.ProcessSessionInitialization(
                new RequestCall
                {
                    CallId = 0,
                    Request = new RequestInit(1, string.Empty, null, false, Azure.Networking.Infrastructure.RingMaster.Requests.RequestInit.RedirectionPolicy.ServerDefault),
                },
                clientSession);

            TestCreate(log, 100, backend, clientSession);
            TestGet(log, 100, backend, clientSession);
            TestSet(log, 100, backend, clientSession);
            TestDelete(log, 100, backend, clientSession);
        }

        private static RingMasterBackendCore CreateBackend()
        {
            RingMasterBackendCore backend = null;
            try
            {
                var backendStarted = new ManualResetEventSlim();

                backend = new RingMasterBackendCore(inMemoryFactory);

                backend.StartService = (p1, p2) => { backendStarted.Set(); };
                backend.Start(CancellationToken.None);
                backend.OnBecomePrimary();

                backendStarted.Wait(30000);
                var backendToReturn = backend;
                backend = null;
                return backendToReturn;
            }
            finally
            {
                if (backend != null)
                {
                    backend.Dispose();
                }
            }
        }

        private static void TestCreate(
            Action<string> log,
            int numberOfThreads,
            RingMasterBackendCore backendCore,
            ClientSession clientSession)
        {
            var sw = new Stopwatch();
            var data = new byte[PayloadLength];

            var countPerThread = TotalNodeCount / numberOfThreads;
            var threads = Enumerable.Range(0, numberOfThreads)
                .Select(n => new Thread(() =>
                {
                    for (int i = 0; i < countPerThread; i++)
                    {
                        var createReq = new RequestCreate(
                            $"/test/{n}/{i}",
                            null,
                            data,
                            null,
                            CreateMode.PersistentSequentialAllowPathCreation,
                            null);

                        backendCore.ProcessMessage(createReq, clientSession, null);
                    }
                }))
                .ToArray();

            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            sw.Start();

            Parallel.ForEach(threads, t => t.Start());
            Parallel.ForEach(threads, t => t.Join());
            sw.Stop();

            var rate = countPerThread * numberOfThreads / sw.Elapsed.TotalSeconds;
            log($"Create nodes: {sw.Elapsed} QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }

        private static void TestGet(
            Action<string> log,
            int numberOfThreads,
            RingMasterBackendCore backendCore,
            ClientSession clientSession)
        {
            var sw = new Stopwatch();

            var countPerThread = TotalNodeCount / numberOfThreads;
            var threads = Enumerable.Range(0, numberOfThreads)
                .Select(n => new Thread(() =>
                {
                    for (int k = 0; k < 10; k++)
                    {
                        for (int i = 0; i < countPerThread; i++)
                        {
                            var getReq = new RequestGetData($"/test/{n}/{i}", null, null, null);
                            backendCore.ProcessMessage(getReq, clientSession, null);
                        }
                    }
                }))
                .ToArray();

            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            sw.Start();

            Parallel.ForEach(threads, t => t.Start());
            Parallel.ForEach(threads, t => t.Join());
            sw.Stop();

            var rate = 10 * countPerThread * numberOfThreads / sw.Elapsed.TotalSeconds;
            log($"Read nodes: {sw.Elapsed} QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }

        private static void TestSet(
            Action<string> log,
            int numberOfThreads,
            RingMasterBackendCore backendCore,
            ClientSession clientSession)
        {
            var sw = new Stopwatch();
            var data = new byte[PayloadLength];

            var countPerThread = TotalNodeCount / numberOfThreads;
            var threads = Enumerable.Range(0, numberOfThreads)
                .Select(n => new Thread(() =>
                {
                    for (int i = 0; i < countPerThread; i++)
                    {
                        var setReq = new RequestSetData($"/test/{n}/{i}", null, data, -1, null);
                        backendCore.ProcessMessage(setReq, clientSession, null);
                    }
                }))
                .ToArray();

            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            sw.Start();

            Parallel.ForEach(threads, t => t.Start());
            Parallel.ForEach(threads, t => t.Join());
            sw.Stop();

            var rate = countPerThread * numberOfThreads / sw.Elapsed.TotalSeconds;
            log($"Update nodes: {sw.Elapsed} QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }

        private static void TestDelete(
            Action<string> log,
            int numberOfThreads,
            RingMasterBackendCore backendCore,
            ClientSession clientSession)
        {
            var sw = new Stopwatch();
            var data = new byte[PayloadLength];

            var countPerThread = TotalNodeCount / numberOfThreads;
            var threads = Enumerable.Range(0, numberOfThreads)
                .Select(n => new Thread(() =>
                {
                    for (int i = 0; i < countPerThread; i++)
                    {
                        var delReq = new RequestDelete($"/test/{n}/{i}", null, -1, null, true);
                        backendCore.ProcessMessage(delReq, clientSession, null);
                    }
                }))
                .ToArray();

            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            sw.Start();

            Parallel.ForEach(threads, t => t.Start());
            Parallel.ForEach(threads, t => t.Join());
            sw.Stop();

            var rate = 10 * countPerThread * numberOfThreads / sw.Elapsed.TotalSeconds;
            log($"Delete nodes: {sw.Elapsed} QPS={rate:G3}");
            log($"  Gen0={GC.CollectionCount(0) - gen0} Gen1={GC.CollectionCount(1) - gen1} Gen2={GC.CollectionCount(2) - gen2}\n");
        }
    }
}
