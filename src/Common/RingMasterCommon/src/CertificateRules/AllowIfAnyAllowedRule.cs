// <copyright file="AllowIfAnyAllowedRule.cs" company="Microsoft">
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
    public class AllowIfAnyAllowedRule : AbstractCertificateRule
    {
        /// <summary>
        /// rules to evaluate
        /// </summary>
        private IEnumerable<AbstractCertificateRule> rules;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowIfAnyAllowedRule"/> class.
        /// </summary>
        /// <param name="rules">rules to evaluate</param>
        public AllowIfAnyAllowedRule(IEnumerable<AbstractCertificateRule> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException("rules");
            }

            this.rules = rules;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowIfAnyAllowedRule"/> class.
        /// </summary>
        /// <param name="rules">rules to evaluate</param>
        public AllowIfAnyAllowedRule(params AbstractCertificateRule[] rules)
            : this((IEnumerable<AbstractCertificateRule>)rules)
        {
        }

        /// <summary>
        /// If any rule provide behavior 'Allowed' this rule returns 'Allowed', otherwise, it returns 'NotAllowed'
        /// If no match is found, returns NotAllowed.
        /// </summary>
        /// <param name="cert">the certificate to evaluate</param>
        /// <param name="chain">the signature chain</param>
        /// <param name="sslPolicyErrors">SSL errors from platform</param>
        /// <returns>Allowed is a match is found. if no match is found, returns NotAllowed.</returns>
        public override Behavior IsValid(X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            foreach (AbstractCertificateRule rule in this.rules)
            {
                Behavior b = rule.IsValid(cert, chain, sslPolicyErrors);

                if (b == Behavior.Allowed || b == Behavior.BreakGlassUnlessBlackListed)
                {
                    return b;
                }
            }

            Trace.TraceInformation("ValidateSslPolicyErrors: AllowIfAnyAllowed: no rule returned Allowed: {0}", CertAccessor.Instance.GetThumbprint(cert));
            return Behavior.NotAllowed;
        }
    }
}