// <copyright file="CertificateValidator.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using CertificateRules;

    /// <summary>
    /// This class helps build the CertificateValidator rules
    /// </summary>
    public class CertificateValidator
    {
        /// <summary>
        /// the default flags to use
        /// </summary>
        public const CertificateRulesFlags DefaultFlags = CertificateRulesFlags.MustCheckCertificateRevocation | CertificateRulesFlags.MustCheckCertificateTrustChain;

        /// <summary>
        /// the additional rules to apply to certificates
        /// </summary>
        private IList<AbstractCertificateRule> extraRulesTovalidateCertificates;

        /// <summary>
        /// instrumentation to use for this SSL wrapping rules
        /// </summary>
        private ICertificateRulesInstrumentation instrumentation;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateValidator"/> class.
        /// </summary>
        /// <param name="identities">identities for this object</param>
        /// <param name="flags">flags for the SSL wrapper</param>
        /// <param name="modifySet">action to modify the default rule set</param>
        public CertificateValidator(CertIdentities identities, CertificateRulesFlags flags, Action<StandardValidationRuleSet> modifySet)
            : this(identities, flags, modifySet, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateValidator"/> class.
        /// </summary>
        /// <param name="identities">identities for this object</param>
        /// <param name="flags">flags for the SSL wrapper</param>
        /// <param name="instrumentation">instrumentation to use</param>
        public CertificateValidator(CertIdentities identities, CertificateRulesFlags flags, ICertificateRulesInstrumentation instrumentation = null)
            : this(identities, flags, null, instrumentation)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateValidator"/> class.
        /// </summary>
        /// <param name="identities">identities for this object</param>
        /// <param name="flags">flags for the SSL wrapper</param>
        /// <param name="modifySet">action to modify the default rule set</param>
        /// <param name="instrumentation">instrumentation to use</param>
        public CertificateValidator(CertIdentities identities, CertificateRulesFlags flags, Action<StandardValidationRuleSet> modifySet, ICertificateRulesInstrumentation instrumentation)
        {
            if (identities == null)
            {
                throw new ArgumentNullException("identities");
            }

            if (instrumentation == null)
            {
                instrumentation = SslWrappingInstrumentation.NullInstrumentation;
            }

            this.instrumentation = instrumentation;

            StandardValidationRuleSet set = new StandardValidationRuleSet();

            set.AddAcceptedCertificates(AbstractCertificateRule.RoleToApply.ClientCert, identities.IsClientCertificateIncluded);
            set.AddAcceptedCertificates(AbstractCertificateRule.RoleToApply.ServerCert, identities.IsServerCertificateIncluded);

            if (identities.IsClientWithNoCertificateAllowed)
            {
                set.AddBreakGlassCertificate(AbstractCertificateRule.RoleToApply.ClientCert, null);
                set.SetChainValidationRequired(AbstractCertificateRule.RoleToApply.ClientCert, false);
            }

            if (identities.IsServerWithNoCertificateAllowed)
            {
                set.AddBreakGlassCertificate(AbstractCertificateRule.RoleToApply.ServerCert, null);
            }

            if ((flags & CertificateRulesFlags.MustCheckCertificateTrustChain) == CertificateRulesFlags.None)
            {
                set.SetChainValidationRequired(AbstractCertificateRule.RoleToApply.AllCerts, false);
            }

            if (modifySet != null)
            {
                modifySet(set);
            }

            this.extraRulesTovalidateCertificates = set.Rules;

            this.BaseRule = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateValidator"/> class.
        /// </summary>
        /// <param name="rule">validation rule.</param>
        /// <param name="instrumentation">instrumentation to use</param>
        public CertificateValidator(AbstractCertificateRule rule, ICertificateRulesInstrumentation instrumentation = null)
        {
            if (instrumentation == null)
            {
                instrumentation = SslWrappingInstrumentation.NullInstrumentation;
            }

            this.instrumentation = instrumentation;

            this.extraRulesTovalidateCertificates = new List<AbstractCertificateRule>();

            if (rule != null)
            {
                this.extraRulesTovalidateCertificates.Add(rule);
            }

            this.BaseRule = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateValidator"/> class.
        /// </summary>
        /// <param name="rules">validation rules.</param>
        /// <param name="instrumentation">instrumentation to use</param>
        public CertificateValidator(IEnumerable<AbstractCertificateRule> rules, ICertificateRulesInstrumentation instrumentation = null)
        {
            if (instrumentation == null)
            {
                instrumentation = SslWrappingInstrumentation.NullInstrumentation;
            }

            this.instrumentation = instrumentation;

            if (rules == null)
            {
                rules = new AbstractCertificateRule[0];
            }

            this.extraRulesTovalidateCertificates = rules.ToArray();

            this.BaseRule = null;
        }

        /// <summary>
        /// Gets or sets the base rule to compute certificate behavior
        /// </summary>
        public AbstractCertificateRule BaseRule { get; set; }

        /// <summary>
        /// returns the SSL wrapper flags for the given configuration
        /// </summary>
        /// <param name="mustCheckCertificateRevocation">if true, it indicates that the certificate revocation must be verified</param>
        /// <param name="mustCheckCertificateTrustChain">if true, it indicates that the certificate chain must be verified</param>
        /// <returns>the SSL wrapper validation flags</returns>
        public static CertificateRulesFlags GetFlags(bool mustCheckCertificateRevocation, bool mustCheckCertificateTrustChain)
        {
            CertificateRulesFlags result = CertificateRulesFlags.None;

            if (mustCheckCertificateRevocation)
            {
                result |= CertificateRulesFlags.MustCheckCertificateRevocation;
            }

            if (mustCheckCertificateTrustChain)
            {
                result |= CertificateRulesFlags.MustCheckCertificateTrustChain;
            }

            return result;
        }

        /// <summary>
        /// Returns the rules built from configuration.
        /// The following are example of configuration settings:
        ///     "SSL.ClientCerts" --> example "MY/LOCALMACHINE/AF45CF47774649C6627F8CB9CDE4D5ACAE5B92F2,MY/LOCALMACHINE/1923316B5190CDEF09EF5BF1C3C491843310695A"
        ///       This setting indicates the thumbprints to be used by client as its possible identities
        ///       additionally, it tells the server what client thumbprints are allowed
        ///     "SSL.ServerCerts" --> example "MY/LOCALMACHINE/AF45CF47774649C6627F8CB9CDE4D5ACAE5B92F2,MY/LOCALMACHINE/1923316B5190CDEF09EF5BC1F3C491843310695A"
        ///       This setting indicates the thumbprint to be used in server as its identity(i.e.the first thumbprint if this is a list)
        ///       additionally, it tells the client what server thumbprints are allowed
        ///     "SSL.BlacklistedThumbprints" --> example "MY/LOCALMACHINE/4565CF47774649C6627F8CB9CDE4D5ACAE5B92F2"
        ///       This setting blacklists certain certificates by thumbprint
        ///     "SSL.BreakGlassThumbprints" --> example "MY/LOCALMACHINE/1235CF47774649C6627F8CB9CDE4D5ACAE5B92F2"
        ///       This setting break-glass certain certificates by thumbprint.
        ///     "SSL.AllowedSigningThumbprints" --> example "*,[*]"
        ///       "*" means any cert signing the given cert is okay, but the cert must be signed by a chain
        ///       "[*]" means the cert can be self-signed
        ///       "*,[*]" is equivalent to not including this setting at all, or to have it empty
        ///       Note: not including this setting at all, or to have it empty means we require the cert to be signed, but we don't 'like' any signing cert, with discards all certs not considered break-glass.
        ///     "SSL.AllowedSubjectNames" --> example "*"
        ///       "*" means any cert subject is okay.
        ///       Note: not including this setting at all, or to have it empty means we don't 'like' any cert subject, with discards all certs not considered break-glass.
        ///     "SSL.MaxValidityCertsInDays" --> example "36500"
        ///       if set, limits the maximum validity time (in days) for certificates. By default, it doesn't limit the validity time.
        ///     "SSL.RelaxValidationForTestCertificates" --> example "false"
        ///       if true, this setting allows for certificates issued for a different use, or for chains not trusted.ONLY to be used on DEV-BOX testing.
        ///       by default the value is "false"
        /// </summary>
        /// <param name="flags">the flags to be used for the SSL wrapper</param>
        /// <param name="getSettingValue">optionally, the function to retrieve config values by setting name. if not provided app.config will be used.</param>
        /// <returns>the set of validation rules from configuration.</returns>
        public static IEnumerable<AbstractCertificateRule> GetCertRulesFromConfig(out CertificateRulesFlags flags, Func<string, string> getSettingValue = null)
        {
            flags = CertificateRulesFlags.MustCheckCertificateRevocation | CertificateRulesFlags.MustCheckCertificateTrustChain;

            Func<string, string[]> getSetting = new Func<string, string[]>(
                settingName =>
                {
                    try
                    {
                        string setvalue = getSettingValue != null ? getSettingValue(settingName) : ConfigurationManager.AppSettings[settingName];

                        if (setvalue == null)
                        {
                            setvalue = string.Empty;
                        }

                        return setvalue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToArray();
                    }
                    catch (Exception)
                    {
                        return new string[0];
                    }
                });

            string[] blacklistedThumbprints = getSetting("SSL.BlacklistedThumbprints");
            string[] breakGlassThumbprints = getSetting("SSL.BreakGlassThumbprints");
            string[] allowedSubjectNames = getSetting("SSL.AllowedSubjectNames");
            string[] allowedSigningThumbprints = getSetting("SSL.AllowedSigningThumbprints");

            string[] maxValidityDays = getSetting("SSL.MaxValidityCertsInDays");

            string[] relaxValidation = getSetting("SSL.RelaxValidationForTestCertificates");
            bool relax = false;

            if (relaxValidation.Length > 0)
            {
                if (bool.TryParse(relaxValidation[0], out relax) && relax)
                {
                    flags &= ~CertificateRulesFlags.MustCheckCertificateTrustChain;
                }
            }

            StandardValidationRuleSet set = new StandardValidationRuleSet();

            set.SetChainValidationRequired(AbstractCertificateRule.RoleToApply.AllCerts, !relax);

            foreach (string thumb in blacklistedThumbprints)
            {
                set.AddBlacklistedCertificateThumbprint(AbstractCertificateRule.RoleToApply.AllCerts, thumb);
            }

            foreach (string thumb in breakGlassThumbprints)
            {
                set.AddBreakGlassCertificateThumbprint(AbstractCertificateRule.RoleToApply.AllCerts, thumb);
            }

            if (allowedSubjectNames.Length == allowedSigningThumbprints.Length)
            {
                for (int i = 0; i < allowedSubjectNames.Length; i++)
                {
                    set.AddAcceptedCertificateSubject(AbstractCertificateRule.RoleToApply.AllCerts, allowedSubjectNames[i], allowedSigningThumbprints[i].Split(';'));
                }
            }
            else
            {
                throw new ArgumentException("configuration must have same number of allowedSubjectNames as AllowedSigningThumbprints");
            }

            // also, we don't want certificates with validity larger than n days
            if (maxValidityDays.Length > 0)
            {
                double days;

                if (double.TryParse(maxValidityDays[0], out days) && days > 0)
                {
                    set.SetMaxExpirationTime(AbstractCertificateRule.RoleToApply.AllCerts, days);
                }
            }

            return set.Rules;
        }

        /// <summary>
        /// Validates the client certificate.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="chain">The chain.</param>
        /// <param name="sslPolicyErrors">The SSL policy errors.</param>
        /// <returns><c>true</c> if the client satisfies the SSL requirements, <c>false</c> otherwise.</returns>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "sender", Justification = "sender not used")]
        public bool ValidateClientCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            return this.ValidateSslPolicyErrors(certificate, chain, sslPolicyErrors, AbstractCertificateRule.RoleToApply.ClientCert);
        }

        /// <summary>
        /// Validates the server certificate.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="chain">The chain.</param>
        /// <param name="sslPolicyErrors">The SSL policy errors.</param>
        /// <returns><c>true</c> if the server satisfies the SSL requirements, <c>false</c> otherwise.</returns>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "sender", Justification = "sender not used")]
        public bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            return this.ValidateSslPolicyErrors(certificate, chain, sslPolicyErrors, AbstractCertificateRule.RoleToApply.ServerCert);
        }

        /// <summary>
        /// Validates SSL Policy Errors
        /// </summary>
        /// <param name="certificate">Certificate that is being validated</param>
        /// <param name="chain">Result of X509 chain build</param>
        /// <param name="sslPolicyErrors">SSL policy errors returned by the validation</param>
        /// <param name="roleToApply">role (clientCert/serverCert) to validate</param>
        /// <returns>True if the certificate trust chain validates and if CRL validation succeeds or CRL is offline. False if trust chain validations fail or CRL says certificate is revoked</returns>
        private bool ValidateSslPolicyErrors(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors, AbstractCertificateRule.RoleToApply roleToApply)
        {
            // the behavior for this certificate
            AbstractCertificateRule.Behavior beh = AbstractCertificateRule.Behavior.Neutral;
            string reason = "no baseRule existed";

            if (this.BaseRule != null)
            {
                beh = this.BaseRule.IsValid(certificate, chain, sslPolicyErrors);
                reason = "baserule " + this.BaseRule.GetType() + " set it to " + beh;
            }

            // evaluate all rules for the given role
            foreach (AbstractCertificateRule rule in this.extraRulesTovalidateCertificates)
            {
                if ((rule.AppliesTo & roleToApply) == AbstractCertificateRule.RoleToApply.None)
                {
                    continue;
                }

                AbstractCertificateRule.Behavior thisB = rule.IsValid(certificate, chain, sslPolicyErrors);

                AbstractCertificateRule.Behavior newBeh = AbstractCertificateRule.Compose(beh, thisB);

                if (beh != newBeh)
                {
                    reason = "rule " + rule.GetType() + " set it to " + newBeh;
                }

                beh = newBeh;

                if (beh == AbstractCertificateRule.Behavior.BlackListed)
                {
                    this.instrumentation.ValidateCertificateCompleted(certificate, succeeded: false, reason: "rule " + rule.GetType() + " blacklisted it");
                    return false;
                }
            }

            if (beh == AbstractCertificateRule.Behavior.Allowed || beh == AbstractCertificateRule.Behavior.BreakGlassUnlessBlackListed)
            {
                this.instrumentation.ValidateCertificateCompleted(certificate, succeeded: true, reason: reason);
                return true;
            }

            this.instrumentation.ValidateCertificateCompleted(certificate, succeeded: false, reason: reason);
            return false;
        }
    }
}