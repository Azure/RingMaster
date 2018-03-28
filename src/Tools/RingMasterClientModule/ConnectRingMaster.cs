// <copyright file="ConnectRingMaster.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;

    /// <summary>
    /// Connects to RingMaster.
    /// </summary>
    [Cmdlet(VerbsCommunications.Connect, "RingMaster")]
    public sealed class ConnectRingMaster : Cmdlet
    {
        /// <summary>
        /// Gets or sets details of the ringmaster server to connect to.
        /// </summary>
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public RingMasterClient.ServerSpec ServerSpec { get; set; }

        /// <summary>
        /// Gets or sets the RingMaster connection string.
        /// </summary>
        [Parameter]
        public string ConnectionString { get; set; } = "127.0.0.1:99";

        /// <summary>
        /// Gets or sets the default timeout for requests.
        /// </summary>
        [Parameter]
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMilliseconds(10000);

        /// <summary>
        /// Gets or sets the frequency with which heartbeats are sent.
        /// </summary>
        [Parameter]
        public TimeSpan HeartBeatInterval { get; set; } = TimeSpan.FromMilliseconds(30000);

        /// <summary>
        /// Gets or sets the length of the request queue.
        /// </summary>
        [Parameter]
        public int RequestQueueLength { get; set; } = 1000;

        /// <inheritdoc />
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "RingMasterSession is created by this cmdlet for use by other cmdlets")]
        protected override void ProcessRecord()
        {
            if (this.ServerSpec == null)
            {
                this.ServerSpec = new RingMasterClient.ServerSpec
                {
                    Endpoints = SecureTransport.ParseConnectionString(this.ConnectionString),
                    UseSecureConnection = false,
                };
            }

            var configuration = new RingMasterClient.Configuration
            {
                DefaultTimeout = this.DefaultTimeout,
                HeartBeatInterval = this.HeartBeatInterval,
                RequestQueueLength = this.RequestQueueLength,
            };

            this.WriteObject(new RingMasterSession(this.ServerSpec, configuration));
        }
    }
}
