// <copyright file="CoreRequestHandler.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using BackendRequestInit = Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.RequestInit;
    using CommonRequestInit = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestInit;
    using IRingMasterRequestHandlerOverlapped = Microsoft.Azure.Networking.Infrastructure.RingMaster.IRingMasterRequestHandlerOverlapped;
    using RedirectionPolicy = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestInit.RedirectionPolicy;

    /// <summary>
    /// CoreRequestHandler class implements <see cref="IRingMasterRequestHandlerOverlapped"/> interface
    /// for a <see cref="ClientSession"/>.
    /// </summary>
    public class CoreRequestHandler : IRingMasterRequestHandlerOverlapped
    {
        private readonly IRingMasterRequestExecutor executor;
        private readonly ClientSession session;
        private readonly MarshallerChannel marshaller = new MarshallerChannel(null);

        private long lastAssignedCallId = 0;
        private bool isDisposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoreRequestHandler"/> class.
        /// </summary>
        /// <param name="executor">Interface to an object that can execute RingMasterRequests</param>
        /// <param name="initRequest">Init request</param>
        public CoreRequestHandler(IRingMasterRequestExecutor executor, CommonRequestInit initRequest = null)
        {
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
            this.session = new ClientSession((requestCall, clientSession, responseAction) =>
            {
                this.executor.ProcessMessage(requestCall.Request, clientSession, responseAction);
            });

            BackendRequestInit backendInitRequest = (initRequest != null)
                ? new BackendRequestInit(initRequest, null)
                : new BackendRequestInit(0, Guid.NewGuid().ToString(), null, true, RedirectionPolicy.RedirectPreferred);

            this.InitResponse = this.executor.ProcessSessionInitialization(
                 new RequestCall
                 {
                     CallId = 0,
                     Request = backendInitRequest,
                 },
                 this.session);
        }

        /// <summary>
        /// Gets the init Response.
        /// </summary>
        public RequestResponse InitResponse { get; private set; }

        /// <summary>
        /// Gets or sets gets the timeout value in milliseconds.
        /// </summary>
        public int Timeout { get; set; }

        /// <inheritdoc />
        public Task<RequestResponse> Request(IRingMasterRequest request)
        {
            var tcs = new TaskCompletionSource<RequestResponse>();
            this.RequestOverlapped(
                request,
                (response, ex) =>
                {
                    if (ex != null)
                    {
                        tcs.SetException(ex);
                    }
                    else
                    {
                        tcs.SetResult(response);
                    }
                });

            return tcs.Task;
        }

        /// <summary>
        /// Handles ringmaster requests.
        /// </summary>
        /// <param name="request">RingMaster request</param>
        /// <param name="onCompletion">Action to execute when the replication is completed</param>
        /// <remarks>
        /// Implementing the <see cref="IRingMasterRequestHandlerOverlapped"/> makes it possible for
        /// several libraries to work directly with the RingMasterBackendCore. This class
        /// is being implemented here to avoid having to expose internal classes outside of
        /// this library.
        /// </remarks>
        public void RequestOverlapped(IRingMasterRequest request, Action<RequestResponse, Exception> onCompletion)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            RequestCall call = this.marshaller.LocalRequest(request);
            call.CallId = (ulong)Interlocked.Increment(ref this.lastAssignedCallId);

            this.executor.ProcessMessage(
                call.Request,
                this.session,
                (response, ex) =>
                {
                    response.CallId = call.CallId;
                    onCompletion?.Invoke(response, ex);
                });
        }

        /// <summary>
        /// Closes the request handler
        /// </summary>
        public void Close()
        {
            this.session.Close();
            this.marshaller.Close();
        }

        /// <summary>
        /// Disposes the request handler.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing && !this.isDisposed)
            {
                this.isDisposed = true;
                this.Close();
                this.marshaller.Dispose();
            }
        }
    }
}
