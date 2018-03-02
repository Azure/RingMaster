// <copyright file="IClientSession.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.KeeperException;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

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
        /// session authentication data
        /// </summary>
        ISessionAuth Auth { get; }
    }
}
