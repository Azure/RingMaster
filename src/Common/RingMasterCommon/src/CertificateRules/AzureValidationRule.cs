// <copyright file="AzureValidationRule.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Validates the certificate according to Azure standards
    /// </summary>
    public class AzureValidationRule : AbstractCertificateRule
    {
        /// <summary>
        /// Default Flags
        /// </summary>
        public const CertificateRulesFlags DefaultFlags = CertificateRulesFlags.MustCheckCertificateRevocation | CertificateRulesFlags.MustCheckCertificateTrustChain;

        /// <summary>
        /// The composed rule to validate
        /// </summary>
        private AbstractCertificateRule rule;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureValidationRule"/> class.
        /// </summary>
        /// <param name="subjectRequired">subject for the certificate to be allowed</param>
        /// <param name="signingThumbprintsRequired">thumbprints allowed for the certificate signing the validated certificate</param>
        /// <param name="forTest">if true some relaxations are made for test in DEV-BOX only</param>
        public AzureValidationRule(string subjectRequired, string[] signingThumbprintsRequired, bool forTest = false)
        {
            this.rule = new AllowIfAllAllowedRule(
                new IsCertificateTimeValidRule().SetNeutralAsAllow(true),
                new ChainIsValidRule(!forTest).SetNeutralAsAllow(true),
                new AllowCertSubjectRule(new string[] { subjectRequired }),
                new AllowSigningCertRule(signingThumbprintsRequired));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureValidationRule"/> class.
        /// </summary>
        /// <param name="isCertificateIncluded">the function that says if a certificate is included in the list</param>
        /// <param name="forTest">if true some relaxations are made for test in DEV-BOX only</param>
        public AzureValidationRule(Func<X509Certificate, bool> isCertificateIncluded, bool forTest = false)
        {
            this.rule = new AllowIfAllAllowedRule(
                new IsCertificateTimeValidRule().SetNeutralAsAllow(true),
                new ChainIsValidRule(!forTest).SetNeutralAsAllow(true),
                new AllowCertificatesRule(isCertificateIncluded));
        }

        /// <summary>
        /// Allows a certificate if it is according to the Azure standards
        /// </summary>
        /// <param name="cert">certificate to validate</param>
        /// <param name="chain">signature chain</param>
        /// <param name="sslPolicyErrors">SSL policy errors on the validation</param>
        /// <returns>the desired behavior</returns>
        public override Behavior IsValid(X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return this.rule.IsValid(cert, chain, sslPolicyErrors);
        }
    }
}