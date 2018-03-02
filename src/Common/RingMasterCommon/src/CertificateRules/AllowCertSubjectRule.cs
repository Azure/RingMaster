// <copyright file="AllowCertSubjectRule.cs" company="Microsoft">
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
    /// allow a given certificate by subject
    /// </summary>
    public class AllowCertSubjectRule : AbstractCertificateRule
    {
        /// <summary>
        /// allowed subjects
        /// </summary>
        private HashSet<string> subjects;

        /// <summary>
        /// if true, allow any subject
        /// </summary>
        private bool allowAny;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowCertSubjectRule"/> class.
        /// </summary>
        /// <param name="subjects">subjects allowed by this rule. A string '*' indicates 'any subject is valid'</param>
        public AllowCertSubjectRule(string[] subjects)
        {
            this.subjects = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            if (subjects == null)
            {
                return;
            }

            foreach (string s in subjects)
            {
                if (s.Equals("*"))
                {
                    this.allowAny = true;
                }
                else
                {
                    this.subjects.Add(s);
                }
            }
        }

        /// <summary>
        /// Allows a certificate if the subject is one of the subjects enumerated in the constructor (or '*' indicating allowAny).
        /// If no match is found, returns NotAllowed.
        /// </summary>
        /// <param name="cert">the certificate to evaluate</param>
        /// <param name="chain">the signature chain</param>
        /// <param name="sslPolicyErrors">SSL errors from platform</param>
        /// <returns>Allowed is a match is found. if no match is found, returns NotAllowed.</returns>
        public override Behavior IsValid(X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (cert == null)
            {
                Trace.TraceInformation("ValidateSslPolicyErrors: AllowCertSubject: cert was null:");
                return Behavior.NotAllowed;
            }

            if (this.allowAny || this.subjects.Contains(CertAccessor.Instance.GetSubject(cert)))
            {
                Trace.TraceInformation("ValidateSslPolicyErrors: AllowCertSubject: Found allowed subject: {0}", CertAccessor.Instance.GetSubject(cert));
                return Behavior.Allowed;
            }

            Trace.TraceError("ValidateSslPolicyErrors: AllowCertSubject: Found no allowed subject: {0}", CertAccessor.Instance.GetSubject(cert));

            return Behavior.NotAllowed;
        }
    }
}