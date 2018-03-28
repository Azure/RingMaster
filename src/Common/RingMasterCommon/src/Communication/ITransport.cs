// <copyright file="ITransport.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface to the transport layer.
    /// </summary>
    public interface ITransport : IDisposable
    {
        /// <summary>
        /// Gets or sets the callback that must be invoked when a new connection is established.
        /// </summary>
        Action<IConnection> OnNewConnection { get; set; }

        /// <summary>
        /// Gets or sets the Callback that must be invoked to negotiate protocol
        /// </summary>
        ProtocolNegotiatorDelegate OnProtocolNegotiation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the connection should use Network or host byte order
        /// </summary>
        bool UseNetworkByteOrder { get; set; }

        /// <summary>
        /// Gets or sets the callback that must be invoked when a connection is lost.
        /// </summary>
        Action OnConnectionLost { get; set; }

        /// <summary>
        /// Close the transport.
        /// </summary>
        void Close();
    }
}