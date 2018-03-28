// <copyright file="RequestSetAuth.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Request to set credentials for a session.
    /// </summary>
    public sealed class RequestSetAuth : BackendRequest<RequestDefinitions.RequestSetAuth>
    {
        private readonly VoidCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetAuth"/> class.
        /// </summary>
        /// <param name="clientId">client identifier.</param>
        /// <param name="cb">callback that must be invoked when this request is completed</param>
        public RequestSetAuth(string clientId, VoidCallbackDelegate cb)
            : this(new RequestDefinitions.RequestSetAuth(clientId), cb)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetAuth"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestSetAuth(RequestDefinitions.RequestSetAuth request, VoidCallbackDelegate callback)
            : base(request)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets the client id.
        /// </summary>
        public string ClientId => this.Request.ClientId;

        /// <inheritdoc />
        public override bool DataEquals(IRingMasterBackendRequest obj)
        {
            RequestSetAuth other = obj as RequestSetAuth;
            if (other == null)
            {
                return false;
            }

            // note we don't need to validate the request type because the previous check covers us on that
            return string.Equals(this.ClientId, other.ClientId);
        }

        /// <inheritdoc />
        protected override void InvokeCallback(int resultCode, object result, IStat stat)
        {
            this.callback(resultCode, null, result);
        }
    }
}