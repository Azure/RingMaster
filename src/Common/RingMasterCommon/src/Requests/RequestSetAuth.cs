﻿// <copyright file="RequestSetAuth.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Request to set credentials for a session.
    /// </summary>
    public class RequestSetAuth : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestSetAuth" /> class.
        /// </summary>
        /// <param name="clientId">The client identifier</param>
        /// <param name="uid">The unique request id</param>
        public RequestSetAuth(string clientId, ulong uid = 0)
            : base(RingMasterRequestType.SetAuth, string.Empty, uid)
        {
            this.ClientId = clientId;
        }

        /// <summary>
        /// Gets or sets the authorization credentials to use for this request or <c>null</c> of the session's authorization credentials must be used.
        /// </summary>
        public override ISessionAuth Auth
        {
            get
            {
                return null;
            }

            set
            {
                if (value != null)
                {
                    throw new NotImplementedException("cannot set auth for RequestInit");
                }
            }
        }

        /// <summary>
        /// Gets the client id.
        /// </summary>
        /// <value>The client identifier.</value>
        public string ClientId { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this this request is read only.
        /// </summary>
        /// <returns><c>true</c> because this request does not modify any data</returns>
        public override bool IsReadOnly()
        {
            return false;
        }
    }
}
