// <copyright file="BlackListThumbprintRule.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System;
    using System.Collections.Generic;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// blacklist a certificate by thumbprint
    /// </summary>
    public class BlackListThumbprintRule : AbstractCertificateRule
    {
        /// <summary>
        /// allowed thumbprints
        /// </summary>
        private HashSet<string> thumbprints;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackListThumbprintRule"/> class.
        /// </summary>
        /// <param name="thumbprints">the thumbprints to blacklist</param>
        public BlackListThumbprintRule(params string[] thumbprints)
        {
            this.thumbprints = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            if (thumbprints == null)
            {
                return;
            }

            foreach (string s in thumbprints)
            {
                this.thumbprints.Add(s);
            }
        }

        /// <summary>
        /// Blacklists a certificate if the thumbprint is one of the thumbprints enumerated in the constructor.
        /// If no match is found, returns Neutral.
        /// </summary>
        /// <param name="cert">the certificate to evaluate</param>
        /// <param name="chain">the signature chain</param>
        /// <param name="sslPolicyErrors">SSL errors from platform</param>
        /// <returns>Blacklisted if a match is found. if no match is found, returns Neutral.</returns>
        public override Behavior IsValid(X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (cert == null)
            {
                return Behavior.Neutral;
            }

            if (this.thumbprints.Contains(CertAccessor.Instance.GetThumbprint(cert)))
            {
                CertificateRulesEventSource.Log.BlackListThumbprintRule_NotAllowed(CertAccessor.Instance.GetThumbprint(cert));
                return Behavior.BlackListed;
            }

            return Behavior.Neutral;
        }
    }
}