// <copyright file="AbstractCertificateRule.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Abstract rule to validate certificates and their chains
    /// </summary>
    public abstract class AbstractCertificateRule
    {
        /// <summary>
        /// indicates what certificate roles this rule applies to
        /// </summary>
        private RoleToApply appliesTo = RoleToApply.AllCerts;

        /// <summary>
        /// The roles this rule apply to
        /// </summary>
        [Flags]
        public enum RoleToApply
        {
            /// <summary>
            /// No applicable cert role
            /// </summary>
            None = 0,

            /// <summary>
            /// The rule applies to client certificates
            /// </summary>
            ClientCert = 1,

            /// <summary>
            /// The rule applies to server certificates
            /// </summary>
            ServerCert = 2,

            /// <summary>
            /// The rule applies to all certificates (client and server)
            /// </summary>
            AllCerts = 3,
        }

        /// <summary>
        /// The behavior this rule recommends for the certificate
        /// </summary>
        public enum Behavior
        {
            /// <summary>
            /// no behavior in the mask
            /// </summary>
            EmptyMask = 0,

            /// <summary>
            /// this certificate is NOT allowed, no matter any other rule
            /// </summary>
            BlackListed = 1,

            /// <summary>
            /// unless there is a blacklist rule, this certificate is allowed
            /// </summary>
            BreakGlassUnlessBlackListed = 2,

            /// <summary>
            /// this certificate is not allowed, unless there is a rule that break-glass (and in that case, also no rule blacklists it).
            /// </summary>
            NotAllowed = 4,

            /// <summary>
            /// this certificate is allowed unless there is any rule that blacklists it, or denies it
            /// </summary>
            Allowed = 8,

            /// <summary>
            /// this rule doesn't change the behavior for this certificate
            /// </summary>
            Neutral = 16,
        }

        /// <summary>
        /// Gets or sets a value indicating the certificate roles this rule applies to
        /// </summary>
        public RoleToApply AppliesTo
        {
            get
            {
                return this.appliesTo;
            }

            set
            {
                this.appliesTo = value;
            }
        }

        /// <summary>
        /// Sets the result of the rule to Allow or NotAllow only, flattening all other responses into one of those two. Neutral means Allow.
        /// </summary>
        /// <param name="neutralIsAllow">if true, this rule needs to return Allow wherever it would return Neutral or BreakGlassUnlessBlackListed</param>
        /// <returns>the rule with the new flag set</returns>
        public AbstractCertificateRule SetNeutralAsAllow(bool neutralIsAllow)
        {
            if (!neutralIsAllow)
            {
                return this;
            }

            AllowIfMaskRule rule = new AllowIfMaskRule(this, Behavior.Allowed | Behavior.Neutral);

            return rule.SetAppliesTo(this.AppliesTo);
        }

        /// <summary>
        /// This method sets the 'apply to' member of the rule, and returns the same rule
        /// </summary>
        /// <param name="apply">the new value for 'apply to'</param>
        /// <returns>this same rule</returns>
        public AbstractCertificateRule SetAppliesTo(RoleToApply apply)
        {
            this.AppliesTo = apply;
            return this;
        }

        /// <summary>
        /// Indicates what this rule prescribes to be for the behavior for the certificate.
        /// </summary>
        /// <param name="cert">the certificate</param>
        /// <param name="chain">the signature chain</param>
        /// <param name="sslPolicyErrors">the SSL errors as produced by the platform</param>
        /// <returns>the behavior as prescribed by this rule</returns>
        public abstract Behavior IsValid(X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors);

        /// <summary>
        /// Composes the base behavior with the new rule behavior.
        /// There is an initial 'base behavior' and subsequent 'new behaviors' that may modify what is the base behavior.
        /// For example, if we have three rules, the final behavior will be computed as follows:
        /// finalBehavior = Compose(Compose(Compose(baseBehavior, behaviorRule1), behaviorRule2), behaviorRule3).
        /// Basically, the idea is that after applying all rules, the final behavior is:
        ///   1) if the certificate is blacklisted in base or in any rule ----------------------> Blacklisted.
        ///   2) (else) if the base behavior or any rule consider the certificate BreakGlass ---> BreakGlass.
        ///   3) (else) if the base behavior is NotAllowed or any rule says NotAllowed ---------> NotAllowed.
        ///   4) (else) if any rule says Allowed -----------------------------------------------> Allowed.
        ///   5) the certificate is accepted only if the final behavior is 'BreakGlass' or 'Allowed'.
        /// </summary>
        /// <param name="baseBehavior">base behavior</param>
        /// <param name="newRuleBehavior">new rule behavior</param>
        /// <returns>the new base behavior</returns>
        internal static Behavior Compose(Behavior baseBehavior, Behavior newRuleBehavior)
        {
            if (baseBehavior == AbstractCertificateRule.Behavior.BlackListed || newRuleBehavior == AbstractCertificateRule.Behavior.BlackListed)
            {
                return AbstractCertificateRule.Behavior.BlackListed;
            }

            switch (newRuleBehavior)
            {
                case AbstractCertificateRule.Behavior.Neutral:
                    {
                        // no change.
                        break;
                    }

                case AbstractCertificateRule.Behavior.BreakGlassUnlessBlackListed:
                    {
                        // mark the behavior as break-glass
                        baseBehavior = newRuleBehavior;
                        break;
                    }

                case AbstractCertificateRule.Behavior.NotAllowed:
                    {
                        if (baseBehavior != AbstractCertificateRule.Behavior.BreakGlassUnlessBlackListed)
                        {
                            // we remember this is not allowed, but keep evaluating, just in case there is a break-glass rule later.
                            baseBehavior = newRuleBehavior;
                        }

                        break;
                    }

                case AbstractCertificateRule.Behavior.Allowed:
                    {
                        if (baseBehavior != AbstractCertificateRule.Behavior.NotAllowed && baseBehavior != AbstractCertificateRule.Behavior.BreakGlassUnlessBlackListed)
                        {
                            // record this as allowed, unless there was an earlier notallowed or break-glass
                            baseBehavior = newRuleBehavior;
                        }

                        break;
                    }

                default:
                    throw new ArgumentException("unknown behavior " + newRuleBehavior);
            }

            return baseBehavior;
        }
    }
}