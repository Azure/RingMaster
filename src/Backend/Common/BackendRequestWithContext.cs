// <copyright file="BackendRequestWithContext.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    using Code = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.RingMasterException.Code;
    using IRingMasterRequest = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.IRingMasterRequest;

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

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override sealed void InvokeCallback(int resultCode, object result, IStat stat, string responsePath)
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
                    this.NotifyComplete((int)Code.Marshallingerror, default(TReturn), stat, responsePath);
                    return;
                }
            }

            this.NotifyComplete(resultCode, resultAsT, stat, responsePath);
        }

        /// <summary>
        /// Notifies that the request has been completed.
        /// </summary>
        /// <param name="resultCode">result code</param>
        /// <param name="result">The result</param>
        /// <param name="stat">The stat</param>
        /// <param name="responsePath">The response path</param>
        protected abstract void NotifyComplete(int resultCode, TReturn result, IStat stat, string responsePath);
    }
}