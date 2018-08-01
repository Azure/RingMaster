// <copyright file="CertificateRulesEventSource.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System;
    using System.Diagnostics.Tracing;

#pragma warning disable SA1600 // No need to document every event here

    /// <summary>
    /// Event Source for Certificate Rules events.
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-CertificateRules")]
    internal sealed class CertificateRulesEventSource : EventSource
    {
        static CertificateRulesEventSource()
        {
        }

        private CertificateRulesEventSource()
        {
        }

        public static CertificateRulesEventSource Log { get; } = new CertificateRulesEventSource();

        [Event(2, Level = EventLevel.Error, Version = 1)]
        public void AllowCertificatesRules_CertWasNull()
        {
            this.WriteEvent(2);
        }

        [Event(3, Level = EventLevel.Informational, Version = 1)]
        public void AllowCertificatesRules_CertAllowed(string thumbprint)
        {
            this.WriteEvent(3, thumbprint);
        }

        [Event(4, Level = EventLevel.Error, Version = 1)]
        public void AllowCertificatesRules_CertNotAllowed(string thumbprint)
        {
            this.WriteEvent(4, thumbprint);
        }

        [Event(5, Level = EventLevel.Error, Version = 1)]
        public void AllowCertSubjectRuleCertificatesRules_CertWasNull()
        {
            this.WriteEvent(5);
        }

        [Event(6, Level = EventLevel.Informational, Version = 1)]
        public void AllowCertSubjectRule_CertAllowed(string thumbprint)
        {
            this.WriteEvent(6, thumbprint);
        }

        [Event(7, Level = EventLevel.Error, Version = 1)]
        public void AllowCertSubjectRule_CertNotAllowed(string thumbprint)
        {
            this.WriteEvent(7, thumbprint);
        }

        [Event(8, Level = EventLevel.LogAlways, Version = 1)]
        public void AllowIfAllAllowedRule_RuleResult(string ruleType, string behavior, string thumbprint)
        {
            this.WriteEvent(8, ruleType, behavior, thumbprint);
        }

        [Event(9, Level = EventLevel.LogAlways, Version = 1)]
        public void AllowIfAllCoherentRule_RuleResult(string ruleType, string behavior)
        {
            this.WriteEvent(9, ruleType, behavior);
        }

        [Event(10, Level = EventLevel.LogAlways, Version = 1)]
        public void AllowIfAllCoherentRule_Result(string behavior)
        {
            this.WriteEvent(10, behavior);
        }

        [Event(11, Level = EventLevel.Error, Version = 1)]
        public void AllowIfAnyAllowed_NotAllowed(string thumbprint)
        {
            this.WriteEvent(11, thumbprint);
        }

        [Event(12, Level = EventLevel.LogAlways, Version = 1)]
        public void AllowIfMaskRule_Result(string ruleType, string behavior)
        {
            this.WriteEvent(12, ruleType, behavior);
        }

        [Event(13, Level = EventLevel.Error, Version = 1)]
        public void AllowSigningCertRule_RemoteCertificateNotAvailable()
        {
            this.WriteEvent(13);
        }

        [Event(14, Level = EventLevel.Error, Version = 1)]
        public void AllowSigningCertRule_NoChainElementsAvailable()
        {
            this.WriteEvent(14);
        }

        [Event(15, Level = EventLevel.Informational, Version = 1)]
        public void AllowSigningCertRule_SelfSignedCertificateAllowed()
        {
            this.WriteEvent(15);
        }

        [Event(16, Level = EventLevel.Error, Version = 1)]
        public void AllowSigningCertRule_SelfSignedCertificateNotAllowed()
        {
            this.WriteEvent(16);
        }

        [Event(17, Level = EventLevel.Informational, Version = 1)]
        public void AllowSigningCertRule_SigningCertificateAllowed(string thumbprint)
        {
            this.WriteEvent(17, thumbprint);
        }

        [Event(18, Level = EventLevel.Error, Version = 1)]
        public void AllowSigningCertRule_SigningCertificateNotAllowed(string thumbprint)
        {
            this.WriteEvent(18, thumbprint);
        }

        [Event(19, Level = EventLevel.Error, Version = 1)]
        public void BlackListThumbprintRule_NotAllowed(string thumbprint)
        {
            this.WriteEvent(19, thumbprint);
        }

        [Event(20, Level = EventLevel.LogAlways, Version = 1)]
        public void BreakGlassCertificatesRule_BreakGlassUnlessBlackListed(string thumbprint)
        {
            this.WriteEvent(20, thumbprint);
        }

        [Event(21, Level = EventLevel.LogAlways, Version = 1)]
        public void BreakGlassThumbprintRule_BreakGlassUnlessBlackListed(string thumbprint)
        {
            this.WriteEvent(21, thumbprint);
        }

        [Event(22, Level = EventLevel.Error, Version = 1)]
        public void CertAccessor_GetCertsFromThumbprintOrFileNameFailed(string exception)
        {
            this.WriteEvent(22, exception);
        }

        [Event(23, Level = EventLevel.Warning, Version = 1)]
        public void ChainIsValidRule_Skipped(string issuer, string subject, string serialNumber, string thumbprint)
        {
            this.WriteEvent(23, issuer, subject, serialNumber, thumbprint);
        }

        [Event(24, Level = EventLevel.Error, Version = 1)]
        public void ChainIsValidRule_RemoteCertificateNotAvailable()
        {
            this.WriteEvent(24);
        }

        [Event(25, Level = EventLevel.LogAlways, Version = 1)]
        public void ChainIsValidRule_CertificateTrustChainValidationNotNeeded(string thumbprint, string sslPolicyErrors)
        {
            this.WriteEvent(25, thumbprint, sslPolicyErrors);
        }

        [Event(26, Level = EventLevel.Error, Version = 1)]
        public void ChainIsValidRule_ChainStatusNotAvailable(string sslPolicyErrors)
        {
            this.WriteEvent(26, sslPolicyErrors);
        }

        [Event(27, Level = EventLevel.Informational, Version = 1)]
        public void ChainIsValidRule_SkippingRevocationCheck(string allowed, string status, string issuer, string subject, string serialNumber, string thumbprint)
        {
            this.WriteEvent(27, allowed, status, issuer, subject, serialNumber, thumbprint);
        }

        [Event(28, Level = EventLevel.Error, Version = 1)]
        public void ChainIsValidRule_NotAllowed(string status, string issuer, string subject, string serialNumber, string thumbprint)
        {
            this.WriteEvent(28, status, issuer, subject, serialNumber, thumbprint);
        }

        [Event(29, Level = EventLevel.LogAlways, Version = 1)]
        public void ValidateCertificate_NullCertificate(bool succeeded, string reason)
        {
            this.WriteEvent(29, succeeded, reason);
        }

        [Event(30, Level = EventLevel.Informational, Version = 1)]
        public void ValidateCertificate_Succeeded(string serialNumber, string issuer, string subject, string thumbprint, string reason)
        {
            this.WriteEvent(30, serialNumber, issuer, subject, thumbprint, reason);
        }

        [Event(31, Level = EventLevel.Error, Version = 1)]
        public void ValidateCertificate_Failed(string serialNumber, string issuer, string subject, string thumbprint, string reason)
        {
            this.WriteEvent(31, serialNumber, issuer, subject, thumbprint, reason);
        }

        [Event(32, Level = EventLevel.Error, Version = 1)]
        public void IsCertificateTimeValidRule_InvalidX509Certificate2(string serialNumber)
        {
            this.WriteEvent(32, serialNumber);
        }

        [Event(33, Level = EventLevel.Error, Version = 1)]
        public void IsCertificateTimeValidRule_NotValid(string serialNumber, string notBefore, string notAfter)
        {
            this.WriteEvent(33, serialNumber, notBefore, notAfter);
        }

        [Event(34, Level = EventLevel.Error, Version = 1)]
        public void NotExtraLongValidityTimeCertificateRule_InvalidX509Certificate2(string serialNumber)
        {
            this.WriteEvent(34, serialNumber);
        }

        [Event(35, Level = EventLevel.Error, Version = 1)]
        public void NotExtraLongValidityTimeCertificateRule_ValidityPeriodExceedsLimit(string serialNumber, double validityPeriodInDays, double maxValidityPeriodInDays)
        {
            this.WriteEvent(35, serialNumber, validityPeriodInDays, maxValidityPeriodInDays);
        }

        [Event(36, Level = EventLevel.Error, Version = 1)]
        public void NotExtraLongValidityTimeCertificateRule_FailedToCalculateValidityTime(string serialNumber, string exception)
        {
            this.WriteEvent(36, serialNumber, exception);
        }
    }
#pragma warning restore
}