// <copyright file="NoSslConnection.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// NoSslConnection class implements a <see cref="ISecureConnectionPolicy"/> where connections
    /// are not encrypted.
    /// </summary>
    public class NoSslConnection : ISecureConnectionPolicy
    {
        /// <summary>
        /// Gets the validated stream on server.
        /// </summary>
        /// <param name="client">TCP client</param>
        /// <param name="timeout">Time to wait for the validation</param>
        /// <param name="cancellationToken">Token to be observed for cancellation signal</param>
        /// <returns>A Task that resolves to the validated stream</returns>
        public Task<Stream> AuthenticateAsServer(TcpClient client, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            return Task.FromResult<Stream>(client.GetStream());
        }

        /// <summary>
        /// Gets the validated stream on client.
        /// </summary>
        /// <param name="serverName">Name of the server.</param>
        /// <param name="client">TCP client</param>
        /// <param name="timeout">Time to wait for the validation</param>
        /// <param name="cancellationToken">Token to be observed for cancellation signal</param>
        /// <returns>A Task that resolves to the validated stream</returns>
        public Task<Stream> AuthenticateAsClient(string serverName, TcpClient client, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            return Task.FromResult<Stream>(client.GetStream());
        }
    }
}
