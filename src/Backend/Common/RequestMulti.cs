// <copyright file="RequestMulti.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System.Collections.Generic;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using IRingMasterRequest = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.IRingMasterRequest;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

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
}