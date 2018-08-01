// <copyright file="RequestSync.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Request to sync a node.
    /// </summary>
    public sealed class RequestSync : BackendRequestWithContext<RequestDefinitions.RequestSync, NoType>
    {
        private readonly VoidCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSync"/> class.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback that must be invoked when the request is completed</param>
        /// <param name="uid">Unique id to assign to the request</param>
        public RequestSync(string path, object context, VoidCallbackDelegate callback, ulong uid = 0)
            : this(new RequestDefinitions.RequestSync(path, MakeUid(uid)), context, callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSync"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="context">Context associated with the request</param>
        /// <param name="callback">Callback that must be invoked when the request is completed</param>
        public RequestSync(RequestDefinitions.RequestSync request, object context, VoidCallbackDelegate callback)
            : base(request, context)
        {
            this.callback = callback;
        }

        /// <inheritdoc />
        protected override void NotifyComplete(int resultCode, NoType ignore, IStat stat, string responsePath)
        {
            this.callback?.Invoke(resultCode, this.Path, this.Context);
        }
    }
}