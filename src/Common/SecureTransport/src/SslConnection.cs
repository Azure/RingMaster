// <copyright file="SslConnection.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules;

    /// <summary>
    /// SslConnection class implements a <see cref="ISecureConnectionPolicy"/> where connections
    /// are encrypted.
    /// </summary>
    /// <remarks>
    /// SslConnection is initialized with a list of acceptable client and server certificates.
    /// - When a server receives a connection from a client, it calls <c>AuthenticateAsServer</c> with the first certificate in the list of acceptable
    ///   server certificates.  If the certificate presented by the client is present in the list of acceptable client certificates, the connection is allowed.
    /// - When a client tries to connect to a server, it calls <c>AuthenticateAsClient</c> with a list of client certificates.
    ///   If the certificate presented by the server is present in the list of acceptable server certificates, the connection is allowed.
    /// </remarks>
    public class SslConnection : ISecureConnectionPolicy
    {
        /// <summary>
        /// Unique Id of the transport associated with this policy.
        /// </summary>
        private readonly long transportId;

        /// <summary>
        /// Configuration settings for SSL connection.
        /// </summary>
        private readonly Configuration configuration;

        /// <summary>
        /// The object providing the identities.
        /// </summary>
        private readonly CertIdentities identities;

        /// <summary>
        /// Validation certificateValidator
        /// </summary>
        private readonly CertificateValidator certificateValidator;

        /// <summary>
        /// Initializes a new instance of the <see cref="SslConnection"/> class.
        /// </summary>
        /// <param name="transportId">Unique Id of the transport associated with this policy</param>
        /// <param name="configuration">Configuration settings</param>
        public SslConnection(long transportId, Configuration configuration)
        {
            this.transportId = transportId;

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if ((configuration.ClientCertificates != null || configuration.ServerCertificates != null) && configuration.Identities != null)
            {
                throw new ArgumentException("ClientCertificates and ServerCertificates must be null if Identities is provided");
            }

            this.configuration = configuration;
            this.identities = configuration.Identities;

            if (this.identities == null)
            {
                X509Certificate[] clientCertificates = this.configuration.ClientCertificates;
                X509Certificate[] serverCertificates = this.configuration.ServerCertificates;

                if (!configuration.StartAsClient && (serverCertificates == null || serverCertificates.Length == 0))
                {
                    SecureTransportEventSource.Log.ServerCertificatesWereNotProvided(this.transportId);
                    throw new ArgumentException("Server certificates were not provided");
                }

                if (configuration.StartAsClient && (clientCertificates == null || clientCertificates.Length == 0))
                {
                    SecureTransportEventSource.Log.ServerCertificatesWereNotProvided(this.transportId);
                    throw new ArgumentException("Client certificates were not provided");
                }

                this.identities = new CertIdentities();
                this.identities.IsClientWithNoCertificateAllowed = clientCertificates == null && !this.FoundAnySubjectRuleMatch(configuration, AbstractCertificateRule.RoleToApply.ClientCert);

                if (clientCertificates == null)
                {
                    SecureTransportEventSource.Log.ClientCertificatesWereNotProvided(this.transportId);
                    clientCertificates = new X509Certificate[0];
                }

                if (serverCertificates == null)
                {
                    SecureTransportEventSource.Log.ServerCertificatesWereNotProvided(this.transportId);
                    serverCertificates = new X509Certificate[0];
                }

                foreach (X509Certificate serverCertificate in serverCertificates)
                {
                    if (serverCertificate != null)
                    {
                        string key = serverCertificate.GetSerialNumberString();
                        if (key != null)
                        {
                            SecureTransportEventSource.Log.SupportedServerCertificate(this.transportId, key);
                        }
                    }
                    else
                    {
                        throw new ArgumentException("One of the Server Certificates is null");
                    }
                }

                foreach (X509Certificate clientCertificate in clientCertificates)
                {
                    if (clientCertificate != null)
                    {
                        string key = clientCertificate.GetSerialNumberString();

                        if (key != null)
                        {
                            SecureTransportEventSource.Log.SupportedClientCertificate(this.transportId, key);
                        }
                    }
                    else
                    {
                        throw new ArgumentException("One of the client certificates is null");
                    }
                }

                this.identities.SetClientIdentities(clientCertificates);
                this.identities.SetServerIdentities(serverCertificates);
            }

            if (this.configuration.ExplicitRule != null)
            {
                this.certificateValidator = new CertificateValidator(this.configuration.ExplicitRule);
            }
            else
            {
                CertificateRulesFlags flags = CertificateValidator.GetFlags(this.configuration.MustCheckCertificateRevocation, this.configuration.MustCheckCertificateTrustChain);

                if (this.configuration.SubjectValidations == null || this.configuration.SubjectValidations.Count == 0)
                {
                    this.certificateValidator = new CertificateValidator(this.identities, flags);
                }
                else
                {
                    Action<StandardValidationRuleSet> modifySet = (s) =>
                    {
                        foreach (SecureTransport.SubjectRuleValidation subjectRule in this.configuration.SubjectValidations)
                        {
                            s.AddAcceptedCertificateSubject(
                                subjectRule.Role,
                                subjectRule.SubjectValidation.CertificateSubject,
                                subjectRule.SubjectValidation.SigningCertThumbprints);
                        }

                        if (this.configuration.BlacklistedThumbprints != null)
                        {
                            foreach (string blacklisted in this.configuration.BlacklistedThumbprints)
                            {
                                s.AddBlacklistedCertificateThumbprint(AbstractCertificateRule.RoleToApply.AllCerts, blacklisted);
                            }
                        }
                    };

                    this.certificateValidator = new CertificateValidator(this.identities, flags, modifySet);
                }
            }
        }

        /// <summary>
        /// Gets the validated stream on server.
        /// </summary>
        /// <param name="client">TCP client</param>
        /// <param name="timeout">Time to wait for the validation</param>
        /// <param name="cancellationToken">Token to be observed for cancellation signal</param>
        /// <returns>A Task that resolves to the validated stream</returns>
        public virtual async Task<Stream> AuthenticateAsServer(TcpClient client, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            SslStream sslStream = null;
            Stream clientStream = client.GetStream();
            try
            {
                // We want to use ONLY the first cert in the given list
                X509Certificate serverCertificate = this.identities.ServerIdentity;
                if (serverCertificate == null)
                {
                    throw SecureTransportException.NoServerCertificate();
                }

                SecureTransportEventSource.Log.AuthenticateAsServer(this.transportId, (int)timeout.TotalMilliseconds, this.configuration.MustCheckCertificateRevocation, this.configuration.MustCheckCertificateTrustChain);
                sslStream = new SslStream(
                    clientStream,
                    false,
                    this.configuration.RemoteCertificateValidationCallback ?? this.certificateValidator.ValidateClientCertificate,
                    this.configuration.LocalCertificateSelectionCallback);

                Task task = sslStream.AuthenticateAsServerAsync(
                    serverCertificate,
                    clientCertificateRequired: this.configuration.IsClientCertificateRequired,
                    enabledSslProtocols: this.configuration.SupportedProtocols,
                    checkCertificateRevocation: this.configuration.MustCheckCertificateRevocation);

                await Task.WhenAny(task, Task.Delay(timeout, cancellationToken));

                if (!task.IsCompleted)
                {
                    throw SecureTransportException.SslValidationTimedOut();
                }

                // Task is already completed, await it to ensure that it successfully
                // completed.
                await task;

                return sslStream;
            }
            catch (Exception ex)
            {
                SecureTransportEventSource.Log.AuthenticateAsServerFailed(this.transportId, ex.ToString());

                if (sslStream != null)
                {
                    sslStream.Dispose();
                }
                else
                {
                    clientStream.Dispose();
                    client.Close();
                }

                throw;
            }
        }

        /// <summary>
        /// Gets the validated stream on client.
        /// </summary>
        /// <param name="serverName">Name of the server.</param>
        /// <param name="client">TCP client</param>
        /// <param name="timeout">Time to wait for the validation</param>
        /// <param name="cancellationToken">Token to be observed for cancellation signal</param>
        /// <returns>A Task that resolves to the validated stream</returns>
        public virtual async Task<Stream> AuthenticateAsClient(string serverName, TcpClient client, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            Stream clientStream = null;
            SslStream sslStream = null;

            try
            {
                clientStream = client.GetStream();

                sslStream = new SslStream(
                    clientStream,
                    false,
                    this.configuration.RemoteCertificateValidationCallback ?? this.certificateValidator.ValidateServerCertificate,
                    this.configuration.LocalCertificateSelectionCallback);

                X509CertificateCollection coll = new X509CertificateCollection(this.identities.ClientIdentities);

                SecureTransportEventSource.Log.AuthenticateAsClient(this.transportId, (int)timeout.TotalMilliseconds, this.configuration.MustCheckCertificateRevocation, this.configuration.MustCheckCertificateTrustChain);
                Task task = sslStream.AuthenticateAsClientAsync(
                    serverName,
                    coll,
                    this.configuration.SupportedProtocols,
                    this.configuration.MustCheckCertificateRevocation);

                await Task.WhenAny(task, Task.Delay(timeout, cancellationToken));

                if (!task.IsCompleted)
                {
                    throw SecureTransportException.SslValidationTimedOut();
                }

                // Task is already completed, await it to ensure that it successfully
                // completed.
                await task;

                return sslStream;
            }
            catch (Exception ex)
            {
                SecureTransportEventSource.Log.AuthenticateAsClientFailed(this.transportId, ex.ToString());

                if (sslStream != null)
                {
                    sslStream.Dispose();
                }
                else
                {
                    if (clientStream != null)
                    {
                        clientStream.Dispose();
                    }
                }

                throw;
            }
        }

        /// <summary>
        /// check if there are any matching subject rules
        /// </summary>
        /// <param name="configuration">configuration to check from</param>
        /// <param name="role">matching certitifcate role</param>
        /// <returns>true, if there is a match</returns>
        private bool FoundAnySubjectRuleMatch(Configuration configuration, AbstractCertificateRule.RoleToApply role)
        {
            if (configuration.SubjectValidations == null || configuration.SubjectValidations.Count == 0)
            {
                return false;
            }

            IEnumerable<SecureTransport.SubjectRuleValidation> matches = configuration.SubjectValidations.Where(
                    (u) =>
                    {
                        return u.Role == role || u.Role == AbstractCertificateRule.RoleToApply.AllCerts;
                    });

            return matches != null && matches.Any();
        }

        /// <summary>
        /// Configuration settings for <see cref="SslConnection"/>.
        /// </summary>
        public class Configuration
        {
            /// <summary>
            /// Gets or sets the protocols supported by the connection.
            /// </summary>
            public SslProtocols SupportedProtocols { get; set; } = SslProtocols.Tls12;

            /// <summary>
            /// Gets or sets the list of client certificates.
            /// </summary>
            public X509Certificate[] ClientCertificates { get; set; }

            /// <summary>
            /// Gets or sets the list of server certificates.
            /// </summary>
            public X509Certificate[] ServerCertificates { get; set; }

            /// <summary>
            /// Gets or sets the delegate responsible for validating the certificate supplied by the remote party.
            /// </summary>
            public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

            /// <summary>
            /// Gets or sets the delegate responsible for selecting the certificate used for authentication.
            /// </summary>
            public LocalCertificateSelectionCallback LocalCertificateSelectionCallback { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the client is asked for a certificate for authentication.
            /// </summary>
            public bool IsClientCertificateRequired { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether the certificate revocation list is checked during authentication.
            /// </summary>
            public bool MustCheckCertificateRevocation { get; set; } = false;

            /// <summary>
            /// Gets or sets a value indicating whether the certificate trust chain must be validated.
            /// </summary>
            public bool MustCheckCertificateTrustChain { get; set; } = true;

            /// <summary>
            /// Gets or sets if present, the additional validation certificateValidator to be used
            /// </summary>
            public AbstractCertificateRule ExplicitRule { get; set; } = null;

            /// <summary>
            /// Gets or sets if present identities are processed from here instead of from the members ClientCertificates and ServerCertificates
            /// </summary>
            public CertIdentities Identities { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether it should start as client
            /// </summary>
            public bool StartAsClient { get; set; }

            /// <summary>
            /// Gets or sets blacklisted thumbprints
            /// </summary>
            public IReadOnlyList<string> BlacklistedThumbprints { get; set; }

            /// <summary>
            /// Gets or sets if present, it provides the subject validation rules
            /// </summary>
            public IReadOnlyList<SecureTransport.SubjectRuleValidation> SubjectValidations { get; set; }
        }
    }
}
