// <copyright file="NotExtraLongValidityTimeCertificateRule.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System;
    using System.Diagnostics;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Validates the certificate doesn't have a very long validity time
    /// </summary>
    public class NotExtraLongValidityTimeCertificateRule : AbstractCertificateRule
    {
        /// <summary>
        /// maximum timespan the validity should be
        /// </summary>
        private TimeSpan maxValidity;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotExtraLongValidityTimeCertificateRule"/> class.
        /// </summary>
        /// <param name="maxValidity">max period the certificate validity is allowed to have.</param>
        public NotExtraLongValidityTimeCertificateRule(TimeSpan maxValidity)
        {
            if (maxValidity.TotalDays < 1)
            {
                throw new ArgumentException("maxValidity must be >= 1 day");
            }

            this.maxValidity = maxValidity;
        }

        /// <summary>
        /// Disallows a certificate unless it has correct validity period.
        /// </summary>
        /// <param name="cert">the certificate to evaluate</param>
        /// <param name="chain">the signature chain</param>
        /// <param name="sslPolicyErrors">SSL errors from platform</param>
        /// <returns>Neutral if the certificate has validity period within limits, otherwise, returns NotAllowed.</returns>
        public override Behavior IsValid(X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (cert == null)
            {
                return Behavior.NotAllowed;
            }

            if (this.HasCorrectValidityTime(cert))
            {
                return Behavior.Neutral;
            }

            return Behavior.NotAllowed;
        }

        /// <summary>
        /// indicates if the cert validity time is within the limits
        /// </summary>
        /// <param name="cert">certificate to validate</param>
        /// <returns>true if the certificate validity period is within the limits</returns>
        private bool HasCorrectValidityTime(X509Certificate cert)
        {
            X509Certificate2 certificate = cert as X509Certificate2;

            if (certificate == null)
            {
                Trace.TraceError("ValidateSslPolicyErrors. NotExtraLongValidityTimeCertificateRule: Certificate {0} is not a valid X509Certificate2", CertAccessor.Instance.GetSerialNumberString(cert));
                return false;
            }

            try
            {
                if (CertAccessor.Instance.NotAfter(certificate) > CertAccessor.Instance.NotBefore(certificate).Add(this.maxValidity))
                {
                    Trace.TraceError("ValidateSslPolicyErrors. NotExtraLongValidityTimeCertificateRule: Certificate {0} is not valid because validity period in days ({1}) exceeds {2}", CertAccessor.Instance.GetSerialNumberString(cert), (CertAccessor.Instance.NotAfter(certificate) - CertAccessor.Instance.NotBefore(certificate)).TotalDays, this.maxValidity.TotalDays);
                    return false;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Certificate {0} is not valid because we couldn't compute validity time properly: {1}", CertAccessor.Instance.GetSerialNumberString(cert), e);
                return false;
            }

            return true;
        }
    }
}