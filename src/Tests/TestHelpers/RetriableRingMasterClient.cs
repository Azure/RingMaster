// <copyright file="RetriableRingMasterClient.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test.Helpers
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// The retriable rm connection
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class RetriableRingMasterClient : IRingMasterRequestHandler, IDisposable
    {
        private const int MaxRetryAttemps = 5;

        private readonly SemaphoreSlim connectionLock = new SemaphoreSlim(1, 1);

        private Func<string, IRingMasterRequestHandler> createClientFunc;

        private IRingMasterRequestHandler ringMasterRequestHandler;

        private Action<string> log;

        private IVegaServiceInfoReader vegaServerReader;

        private volatile int serverGeneration = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetriableRingMasterClient" /> class.
        /// </summary>
        /// <param name="createClient">The create client function.</param>
        /// <param name="server">the server</param>
        /// <param name="vegaServiceInfoReader">The vega service information reader.</param>
        /// <param name="log">the logger function</param>
        public RetriableRingMasterClient(Func<string, IRingMasterRequestHandler> createClient, string server, IVegaServiceInfoReader vegaServiceInfoReader = null, Action<string> log = null)
        {
            this.createClientFunc = createClient;
            this.ringMasterRequestHandler = createClient(server);
            this.vegaServerReader = vegaServiceInfoReader ?? new VegaServiceInfoReader();
            this.log = log ?? (s => Trace.TraceInformation(s));
        }

        /// <summary>
        /// Gets or sets the number of milliseconds to wait before a request is timed out.
        /// </summary>
        public int Timeout
        {
            get
            {
                return this.ringMasterRequestHandler.Timeout;
            }

            set
            {
                this.ringMasterRequestHandler.Timeout = value;
            }
        }

        /// <summary>
        /// Close the RequestForwarder.
        /// </summary>
        public void Close()
        {
            this.ringMasterRequestHandler.Close();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Requests the specified request function.
        /// </summary>
        /// <param name="requestFunc">The request function.</param>
        /// <returns>async task</returns>
        public Task Request(Func<IRingMasterRequestHandler, Task> requestFunc)
        {
            return this.Request<int>(
                ringMaster =>
                {
                    requestFunc(ringMaster);
                    return Task.FromResult(0);
                });
        }

        /// <summary>
        /// Requests the specified request function.
        /// </summary>
        /// <typeparam name="T">the return type of the request</typeparam>
        /// <param name="requestFunc">The request function.</param>
        /// <returns>
        /// async task
        /// </returns>
        public async Task<T> Request<T>(Func<IRingMasterRequestHandler, Task<T>> requestFunc)
        {
            int retryWaitTimeMs = 500;
            IRingMasterRequestHandler requestHandler;

            for (int retryAttemp = 1; retryAttemp <= MaxRetryAttemps; retryAttemp++)
            {
                bool needReconnect = false;
                int currentGeneration = -1;

                try
                {
                    currentGeneration = this.serverGeneration;

                    requestHandler = this.ringMasterRequestHandler;
                    var response = await requestFunc(requestHandler);

                    var result = response as RequestResponse;

                    if (result != null && result.ResultCode == (int)RingMasterException.Code.Operationtimeout)
                    {
                        // do nothing. Keep retry
                    }
                    else if (result != null && result.ResultCode == (int)RingMasterException.Code.OperationCancelled)
                    {
                        // non retriable exception, reconnect immediately.
                        needReconnect = true;
                    }
                    else
                    {
                        // happy path. return immediately.
                        return response;
                    }
                }
                catch (RingMasterException ex)
                {
                    if (ex.ErrorCode == RingMasterException.Code.Operationtimeout)
                    {
                        // do nothing. Keep retry
                    }
                    else if (ex.ErrorCode == RingMasterException.Code.OperationCancelled)
                    {
                        needReconnect = true;
                    }
                    else
                    {
                        throw;
                    }
                }

                needReconnect = needReconnect || retryAttemp == MaxRetryAttemps;
                if (needReconnect)
                {
                    await this.TryReconnectToNewGeneration(currentGeneration + 1);
                }
                else
                {
                    // Exponential backoff before retry
                    await Task.Delay(retryWaitTimeMs *= 2);
                }
            }

            requestHandler = this.ringMasterRequestHandler;
            return await requestFunc(requestHandler);
        }

        /// <summary>
        /// Process a request
        /// </summary>
        /// <param name="request">Request to be processed</param>
        /// <returns>
        /// Task that resolves to the response corresponding to the request
        /// </returns>
        public Task<RequestResponse> Request(IRingMasterRequest request)
        {
            return this.Request<RequestResponse>(ringMaster =>
            {
                return ringMaster.Request(request);
            });
        }

        private async Task TryReconnectToNewGeneration(int generation)
        {
            if (this.serverGeneration >= generation)
            {
                return;
            }

            try
            {
                await this.connectionLock.WaitAsync();

                if (this.serverGeneration < generation)
                {
                    var newServer = (await this.vegaServerReader.GetVegaServiceInfo()).Item1;

                    this.serverGeneration++;
                    var newRequestHandler = this.createClientFunc(newServer);
                    Interlocked.Exchange(ref this.ringMasterRequestHandler, newRequestHandler);
                }
            }
            catch (Exception ex)
            {
                this.log($"Exception when creating new client. {ex.ToString()}");
                throw;
            }
            finally
            {
                this.connectionLock.Release();
            }
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                this.connectionLock.Dispose();
                this.ringMasterRequestHandler.Dispose();
            }
        }
    }
}
