// <copyright file="RequestGetChildren.cs" company="Microsoft Corporation">
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

        /// <inheritdoc />
        protected override void NotifyComplete(int resultCode, IReadOnlyList<string> result, IStat stat)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, result, stat);
        }

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
    }
}