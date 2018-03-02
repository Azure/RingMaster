// <copyright file="AllowIfMaskRule.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System;
    using System.Diagnostics;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// allow a given certificate if the given rule follows the given mask
    /// </summary>
    public class AllowIfMaskRule : AbstractCertificateRule
    {
        /// <summary>
        /// the mask to compare with
        /// </summary>
        private Behavior mask;

        /// <summary>
        /// the rule to evaluate
        /// </summary>
        private AbstractCertificateRule rule;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowIfMaskRule"/> class.
        /// </summary>
        /// <param name="rule">rule to evaluate</param>
        /// <param name="mask">mask to compare with</param>
        public AllowIfMaskRule(AbstractCertificateRule rule, Behavior mask)
        {
            if (rule == null)
            {
                throw new ArgumentNullException("rule");
            }

            this.rule = rule;
            this.mask = mask;
        }

        /// <summary>
        /// If the rule provides behavior intersecting with the mask, this returns Allowed
        /// If no match is found, returns NotAllowed.
        /// </summary>
        /// <param name="cert">the certificate to evaluate</param>
        /// <param name="chain">the signature chain</param>
        /// <param name="sslPolicyErrors">SSL errors from platform</param>
        /// <returns>Allowed is a match is found. if no match is found, returns NotAllowed.</returns>
        public override Behavior IsValid(X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            Behavior b = this.rule.IsValid(cert, chain, sslPolicyErrors);

            if (b == this.mask || (b & this.mask) != Behavior.EmptyMask)
            {
                return Behavior.Allowed;
            }

            Trace.TraceInformation("ValidateSslPolicyErrors: AllowIfMaskRule: rule {0} returned {1}", this.rule.GetType(), b);
            return b == Behavior.BlackListed ? b : Behavior.NotAllowed;
        }
    }
}