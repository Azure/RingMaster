// <copyright file="MockStatefulService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Mock stateful service replica.
    /// </summary>
    internal sealed class MockStatefulService : IDisposable
    {
        private static int replicaIdGen;

        private readonly string partitionName;
        private readonly string replicaName;
        private readonly RingMasterBackendCore backend;
        private readonly MultiInstancePersistedDataFactory dataFactory;
        private readonly ClientSession clientSession;

        private readonly ManualResetEventSlim primaryDemotionCompleted;

        private int errorCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockStatefulService"/> class.
        /// </summary>
        /// <param name="partitionName">Name of the partition.</param>
        public MockStatefulService(string partitionName)
        {
            this.partitionName = partitionName;
            this.replicaName = $"IN-{Interlocked.Increment(ref replicaIdGen)}";

            this.dataFactory = new MultiInstancePersistedDataFactory(
                this.partitionName,
                this.replicaName,
                CancellationToken.None);

            this.backend = new RingMasterBackendCore(this.dataFactory);

            this.clientSession = new ClientSession((requestCall, cs, responseAction) =>
            {
                this.backend.ProcessMessage(requestCall.Request, cs, responseAction);
            });

            this.primaryDemotionCompleted = new ManualResetEventSlim(true);
            this.errorCount = 0;
        }

        /// <summary>
        /// Gets a value indicating whether the current replica is primary and initialization is completed.
        /// </summary>
        public ManualResetEventSlim PrimaryInitialized { get; } = new ManualResetEventSlim(false);

        /// <summary>
        /// Gets the number of errors reported by the backend
        /// </summary>
        public int ErrorCount => this.errorCount;

        /// <inheritdoc />
        public void Dispose()
        {
            this.backend.Dispose();
            this.dataFactory.Dispose();
        }

        /// <summary>
        /// Mock RunAsync in Service Fabric.
        /// </summary>
        /// <param name="cancellation">Cancellation token to indicate primary status lost.</param>
        /// <returns>Async task object.</returns>
        public Task RunAsync(CancellationToken cancellation)
        {
            Trace.TraceInformation($"RunAsync started on {this.partitionName} - {this.replicaName}");

            // Start backend.
            _ = Task.Run(() =>
            {
                var backendStarted = new ManualResetEventSlim();

                this.backend.StartService = (p1, p2) => { backendStarted.Set(); };
                this.backend.OnBackendRestartFailure = o =>
                {
                    Interlocked.Increment(ref this.errorCount);
                    throw new ApplicationException("Backend failed to restart");
                };
                this.dataFactory.OnFatalError = (msg, ex) =>
                {
                    Interlocked.Increment(ref this.errorCount);
                    throw ex;
                };

                this.primaryDemotionCompleted.Wait(cancellation);

                var unused = Task.Run(() =>
                {
                    this.backend.Start(cancellation);
                    this.backend.OnBecomePrimary();
                });

                backendStarted.Wait(cancellation);

                this.backend.ProcessSessionInitialization(
                    new Azure.Networking.Infrastructure.RingMaster.Backend.RequestCall
                    {
                        CallId = 0,
                        Request = new Azure.Networking.Infrastructure.RingMaster.Backend.RequestInit(
                            1,
                            string.Empty,
                            null,
                            false,
                            Azure.Networking.Infrastructure.RingMaster.Requests.RequestInit.RedirectionPolicy.ServerDefault),
                    },
                    this.clientSession);

                this.PrimaryInitialized.Set();
                this.primaryDemotionCompleted.Reset();
            });

            cancellation.Register(() =>
            {
                Trace.TraceInformation($"RunAsync cancelled on {this.partitionName} - {this.replicaName}");

                this.PrimaryInitialized.Reset();

                this.backend.OnPrimaryStatusLost().GetAwaiter().GetResult();

                this.primaryDemotionCompleted.Set();
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sends a request to the backend.
        /// </summary>
        /// <param name="req">request to process.</param>
        /// <returns>Response from the backend.</returns>
        public (RequestResponse Response, Exception Exception) Request(IRingMasterBackendRequest req)
        {
            var completed = new ManualResetEventSlim();
            var response = default(RequestResponse);
            var exception = default(Exception);

            this.backend.ProcessMessage(req, this.clientSession, (resp, ex) =>
            {
                response = resp;
                exception = ex;
                completed.Set();
            });

            if (!completed.Wait(1000))
            {
                return (null, new InvalidOperationException($"Backend doesn't process request in time for caller!"));
            }
            else
            {
                return (response, exception);
            }
        }
    }
}
