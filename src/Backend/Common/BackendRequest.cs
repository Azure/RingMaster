// <copyright file="BackendRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using IRingMasterRequest = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.IRingMasterRequest;
    using ISessionAuth = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.ISessionAuth;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using RingMasterRequestType = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RingMasterRequestType;

#pragma warning disable SA1402

    /// <summary>
    /// Base class for BackendRequests. Implemements support for storing and invoking completion callback and calculating completion time.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request that is represented by this BackendRequest</typeparam>
    /// <remarks>
    /// This class implements <see cref="IRingMasterRequest"/> interface by forwarding to <typeparamref name="TRequest"/>.
    /// </remarks>
    public abstract class BackendRequest<TRequest> : IRingMasterBackendRequest
        where TRequest : class, IRingMasterRequest
    {
        private readonly TRequest request;
        private RequestDelegates.OnAfterComplete onComplete;
        private long completionTimeInTicks;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackendRequest{TRequest}"/> class.
        /// </summary>
        /// <param name="request">Requst object</param>
        protected BackendRequest(TRequest request)
        {
            this.request = request;
        }

        /// <summary>
        /// Gets the type of the request.
        /// </summary>
        public RingMasterRequestType RequestType => this.request.RequestType;

        /// <summary>
        /// Gets the unique id of the request.
        /// </summary>
        public ulong Uid => this.request.Uid;

        /// <summary>
        /// Gets or sets the time stream associated with this request.
        /// </summary>
        public ulong TimeStreamId
        {
            get
            {
                return this.request.TimeStreamId;
            }

            set
            {
                this.request.TimeStreamId = value;
            }
        }

        /// <summary>
        /// Gets or sets the path of the node associated with this request.
        /// </summary>
        public string Path
        {
            get
            {
                return this.request.Path;
            }

            set
            {
                this.request.Path = value;
            }
        }

        /// <summary>
        /// Gets or sets the authentication credentials to use for this request or <c>null</c> of the session's authentication credentials must be used.
        /// </summary>
        public ISessionAuth Auth
        {
            get
            {
                return this.request.Auth;
            }

            set
            {
                this.request.Auth = value;
            }
        }

        /// <summary>
        /// Gets or sets the information about which transaction id and transaction time to use for this request or null to indicate that ring master must
        /// assign those values.
        /// </summary>
        public IOperationOverrides Overrides
        {
            get
            {
                return this.request.Overrides;
            }

            set
            {
                this.request.Overrides = value;
            }
        }

        /// <summary>
        /// Gets or sets the execution queue id to queue this command into
        /// </summary>
        public Guid ExecutionQueueId
        {
            get
            {
                return this.request.ExecutionQueueId;
            }

            set
            {
                this.request.ExecutionQueueId = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum time allowed for this request to be waiting on the execution queue
        /// </summary>
        public int ExecutionQueueTimeoutMillis
        {
            get
            {
                return this.request.ExecutionQueueTimeoutMillis;
            }

            set
            {
                this.request.ExecutionQueueTimeoutMillis = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="IRingMasterRequest"/> that is being wrapped by this request.
        /// </summary>
        public IRingMasterRequest WrappedRequest => this.Request;

        /// <summary>
        /// Gets the request object
        /// </summary>
        protected TRequest Request => this.request;

        /// <summary>
        /// Gets a value indicating whether this request is readonly
        /// </summary>
        /// <returns><c>true</c> if this request is read only</returns>
        public bool IsReadOnly()
        {
            return this.request.IsReadOnly();
        }

        /// <summary>
        /// Sets the callback that must be invoked after this request is completed.
        /// </summary>
        /// <param name="onComplete">The callback that must be invoked</param>
        public void SetOnAfterComplete(RequestDelegates.OnAfterComplete onComplete)
        {
            if (this.onComplete == null || this.onComplete == onComplete)
            {
                this.onComplete = onComplete;
            }
            else
            {
                RequestDelegates.OnAfterComplete prevOnComplete = this.onComplete;
                this.onComplete = (IRingMasterBackendRequest req, int resultcode, double timeInMillis) =>
                {
                    prevOnComplete(req, resultcode, timeInMillis);
                    onComplete(req, resultcode, timeInMillis);
                };
            }

            this.completionTimeInTicks = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// Notifies that the request has been completed.
        /// </summary>
        /// <param name="resultCode">The result code.</param>
        /// <param name="result">The result.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="responsePath">The response path.</param>
        public void NotifyComplete(int resultCode, object result, IStat stat, string responsePath)
        {
            this.InvokeCallback(resultCode, result, stat, responsePath);
            if (this.onComplete != null)
            {
                double time = 1.0 * (DateTime.UtcNow.Ticks - this.completionTimeInTicks) / TimeSpan.TicksPerMillisecond;
                this.onComplete(this, resultCode, time);
            }
        }

        /// <summary>
        /// Compares this <see cref="IRingMasterBackendRequest"/> with the given request.
        /// </summary>
        /// <param name="ringMasterRequest">The request to compare with</param>
        /// <returns><c>true</c> if objects are equal (including data), <c>false</c> otherwise.</returns>
        public abstract bool DataEquals(IRingMasterBackendRequest ringMasterRequest);

        /// <summary>
        /// Invokes the callback method when the notification is completed
        /// </summary>
        /// <param name="resultCode">Result code</param>
        /// <param name="result">Result object</param>
        /// <param name="stat">Stat object</param>
        /// <param name="responsePath">Response path</param>
        protected abstract void InvokeCallback(int resultCode, object result, IStat stat, string responsePath);
    }

    /// <summary>
    /// Backend request
    /// </summary>
    internal static class BackendRequest
    {
        /// <summary>
        /// Wraps a request to backend request object
        /// </summary>
        /// <param name="request">Request to be wrapped</param>
        /// <returns>Backend request object</returns>
        public static IRingMasterBackendRequest Wrap(IRingMasterRequest request)
        {
            IRingMasterBackendRequest wrap = request as IRingMasterBackendRequest;
            if (wrap != null)
            {
                return wrap;
            }

            switch (request.RequestType)
            {
                case RingMasterRequestType.Init:
                    return new RequestInit((RequestDefinitions.RequestInit)request, null);
                case RingMasterRequestType.Create:
                    return new RequestCreate((RequestDefinitions.RequestCreate)request, null, null);
                case RingMasterRequestType.Delete:
                    return new RequestDelete((RequestDefinitions.RequestDelete)request, null, null);
                case RingMasterRequestType.Sync:
                    return new RequestSync((RequestDefinitions.RequestSync)request, null, null);
                case RingMasterRequestType.Exists:
                    return new RequestExists((RequestDefinitions.RequestExists)request, null, null);
                case RingMasterRequestType.GetAcl:
                    return new RequestGetAcl((RequestDefinitions.RequestGetAcl)request, null, null);
                case RingMasterRequestType.GetData:
                    return new RequestGetData((RequestDefinitions.RequestGetData)request, null, null);
                case RingMasterRequestType.GetChildren:
                    return new RequestGetChildren((RequestDefinitions.RequestGetChildren)request, null, null);
                case RingMasterRequestType.SetData:
                    return new RequestSetData((RequestDefinitions.RequestSetData)request, null, null);
                case RingMasterRequestType.SetAcl:
                    return new RequestSetAcl((RequestDefinitions.RequestSetAcl)request, null, null);
                case RingMasterRequestType.SetAuth:
                    return new RequestSetAuth((RequestDefinitions.RequestSetAuth)request, null);
                case RingMasterRequestType.Check:
                    return new RequestCheck((RequestDefinitions.RequestCheck)request, null, null);
                case RingMasterRequestType.Multi:
                    return new RequestMulti((RequestDefinitions.RequestMulti)request, null, null);
                case RingMasterRequestType.Batch:
                    return new RequestBatch((RequestDefinitions.RequestBatch)request, null, null);
                case RingMasterRequestType.Move:
                    return new RequestMove((RequestDefinitions.RequestMove)request, null, null);
                case RingMasterRequestType.GetSubtree:
                    return new RequestGetSubtree((RequestDefinitions.RequestGetSubtree)request, null, null);
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Wraps a collection of requests to backend request objects
        /// </summary>
        /// <param name="requests">Requests to be wrapped</param>
        /// <returns>Collection of wrapped objects</returns>
        public static IReadOnlyList<IRingMasterBackendRequest> Wrap(IReadOnlyList<RequestDefinitions.IRingMasterRequest> requests)
        {
            IRingMasterBackendRequest[] wrappedRequests = new IRingMasterBackendRequest[requests.Count];
            for (int i = 0; i < requests.Count; i++)
            {
                wrappedRequests[i] = Wrap(requests[i]);
            }

            return wrappedRequests;
        }
    }
}