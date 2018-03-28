// <copyright file="RequestDelete.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Request to delete a node.
    /// </summary>
    public sealed class RequestDelete : BackendRequestNoResult<RequestDefinitions.RequestDelete>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestDelete"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="version">Version of the node must match this value for delete to succeed</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="cascade">If <c>true</c>, delete will recursively delete child nodes</param>
        /// <param name="uid">Unique Id to assign to the request</param>
        public RequestDelete(string path, object context, int version, VoidCallbackDelegate callback, bool cascade, ulong uid = 0)
            : this(path, context, version, callback, cascade ? DeleteMode.CascadeDelete : DeleteMode.None, uid)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestDelete"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="version">Version of the node must match this value for delete to succeed</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        /// <param name="deletemode">Delete options for the operation</param>
        /// <param name="uid">Unique Id to assign to the request</param>
        public RequestDelete(string path, object context, int version, VoidCallbackDelegate callback, DeleteMode deletemode = DeleteMode.None, ulong uid = 0)
            : this(new RequestDefinitions.RequestDelete(path, version, deletemode, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestDelete"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestDelete(RequestDefinitions.RequestDelete request, object context, VoidCallbackDelegate callback)
            : base(request, context, callback)
        {
        }

        /// <summary>
        /// Gets a value indicating whether all child nodes will be deleted recursively
        /// </summary>
        public bool IsCascade => this.Request.IsCascade;

        /// <summary>
        /// Gets the delete mode.
        /// </summary>
        public DeleteMode DeleteMode => this.Request.DeleteMode;

        /// <summary>
        /// Gets the expected value of the node version for delete to succeed.
        /// </summary>
        /// <value>The version.</value>
        public override int Version => this.Request.Version;
    }
}