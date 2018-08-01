// <copyright file="RequestGetAcl.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System.Collections.Generic;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

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

        /// <inheritdoc />
        protected override void NotifyComplete(int resultCode, IReadOnlyList<Acl> result, IStat stat, string responsePath)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, result, stat);
        }
    }
}