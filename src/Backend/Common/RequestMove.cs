// <copyright file="RequestMove.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Request to move a node to a new location.
    /// </summary>
    public sealed class RequestMove : BackendRequestWithContext<RequestDefinitions.RequestMove, string>
    {
        private readonly StringCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMove"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="version">Expected version of the node</param>
        /// <param name="pathDst">Path to the node tha twill become the new parent of the moved node</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="movemode">Specifies how the node must be moved</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestMove(string path, object context, int version, string pathDst, StringCallbackDelegate callback, MoveMode movemode = MoveMode.None, ulong uid = 0)
            : this(new RequestDefinitions.RequestMove(path, version, pathDst, movemode, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMove"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestMove(RequestDefinitions.RequestMove request, object context, StringCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets a value that specifies how the node will be moved.
        /// </summary>
        public MoveMode MoveMode => this.Request.MoveMode;

        /// <summary>
        /// Gets the expected version of the source node.
        /// </summary>
        public int Version => this.Request.Version;

        /// <summary>
        /// Gets the path to the node that will become the new parent of the moved node.
        /// </summary>
        public string PathDst => this.Request.PathDst;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            hash ^= this.MoveMode.GetHashCode();

            if (this.PathDst != null)
            {
                hash ^= this.PathDst.GetHashCode();
            }

            hash ^= this.Version.GetHashCode();

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
            RequestMove other = obj as RequestMove;

            if (other == null)
            {
                return false;
            }

            if (!base.Equals(obj))
            {
                return false;
            }

            if (this.MoveMode != other.MoveMode)
            {
                return false;
            }

            if (this.Version != other.Version)
            {
                return false;
            }

            if (!string.Equals(this.Path, other.Path))
            {
                return false;
            }

            return string.Equals(this.PathDst, other.PathDst);
        }

        /// <inheritdoc />
        protected override void NotifyComplete(int resultCode, string result, IStat stat, string responsePath)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context, result);
        }
    }
}