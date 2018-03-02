// <copyright file="StandardValidationRuleSet.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Rule Builder to validate certificates and their chains according to standard policies
    /// </summary>
    public class StandardValidationRuleSet
    {
        /// <summary>
        /// data for the client certificate validation rules
        /// </summary>
        private readonly Data clientData = (Data)new Data().SetAppliesTo(AbstractCertificateRule.RoleToApply.ClientCert);

        /// <summary>
        /// data for the server certificate validation rules
        /// </summary>
        private readonly Data serverData = (Data)new Data().SetAppliesTo(AbstractCertificateRule.RoleToApply.ServerCert);

        /// <summary>
        /// Gets the client Certificate Validation Rule
        /// </summary>
        public AbstractCertificateRule ClientRule
        {
            get
            {
                return this.clientData;
            }
        }

        /// <summary>
        /// Gets the server Certificate Validation Rule
        /// </summary>
        public AbstractCertificateRule ServerRule
        {
            get
            {
                return this.serverData;
            }
        }

        /// <summary>
        /// Gets all Certificate Validation Rules
        /// </summary>
        public AbstractCertificateRule[] Rules
        {
            get
            {
                return new AbstractCertificateRule[] { this.ClientRule, this.ServerRule };
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the rules are for DEV-BOX test only
        /// </summary>
        public bool IsForTest
        {
            get
            {
                return this.clientData.ForTest;
            }

            set
            {
                this.clientData.DoInLock(() =>
                {
                    this.clientData.ForTest = value;
                });

                this.serverData.DoInLock(() =>
                {
                    this.serverData.ForTest = value;
                });
            }
        }

        /// <summary>
        /// Clear all entries in the object
        /// </summary>
        /// <param name="role">the role mask the operation applies to</param>
        public void Clear(AbstractCertificateRule.RoleToApply role)
        {
            foreach (Data d in this.GetConcerningData(role))
            {
                d.DoInLock(() =>
                {
                    d.AcceptedCerts.Clear();
                    d.AcceptedSubjects.Clear();
                    d.BlackListedCerts.Clear();
                    d.BreakGlassCerts.Clear();
                    d.AcceptedCertsFunctions.Clear();
                });
            }
        }

        /// <summary>
        /// Adds a function to consider certificates for accepting them
        /// </summary>
        /// <param name="role">the role mask the operation applies to</param>
        /// <param name="isCertIncluded">the function</param>
        public void AddAcceptedCertificates(AbstractCertificateRule.RoleToApply role, Func<X509Certificate, bool> isCertIncluded)
        {
            if (isCertIncluded == null)
            {
                throw new ArgumentNullException("isCertIncluded");
            }

            foreach (Data d in this.GetConcerningData(role))
            {
                d.DoInLock(() =>
                {
                    d.AcceptedCertsFunctions.Add(isCertIncluded);
                });
            }
        }

        /// <summary>
        /// Sets the requirement (by default is on) to validate the chain
        /// </summary>
        /// <param name="role">validation role mask for the addition</param>
        /// <param name="required">if true, the chain needs to be validated</param>
        public void SetChainValidationRequired(AbstractCertificateRule.RoleToApply role, bool required)
        {
            foreach (Data d in this.GetConcerningData(role))
            {
                d.DoInLock(() =>
                {
                    d.ForTest = !required;
                });
            }
        }

        /// <summary>
        /// Adds a certificate to be accepted
        /// </summary>
        /// <param name="role">validation role mask for the addition</param>
        /// <param name="cert">certificate to be accepted</param>
        public void AddAcceptedCertificate(AbstractCertificateRule.RoleToApply role, X509Certificate cert)
        {
            foreach (Data d in this.GetConcerningData(role))
            {
                d.DoInLock(() =>
               {
                   d.AcceptedCerts.Add(cert);
               });
            }
        }

        /// <summary>
        /// Adds a certificate subject to be accepted
        /// </summary>
        /// <param name="role">validation role mask for the addition</param>
        /// <param name="subject">subject accepted</param>
        /// <param name="signingthumbprintList">signing thumbprints accepted (separated by semicolon</param>
        public void AddAcceptedCertificateSubject(AbstractCertificateRule.RoleToApply role, string subject, IReadOnlyList<string> signingthumbprintList)
        {
            foreach (Data d in this.GetConcerningData(role))
            {
                d.DoInLock(() =>
                {
                    d.AcceptedSubjects.Add(new SubjectValidationRuleSet { SubjectName = subject, IssuerThumbprints = signingthumbprintList });
                });
            }
        }

        /// <summary>
        /// Adds a certificate to be accepted
        /// </summary>
        /// <param name="role">validation role mask for the addition</param>
        /// <param name="thumbprint">thumbprint to be accepted</param>
        public void AddAcceptedCertificateThumbprint(AbstractCertificateRule.RoleToApply role, string thumbprint)
        {
            this.AddAcceptedCertificate(role, CertAccessor.Instance.GetCertsFromThumbPrintOrFileName(new string[] { thumbprint })[0]);
        }

        /// <summary>
        /// Adds a certificate to be considered break-glass
        /// </summary>
        /// <param name="role">validation role mask for the addition</param>
        /// <param name="cert">certificate to be considered break-glass</param>
        public void AddBreakGlassCertificate(AbstractCertificateRule.RoleToApply role, X509Certificate cert)
        {
            foreach (Data d in this.GetConcerningData(role))
            {
                d.DoInLock(() =>
                {
                    d.BreakGlassCerts.Add(cert);
                });
            }
        }

        /// <summary>
        /// Adds a certificate to be considered break-glass
        /// </summary>
        /// <param name="role">validation role mask for the addition</param>
        /// <param name="thumbprint">thumbprint to be considered break-glass</param>
        public void AddBreakGlassCertificateThumbprint(AbstractCertificateRule.RoleToApply role, string thumbprint)
        {
            this.AddBreakGlassCertificate(role, CertAccessor.Instance.GetCertsFromThumbPrintOrFileName(new string[] { thumbprint })[0]);
        }

        /// <summary>
        /// Adds a certificate to be blacklisted
        /// </summary>
        /// <param name="role">validation role mask for the addition</param>
        /// <param name="thumbprint">thumbprint to be blacklisted</param>
        public void AddBlacklistedCertificateThumbprint(AbstractCertificateRule.RoleToApply role, string thumbprint)
        {
            foreach (Data d in this.GetConcerningData(role))
            {
                d.DoInLock(() =>
                {
                    d.BlackListedCerts.Add(thumbprint);
                });
            }
        }

        /// <summary>
        /// sets the maximum expiration time for certificates
        /// </summary>
        /// <param name="role">validation role mask for the addition</param>
        /// <param name="maxDays">maximum days allowed</param>
        public void SetMaxExpirationTime(AbstractCertificateRule.RoleToApply role, double maxDays)
        {
            foreach (Data d in this.GetConcerningData(role))
            {
                d.DoInLock(() =>
                {
                    d.MaxExpirationTime = TimeSpan.FromDays(maxDays);
                });
            }
        }

        /// <summary>
        /// returns the different data objects concerning to the given role validation
        /// </summary>
        /// <param name="role">the role validation</param>
        /// <returns>the different data objects related to the given role</returns>
        private IEnumerable<Data> GetConcerningData(AbstractCertificateRule.RoleToApply role)
        {
            if ((role & AbstractCertificateRule.RoleToApply.ClientCert) == AbstractCertificateRule.RoleToApply.ClientCert)
            {
                yield return this.clientData;
            }

            if ((role & AbstractCertificateRule.RoleToApply.ServerCert) == AbstractCertificateRule.RoleToApply.ServerCert)
            {
                yield return this.serverData;
            }
        }

        /// <summary>
        /// Subject validation rule set, containing certificate subject name and its possible issuer thumbprints
        /// </summary>
        internal struct SubjectValidationRuleSet
        {
            /// <summary>
            /// Gets or sets the issuer thumbprints
            /// </summary>
            public IReadOnlyList<string> IssuerThumbprints { get; set; }

            /// <summary>
            /// Gets or sets the subject name of certificate
            /// </summary>
            public string SubjectName { get; set; }
        }

        /// <summary>
        /// this class is a rule that is composed from the accepted certs/subjects, considered break-glass, and blacklisted lists
        /// </summary>
        private class Data : AbstractCertificateRule
        {
            /// <summary>
            /// lock object for the cached rule
            /// </summary>
            private readonly object cachedRuleLock = new object();

            /// <summary>
            /// cached rule
            /// </summary>
            private AbstractCertificateRule cachedRule = null;

            /// <summary>
            /// accepted certificates
            /// </summary>
            private HashSet<X509Certificate> acceptedCerts = new HashSet<X509Certificate>(CertAccessor.Instance);

            /// <summary>
            /// break-glass certificates
            /// </summary>
            private HashSet<X509Certificate> breakGlassCerts = new HashSet<X509Certificate>(CertAccessor.Instance);

            /// <summary>
            /// blacklisted certificates
            /// </summary>
            private HashSet<string> blackListedCerts = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            /// <summary>
            /// functions to compute cert that are in the allow list
            /// </summary>
            private HashSet<Func<X509Certificate, bool>> acceptedCertsFunctions = new HashSet<Func<X509Certificate, bool>>();

            /// <summary>
            /// accepted subjects
            /// </summary>
            private List<SubjectValidationRuleSet> acceptedSubjects = new List<SubjectValidationRuleSet>();

            /// <summary>
            /// the maximum expiration time allowed for certificates
            /// </summary>
            private TimeSpan maxExpirationTime = TimeSpan.MaxValue;

            /// <summary>
            /// Gets the accepted certificates
            /// </summary>
            internal HashSet<X509Certificate> AcceptedCerts
            {
                get
                {
                    return this.acceptedCerts;
                }
            }

            /// <summary>
            /// Gets the considered break-glass certificates
            /// </summary>
            internal HashSet<X509Certificate> BreakGlassCerts
            {
                get
                {
                    return this.breakGlassCerts;
                }
            }

            /// <summary>
            /// Gets the blacklisted certificates
            /// </summary>
            internal HashSet<string> BlackListedCerts
            {
                get
                {
                    return this.blackListedCerts;
                }
            }

            /// <summary>
            /// Gets the hash of functions to compute if a certificate is in the accepted list
            /// </summary>
            internal HashSet<Func<X509Certificate, bool>> AcceptedCertsFunctions
            {
                get
                {
                    return this.acceptedCertsFunctions;
                }
            }

            /// <summary>
            /// Gets the accepted subjects
            /// </summary>
            internal List<SubjectValidationRuleSet> AcceptedSubjects
            {
                get
                {
                    return this.acceptedSubjects;
                }
            }

            /// <summary>
            /// Gets or sets a value indicating whether this rule is for DEV-BOX test
            /// </summary>
            internal bool ForTest { get; set; }

            /// <summary>
            /// Gets or sets the maximum expiration time for accepted certificates
            /// </summary>
            internal TimeSpan MaxExpirationTime
            {
                get
                {
                    return this.maxExpirationTime;
                }

                set
                {
                    this.maxExpirationTime = value;
                }
            }

            /// <summary>
            /// Validates the certificate according to the configuration
            /// </summary>
            /// <param name="cert">certificate to validate</param>
            /// <param name="chain">certificate chain</param>
            /// <param name="sslPolicyErrors">SSL policy errors coming from the platform</param>
            /// <returns>the behavior to follow for this certificate</returns>
            public override Behavior IsValid(X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {
                AbstractCertificateRule rule = this.cachedRule;

                if (rule == null)
                {
                    lock (this.cachedRuleLock)
                    {
                        if (this.cachedRule == null)
                        {
                            this.cachedRule = this.RecomputeRuleSnapshot();
                        }

                        rule = this.cachedRule;
                    }
                }

                return rule.IsValid(cert, chain, sslPolicyErrors);
            }

            /// <summary>
            /// takes the action within a lock
            /// </summary>
            /// <param name="action">action to run</param>
            internal void DoInLock(Action action)
            {
                lock (this.cachedRuleLock)
                {
                    if (action != null)
                    {
                        action();
                    }

                    this.cachedRule = null;
                }
            }

            /// <summary>
            /// Re-computes the rule according to the configuration
            /// </summary>
            /// <returns>the new rule snapshot</returns>
            private AbstractCertificateRule RecomputeRuleSnapshot()
            {
                List<AbstractCertificateRule> accepted = new List<AbstractCertificateRule>();

                if (this.AcceptedCerts.Any())
                {
                    accepted.Add(new AzureValidationRule(this.Clone(this.AcceptedCerts).Contains, this.ForTest));
                }

                foreach (SubjectValidationRuleSet pair in this.AcceptedSubjects)
                {
                    accepted.Add(new AzureValidationRule(pair.SubjectName, pair.IssuerThumbprints.ToArray(), this.ForTest));
                }

                foreach (Func<X509Certificate, bool> func in this.AcceptedCertsFunctions)
                {
                    accepted.Add(new AzureValidationRule(func, this.ForTest));
                }

                List<AbstractCertificateRule> rules = new List<AbstractCertificateRule>();

                if (accepted.Count > 0)
                {
                    rules.Add(new AllowIfAnyAllowedRule(accepted));
                }

                if (this.BreakGlassCerts.Any())
                {
                    rules.Add(new BreakGlassCertificatesRule(this.BreakGlassCerts.ToArray(), allowNullInArray: true));
                }

                if (this.BlackListedCerts.Any())
                {
                    rules.Add(new BlackListThumbprintRule(this.BlackListedCerts.ToArray()));
                }

                if (this.MaxExpirationTime != TimeSpan.MaxValue)
                {
                    rules.Add(new NotExtraLongValidityTimeCertificateRule(this.MaxExpirationTime));
                }

                return new AllowIfAllCoherentRule(rules.ToArray());
            }

            /// <summary>
            /// Clones a hash Set of certificates
            /// </summary>
            /// <param name="hash">the original hash set of certificates</param>
            /// <returns>the cloned hash set of certificates</returns>
            private HashSet<X509Certificate> Clone(HashSet<X509Certificate> hash)
            {
                return new HashSet<X509Certificate>(hash, CertAccessor.Instance);
            }
        }
    }
}