// <copyright file="AllowCertSubjectRule.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System;
    using System.Collections.Generic;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;

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
        /// if true, compare cert subject to allowed wildcard subject for match
        /// </summary>
        private bool isWildCard;

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
                else if (IsWildCard(s))
                {
                    this.isWildCard = true;
                    this.subjects.Add(s);
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
                CertificateRulesEventSource.Log.AllowCertSubjectRuleCertificatesRules_CertWasNull();
                return Behavior.NotAllowed;
            }

            if (this.isWildCard)
            {
                if (this.WildcardSubjectsMatch(cert, this.subjects))
                {
                    CertificateRulesEventSource.Log.AllowCertSubjectRule_CertAllowed(CertAccessor.Instance.GetSubject(cert));
                    return Behavior.Allowed;
                }
            }

            if (this.allowAny || this.subjects.Contains(CertAccessor.Instance.GetSubject(cert)))
            {
                CertificateRulesEventSource.Log.AllowCertSubjectRule_CertAllowed(CertAccessor.Instance.GetSubject(cert));
                return Behavior.Allowed;
            }

            CertificateRulesEventSource.Log.AllowCertSubjectRule_CertNotAllowed(CertAccessor.Instance.GetSubject(cert));
            return Behavior.NotAllowed;
        }

        // Return Regex pattern
        private static string WildcardSubjectToRegex(string wildcardSubject)
        {
            // RFC 952 - http://tools.ietf.org/html/rfc952
            // A "name" (Net, Host, Gateway, or Domain name) is a text string up
            // to 24 characters drawn from the alphabet (A-Z), digits (0-9), minus
            // sign (-), and period (.).  Note that periods are only allowed when
            // they serve to delimit components of "domain style names".
            // RFC 2818 - http://www.ietf.org/rfc/rfc2818.txt
            // "...Names may contain the wildcard character * which is considered to match any single domain name
            // component or component fragment. E.g., *.a.com matches foo.a.com but
            // not bar.foo.a.com... "
            return '^' + Regex.Escape(wildcardSubject).Replace("\\*", "[a-zA-Z0-9-]*").Replace("\\?", "[a-zA-Z0-9-]?") + '$';
        }

        /// <summary>
        /// Checks if allowed subjects contains wildcard.
        /// If no match is found, returns NotAllowed.
        /// </summary>
        /// <param name="allowedSubject">the allowed certificate subject name to evaluate</param>
        /// <returns>True if wildcard subjectname is found in allowed subjects, else false.</returns>
        private static bool IsWildCard(string allowedSubject)
        {
            for (int i = 0; i < allowedSubject.Length; i++)
            {
                if (allowedSubject[i] == '*' || allowedSubject[i] == '?')
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Compare allowed wildcard subject to actual certificate subject name, evaluate for match.
        /// </summary>
        /// <param name="cert">The certificate to pull subject from</param>
        /// <param name="allowedSubjectNames">Allow listed subject names</param>
        /// <returns>True if match, else false</returns>
        private bool WildcardSubjectsMatch(X509Certificate cert, HashSet<string> allowedSubjectNames)
        {
            foreach (string subjectName in allowedSubjectNames)
            {
                if (IsWildCard(subjectName))
                {
                    string pattern = WildcardSubjectToRegex(subjectName);
                    if (string.Equals(subjectName, CertAccessor.Instance.GetSubject(cert), StringComparison.OrdinalIgnoreCase) ||
                        Regex.IsMatch(CertAccessor.Instance.GetSubject(cert), pattern, RegexOptions.IgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}