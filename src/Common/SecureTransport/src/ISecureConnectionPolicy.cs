// <copyright file="ISecureConnectionPolicy.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface to a secure connection policy.
    /// </summary>
    public interface ISecureConnectionPolicy
    {
        /// <summary>
        /// Authenticates the secure connection as a server
        /// </summary>
        /// <param name="client">Client connection</param>
        /// <param name="timeout">Validation timeout</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stream to send receive data</returns>
        Task<Stream> AuthenticateAsServer(TcpClient client, TimeSpan timeout, CancellationToken cancellationToken);

        /// <summary>
        /// Authenticates the secure connection as a client
        /// </summary>
        /// <param name="serverName">Name of the server</param>
        /// <param name="client">Client connection</param>
        /// <param name="timeout">Validation timeout</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stream to send receive data</returns>
        Task<Stream> AuthenticateAsClient(string serverName, TcpClient client, TimeSpan timeout, CancellationToken cancellationToken);
    }
}
