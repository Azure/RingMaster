// <copyright file="RequestSetAcl.cs" company="Microsoft Corporation">
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override void NotifyComplete(int resultCode, NoType ign, IStat stat, string responsePath)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, stat);
        }
    }
}