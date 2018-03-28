// <copyright file="NoSslWrapping.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;

    /// <summary>
    /// Class NoSslWrapping.
    /// </summary>
    public class NoSslWrapping : SslWrapping
    {
        /// <summary>
        /// Gets the validated stream on server.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <returns>Stream.</returns>
        public override Stream GetValidatedStreamOnServer(TcpClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            return client.GetStream();
        }

        /// <summary>
        /// Gets the validated stream on client.
        /// </summary>
        /// <param name="serverName">Name of the server.</param>
        /// <param name="client">The client.</param>
        /// <returns>Stream.</returns>
        public override Stream GetValidatedStreamOnClient(string serverName, TcpClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            return client.GetStream();
        }
    }
}
