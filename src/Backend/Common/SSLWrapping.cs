// <copyright file="SSLWrapping.cs" company="Microsoft Corporation">
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
    /// Class SslWrapping.
    /// This abstracts and encapsulates the details about providing with SSL a regular stream.
    /// It receives a list of thumbprints valid for the client, and a list of thumbprints valid for the server.
    /// If you are a server, the first of the list of server thumbprints will be used, and you will validate clients to have any of the certs in the client cert list.
    /// If you are a client, you will include all the client certs provided in the list on your connection, and will validate the server to use any of the server certs in the provided server certs list.
    /// </summary>
    public class SslWrapping
    {
        /// <summary>
        /// The cert identities to be used
        /// </summary>
        private CertificateRules.CertIdentities identities;

        /// <summary>
        /// Whether to check certificate revocation
        /// </summary>
        private bool mustCheckCertificateRevocation;

        /// <summary>
        /// Whether to check certificate trust chain.
        /// </summary>
        private bool mustCheckCertificateTrustChain;

        /// <summary>
        /// The rules for cert validation
        /// </summary>
        private CertificateValidator rules;

        /// <summary>
        /// Initializes a new instance of the <see cref="SslWrapping"/> class.
        /// </summary>
        /// <param name="clientThumbprints">The client thumbprints.</param>
        /// <param name="serverThumbprints">The server thumbprints.</param>
        /// <param name="mustCheckCertificateRevocation">Whether certificate revocation checking is enabled</param>
        /// <param name="mustCheckCertificateTrustChain">Wheteher certificate trust chain checking is enabled</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "GetCertsFromThumbPrintOrFileName needs to be virtual for testing")]
        public SslWrapping(string[] clientThumbprints, string[] serverThumbprints, bool mustCheckCertificateRevocation, bool mustCheckCertificateTrustChain)
        {
            this.Initialize(StaticGetCertsFromThumbPrintOrFileName(clientThumbprints), StaticGetCertsFromThumbPrintOrFileName(serverThumbprints), null, CertificateValidator.GetFlags(mustCheckCertificateRevocation, mustCheckCertificateTrustChain));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SslWrapping"/> class.
        /// </summary>
        /// <param name="clientCerts">The client certs.</param>
        /// <param name="serverCerts">The server certs.</param>
        /// <param name="mustCheckCertificateRevocation">Whether certificate revocation checking is enabled</param>
        /// <param name="mustCheckCertificateTrustChain">Wheteher certificate trust chain checking is enabled</param>
        public SslWrapping(X509Certificate[] clientCerts, X509Certificate[] serverCerts, bool mustCheckCertificateRevocation, bool mustCheckCertificateTrustChain)
            : this(clientCerts, serverCerts, null, CertificateValidator.GetFlags(mustCheckCertificateRevocation, mustCheckCertificateTrustChain))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SslWrapping"/> class.
        /// </summary>
        /// <param name="clientThumbprints">The client thumbprints.</param>
        /// <param name="serverThumbprints">The server thumbprints.</param>
        /// <param name="flags">validation flags</param>
        /// <param name="rules">any additional rules to validate certificates</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "GetCertsFromThumbPrintOrFileName needs to be virtual for testing")]
        public SslWrapping(string[] clientThumbprints, string[] serverThumbprints, IEnumerable<CertificateRules.AbstractCertificateRule> rules, CertificateRules.CertificateRulesFlags flags = CertificateValidator.DefaultFlags)
        {
            this.Initialize(StaticGetCertsFromThumbPrintOrFileName(clientThumbprints), StaticGetCertsFromThumbPrintOrFileName(serverThumbprints), rules, flags);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SslWrapping"/> class.
        /// </summary>
        /// <param name="clientThumbprints">The client thumbprints.</param>
        /// <param name="serverThumbprints">The server thumbprints.</param>
        /// <param name="flags">validation flags</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "GetCertsFromThumbPrintOrFileName needs to be virtual for testing")]
        public SslWrapping(string[] clientThumbprints, string[] serverThumbprints, CertificateRules.CertificateRulesFlags flags = CertificateValidator.DefaultFlags)
        {
            this.Initialize(StaticGetCertsFromThumbPrintOrFileName(clientThumbprints), StaticGetCertsFromThumbPrintOrFileName(serverThumbprints), null, flags);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SslWrapping"/> class.
        /// </summary>
        /// <param name="clientCerts">The client certs.</param>
        /// <param name="serverCerts">The server certs.</param>
        /// <param name="flags">wrapper validation flags</param>
        /// <param name="rules">any additional rules to validate certificates</param>
        public SslWrapping(X509Certificate[] clientCerts, X509Certificate[] serverCerts, IEnumerable<CertificateRules.AbstractCertificateRule> rules, CertificateRules.CertificateRulesFlags flags = CertificateValidator.DefaultFlags)
        {
            this.Initialize(clientCerts, serverCerts, rules, flags);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SslWrapping"/> class.
        /// </summary>
        /// <param name="identities">The identities object to use.</param>
        /// <param name="flags">wrapper validation flags</param>
        /// <param name="rules">any additional rules to validate certificates</param>
        public SslWrapping(CertificateRules.CertIdentities identities, IEnumerable<CertificateRules.AbstractCertificateRule> rules, CertificateRules.CertificateRulesFlags flags = CertificateValidator.DefaultFlags)
        {
            this.Initialize(identities, rules, flags);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SslWrapping"/> class.
        /// </summary>
        protected SslWrapping()
        {
        }

        /// <summary>
        /// Gets the identities.
        /// </summary>
        public CertificateRules.CertIdentities Identities => this.identities;

        /// <summary>
        /// Gets a value indicating whether to check certificate revocation.
        /// </summary>
        public bool MustCheckCertificateRevocation => this.mustCheckCertificateRevocation;

        /// <summary>
        /// Gets a value indicating whether to check the certificate trust chain.
        /// </summary>
        public bool MustCheckCertificateTrustChain => this.mustCheckCertificateTrustChain;

        /// <summary>
        /// Gets or sets the supported protocols
        /// </summary>
        protected SslProtocols SupportedProtocols { get; set; } = SslProtocols.Tls12;

        /// <summary>
        /// Gets or sets the supported protocols as client
        /// </summary>
        protected SslProtocols SupportedProtocolsAsClient { get; set; } = SslProtocols.Tls12;

        /// <summary>
        /// Gets the name of the certs from thumb print or file.
        /// </summary>
        /// <param name="thumbprint">The thumbprint.</param>
        /// <returns>X509Certificate[].</returns>
        /// <exception cref="System.Exception">
        /// two certs found in the same path  + tp
        /// or
        /// no cert found in path  + tp
        /// </exception>
        public static X509Certificate[] StaticGetCertsFromThumbPrintOrFileName(string[] thumbprint)
        {
            return CertificateRules.CertAccessor.Instance.GetCertsFromThumbPrintOrFileName(thumbprint);
        }

        /// <summary>
        /// Displays the certificate information.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public static void DisplayCertificateInformation(SslStream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            X509Certificate localCertificate = null;
            try
            {
                localCertificate = stream.LocalCertificate;
            }
            catch (InvalidOperationException)
            {
            }

            if (localCertificate != null)
            {
                Trace.WriteLine(
                    $"Local cert was issued to {localCertificate.Subject} "
                    + $"(thumbprint {CertificateRules.CertAccessor.Instance.GetThumbprint(localCertificate)}) and is valid from "
                    + $"{localCertificate.GetEffectiveDateString()} until "
                    + $"{localCertificate.GetExpirationDateString()}.");
            }
            else
            {
                Trace.TraceError("Local certificate is null.");
            }

            // Display the properties of the client's certificate.
            X509Certificate remoteCertificate = null;

            try
            {
                remoteCertificate = stream.RemoteCertificate;
            }
            catch (InvalidOperationException)
            {
            }

            if (remoteCertificate != null)
            {
                Trace.TraceInformation(
                    "Remote cert was issued to {0} (thumbprint {1}) and is valid from {2} until {3}.",
                    remoteCertificate.Subject,
                    CertificateRules.CertAccessor.Instance.GetThumbprint(remoteCertificate),
                    remoteCertificate.GetEffectiveDateString(),
                    remoteCertificate.GetExpirationDateString());
            }
            else
            {
                Trace.TraceWarning("Remote certificate is null.");
            }
        }

        /// <summary>
        /// returns the cert subject used by the client
        /// </summary>
        /// <param name="str">The stream</param>
        /// <returns>The client cert subject</returns>
        public virtual string GetClientCertSubjectFromStream(Stream str)
        {
            SslStream sslstr = str as SslStream;

            X509Certificate remoteCertificate = sslstr?.RemoteCertificate;

            if (remoteCertificate == null)
            {
                return string.Empty;
            }

            return CertificateRules.CertAccessor.Instance.GetSubject(remoteCertificate);
        }

        /// <summary>
        /// Gets the validated stream on server.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <returns>Stream.</returns>
        public virtual Stream GetValidatedStreamOnServer(TcpClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            SslStream sslStream = null;

            // locate a cert that passes the tests, but do not use it yet
            Stream ns = client.GetStream();

            string clientEndPoint = string.Empty;
            try
            {
                clientEndPoint = client.Client.RemoteEndPoint.ToString();

                X509Certificate serverIdentityCert = this.identities.ServerIdentity;

                if (serverIdentityCert == null)
                {
                    // if no good cert found, throw
                    throw new InvalidOperationException("server certification not provided, no authentication attempted");
                }

                // otherwise, validate with the cert for real
                sslStream = this.CreateSslStream(
                                ns,
                                false,
                                this.rules.ValidateClientCertificate,
                                null);

                Trace.TraceInformation("SslWrapping.GetValidatedStreamOnServer AuthenticateAsServer-Started clientEndpoint={0}", clientEndPoint);
                sslStream.AuthenticateAsServer(serverIdentityCert, true, this.SupportedProtocols, this.mustCheckCertificateRevocation);
                Trace.TraceInformation("SslWrapping.GetValidatedStreamOnServer AuthenticateAsServer-Completed clientEndpoint={0}", clientEndPoint);

                // Display the properties and settings for the authenticated stream.
                // DisplaySecurityLevel(sslStream);
                // DisplaySecurityServices(sslStream);
                // DisplayStreamProperties(sslStream);
                DisplayCertificateInformation(sslStream);
            }
            catch (Exception ex)
            {
                Trace.TraceError("SslWrapping.GetValidatedStreamOnServer AuthenticateAsServer-Failed clientEndpoint={0}, exception={1}", clientEndPoint, ex);
                if (sslStream != null)
                {
                    sslStream.Dispose();
                }
                else
                {
                    // ns cannot be null here
                    ns.Dispose();
                    client.Close();
                }

                throw;
            }

            return sslStream;
        }

        /// <summary>
        /// Gets the SSL stream after validation on the server
        /// </summary>
        /// <param name="client">tcp client</param>
        /// <param name="timeout">validation timeout</param>
        /// <returns>the sslstream after validation on the server.</returns>
        public Stream GetValidatedStreamOnServer(TcpClient client, TimeSpan timeout)
        {
            return this.ExecuteValidateStreamFunc(client, timeout, () => this.GetValidatedStreamOnServer(client));
        }

        /// <summary>
        /// Gets the validated stream on client.
        /// </summary>
        /// <param name="serverName">Name of the server.</param>
        /// <param name="client">The client.</param>
        /// <returns>The SSlStream, after validation on the client</returns>
        public virtual Stream GetValidatedStreamOnClient(string serverName, TcpClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            Stream ns = null;
            SslStream sslStream = null;

            if (this.identities.ClientIdentities == null || this.identities.ClientIdentities.Length == 0)
            {
                // if no good cert found, throw
                throw new InvalidOperationException("client certification not provided, no authentication attempted");
            }

            try
            {
                ns = client.GetStream();

                sslStream = this.CreateSslStream(
                    ns,
                    false,
                    this.rules.ValidateServerCertificate,
                    null);

                // The server name must match the name on the server certificate.
                sslStream.AuthenticateAsClient(serverName, new X509CertificateCollection(this.identities.ClientIdentities), this.SupportedProtocolsAsClient, this.mustCheckCertificateRevocation);

                // Display the properties and settings for the authenticated stream.
                // DisplaySecurityLevel(sslStream);
                // DisplaySecurityServices(sslStream);
                // DisplayStreamProperties(sslStream);
                DisplayCertificateInformation(sslStream);

                return sslStream;
            }
            catch (Exception)
            {
                if (sslStream != null)
                {
                    sslStream.Dispose();
                }
                else
                {
                    ns?.Dispose();
                }

                throw;
            }
        }

        /// <summary>
        /// Gets the validated stream on client.
        /// </summary>
        /// <param name="serverName">server name</param>
        /// <param name="client">tcl client</param>
        /// <param name="timeout">validation timeout</param>
        /// <returns>The SSlStream, after validation on the client</returns>
        public Stream GetValidatedStreamOnClient(string serverName, TcpClient client, TimeSpan timeout)
        {
            return this.ExecuteValidateStreamFunc(client, timeout, () => this.GetValidatedStreamOnClient(serverName, client));
        }

        /// <summary>
        /// Creates new instance of the System.Net.Security.SslStream class using the
        /// specified System.IO.Stream, stream closure behavior, certificate validation delegate
        /// and certificate selection delegate.
        /// </summary>
        /// <param name="innerStream">A System.IO.Stream object used by the System.Net.Security.SslStream for sending and receiving data.</param>
        /// <param name="leaveInnerStreamOpen">A Boolean value that indicates the closure behavior of the System.IO.Stream object used by the System.Net.Security.SslStream for sending and receiving data. This parameter indicates if the inner stream is left open.</param>
        /// <param name="userCertificateValidationCallback">A System.Net.Security.RemoteCertificateValidationCallback delegate responsible for validating the certificate supplied by the remote party.</param>
        /// <param name="userCertificateSelectionCallback">A System.Net.Security.LocalCertificateSelectionCallback delegate responsible for selecting the certificate used for authentication.</param>
        /// <returns>the SslStream created</returns>
        protected virtual SslStream CreateSslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback userCertificateValidationCallback, LocalCertificateSelectionCallback userCertificateSelectionCallback)
        {
            return new SslStream(
                                innerStream,
                                leaveInnerStreamOpen,
                                userCertificateValidationCallback,
                                userCertificateSelectionCallback);
        }

        /// <summary>
        /// Executes the validation function with a timeout
        /// </summary>
        /// <param name="client">tcl client</param>
        /// <param name="timeout">validation timeout</param>
        /// <param name="validateStreamFunc">validation function</param>
        /// <returns>validated ssl stream</returns>
        private Stream ExecuteValidateStreamFunc(TcpClient client, TimeSpan timeout, Func<Stream> validateStreamFunc)
        {
            Timer expTimer = null;
            bool triggered = false;
            object triggerObj = new object();

            if (timeout != Timeout.InfiniteTimeSpan)
            {
                expTimer = new Timer(
                    o =>
                    {
                        lock (triggerObj)
                        {
                            if (expTimer == null)
                            {
                                return;
                            }

                            triggered = true;
                            client.Client.Close();
                        }
                    },
                    null,
                    timeout,
                    Timeout.InfiniteTimeSpan);
            }

            try
            {
                return validateStreamFunc();
            }
            catch (Exception)
            {
                lock (triggerObj)
                {
                    // if the timeout was not triggered, cancel it and throw the exception now.
                    // however, if the timeout triggered, this exception is the channel being closed, and we need to throw the timeout exception below.
                    if (!triggered)
                    {
                        if (expTimer != null)
                        {
                            expTimer.Change(Timeout.Infinite, Timeout.Infinite);
                            expTimer = null;
                        }

                        throw;
                    }
                }
            }
            finally
            {
                lock (triggerObj)
                {
                    if (expTimer != null)
                    {
                        expTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        expTimer = null;
                    }
                }
            }

            throw new TimeoutException("validation didn't happen in " + timeout.TotalMilliseconds + " ms");
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="identities">The identities object to use.</param>
        /// <param name="rules">any additional rules to validate certificates</param>
        /// <param name="flags">wrapper validation flags</param>
        /// <exception cref="System.ArgumentException">serverCerts</exception>
        private void Initialize(CertificateRules.CertIdentities identities, IEnumerable<CertificateRules.AbstractCertificateRule> rules, CertificateRules.CertificateRulesFlags flags)
        {
            if (identities == null)
            {
                throw new ArgumentNullException(nameof(identities));
            }

            // record and validate the identities provided
            this.identities = identities;

            if (identities.ServerIdentity != null && !CertificateRules.IsCertificateTimeValidRule.IsValidCertificate(identities.ServerIdentity))
            {
                throw new ArgumentException("bad servercertificate");
            }

            foreach (X509Certificate cert in identities.ClientIdentities)
            {
                if (!CertificateRules.IsCertificateTimeValidRule.IsValidCertificate(cert))
                {
                    throw new ArgumentException("bad client certificate");
                }
            }

            this.mustCheckCertificateRevocation = (flags & CertificateRules.CertificateRulesFlags.MustCheckCertificateRevocation) == CertificateRules.CertificateRulesFlags.MustCheckCertificateRevocation;
            this.mustCheckCertificateTrustChain = (flags & CertificateRules.CertificateRulesFlags.MustCheckCertificateTrustChain) == CertificateRules.CertificateRulesFlags.MustCheckCertificateTrustChain;

            if (rules != null)
            {
                this.rules = new CertificateValidator(rules);
            }
            else
            {
                this.rules = new CertificateValidator(identities, flags);
            }
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="clientCerts">The client certs.</param>
        /// <param name="serverCerts">The server certs.</param>
        /// <param name="rules">any additional rules to validate certificates</param>
        /// <param name="flags">wrapper validation flags</param>
        /// <exception cref="System.ArgumentException">serverCerts</exception>
        private void Initialize(X509Certificate[] clientCerts, X509Certificate[] serverCerts, IEnumerable<CertificateRules.AbstractCertificateRule> rules, CertificateRules.CertificateRulesFlags flags)
        {
            CertificateRules.CertIdentities ident = new CertificateRules.CertIdentities();
            ident.SetClientIdentities(clientCerts);
            ident.SetServerIdentities(serverCerts);

            this.Initialize(ident, rules, flags);
        }
    }
}
