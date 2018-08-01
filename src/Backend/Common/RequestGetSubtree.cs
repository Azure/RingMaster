// <copyright file="RequestGetSubtree.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using GetSubtreeOptions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetSubtree.GetSubtreeOptions;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Request to get the data of the subtree under a node.
    /// </summary>
    public sealed class RequestGetSubtree : BackendRequestWithContext<RequestDefinitions.RequestGetSubtree, byte[]>
    {
        private readonly SubtreeDataCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetSubtree"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="retievalCondition">Retrieval condition for this request</param>
        /// <param name="options">Requset options</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestGetSubtree(string path, string retievalCondition, GetSubtreeOptions options, object context, SubtreeDataCallbackDelegate callback, ulong uid = 0)
            : this(new RequestDefinitions.RequestGetSubtree(path, retievalCondition, options, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestGetSubtree"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestGetSubtree(RequestDefinitions.RequestGetSubtree request, object context, SubtreeDataCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets a value indicating whether the result should contain stats.
        /// </summary>
        public bool IncludeStats => this.Request.IncludeStats;

        /// <summary>
        /// Gets the options for this request.
        /// </summary>
        public GetSubtreeOptions Options => this.Request.Options;

        /// <summary>
        /// Gets the retrieval condition for this request.
        /// </summary>
        public string RetreivalCondition => this.Request.RetrievalCondition;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            if (this.RetreivalCondition != null)
            {
                hash ^= this.RetreivalCondition.GetHashCode();
            }

            return hash;
        }

        /// <inheritdoc />
        public override bool DataEquals(IRingMasterBackendRequest obj)
        {
            RequestGetSubtree other = obj as RequestGetSubtree;

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
            RequestGetSubtree other = obj as RequestGetSubtree;

            if (other == null)
            {
                return false;
            }

            if (!this.DataEquals(other))
            {
                return false;
            }

            if (this.RetreivalCondition != other.RetreivalCondition)
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        protected override void NotifyComplete(int resultCode, byte[] data, IStat stat, string responsePath)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, data, responsePath);
        }
    }
}
