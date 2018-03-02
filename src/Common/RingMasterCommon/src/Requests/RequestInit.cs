// <copyright file="RequestInit.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Request to initialize a session established by a client.
    /// </summary>
    public class RequestInit : AbstractRingMasterRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestInit"/> class.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="sessionPwd">The session password.</param>
        /// <param name="readOnlyInterfaceRequiresLocks"> if true (default) the session requires read operations to use locks. if false, reads will be lock free and <c>ApiError</c> may be returned upon concurrency issues</param>
        /// <param name="redirection">the redirection policy for this session</param>
        public RequestInit(ulong sessionId, string sessionPwd, bool readOnlyInterfaceRequiresLocks = true, RedirectionPolicy redirection = RedirectionPolicy.ServerDefault)
            : base(RingMasterRequestType.Init, string.Empty, 0)
        {
            this.SessionId = sessionId;
            this.SessionPwd = sessionPwd;
            this.ROInterfaceRequiresLocks = readOnlyInterfaceRequiresLocks;
            this.Redirection = redirection;
        }

        /// <summary>
        /// Policy that specifies how the server must handle requests when it is not a primary.
        /// </summary>
        public enum RedirectionPolicy : byte
        {
            /// <summary>
            /// Use the default policy of the server.
            /// </summary>
            ServerDefault = 0,

            /// <summary>
            /// If the server is not the primary, respond with an error message and indicate the current
            /// primary if that information is available.
            /// </summary>
            RedirectPreferred,

            /// <summary>
            /// Transparently forward requests to the current primary.
            /// </summary>
            ForwardPreferred
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
        /// Gets a value indicating whether the session requests for read only operations to be lock-free, meaning <c>ApiError</c> may be thrown if race conditions happen during read only operations.
        /// </summary>
        public bool ROInterfaceRequiresLocks { get; private set; }

        /// <summary>
        /// Gets the policy for redirection to primary/master
        /// </summary>
        public RedirectionPolicy Redirection { get; private set; }

        /// <summary>
        /// Gets the session identifier.
        /// </summary>
        /// <value>The session identifier.</value>
        public ulong SessionId { get; private set; }

        /// <summary>
        /// Gets the session password.
        /// </summary>
        /// <value>The session password.</value>
        public string SessionPwd { get; private set; }

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
