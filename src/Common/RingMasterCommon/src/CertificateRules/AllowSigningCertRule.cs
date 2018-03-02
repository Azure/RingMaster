// <copyright file="AllowSigningCertRule.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// allow a certificate if it was signed by a cert with a given subject
    /// </summary>
    public class AllowSigningCertRule : AbstractCertificateRule
    {
        /// <summary>
        /// allowed subjects
        /// </summary>
        private HashSet<string> thumbprints;

        /// <summary>
        /// if true all signed certificates are valid
        /// </summary>
        private bool allowAny;

        /// <summary>
        /// if true self-signed certificates are valid
        /// </summary>
        private bool allowSelf;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowSigningCertRule"/> class.
        /// </summary>
        /// <param name="thumbprints">thumbprints to be allowed, or '*' to allow any signing cert, or '[*]' to allow self-signed certificates</param>
        public AllowSigningCertRule(string[] thumbprints)
        {
            this.thumbprints = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            if (thumbprints == null)
            {
                return;
            }

            foreach (string s in thumbprints)
            {
                if (s.Equals("*"))
                {
                    this.allowAny = true;
                }
                else if (s.Equals("[*]"))
                {
                    this.allowSelf = true;
                }
                else
                {
                    this.thumbprints.Add(s);
                }
            }
        }

        /// <summary>
        /// allows a certificate if the certificate was signed by a second cert whose subject is in the list enumerated in the constructor.
        /// If no match is found, returns NotAllowed.
        /// </summary>
        /// <param name="cert">the certificate to evaluate</param>
        /// <param name="chain">the signature chain</param>
        /// <param name="sslPolicyErrors">SSL errors from platform</param>
        /// <returns>If CRL is down, it returns Neutral; else, if a subject signature match is found, it returns Neutral; else, if self-signed was allowed in the constructor and the cert is self-signed, it returns Neutral; else, it returns NotAllowed.</returns>
        public override Behavior IsValid(X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (cert == null || (sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) == SslPolicyErrors.RemoteCertificateNotAvailable)
            {
                Trace.TraceError("ValidateSslPolicyErrors: AllowSigningCertRule: no remote certificate available. Certificate error: {0}", sslPolicyErrors);
                return Behavior.NotAllowed;
            }

            if (CertAccessor.Instance.ChainElementsCount(chain) == 0)
            {
                Trace.TraceError("ValidateSslPolicyErrors: AllowSigningCertRule: no ChainElements available. Certificate error: {0}", sslPolicyErrors);
                return Behavior.NotAllowed;
            }

            // the cert must not be self-signed (i.e. the chain must contain some signing certs)
            if (CertAccessor.Instance.ChainElementsCount(chain) == 1)
            {
                if (this.allowSelf)
                {
                    Trace.TraceWarning("ValidateSslPolicyErrors: AllowSigningCertRule: cert seems to be self-signed, which is allowed from config.");
                    return Behavior.Allowed;
                }

                Trace.TraceError("ValidateSslPolicyErrors: AllowSigningCertRule: cert seems to be self-signed. Certificate error: {0}", sslPolicyErrors);
                return Behavior.NotAllowed;
            }

            X509Certificate signingCert = CertAccessor.Instance.ChainCertificateAtPosition(chain, 1);

            string signing = CertAccessor.Instance.GetThumbprint(signingCert);

            if (this.allowAny || this.thumbprints.Contains(signing))
            {
                Trace.TraceInformation("ValidateSslPolicyErrors: AllowSigningCertRule: Found signing cert to be one allowed: {0}", signing);
                return Behavior.Allowed;
            }

            Trace.TraceError("ValidateSslPolicyErrors: AllowSigningCertRule: Found no signing cert to be one allowed: {0}", signing);
            return Behavior.NotAllowed;
        }
    }
}