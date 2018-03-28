// <copyright file="RequestInit.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    using Code = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.RingMasterException.Code;
    using GetDataOptions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.GetDataOptions;
    using IGetDataOptionArgument = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestGetData.IGetDataOptionArgument;
    using IRingMasterRequest = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.IRingMasterRequest;
    using ISessionAuth = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.ISessionAuth;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using RingMasterRequestType = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RingMasterRequestType;

    /// <summary>
    /// Request to initialize a session established by a client.
    /// </summary>
    public sealed class RequestInit : BackendRequest<RequestDefinitions.RequestInit>
    {
        private readonly VoidCallbackDelegate callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestInit"/> class.
        /// </summary>
        /// <param name="sessionId">session identifier.</param>
        /// <param name="sessionPwd">session password.</param>
        /// <param name="callback">The callback to invoke when this request is completed</param>
        /// <param name="rOInterfaceRequiresLocks"> if true (default) the session requires read operations to use locks. if false, reads will be lockfree and ApiError may be returned upon concurrency issues</param>
        /// <param name="redirection">redirection policy for this session</param>
        public RequestInit(
            ulong sessionId,
            string sessionPwd,
            VoidCallbackDelegate callback,
            bool rOInterfaceRequiresLocks,
            RequestDefinitions.RequestInit.RedirectionPolicy redirection)
            : this(new RequestDefinitions.RequestInit(sessionId, sessionPwd, rOInterfaceRequiresLocks, redirection), callback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestInit"/> class.
        /// </summary>
        /// <param name="request">Request to wrap</param>
        /// <param name="callback">Callback to invoke when the request is completed</param>
        public RequestInit(RequestDefinitions.RequestInit request, VoidCallbackDelegate callback)
            : base(request)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Gets a value indicating whether the session requests for read only operations to be lock-free, meaning <c>ApiError</c> may be thrown if race conditions happen during read only operations.
        /// </summary>
        public bool RoInterfaceRequiresLocks => this.Request.ROInterfaceRequiresLocks;

        /// <summary>
        /// Gets the policy for redirection to primary/master
        /// </summary>
        public RequestDefinitions.RequestInit.RedirectionPolicy Redirection => this.Request.Redirection;

        /// <summary>
        /// Gets the session identifier.
        /// </summary>
        public ulong SessionId => this.Request.SessionId;

        /// <summary>
        /// Gets the session password.
        /// </summary>
        public string SessionPwd => this.Request.SessionPwd;

        /// <inheritdoc />
        public override bool DataEquals(IRingMasterBackendRequest obj)
        {
            RequestInit other = obj as RequestInit;

            // note we don't need to validate the request type because the previous check covers us on that
            if (this.SessionId != other?.SessionId)
            {
                return false;
            }

            return string.Equals(this.SessionPwd, other.SessionPwd);
        }

        /// <inheritdoc />
        protected override void InvokeCallback(int resultCode, object result, IStat stat)
        {
            this.callback(resultCode, null, result);
        }
    }
}