// <copyright file="BackendCompoundRequest.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using IRingMasterRequest = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.IRingMasterRequest;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

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

        /// <inheritdoc />
        protected override void NotifyComplete(int resultCode, IReadOnlyList<OpResult> result, IStat stat, string responsePath)
        {
            this.callback?.Invoke(resultCode, result, this.Context);
        }
    }
}