// <copyright file="ConnectPerformance.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Performance
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    public class ConnectPerformance : IDisposable
    {
        private readonly List<string> nodeList = new List<string>();
        private readonly CancellationToken cancellationToken;
        private readonly IInstrumentation instrumentation;
        private readonly SemaphoreSlim semaphore;
        private readonly List<IRingMasterRequestHandler> connections = new List<IRingMasterRequestHandler>();

        public ConnectPerformance(IInstrumentation instrumentation, int maxConcurrentRequests, CancellationToken cancellationToken)
        {
            this.instrumentation = instrumentation;
            this.semaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
            this.cancellationToken = cancellationToken;
        }

        public interface IInstrumentation
        {
            void ConnectionCreated(int connectionCount, TimeSpan elapsed);

            void RequestSucceeded(TimeSpan elapsed);

            void RequestFailed();
        }

        /// <summary>
        /// Establish the specified number of connections to ringmaster.
        /// </summary>
        /// <param name="connectToRingMsater">Function that connects to RingMaster</param>
        /// <param name="numConnections">Number of connections to establish</param>
        public void EstablishConnections(Func<IRingMasterRequestHandler> connectToRingMaster, int numConnections)
        {
            if (connectToRingMaster == null)
            {
                throw new ArgumentNullException(nameof(connectToRingMaster));
            }

            for (int i = 0; i < numConnections; i++)
            {
                var timer = Stopwatch.StartNew();
                var ringMasterClient = connectToRingMaster();
                this.connections.Add(ringMasterClient);
                timer.Stop();
                this.instrumentation?.ConnectionCreated(i, timer.Elapsed);
            }
        }

        /// <summary>
        /// Queue Exists requests.
        /// </summary>
        /// <param name="path">Path to query</param>
        public void QueueRequests(string path)
        {
            Trace.TraceInformation($"ConnectPerformance.QueueRequests: connectionCount={this.connections.Count}");

            var random = new RandomGenerator();
            ulong requestId = 0;
            while (!this.cancellationToken.IsCancellationRequested)
            {
                var index = random.GetRandomInt(0, this.connections.Count);
                var ringMaster = this.connections[index];

                var existsRequest = new RequestExists(path, watcher: null, uid: requestId++);

                var timer = Stopwatch.StartNew();
                this.semaphore.Wait();

                ringMaster.Request(existsRequest).ContinueWith(responseTask =>
                {
                    this.semaphore.Release();
                    timer.Stop();

                    try
                    {
                        RequestResponse response = responseTask.Result;
                        if (response.ResultCode == (int)RingMasterException.Code.Ok)
                        {
                            this.instrumentation?.RequestSucceeded(timer.Elapsed);
                        }
                        else
                        {
                            this.instrumentation?.RequestFailed();
                        }
                    }
                    catch (Exception)
                    {
                        this.instrumentation?.RequestFailed();
                    }
                });
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                this.semaphore.Dispose();

                foreach (var connection in this.connections)
                {
                    connection.Dispose();
                }
            }
        }
    }
}
