// <copyright file="SessionAuth.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;

    /// <summary>
    /// Session authentication data
    /// </summary>
    public class SessionAuth : ISessionAuth
    {
        /// <summary>
        /// Digest Id of the client.
        /// </summary>
        private string clientDigest;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionAuth"/> class with default values.
        /// </summary>
        public SessionAuth()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionAuth"/> class.
        /// </summary>
        /// <param name="clientIP">IP Address of the client</param>
        /// <param name="clientIdentity">Identity of the client</param>
        /// <param name="clientDigest">Digest Id of the client</param>
        /// <param name="isSuperSession">Value indicating whether this is a super user session</param>
        public SessionAuth(string clientIP, string clientIdentity, string clientDigest, bool isSuperSession)
        {
            this.ClientIP = clientIP;
            this.ClientIdentity = clientIdentity;
            this.ClientDigest = clientDigest;
            this.IsSuperSession = isSuperSession;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionAuth"/> class from another instance.
        /// </summary>
        /// <param name="auth">Instance to clone</param>
        public SessionAuth(ISessionAuth auth)
        {
            if (auth == null)
            {
                throw new ArgumentNullException("auth");
            }

            this.IsSuperSession = auth.IsSuperSession;
            this.ClientDigest = auth.ClientDigest;
            this.ClientIdentity = auth.ClientIdentity;
            this.ClientIP = auth.ClientIP;
        }

        /// <summary>
        /// Gets or sets the IP of the client
        /// </summary>
        public string ClientIP { get; set; }

        /// <summary>
        /// Gets or sets the Identity of the client
        /// </summary>
        public string ClientIdentity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this session is a supersession
        /// </summary>
        public bool IsSuperSession { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this session is lock-free even for write operations.
        /// </summary>
        public bool IsLockFreeSession { get; set; }

        /// <summary>
        /// Gets or sets the Digest Id of the client on this session
        /// </summary>
        public string ClientDigest
        {
            get
            {
                return this.clientDigest;
            }

            set
            {
                this.clientDigest = value;
                this.IsSuperSession = this.clientDigest == "digest:root";
            }
        }

        /// <summary>
        /// Clone a <see cref="ISessionAuth"/> object.
        /// </summary>
        /// <param name="auth"><see cref="ISessionAuth"/> to clone</param>
        /// <returns>Cloned object</returns>
        public static SessionAuth Clone(ISessionAuth auth)
        {
            if (auth == null)
            {
                return null;
            }

            if (auth.ClientIP == null && auth.ClientIdentity == null && !auth.IsSuperSession)
            {
                return null;
            }

            return new SessionAuth(auth);
        }

        /// <summary>
        /// Gets a string representation of <see cref="SessionAuth"/>
        /// </summary>
        /// <returns>String representation of <see cref="SessionAuth"/></returns>
        public override string ToString()
        {
            return string.Format("[SessionAuth: IsSuperSession={0} ClientIP={1} ClientIdentity={2} ClientDigest={3}]", this.IsSuperSession, this.ClientIP, this.ClientIdentity, this.ClientDigest);
        }
    }
}
