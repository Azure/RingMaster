// <copyright file="ConnectRingMaster.cs" company="Microsoft">
//     Copyright ©  2018
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
        /// Gets or sets the RingMaster connection string.
        /// </summary>
        [Parameter]
        public string ConnectionString { get; set; } = "127.0.0.1:99";

        /// <summary>
        /// Gets or sets the thumbprint of the client certificate to use to connect to ringmaster.
        /// </summary>
        [Parameter]
        public string ClientCertificateThumbprint { get; set; }

        /// <summary>
        /// Gets or sets the thumbprints of server certificates that can be accepted.
        /// </summary>
        [Parameter]
        public string[] AcceptedServerCertificateThumbprints { get; set; }

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
            bool mustUseSecureConnection = (this.ClientCertificateThumbprint != null)
                && (this.AcceptedServerCertificateThumbprints != null)
                && (this.AcceptedServerCertificateThumbprints.Length > 0);

            var serverSpec = new RingMasterClient.ServerSpec
            {
                Endpoints = SecureTransport.ParseConnectionString(this.ConnectionString),
                UseSecureConnection = mustUseSecureConnection
            };

            if (serverSpec.UseSecureConnection)
            {
                string[] clientCerts = new string[] { this.ClientCertificateThumbprint };
                serverSpec.ClientCertificate = SecureTransport.GetCertificatesFromThumbPrintOrFileName(clientCerts)[0];
                serverSpec.AcceptedServerCertificates = SecureTransport.GetCertificatesFromThumbPrintOrFileName(this.AcceptedServerCertificateThumbprints);
            }

            var configuration = new RingMasterClient.Configuration
            {
                DefaultTimeout = this.DefaultTimeout,
                HeartBeatInterval = this.HeartBeatInterval,
                RequestQueueLength = this.RequestQueueLength
            };

            this.WriteObject(new RingMasterSession(serverSpec, configuration));
        }
    }
}
