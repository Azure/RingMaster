// <copyright file="RequestCreate.cs" company="Microsoft Corporation">
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

        /// <summary>
        /// Notifies the complete.
        /// </summary>
        /// <param name="resultCode">The result code.</param>
        /// <param name="result">The result.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="responsePath">The response path.</param>
        protected override void NotifyComplete(int resultCode, string result, IStat stat, string responsePath)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, result);
        }
    }
}