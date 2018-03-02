// <copyright file="BreakGlassThumbprintRule.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// allow no matter what (i.e. break-glass) a certificate by thumbprint
    /// </summary>
    public class BreakGlassThumbprintRule : AbstractCertificateRule
    {
        /// <summary>
        /// allow no matter what (i.e. break-glass) thumbprints
        /// </summary>
        private HashSet<string> thumbprints;

        /// <summary>
        /// if true all certificates are allow no matter what (i.e. break-glass)
        /// </summary>
        private bool allowAny;

        /// <summary>
        /// Initializes a new instance of the <see cref="BreakGlassThumbprintRule"/> class.
        /// </summary>
        /// <param name="thumbprints">thumbprints to be considered break-glass, or '*' for consider break-glass all thumbprints</param>
        public BreakGlassThumbprintRule(params string[] thumbprints)
        {
            this.thumbprints = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            if (thumbprints == null)
            {
                return;
            }

            foreach (string s in thumbprints)
            {
                if (s.Equals("*"))
                {
                    this.allowAny = true;
                }
                else
                {
                    this.thumbprints.Add(s);
                }
            }
        }

        /// <summary>
        /// Considers break-glass a certificate if the thumbprint is one of the thumbprints enumerated in the constructor.
        /// If no match is found, returns Neutral.
        /// </summary>
        /// <param name="cert">the certificate to evaluate</param>
        /// <param name="chain">the signature chain</param>
        /// <param name="sslPolicyErrors">SSL errors from platform</param>
        /// <returns>BreakGlassUnlessBlackListed if a match is found. if no match is found, returns Neutral.</returns>
        public override Behavior IsValid(X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // note that, unlike the break-glass rule, the breakglassthumbprint rule doesn't allow for an empty certificate
            if (cert == null)
            {
                return Behavior.Neutral;
            }

            if (this.allowAny || this.thumbprints.Contains(CertAccessor.Instance.GetThumbprint(cert)))
            {
                Trace.TraceInformation("ValidateSslPolicyErrors: BreakGlassThumbprint: Found breakglass thumbprint: {0}", CertAccessor.Instance.GetThumbprint(cert));
                return Behavior.BreakGlassUnlessBlackListed;
            }

            return Behavior.Neutral;
        }
    }
}