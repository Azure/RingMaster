// <copyright file="NewRingMasterServerSpec.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.ClientModule
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;

    /// <summary>
    /// Creates a new RingMaster ServerSpec.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "RingMasterServerSpec")]
    public sealed class NewRingMasterServerSpec : Cmdlet
    {
        /// <summary>
        /// Gets or sets the RingMaster connection string.
        /// </summary>
        [Parameter]
        public string ConnectionString { get; set; } = "127.0.0.1:99";

        /// <summary>
        /// Gets or sets the thumbprint of the certificate that the client will use to identify itself to the server.
        /// </summary>
        [Parameter]
        public string ClientCertificateThumbprint { get; set; }

        /// <summary>
        /// Gets or sets the list of thumbprints of certificates that the server can present.
        /// </summary>
        [Parameter]
        public string[] AcceptedServerCertificateThumbprints { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether certificate revocation check must be skipped.
        /// </summary>
        [Parameter]
        public SwitchParameter NoCertificateRevocationCheck { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether certificate trust chain check must be skipped.
        /// </summary>
        [Parameter]
        public SwitchParameter NoTrustChainCheck { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the subject allowed for certificate validation A
        /// </summary>
        [Parameter]
        public string CertValidationASubject { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the thumbprints allowed for certificate validation A
        /// </summary>
        [Parameter]
        public string[] CertValidationASigningThumbprints { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the subject allowed for certificate validation B
        /// </summary>
        [Parameter]
        public string CertValidationBSubject { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the thumbprints allowed for certificate validation A
        /// </summary>
        [Parameter]
        public string[] CertValidationBSigningThumbprints { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord()
        {
            var serverSpec = new RingMasterClient.ServerSpec
            {
                Endpoints = SecureTransport.ParseConnectionString(this.ConnectionString),
                UseSecureConnection = true,
                MustCheckCertificateRevocation = !this.NoCertificateRevocationCheck.IsPresent,
                MustCheckCertificateTrustChain = !this.NoTrustChainCheck.IsPresent,
                CertValidationASubject = this.CertValidationASubject,
                CertValidationASigningThumbprints = this.CertValidationASigningThumbprints,
                CertValidationBSubject = this.CertValidationBSubject,
                CertValidationBSigningThumbprints = this.CertValidationBSigningThumbprints,
            };

            string[] clientCerts = new string[] { this.ClientCertificateThumbprint };
            serverSpec.ClientCertificate = SecureTransport.GetCertificatesFromThumbPrintOrFileName(clientCerts)[0];
            serverSpec.AcceptedServerCertificates = SecureTransport.GetCertificatesFromThumbPrintOrFileName(this.AcceptedServerCertificateThumbprints);

            this.WriteObject(serverSpec);
        }
    }
}
