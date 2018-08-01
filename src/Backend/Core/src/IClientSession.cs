// <copyright file="IClientSession.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using ISessionAuth = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.ISessionAuth;

    /// <summary>
    /// Interface IClientSession
    /// </summary>
    public interface IClientSession
    {
        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        /// <value>The session identifier.</value>
        ulong SessionId { get; set; }

        /// <summary>
        /// Gets or sets the name of the friendly.
        /// </summary>
        /// <value>The name of the friendly.</value>
        string FriendlyName { get; set; }

        /// <summary>
        /// Gets session authentication data
        /// </summary>
        ISessionAuth Auth { get; }
    }
}
