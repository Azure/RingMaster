// <copyright file="BreakGlassCertificatesRule.cs" company="Microsoft">
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
    /// considers a certificate a break-glass (allow no matter what) certificate
    /// </summary>
    public class BreakGlassCertificatesRule : AbstractCertificateRule
    {
        /// <summary>
        /// break-glass certificates
        /// </summary>
        private HashSet<X509Certificate> certs;

        /// <summary>
        /// Initializes a new instance of the <see cref="BreakGlassCertificatesRule"/> class.
        /// </summary>
        /// <param name="certs">the certificates to allow no matter what (i.e. break-glass)</param>
        /// <param name="allowNullInArray">if true (default is false) a null in the array means the other party may present no certificate</param>
        public BreakGlassCertificatesRule(X509Certificate[] certs, bool allowNullInArray = false)
        {
            if (certs == null)
            {
                throw new ArgumentNullException("certs");
            }

            this.certs = new HashSet<X509Certificate>(CertAccessor.Instance);

            if (certs == null)
            {
                return;
            }

            foreach (X509Certificate cert in certs)
            {
                if (cert == null && !allowNullInArray)
                {
                    throw new ArgumentException("It is not legal to pass a null in the allowed certificate list. If you intended to allow the other party can include no certificate, you need to call this constructor with allowNullArray argument set to true");
                }

                if (cert != null && !IsCertificateTimeValidRule.IsValidCertificate(cert))
                {
                    throw new ArgumentException("not valid certificate: " + CertAccessor.Instance.GetThumbprint(cert));
                }

                this.certs.Add(cert);
            }
        }

        /// <summary>
        /// Considers break-glass a certificate if it is one of the certificates enumerated in the constructor.
        /// If no match is found, returns Neutral.
        /// </summary>
        /// <param name="cert">the certificate to evaluate</param>
        /// <param name="chain">the signature chain</param>
        /// <param name="sslPolicyErrors">SSL errors from platform</param>
        /// <returns>BreakGlassUnlessBlackListed if a match is found. if no match is found, returns Neutral.</returns>
        public override Behavior IsValid(X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // here, cert == null may be contained in the certs list (meaning, it is okay for the other party to present no certificate).
            if (this.certs.Contains(cert))
            {
                Trace.TraceInformation("ValidateSslPolicyErrors: BreakGlassCertificates: Found breakglass thumbprint: {0}", CertAccessor.Instance.GetThumbprint(cert));
                return Behavior.BreakGlassUnlessBlackListed;
            }

            return Behavior.Neutral;
        }
    }
}