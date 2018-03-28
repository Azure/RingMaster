// <copyright file="BackendRequestNoResult.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using IRingMasterRequest = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.IRingMasterRequest;

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

        /// <summary>
        /// Gets the version of the request
        /// </summary>
        public abstract int Version { get; }

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
    }
}