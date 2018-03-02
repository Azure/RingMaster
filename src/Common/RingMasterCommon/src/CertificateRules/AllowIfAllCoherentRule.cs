// <copyright file="AllowIfAllCoherentRule.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// allow a given certificate if all rules agree in allowing it (by returning Allowed)
    /// </summary>
    public class AllowIfAllCoherentRule : AbstractCertificateRule
    {
        /// <summary>
        /// rules to evaluate
        /// </summary>
        private IEnumerable<AbstractCertificateRule> rules;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowIfAllCoherentRule"/> class.
        /// </summary>
        /// <param name="rules">rules to evaluate</param>
        public AllowIfAllCoherentRule(IEnumerable<AbstractCertificateRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException("rules");
            }

            this.rules = rules;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowIfAllCoherentRule"/> class.
        /// </summary>
        /// <param name="rules">rules to evaluate</param>
        public AllowIfAllCoherentRule(params AbstractCertificateRule[] rules)
            : this((IEnumerable<AbstractCertificateRule>)rules)
        {
        }

        /// <summary>
        /// The behavior is:
        ///   1) if the certificate is blacklisted in base or in any rule ----------------------> Blacklisted.
        ///   2) (else) if the base behavior or any rule consider the certificate break-glass --> BreakGlass.
        ///   3) (else) if the base behavior is NotAllowed or any rule says NotAllowed ---------> NotAllowed.
        ///   4) (else) if any rule says Allowed -----------------------------------------------> Allowed.
        ///   5) the certificate is accepted only if the final behavior is 'BreakGlass' or 'Allowed'.
        /// </summary>
        /// <param name="cert">the certificate to evaluate</param>
        /// <param name="chain">the signature chain</param>
        /// <param name="sslPolicyErrors">SSL errors from platform</param>
        /// <returns>Allowed is a match is found. if no match is found, returns NotAllowed.</returns>
        public override Behavior IsValid(X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            Behavior baseBeh = Behavior.Neutral;

            foreach (AbstractCertificateRule rule in this.rules)
            {
                Behavior b = rule.IsValid(cert, chain, sslPolicyErrors);

                baseBeh = AbstractCertificateRule.Compose(baseBeh, b);

                if (baseBeh == Behavior.BlackListed)
                {
                    Trace.TraceInformation("ValidateSslPolicyErrors: AllowIfAllCoherent: rule {0} returned {1}", rule.GetType(), b);
                    return Behavior.NotAllowed;
                }
            }

            switch (baseBeh)
            {
                case Behavior.Allowed:
                case Behavior.BreakGlassUnlessBlackListed:
                    return Behavior.Allowed;
                case Behavior.Neutral:
                    return Behavior.Neutral;
                case Behavior.BlackListed:
                case Behavior.NotAllowed:
                    Trace.TraceInformation("ValidateSslPolicyErrors: AllowIfAllCoherent: rules returned {0}", baseBeh);
                    return Behavior.NotAllowed;
                default:
                    return Behavior.NotAllowed;
            }
        }
    }
}