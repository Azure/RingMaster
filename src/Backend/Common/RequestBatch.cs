// <copyright file="RequestBatch.cs" company="Microsoft Corporation">
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
}