﻿// <copyright file="AllowCertificatesRule.cs" company="Microsoft">
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
    /// Allow a certificate by instance
    /// </summary>
    public class AllowCertificatesRule : AbstractCertificateRule
    {
        /// <summary>
        /// function to see if the cert is allowed
        /// </summary>
        private Func<X509Certificate, bool> isCertificateIncluded;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowCertificatesRule"/> class.
        /// </summary>
        /// <param name="isCertificateIncluded">the function that says if a certificate is included in the list</param>
        public AllowCertificatesRule(Func<X509Certificate, bool> isCertificateIncluded)
        {
            if (isCertificateIncluded == null)
            {
                throw new ArgumentNullException("isCertificateIncluded");
            }

            this.isCertificateIncluded = isCertificateIncluded;
        }

        /// <summary>
        /// Allows a certificate if it is one of the certificates enumerated in the constructor.
        /// If no match is found, returns NotAllowed.
        /// </summary>
        /// <param name="cert">the certificate to evaluate</param>
        /// <param name="chain">the signature chain</param>
        /// <param name="sslPolicyErrors">SSL errors from platform</param>
        /// <returns>Allowed if a match is found. if no match is found, returns NotAllowed.</returns>
        public override Behavior IsValid(X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (cert == null)
            {
                Trace.TraceInformation("ValidateSslPolicyErrors: AllowCertificates: cert was null:");
                return Behavior.NotAllowed;
            }

            if (this.isCertificateIncluded(cert))
            {
                Trace.TraceInformation("ValidateSslPolicyErrors: AllowCertificates: Found allowed certificate: {0}", CertAccessor.Instance.GetThumbprint(cert));
                return Behavior.Allowed;
            }

            Trace.TraceError("ValidateSslPolicyErrors: AllowCertificates: Found no allowed certificate: {0}", CertAccessor.Instance.GetThumbprint(cert));

            return Behavior.NotAllowed;
        }
    }
}