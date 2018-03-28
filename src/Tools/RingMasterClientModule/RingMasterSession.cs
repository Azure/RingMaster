// <copyright file="RingMasterSession.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Represents a connection to RingMaster.
    /// </summary>
    public sealed class RingMasterSession : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterSession" /> class.
        /// </summary>
        /// <param name="serverSpec">Server spec</param>
        /// <param name="configuration">Client configuration</param>
        internal RingMasterSession(
            RingMasterClient.ServerSpec serverSpec,
            RingMasterClient.Configuration configuration)
        {
            this.ServerSpec = serverSpec;
            this.Configuration = configuration;
            this.Client = new RingMasterClient(serverSpec, configuration, instrumentation: null, cancellationToken: CancellationToken.None);
        }

        /// <summary>
        /// Gets the RingMaster auth id.
        /// </summary>
        public Id Id { get; internal set; }

        /// <summary>
        /// Gets the RingMaster Server Specification.
        /// </summary>
        public RingMasterClient.ServerSpec ServerSpec { get; private set; }

        /// <summary>
        /// Gets the RingMaster client configuration.
        /// </summary>
        public RingMasterClient.Configuration Configuration { get; private set; }

        /// <summary>
        /// Gets the RingMaster client.
        /// </summary>
        internal RingMasterClient Client { get; private set; }

        /// <summary>
        /// Dispose this session.
        /// </summary>
        public void Dispose()
        {
            this.Client.Dispose();
        }
    }
}
