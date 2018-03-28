// <copyright file="ChainIsValidRule.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Check that the signing chain is valid
    /// </summary>
    public class ChainIsValidRule : AbstractCertificateRule
    {
        /// <summary>
        /// the default allowed status
        /// </summary>
        private const X509ChainStatusFlags DefaultAllowed = X509ChainStatusFlags.OfflineRevocation | X509ChainStatusFlags.RevocationStatusUnknown;

        /// <summary>
        /// the string describing the default allowed status
        /// </summary>
        private const string DefaultAllowedString = "CRL server offline";

        /// <summary>
        /// we need to validate the chain
        /// </summary>
        private bool validateChain;

        /// <summary>
        /// what flags are allowed
        /// </summary>
        private X509ChainStatusFlags allowed = DefaultAllowed;

        /// <summary>
        /// string describes the disallowed flags
        /// </summary>
        private string allowedString = DefaultAllowedString;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChainIsValidRule"/> class
        /// </summary>
        /// <param name="validateChain">true if we need to validate the chain</param>
        public ChainIsValidRule(bool validateChain)
        {
            this.validateChain = validateChain;
        }

        /// <summary>
        /// Validates the certificate and its chain
        /// </summary>
        /// <param name="certificate">certificate to validate</param>
        /// <param name="chain">chain to validate</param>
        /// <param name="sslPolicyErrors">SSL policy errors</param>
        /// <returns>the behavior according to this rule. If there is no CRL, neutral. If there is no need to validate chain, neutral. If there is no SSL policy errors, neutral. Otherwise, if the chain is empty, or there is any chain state not permitted, NotAllowed. Otherwise, neutral.</returns>
        public override Behavior IsValid(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (!this.validateChain)
            {
                CertificateRulesEventSource.Log.ChainIsValidRule_Skipped(
                    CertAccessor.Instance.GetIssuer(certificate),
                    CertAccessor.Instance.GetSubject(certificate),
                    CertAccessor.Instance.GetSerialNumberString(certificate),
                    CertAccessor.Instance.GetThumbprint(certificate));
                return Behavior.Neutral;
            }

            // If the other party didn't provide a certificate, deny
            if (certificate == null || (sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) == SslPolicyErrors.RemoteCertificateNotAvailable)
            {
                CertificateRulesEventSource.Log.ChainIsValidRule_RemoteCertificateNotAvailable();
                return Behavior.NotAllowed;
            }

            // Per Azure Security Requirement https://requirements.azurewebsites.net/Requirements/Details/6682#guide
            // we are required to check certificate revocation but may skip the check if the CRL server is offline.
            // See https://technet.microsoft.com/en-us/library/ee619754(v=ws.10).aspx for information on how Windows Crypto APIs
            // deal with offline CRLs. In summary, the first thread will attempt to retrieve it with a 15 second timeout. If it fails
            // then the retreival will continue in the background while all subsequent calls immediately return with CRL offline error until
            // the background download can succeed - it does not block.
            if (sslPolicyErrors == SslPolicyErrors.None || sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
            {
                CertificateRulesEventSource.Log.ChainIsValidRule_CertificateTrustChainValidationNotNeeded(
                    CertAccessor.Instance.GetThumbprint(certificate),
                    sslPolicyErrors.ToString());
                return Behavior.Neutral;
            }

            X509ChainStatus[] chainStatus = null;

            if (chain != null)
            {
                chainStatus = CertAccessor.Instance.ChainStatus(chain);
            }

            if (chainStatus == null || chainStatus.Length == 0)
            {
                CertificateRulesEventSource.Log.ChainIsValidRule_ChainStatusNotAvailable(sslPolicyErrors.ToString());
                return Behavior.NotAllowed;
            }

            foreach (X509ChainStatus status in chainStatus)
            {
                if (this.IsStatePermitted(status.Status))
                {
                    CertificateRulesEventSource.Log.ChainIsValidRule_SkippingRevocationCheck(
                        this.allowedString,
                        status.Status.ToString(),
                        CertAccessor.Instance.GetIssuer(certificate),
                        CertAccessor.Instance.GetSubject(certificate),
                        CertAccessor.Instance.GetSerialNumberString(certificate),
                        CertAccessor.Instance.GetThumbprint(certificate));
                }
                else
                {
                    CertificateRulesEventSource.Log.ChainIsValidRule_NotAllowed(
                        status.Status.ToString(),
                        CertAccessor.Instance.GetIssuer(certificate),
                        CertAccessor.Instance.GetSubject(certificate),
                        CertAccessor.Instance.GetSerialNumberString(certificate),
                        CertAccessor.Instance.GetThumbprint(certificate));

                    return Behavior.NotAllowed;
                }
            }

            return Behavior.Neutral;
        }

        /// <summary>
        /// Indicates if the state is permitted
        /// </summary>
        /// <param name="status">the status</param>
        /// <returns>true if it is permitted</returns>
        private bool IsStatePermitted(X509ChainStatusFlags status)
        {
            return (status & ~this.allowed) == X509ChainStatusFlags.NoError;
        }
    }
}