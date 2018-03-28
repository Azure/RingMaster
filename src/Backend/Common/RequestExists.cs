// <copyright file="RequestExists.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

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

        /// <inheritdoc />
        protected override void NotifyComplete(int resultCode, IStat result, IStat stat)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, result);
        }
    }
}