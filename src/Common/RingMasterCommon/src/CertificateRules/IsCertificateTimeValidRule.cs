// <copyright file="IsCertificateTimeValidRule.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System;
    using System.Diagnostics;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Validates the certificate is valid with regards to time
    /// </summary>
    public class IsCertificateTimeValidRule : AbstractCertificateRule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IsCertificateTimeValidRule"/> class.
        /// </summary>
        public IsCertificateTimeValidRule()
        {
        }

        /// <summary>
        /// Validates the certificate
        /// </summary>
        /// <param name="cert">the certificate</param>
        /// <returns>true if the certificate has a valid expiration time</returns>
        public static bool IsValidCertificate(X509Certificate cert)
        {
            if (cert == null)
            {
                return false;
            }

            X509Certificate2 certificate = cert as X509Certificate2;

            if (certificate == null)
            {
                Trace.TraceError("ValidateSslPolicyErrors. IsValidCertificateRule: Certificate {0} is not a valid X509Certificate2", CertAccessor.Instance.GetSerialNumberString(cert));
                return false;
            }

            // NotBefore and NotAfter are in local time.
            DateTime now = DateTime.Now;

            if ((CertAccessor.Instance.NotBefore(certificate) > now) || (CertAccessor.Instance.NotAfter(certificate) < now))
            {
                Trace.TraceError("ValidateSslPolicyErrors. IsValidCertificateRule: Certificate {0} is not valid before {1} or after {2}", CertAccessor.Instance.GetSerialNumberString(cert), CertAccessor.Instance.NotBefore(certificate), CertAccessor.Instance.NotAfter(certificate));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Disallows a certificate unless it has valid expiration date.
        /// </summary>
        /// <param name="cert">the certificate to evaluate</param>
        /// <param name="chain">the signature chain</param>
        /// <param name="sslPolicyErrors">SSL errors from platform</param>
        /// <returns>Neutral if the certificate has valid expiration date, otherwise, returns NotAllowed.</returns>
        public override Behavior IsValid(X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (cert == null)
            {
                return Behavior.NotAllowed;
            }

            if (IsValidCertificate(cert))
            {
                return Behavior.Neutral;
            }

            return Behavior.NotAllowed;
        }
    }
}