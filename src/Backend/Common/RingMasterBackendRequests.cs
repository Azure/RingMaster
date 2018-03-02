// <copyright file="RingMasterBackendRequests.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    using Code = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.RingMasterException.Code;
    using GetDataOptions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.GetDataOptions;
    using IGetDataOptionArgument = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.IGetDataOptionArgument;
    using IRingMasterRequest = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.IRingMasterRequest;
    using ISessionAuth = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.ISessionAuth;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using RingMasterRequestType = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RingMasterRequestType;

    /// <summary>
    /// Class RequestDelegates.
    /// </summary>
    public class RequestDelegates
    {
        /// <summary>
        /// Delegate OnAfterComplete
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="resultcode">The resultcode.</param>
        /// <param name="timeInMillis">The time in millis.</param>
        public delegate void OnAfterComplete(IRingMasterBackendRequest req, int resultcode, double timeInMillis);
    }

    /// <summary>
    /// IRingMasterBackendRequest interface extends IRingMasterRequest with support for adding callbacks to be invoked after the
    /// request has been completed.
    /// </summary>
    public interface IRingMasterBackendRequest : IRingMasterRequest
    {
        /// <summary>
        /// Gets the <see cref="IRingMasterRequest"/> that is being wrapped by this request.
        /// </summary>
        IRingMasterRequest WrappedRequest { get; }

        /// <summary>
        /// Sets the on after complete.
        /// </summary>
        /// <param name="onComplete">The on complete.</param>
        void SetOnAfterComplete(RequestDelegates.OnAfterComplete onComplete);

        /// <summary>
        /// Notifies the complete.
        /// </summary>
        /// <param name="resultCode">The result code.</param>
        /// <param name="result">The result.</param>
        /// <param name="stat">The stat.</param>
        void NotifyComplete(int resultCode, object result, IStat stat);

        /// <summary>
        /// Datas the equals.
        /// </summary>
        /// <param name="ringMasterRequest">The ring master request.</param>
        /// <returns><c>true</c> if objects are equal (including data), <c>false</c> otherwise.</returns>
        bool DataEquals(IRingMasterBackendRequest ringMasterRequest);
    }

    /// <summary>
    /// IRingMasterBackendCompoundRequest defines a request that is a collection of other requests.
    /// </summary>
    public interface IRingMasterBackendCompondRequest : IRingMasterBackendRequest
    {
        bool CompleteSynchronously { get; set; }

        IReadOnlyList<IRingMasterBackendRequest> Requests { get; }
    }

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
        /// Gets a value indicating whether this request is readonly
        /// </summary>
        /// <returns><c>true</c> if this request is read only</returns>
        public bool IsReadOnly()
        {
            return this.request.IsReadOnly();
        }

        /// <summary>
        /// Gets the <see cref="IRingMasterRequest"/> that is being wrapped by this request.
        /// </summary>
        public IRingMasterRequest WrappedRequest => this.Request;

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
        public void NotifyComplete(int resultCode, object result, IStat stat)
        {
            this.InvokeCallback(resultCode, result, stat);
            if (this.onComplete != null)
            {
                double time = 1.0 * (DateTime.UtcNow.Ticks - this.completionTimeInTicks) / TimeSpan.TicksPerMillisecond;
                this.onComplete(this, resultCode, time);
            }
        }

        protected abstract void InvokeCallback(int resultCode, object result, IStat stat);

        /// <summary>
        /// Compares this <see cref="IRingMasterBackendRequest"/> with the given request.
        /// </summary>
        /// <param name="ringMasterRequest">The request to compare with</param>
        /// <returns><c>true</c> if objects are equal (including data), <c>false</c> otherwise.</returns>
        public abstract bool DataEquals(IRingMasterBackendRequest ringMasterRequest);

        protected TRequest Request => this.request;
    }

    /// <summary>
    /// Request to initialize a session established by a client.
    /// </summary>
    public sealed class RequestInit : BackendRequest<RequestDefinitions.RequestInit>
    {
        private readonly VoidCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestInit"/> class.
        /// </summary>
        /// <param name="sessionId">session identifier.</param>
        /// <param name="sessionPwd">session password.</param>
        /// <param name="callback">The callback to invoke when this request is completed</param>
        /// <param name="rOInterfaceRequiresLocks"> if true (default) the session requires read operations to use locks. if false, reads will be lockfree and ApiError may be returned upon concurrency issues</param>
        /// <param name="redirection">redirection policy for this session</param>
        public RequestInit(
            ulong sessionId,
            string sessionPwd,
            VoidCallbackDelegate callback,
            bool rOInterfaceRequiresLocks,
            RequestDefinitions.RequestInit.RedirectionPolicy redirection)
            : this(new RequestDefinitions.RequestInit(sessionId, sessionPwd, rOInterfaceRequiresLocks, redirection), callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestInit"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestInit(RequestDefinitions.RequestInit request, VoidCallbackDelegate callback)
            : base(request)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets a value indicating whether the session requests for read only operations to be lock-free, meaning <c>ApiError</c> may be thrown if race conditions happen during read only operations.
        /// </summary>
        public bool RoInterfaceRequiresLocks => this.Request.ROInterfaceRequiresLocks;

        /// <summary>
        /// Gets the policy for redirection to primary/master
        /// </summary>
        public RequestDefinitions.RequestInit.RedirectionPolicy Redirection => this.Request.Redirection;

        /// <summary>
        /// Gets the session identifier.
        /// </summary>
        public ulong SessionId => this.Request.SessionId;

        /// <summary>
        /// Gets the session password.
        /// </summary>
        public string SessionPwd => this.Request.SessionPwd;

        protected override void InvokeCallback(int resultCode, object result, IStat stat)
        {
            this.callback(resultCode, null, result);
        }

        public override bool DataEquals(IRingMasterBackendRequest obj)
        {
            RequestInit other = obj as RequestInit;

            // note we don't need to validate the request type because the previous check covers us on that
            if (this.SessionId != other?.SessionId)
            {
                return false;
            }

            return string.Equals(this.SessionPwd, other.SessionPwd);
        }
    }

    /// <summary>
    /// Request to set credentials for a session.
    /// </summary>
    public sealed class RequestSetAuth : BackendRequest<RequestDefinitions.RequestSetAuth>
    {
        private readonly VoidCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetAuth"/> class.
        /// </summary>
        /// <param name="clientId">client identifier.</param>
        /// <param name="cb">callback that must be invoked when this request is completed</param>
        public RequestSetAuth(string clientId, VoidCallbackDelegate cb)
            : this(new RequestDefinitions.RequestSetAuth(clientId), cb)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetAuth"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestSetAuth(RequestDefinitions.RequestSetAuth request, VoidCallbackDelegate callback)
            : base(request)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets the client id.
        /// </summary>
        public string ClientId => this.Request.ClientId;

        protected override void InvokeCallback(int resultCode, object result, IStat stat)
        {
            this.callback(resultCode, null, result);
        }

        public override bool DataEquals(IRingMasterBackendRequest obj)
        {
            RequestSetAuth other = obj as RequestSetAuth;
            if (other == null)
            {
                return false;
            }

            // note we don't need to validate the request type because the previous check covers us on that
            return string.Equals(this.ClientId, other.ClientId);
        }
    }

    /// <summary>
    /// Implements the ability to associate a context with a request.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request</typeparam>
    /// <typeparam name="TReturn">Type of the return value associated with the request</typeparam>
    public abstract class BackendRequestWithContext<TRequest, TReturn> : BackendRequest<TRequest>
        where TRequest : class, IRingMasterRequest
        where TReturn : class
    {
        /// <summary>
        /// Source of unique ids.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "Provider should not change.")]
        private static readonly UIdProvider UidProvider = new UIdProvider();

        private readonly object context;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackendRequestWithContext{TRequest, TReturn}"/> class.
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="context">The context associated with the request</param>
        protected BackendRequestWithContext(TRequest request, object context)
            : base(request)
        {
            this.context = context;
        }

        /// <summary>
        /// Gets the context associated with this request.
        /// </summary>
        public object Context => this.context;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = this.GetType().GetHashCode();

            hash ^= this.Uid.GetHashCode();

            if (this.Path != null)
            {
                hash ^= this.Path.GetHashCode();
            }

            if (this.Context != null)
            {
                hash ^= this.Context.GetHashCode();
            }

            hash ^= this.ExecutionQueueId.GetHashCode();

            hash ^= this.ExecutionQueueTimeoutMillis.GetHashCode();

            return hash;
        }

        public override bool DataEquals(IRingMasterBackendRequest obj)
        {
            BackendRequestWithContext<TRequest, TReturn> other = obj as BackendRequestWithContext<TRequest, TReturn>;

            // note we don't need to validate the request type because the previous check covers us on that
            if (this.Uid != other?.Uid)
            {
                return false;
            }

            if (this.ExecutionQueueTimeoutMillis != other.ExecutionQueueTimeoutMillis)
            {
                return false;
            }

            if (Guid.Equals(this.ExecutionQueueId, other.ExecutionQueueId))
            {
                return false;
            }

            if (!string.Equals(this.Path, other.Path))
            {
                return false;
            }

            return EqualityHelper.Equals(this.context, other.context);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            BackendRequestWithContext<TRequest, TReturn> other = obj as BackendRequestWithContext<TRequest, TReturn>;

            return this.DataEquals(other);
        }

        /// <summary>
        /// Assigns a unique uid if the given uid is zero.
        /// </summary>
        /// <param name="uid">The uid to assign (or zero)</param>
        /// <returns>A unique id</returns>
        protected static ulong MakeUid(ulong uid)
        {
            if (uid == 0)
            {
                return UidProvider.NextUniqueId();
            }

            return uid;
        }

        protected override sealed void InvokeCallback(int resultCode, object result, IStat stat)
        {
            TReturn resultAsT = null;
            if (result != null)
            {
                try
                {
                    resultAsT = (TReturn)result;
                }
                catch (InvalidCastException)
                {
                    this.NotifyComplete((int)Code.Marshallingerror, default(TReturn), stat);
                    return;
                }
            }

            this.NotifyComplete(resultCode, resultAsT, stat);
        }

        /// <summary>
        /// Notifies that the request has been completed.
        /// </summary>
        /// <param name="resultCode">result code</param>
        /// <param name="result">The result</param>
        /// <param name="stat">The stat</param>
        protected abstract void NotifyComplete(int resultCode, TReturn result, IStat stat);
    }

    /// <summary>
    /// Request to create a new node.
    /// </summary>
    public sealed class RequestCreate : BackendRequestWithContext<RequestDefinitions.RequestCreate, string>
    {
        private readonly StringCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCreate"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the invocation</param>
        /// <param name="data">Data that will be associated with the newly created node</param>
        /// <param name="acl">List of <see cref="Acl"/>s that will be associated wiht the newly created node</param>
        /// <param name="createMode">Specifies how the node must be created</param>
        /// <param name="callback">Callback that must be invoked when the request is completed</param>
        /// <param name="uid">Optional unique id to assign to the request</param>
        public RequestCreate(string path, object context, byte[] data, IReadOnlyList<Acl> acl, CreateMode createMode, StringCallbackDelegate callback, ulong uid = 0)
            : this(new RequestDefinitions.RequestCreate(path, data, acl, createMode, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCreate"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the invocation</param>
        /// <param name="callback">Callback that must be invoked when the request is completed</param>
        public RequestCreate(RequestDefinitions.RequestCreate request, object context, StringCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets or sets the content that will be stored in the node when it is created.
        /// </summary>
        public byte[] Data
        {
            get
            {
                return this.Request.Data;
            }

            set
            {
                this.Request.Data = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Acl"/>s that will be associated with the node when it is created.
        /// </summary>
        public IReadOnlyList<Acl> Acl
        {
            get
            {
                return this.Request.Acl;
            }

            set
            {
                this.Request.Acl = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that specifies how the node will be created.
        /// </summary>
        /// <value>The create mode.</value>
        public CreateMode CreateMode
        {
            get
            {
                return this.Request.CreateMode;
            }

            set
            {
                this.Request.CreateMode = value;
            }
        }

        /// <summary>
        /// Gets the callback.
        /// </summary>
        internal StringCallbackDelegate Callback => this.callback;

        /// <summary>
        /// Notifies the complete.
        /// </summary>
        /// <param name="resultCode">The result code.</param>
        /// <param name="result">The result.</param>
        /// <param name="stat">The stat.</param>
        protected override void NotifyComplete(int resultCode, string result, IStat stat)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, result);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            hash ^= this.CreateMode.GetHashCode();

            if (this.Data != null)
            {
                hash ^= this.Data.GetHashCode();
            }

            if (this.Acl != null)
            {
                hash ^= this.Acl.GetHashCode();
            }

            if (this.Context != null)
            {
                hash ^= this.Context.GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            RequestCreate other = obj as RequestCreate;
            if (other == null)
            {
                return false;
            }

            if (!base.Equals(obj))
            {
                return false;
            }

            if (this.CreateMode != other.CreateMode)
            {
                return false;
            }

            if (!EqualityHelper.Equals(this.Acl, other.Acl))
            {
                return false;
            }

            return EqualityHelper.Equals(this.Data, other.Data);
        }
    }

    /// <summary>
    /// Request to execute a list of <see cref="IRingMasterRequest"/>s as a transaction.
    /// </summary>
    public sealed class RequestMulti : BackendCompoundRequest<RequestDefinitions.RequestMulti>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMulti"/> class.
        /// </summary>
        /// <param name="operations">The operations to execute</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="completeSynchronously">If true, the multi will invoke cb only after the operations have been replicated locally.</param>
        /// <param name="uid">Optional unique id to assign to the request</param>
        public RequestMulti(
            IReadOnlyList<Op> operations,
            object context,
            OpsResultCallbackDelegate callback,
            bool completeSynchronously = false,
            ulong uid = 0)
            : this(
                  new RequestDefinitions.RequestMulti(operations, completeSynchronously, MakeUid(uid)),
                  context,
                  callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMulti"/> class.
        /// </summary>
        /// <param name="operations">The operations to execute</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="completeSynchronously">If true, the multi will invoke cb only after the operations have been replicated locally.</param>
        /// <param name="scheduledName">the name of the scheduled command (or null if this will not be an scheduled command)</param>
        /// <param name="uid">Optional unique id to assign to the request</param>
        public RequestMulti(
            IReadOnlyList<Op> operations,
            object context,
            OpsResultCallbackDelegate callback,
            bool completeSynchronously,
            string scheduledName,
            ulong uid = 0)
            : this(
                  new RequestDefinitions.RequestMulti(operations, completeSynchronously, scheduledName, MakeUid(uid)),
                  context,
                  callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMulti"/> class.
        /// </summary>
        /// <param name="requests">The requests to execute</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="completeSynchronously">If true, the multi will invoke cb only after the operations have been replicated locally.</param>
        /// <param name="uid">Optional unique id to assign to the request</param>
        public RequestMulti(
            IRingMasterBackendRequest[] requests,
            object context,
            OpsResultCallbackDelegate callback,
            bool completeSynchronously = false,
            ulong uid = 0)
            : this(
                  new RequestDefinitions.RequestMulti(requests, completeSynchronously, null, MakeUid(uid)),
                  context,
                  callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMulti"/> class.
        /// </summary>
        /// <param name="requests">The requests to execute</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="completeSynchronously">If true, the multi will invoke cb only after the operations have been replicated locally.</param>
        /// <param name="scheduledName">the name of the scheduled command (or null if this will not be an scheduled command)</param>
        /// <param name="uid">Optional unique id to assign to the request</param>
        public RequestMulti(
            IRingMasterBackendRequest[] requests,
            object context,
            OpsResultCallbackDelegate callback,
            bool completeSynchronously,
            string scheduledName,
            ulong uid = 0)
            : this(
                  new RequestDefinitions.RequestMulti(requests, completeSynchronously, scheduledName, MakeUid(uid)),
                  context,
                  callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMulti"/> class.
        /// </summary>
        /// <param name="request">The request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        internal RequestMulti(
            RequestDefinitions.RequestMulti request,
            object context,
            OpsResultCallbackDelegate callback)
            : base(request, BackendRequest.Wrap(request.Requests), context, callback)
        {
        }

        /// <summary>
        /// Gets or sets a string, which if not null makes this command be inserted with the given name (must be unique) into the RingMaster backend scheduler command queue for later background execution
        /// </summary>
        public string ScheduledName
        {
            get
            {
                return this.Request.ScheduledName;
            }

            set
            {
                this.Request.ScheduledName = value;
            }
        }
    }

    /// <summary>
    /// Request to execute a list of <see cref="IRingMasterRequest"/>s as a batch.
    /// </summary>
    public sealed class RequestBatch : BackendCompoundRequest<RequestDefinitions.RequestBatch>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestBatch"/> class.
        /// </summary>
        /// <param name="operations">The operations to execute</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="completeSynchronously">If true, the multi will invoke cb only after the operations have been replicated locally.</param>
        /// <param name="uid">Optional unique id to assign to the request</param>
        public RequestBatch(
            IReadOnlyList<Op> operations,
            object context,
            OpsResultCallbackDelegate callback,
            bool completeSynchronously = false,
            ulong uid = 0)
            : this(
                  new RequestDefinitions.RequestBatch(operations, completeSynchronously, MakeUid(uid)),
                  context,
                  callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestBatch"/> class.
        /// </summary>
        /// <param name="requests">The requests to execute</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="completeSynchronously">If true, the multi will invoke cb only after the operations have been replicated locally.</param>
        /// <param name="uid">Optional unique id to assign to the request</param>
        public RequestBatch(
            IRingMasterBackendRequest[] requests,
            object context,
            OpsResultCallbackDelegate callback,
            bool completeSynchronously = false,
            ulong uid = 0)
            : this(
                  new RequestDefinitions.RequestBatch(requests, completeSynchronously, MakeUid(uid)),
                  context,
                  callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestBatch"/> class.
        /// </summary>
        /// <param name="request">The request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        internal RequestBatch(
            RequestDefinitions.RequestBatch request,
            object context,
            OpsResultCallbackDelegate callback)
            : base(request, BackendRequest.Wrap(request.Requests), context, callback)
        {
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            RequestBatch other = obj as RequestBatch;
            if (other == null)
            {
                return false;
            }

            return base.Equals(obj);
        }
    }

    /// <summary>
    /// Base class for <see cref="IRingMasterRequest"/>s that are composed of other requests.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request</typeparam>
    public class BackendCompoundRequest<TRequest> : BackendRequestWithContext<TRequest, IReadOnlyList<OpResult>>, IRingMasterBackendCompondRequest
        where TRequest : RequestDefinitions.AbstractRingMasterCompoundRequest
    {
        private readonly OpsResultCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackendCompoundRequest{R}"/> class.
        /// </summary>
        /// <param name="request">The compound request to wrap</param>
        /// <param name="wrappedRequests">The wrapped requests</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public BackendCompoundRequest(
            TRequest request,
            IReadOnlyList<IRingMasterBackendRequest> wrappedRequests,
            object context,
            OpsResultCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
            this.Requests = wrappedRequests;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this operation needs to be completed synchronously (i.e. the server must ensure durability before returning)
        /// </summary>
        public bool CompleteSynchronously
        {
            get
            {
                return this.Request.CompleteSynchronously;
            }

            set
            {
                this.Request.CompleteSynchronously = value;
            }
        }

        /// <summary>
        ///  Gets the requests associated with this compound request.
        /// </summary>
        public IReadOnlyList<IRingMasterBackendRequest> Requests { get; }

        protected override void NotifyComplete(int resultCode, IReadOnlyList<OpResult> result, IStat stat)
        {
            this.callback?.Invoke(resultCode, result, this.Context);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            hash ^= this.Requests.Count.GetHashCode();

            foreach (IRingMasterBackendRequest t in this.Requests)
            {
                hash ^= t.GetHashCode();
            }

            if (this.Context != null)
            {
                hash ^= this.Context.GetHashCode();
            }

            hash ^= this.CompleteSynchronously.GetHashCode();

            return hash;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            BackendCompoundRequest<TRequest> other = obj as BackendCompoundRequest<TRequest>;

            if (this.CompleteSynchronously != other?.CompleteSynchronously)
            {
                return false;
            }

            if (this.Requests == other.Requests)
            {
                return true;
            }

            if (this.Requests == null || other.Requests == null)
            {
                return false;
            }

            if (this.Requests.Count != other.Requests.Count)
            {
                return false;
            }

            if (!base.Equals(obj))
            {
                return false;
            }

            return this.Requests.SequenceEqual(other.Requests);
        }
    }

    /// <summary>
    /// Class NoType. This class cannot be inherited.
    /// </summary>
    public sealed class NoType
    {
    }

    /// <summary>
    /// Baseclass for requests which do not return a result.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request</typeparam>
    public abstract class BackendRequestNoResult<TRequest> : BackendRequestWithContext<TRequest, NoType>
        where TRequest : class, IRingMasterRequest
    {
        private readonly VoidCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackendRequestNoResult{TRequest}"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        protected BackendRequestNoResult(TRequest request, object context, VoidCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        public abstract int Version { get; }

        /// <summary>
        /// Notifies the complete.
        /// </summary>
        /// <param name="resultCode">The result code.</param>
        /// <param name="ignore">The ignore.</param>
        /// <param name="stat">The stat.</param>
        protected override void NotifyComplete(int resultCode, NoType ignore, IStat stat)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            hash ^= this.Version.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            BackendRequestNoResult<TRequest> other = obj as BackendRequestNoResult<TRequest>;

            if (this.RequestType != other?.RequestType)
            {
                return false;
            }

            if (!base.Equals(obj))
            {
                return false;
            }

            if (this.Version != other.Version)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Request to delete a node.
    /// </summary>
    public sealed class RequestDelete : BackendRequestNoResult<RequestDefinitions.RequestDelete>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestDelete"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="version">Version of the node must match this value for delete to succeed</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="cascade">If <c>true</c>, delete will recursively delete child nodes</param>
        /// <param name="uid">Unique Id to assign to the request</param>
        public RequestDelete(string path, object context, int version, VoidCallbackDelegate callback, bool cascade, ulong uid = 0)
            : this(path, context, version, callback, cascade ? DeleteMode.CascadeDelete : DeleteMode.None, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestDelete"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="version">Version of the node must match this value for delete to succeed</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="deletemode">Delete options for the operation</param>
        /// <param name="uid">Unique Id to assign to the request</param>
        public RequestDelete(string path, object context, int version, VoidCallbackDelegate callback, DeleteMode deletemode = DeleteMode.None, ulong uid = 0)
            : this(new RequestDefinitions.RequestDelete(path, version, deletemode, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestDelete"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestDelete(RequestDefinitions.RequestDelete request, object context, VoidCallbackDelegate callback)
            : base(request, context, callback)
        {
        }

        /// <summary>
        /// Gets a value indicating whether all child nodes will be deleted recursively
        /// </summary>
        public bool IsCascade => this.Request.IsCascade;

        /// <summary>
        /// Gets the delete mode.
        /// </summary>
        public DeleteMode DeleteMode => this.Request.DeleteMode;

        /// <summary>
        /// Gets the expected value of the node version for delete to succeed.
        /// </summary>
        /// <value>The version.</value>
        public override int Version => this.Request.Version;
    }

    /// <summary>
    /// Request to move a node to a new location.
    /// </summary>
    public sealed class RequestMove : BackendRequestWithContext<RequestDefinitions.RequestMove, string>
    {
        private readonly StringCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMove"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="version">Expected version of the node</param>
        /// <param name="pathDst">Path to the node tha twill become the new parent of the moved node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="movemode">Specifies how the node must be moved</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestMove(string path, object context, int version, string pathDst, StringCallbackDelegate callback, MoveMode movemode = MoveMode.None, ulong uid = 0)
            : this(new RequestDefinitions.RequestMove(path, version, pathDst, movemode, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMove"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestMove(RequestDefinitions.RequestMove request, object context, StringCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets a value that specifies how the node will be moved.
        /// </summary>
        public MoveMode MoveMode => this.Request.MoveMode;

        /// <summary>
        /// Gets the expected version of the source node.
        /// </summary>
        public int Version => this.Request.Version;

        /// <summary>
        /// Gets the path to the node that will become the new parent of the moved node.
        /// </summary>
        public string PathDst => this.Request.PathDst;

        protected override void NotifyComplete(int resultCode, string result, IStat stat)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, result);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            hash ^= this.MoveMode.GetHashCode();

            if (this.PathDst != null)
            {
                hash ^= this.PathDst.GetHashCode();
            }

            hash ^= this.Version.GetHashCode();

            if (this.Context != null)
            {
                hash ^= this.Context.GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            RequestMove other = obj as RequestMove;

            if (other == null)
            {
                return false;
            }

            if (!base.Equals(obj))
            {
                return false;
            }

            if (this.MoveMode != other.MoveMode)
            {
                return false;
            }

            if (this.Version != other.Version)
            {
                return false;
            }

            if (!string.Equals(this.Path, other.Path))
            {
                return false;
            }

            return string.Equals(this.PathDst, other.PathDst);
        }
    }

    /// <summary>
    /// Request to check if the Version, CVersion and AVersion on the node are equal to the values
    /// specified in this request.
    /// </summary>
    public sealed class RequestCheck : BackendRequestNoResult<RequestDefinitions.RequestCheck>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCheck"/> class.
        /// </summary>
        /// <param name="path">Path of the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="version">Expected value of the node's data version</param>
        /// <param name="cversion">Expected value of the node's children version</param>
        /// <param name="aversion">Expected value of the node's <see cref="Acl"/> version</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestCheck(string path, object context, int version, int cversion, int aversion, VoidCallbackDelegate callback, ulong uid = 0)
            : this(path, context, version, cversion, aversion, Guid.Empty, RequestDefinitions.RequestCheck.UniqueIncarnationIdType.None, callback, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCheck"/> class.
        /// </summary>
        /// <param name="path">Path of the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="version">Expected value of the node's data version</param>
        /// <param name="uniqueIncarnation">Expected value of the unique incarnation id</param>
        /// <param name="uniqueIncarnationIdKind">Expected kind of unique incarnation id</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestCheck(
            string path,
            object context,
            int version,
            Guid uniqueIncarnation,
            RequestDefinitions.RequestCheck.UniqueIncarnationIdType uniqueIncarnationIdKind,
            VoidCallbackDelegate callback,
            ulong uid = 0)
            : this(path, context, version, -1, -1, uniqueIncarnation, uniqueIncarnationIdKind, callback, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCheck"/> class.
        /// </summary>
        /// <param name="path">Path of the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="version">Expected value of the node's data version</param>
        /// <param name="cversion">Expected value of the node's children version</param>
        /// <param name="aversion">Expected value of the node's <see cref="Acl"/> version</param>
        /// <param name="uniqueIncarnation">Expected value of the unique incarnation id</param>
        /// <param name="uniqueIncarnationIdKind">Expected kind of unique incarnation id</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestCheck(
            string path,
            object context,
            int version,
            int cversion,
            int aversion,
            Guid uniqueIncarnation,
            RequestDefinitions.RequestCheck.UniqueIncarnationIdType uniqueIncarnationIdKind,
            VoidCallbackDelegate callback,
            ulong uid = 0)
            : this(new RequestDefinitions.RequestCheck(path, version, cversion, aversion, uniqueIncarnation, uniqueIncarnationIdKind, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCheck"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestCheck(
            RequestDefinitions.RequestCheck request,
            object context,
            VoidCallbackDelegate callback)
            : base(request, context, callback)
        {
        }

        /// <summary>
        /// Gets or sets the expected unique incarnation Id.
        /// </summary>
        public Guid UniqueIncarnationId => this.Request.UniqueIncarnationId;

        /// <summary>
        /// Gets the type of unique incarnation id.
        /// </summary>
        public RequestDefinitions.RequestCheck.UniqueIncarnationIdType UniqueIncarnationIdKind => this.Request.UniqueIncarnationIdKind;

        /// <summary>
        /// Gets the expected data version.
        /// </summary>
        public override int Version => this.Request.Version;

        /// <summary>
        /// Gets the expected children version.
        /// </summary>
        public int CVersion => this.Request.CVersion;

        /// <summary>
        /// Gets the expected <see cref="Acl"/> version.
        /// </summary>
        public int AVersion => this.Request.AVersion;
    }

    /// <summary>
    /// Request to sync a node.
    /// </summary>
    public sealed class RequestSync : BackendRequestWithContext<RequestDefinitions.RequestSync, NoType>
    {
        private readonly VoidCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSync"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback that must be invoked when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestSync(string path, object context, VoidCallbackDelegate callback, ulong uid = 0)
            : this(new RequestDefinitions.RequestSync(path, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSync"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback that must be invoked when the request is completed</param>
        public RequestSync(RequestDefinitions.RequestSync request, object context, VoidCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        protected override void NotifyComplete(int resultCode, NoType ignore, IStat stat)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context);
        }
    }

    /// <summary>
    /// Request to check whether a node exists.
    /// </summary>
    public sealed class RequestExists : BackendRequestWithContext<RequestDefinitions.RequestExists, IStat>
    {
        private readonly StatCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestExists"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="watcher">Optional watcher to set on the node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestExists(string path, object context, IWatcher watcher, StatCallbackDelegate callback, ulong uid = 0)
            : this(new RequestDefinitions.RequestExists(path, watcher, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestExists"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestExists(RequestDefinitions.RequestExists request, object context, StatCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets or sets the watcher that will be set on the node.
        /// </summary>
        public IWatcher Watcher
        {
            get
            {
                return this.Request.Watcher;
            }

            set
            {
                this.Request.Watcher = value;
            }
        }

        protected override void NotifyComplete(int resultCode, IStat result, IStat stat)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, result);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            if (this.Watcher != null)
            {
                hash ^= this.Watcher.GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            RequestExists other = obj as RequestExists;
            if (other == null)
            {
                return false;
            }

            if (!base.Equals(obj))
            {
                return false;
            }

            return EqualityHelper.Equals(this.Watcher, other.Watcher);
        }
    }

    /// <summary>
    /// Request to get the <see cref="Acl"/>s associated with a node.
    /// </summary>
    public sealed class RequestGetAcl : BackendRequestWithContext<RequestDefinitions.RequestGetAcl, IReadOnlyList<Acl>>
    {
        private readonly AclCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetAcl"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="stat">Expected <see cref="IStat"/> associated with the node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestGetAcl(string path, object context, IStat stat, AclCallbackDelegate callback, ulong uid = 0)
            : this(new RequestDefinitions.RequestGetAcl(path, stat, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetAcl"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestGetAcl(RequestDefinitions.RequestGetAcl request, object context, AclCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets the expected <see cref="IStat"/> associated with the node.
        /// </summary>
        /// <value>The stat.</value>
        public IStat Stat => this.Request.Stat;

        protected override void NotifyComplete(int resultCode, IReadOnlyList<Acl> result, IStat stat)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, result, stat);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            if (this.Stat != null)
            {
                hash ^= this.Stat.GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            RequestGetAcl other = obj as RequestGetAcl;
            if (other == null)
            {
                return false;
            }

            if (!base.Equals(obj))
            {
                return false;
            }

            return EqualityHelper.Equals(this.Stat, other.Stat);
        }
    }

    /// <summary>
    /// Request to change the data associated with a node.
    /// </summary>
    public sealed class RequestSetData : BackendRequestWithContext<RequestDefinitions.RequestSetData, NoType>
    {
        private readonly StatCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetData"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="data">Data to set on the node</param>
        /// <param name="version">Expected version of data on the node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="dataCommand">Indicates whether the data is an encoded command</param>
        /// <param name="uid">Unique Id to assign to the request</param>
        public RequestSetData(string path, object context, byte[] data, int version, StatCallbackDelegate callback, bool dataCommand = false,
            ulong uid = 0)
            : this(new RequestDefinitions.RequestSetData(path, data, version, dataCommand, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetData"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestSetData(RequestDefinitions.RequestSetData request, object context, StatCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets the data that must be set on the node.
        /// </summary>
        public byte[] Data => this.Request.Data;

        /// <summary>
        /// Gets the expected version of data on the node.
        /// </summary>
        public int Version => this.Request.Version;

        /// <summary>
        /// Gets a value indicating whether the contents are encoded commands that specify how the node's data must be manipulated.
        /// </summary>
        public bool IsDataCommand => this.Request.IsDataCommand;

        protected override void NotifyComplete(int resultCode, NoType ign, IStat stat)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, stat);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            hash ^= this.Version.GetHashCode();

            hash ^= this.IsDataCommand.GetHashCode();

            if (this.Data != null)
            {
                int arrayhash = this.Data.Length;

                for (int i = 0; i < this.Data.Length; i++)
                {
                    hash ^= this.Data[i].GetHashCode() << (i % 32);
                }

                hash ^= arrayhash;
            }

            return hash;
        }

        public override bool DataEquals(IRingMasterBackendRequest obj)
        {
            RequestSetData other = obj as RequestSetData;

            if (this.Version != other?.Version)
            {
                return false;
            }

            if (this.IsDataCommand != other.IsDataCommand)
            {
                return false;
            }

            if (!base.DataEquals(other))
            {
                return false;
            }

            return EqualityHelper.Equals(this.Data, other.Data);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return this.DataEquals(obj as IRingMasterBackendRequest);
        }
    }

    /// <summary>
    /// Request to set a list of <see cref="Acl"/>s on the node.
    /// </summary>
    public sealed class RequestSetAcl : BackendRequestWithContext<RequestDefinitions.RequestSetAcl, NoType>
    {
        private readonly StatCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetAcl"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="acl">List of <see cref="Acl"/>s that must be set on the nodes</param>
        /// <param name="version">Expected value of the <c>Aversion</c> on the node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestSetAcl(string path, object context, IReadOnlyList<Acl> acl, int version, StatCallbackDelegate callback, ulong uid = 0)
            : this(new RequestDefinitions.RequestSetAcl(path, acl, version, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetAcl"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestSetAcl(RequestDefinitions.RequestSetAcl request, object context, StatCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets the list of <see cref="Acl"/>s that will be set on the node.
        /// </summary>
        public IReadOnlyList<Acl> Acl => this.Request.Acl;

        /// <summary>
        /// Gets the expected value of the node's <c>Aversion</c>.
        /// </summary>
        public int Version => this.Request.Version;

        protected override void NotifyComplete(int resultCode, NoType ign, IStat stat)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, stat);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            hash ^= this.Version.GetHashCode();

            if (this.Acl != null)
            {
                int arrayhash = this.Acl.Count;

                for (int i = 0; i < this.Acl.Count; i++)
                {
                    hash ^= this.Acl[i].GetHashCode() << (i % 32);
                }

                hash ^= arrayhash;
            }

            return hash;
        }

        public override bool DataEquals(IRingMasterBackendRequest obj)
        {
            RequestSetAcl other = obj as RequestSetAcl;

            if (other == null)
            {
                return false;
            }

            if (!base.DataEquals(other))
            {
                return false;
            }

            if (this.Version != other.Version)
            {
                return false;
            }

            return EqualityHelper.Equals(this.Acl, other.Acl);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return this.DataEquals(obj as IRingMasterBackendRequest);
        }
    }

    /// <summary>
    /// Request to get the data associated with node.
    /// </summary>
    public sealed class RequestGetData : BackendRequestWithContext<RequestDefinitions.RequestGetData, byte[]>
    {
        private readonly DataCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetData"/> class.
        /// </summary>
        /// <param name="path">Path to the node.</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="watcher"><see cref="IWatcher"/> to associate with the node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestGetData(string path, object context, IWatcher watcher, DataCallbackDelegate callback, ulong uid = 0)
            : this(path, GetDataOptions.None, context, watcher, callback, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetData"/> class.
        /// </summary>
        /// <param name="path">Path to the node.</param>
        /// <param name="faultbackOnParentData">if true, if the requested path doesnt exist, it will return the data for the first ancestor with some non-null data</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="watcher"><see cref="IWatcher"/> to associate with the node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestGetData(string path, bool faultbackOnParentData, object context, IWatcher watcher, DataCallbackDelegate callback, ulong uid = 0)
            : this(path, faultbackOnParentData ? GetDataOptions.FaultbackOnParentData : GetDataOptions.None, context, watcher, callback, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetData"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="options">the options for this request</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="watcher"><see cref="IWatcher"/> to associate with the node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestGetData(string path, GetDataOptions options, object context, IWatcher watcher, DataCallbackDelegate callback, ulong uid = 0)
            : this(path, options, null, context, watcher, callback, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetData"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="options">the options for this request</param>
        /// <param name="optionArgument">Arguments for the option</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="watcher"><see cref="IWatcher"/> to associate with the node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestGetData(string path, GetDataOptions options, IGetDataOptionArgument optionArgument, object context, IWatcher watcher, DataCallbackDelegate callback, ulong uid = 0)
            : this(new RequestDefinitions.RequestGetData(path, options, optionArgument, watcher, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetData"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestGetData(RequestDefinitions.RequestGetData request, object context, DataCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets or sets the watcher that will be set on the node.
        /// </summary>
        public IWatcher Watcher
        {
            get
            {
                return this.Request.Watcher;
            }

            set
            {
                this.Request.Watcher = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether in the case the requested path not existing, this request will return the data
        /// associated with the first ancestor of the given path that exists and has non-null data on it.
        /// </summary>
        public bool FaultbackOnParentData => this.Request.FaultbackOnParentData;

        /// <summary>
        /// Gets a value indicating whether the result will not contain a stat.
        /// </summary>
        public bool NoStatRequired => this.Request.NoStatRequired;

        /// <summary>
        /// Gets a value indicating whether the path in this request is literal, meaning the wildcards in tree should be ignored.
        /// </summary>
        public bool NoWildcardsForPath => this.Request.NoWildcardsForPath;

        /// <summary>
        /// Gets all options that have been specified for this request
        /// </summary>
        public GetDataOptions Options => this.Request.Options;

        /// <summary>
        /// Gets argument for the specified option (if any).
        /// </summary>
        public IGetDataOptionArgument OptionArgument => this.Request.OptionArgument;

        protected override void NotifyComplete(int resultCode, byte[] data, IStat stat)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, data, stat);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            if (this.Watcher != null)
            {
                hash ^= this.Watcher.GetHashCode();
            }

            return hash;
        }

        public override bool DataEquals(IRingMasterBackendRequest obj)
        {
            RequestGetData other = obj as RequestGetData;

            if (other == null)
            {
                return false;
            }

            if (!base.DataEquals(other))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            RequestGetData other = obj as RequestGetData;

            if (other == null)
            {
                return false;
            }

            if (!this.DataEquals(other))
            {
                return false;
            }

            if (this.Watcher != other.Watcher)
            {
                return false;
            }

            if (this.Options != other.Options)
            {
                return false;
            }

            return EqualityHelper.Equals(this.Watcher, other.Watcher);
        }
    }

    /// <summary>
    /// Request to get the names of the children of a node.
    /// </summary>
    public sealed class RequestGetChildren : BackendRequestWithContext<RequestDefinitions.RequestGetChildren, IReadOnlyList<string>>
    {
        private readonly Children2CallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetChildren"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="watcher">Optional <see cref="IWatcher"/> to set on the node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="retrievalCondition">Retrieval conditions</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestGetChildren(string path, object context, IWatcher watcher, ChildrenCallbackDelegate callback, string retrievalCondition = null, ulong uid = 0)
            : this(path, context, watcher, WrapCallback(callback), retrievalCondition, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetChildren"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="watcher">Optional <see cref="IWatcher"/> to set on the node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="retrievalCondition">Retrieval conditions</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestGetChildren(string path, object context, IWatcher watcher, Children2CallbackDelegate callback, string retrievalCondition = null, ulong uid = 0)
            : this(new RequestDefinitions.RequestGetChildren(path, watcher, retrievalCondition, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetChildren"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestGetChildren(RequestDefinitions.RequestGetChildren request, object context, Children2CallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets or sets the watcher that will be set on the node.
        /// </summary>
        public IWatcher Watcher
        {
            get
            {
                return this.Request.Watcher;
            }

            set
            {
                this.Request.Watcher = value;
            }
        }

        /// <summary>
        /// Gets the retrieval condition definition for the names of the children to retrieve
        /// </summary>
        /// <remarks>
        /// <c> Retrieval condition is in the form >:[top]:[startingChildName].
        /// valid interval definitions:
        ///
        ///   ">:[Top]:[ChildName]"     ... returns the elements greater than the [ChildName] limited to Top count
        ///                                 so ">:1000:contoso" means give me first 1000 childrens greater than contoso
        ///                                 so ">::contoso"     means give me all childrens greater than contoso
        ///                                 so ">:1000:"        means give me first 1000 elements
        /// </c>
        /// </remarks>
        public string RetrievalCondition => this.Request.RetrievalCondition;

        /// <summary>
        /// Wraps the callback.
        /// </summary>
        /// <param name="callback">Callback to wrap</param>
        /// <returns>Wrapped callback</returns>
        private static Children2CallbackDelegate WrapCallback(ChildrenCallbackDelegate callback)
        {
            if (callback == null)
            {
                return null;
            }

            return (int rc, string path, object ctx, IReadOnlyList<string> children, IStat stat) =>
                {
                    callback(rc, path, ctx, children);
                };
        }

        protected override void NotifyComplete(int resultCode, IReadOnlyList<string> result, IStat stat)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, result, stat);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            if (this.Watcher != null)
            {
                hash ^= this.Watcher.GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// Datas the equals.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns><c>true</c> if objects are equal (including data), <c>false</c> otherwise.</returns>
        public override bool DataEquals(IRingMasterBackendRequest obj)
        {
            RequestGetChildren other = obj as RequestGetChildren;

            if (other == null)
            {
                return false;
            }

            if (!base.DataEquals(other))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            RequestGetChildren other = obj as RequestGetChildren;

            if (other == null)
            {
                return false;
            }

            if (!this.DataEquals(other))
            {
                return false;
            }

            if (!string.Equals(this.RetrievalCondition, other.RetrievalCondition))
            {
                return false;
            }

            if (this.Watcher == other.Watcher)
            {
                return true;
            }

            return EqualityHelper.Equals(this.Watcher, other.Watcher);
        }
    }

    internal static class BackendRequest
    {
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
            }

            throw new InvalidOperationException();
        }

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
