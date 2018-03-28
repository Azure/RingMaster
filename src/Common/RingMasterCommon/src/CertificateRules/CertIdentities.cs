// <copyright file="CertIdentities.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// certificates identities
    /// </summary>
    public class CertIdentities
    {
        /// <summary>
        /// client identities to use
        /// </summary>
        private X509Certificate[] clientIdentities = null;

        /// <summary>
        /// server identity to use
        /// </summary>
        private X509Certificate serverIdentity;

        /// <summary>
        /// lookup for client identities to use
        /// </summary>
        private HashSet<X509Certificate> clientIdentitiesLookup;

        /// <summary>
        /// lookup for server identities to use
        /// </summary>
        private HashSet<X509Certificate> serverIdentitiesLookup;

        /// <summary>
        /// Gets the client identities to use
        /// </summary>
        public virtual X509Certificate[] ClientIdentities
        {
            get
            {
                return this.clientIdentities;
            }
        }

        /// <summary>
        /// Gets the server identity to use
        /// </summary>
        public virtual X509Certificate ServerIdentity
        {
            get
            {
                return this.serverIdentity;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether it is allowed clients with no certificate.
        /// </summary>
        public virtual bool IsClientWithNoCertificateAllowed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it is allowed servers with no certificate.
        /// </summary>
        public virtual bool IsServerWithNoCertificateAllowed { get; set; }

        /// <summary>
        /// Sets the client identities
        /// </summary>
        /// <param name="clientCerts">the certificates that can be used as client certificates</param>
        public virtual void SetClientIdentities(X509Certificate[] clientCerts)
        {
            if (clientCerts == null)
            {
                throw new ArgumentNullException(nameof(clientCerts));
            }

            X509Certificate[] newArray = clientCerts.ToArray();

            HashSet<X509Certificate> newHash = new HashSet<X509Certificate>(CertAccessor.Instance);
            foreach (X509Certificate c in newArray)
            {
                newHash.Add(c);
            }

            this.clientIdentities = newArray;
            this.clientIdentitiesLookup = newHash;
        }

        /// <summary>
        /// Sets the server identities
        /// </summary>
        /// <param name="serverCerts">the certificates that can be used as server identities. The first one is the one the process will use if it initiates a connection.</param>
        public virtual void SetServerIdentities(X509Certificate[] serverCerts)
        {
            if (serverCerts == null)
            {
                throw new ArgumentNullException(nameof(serverCerts));
            }

            X509Certificate[] newArray = serverCerts.ToArray();

            HashSet<X509Certificate> newHash = new HashSet<X509Certificate>(CertAccessor.Instance);
            foreach (X509Certificate c in newArray)
            {
                newHash.Add(c);
            }

            if (newArray.Length > 0)
            {
                this.serverIdentity = newArray[0];
            }

            this.serverIdentitiesLookup = newHash;
        }

        /// <summary>
        /// queries the instance to see if the given certificate is included in the list of client identities
        /// </summary>
        /// <param name="cert">certificate queried about</param>
        /// <returns>true if it is a client identity</returns>
        public virtual bool IsClientCertificateIncluded(X509Certificate cert)
        {
            if (this.IsClientWithNoCertificateAllowed)
            {
                return true;
            }

            return this.clientIdentitiesLookup.Contains(cert);
        }

        /// <summary>
        /// queries the instance to see if the given certificate is included in the list of server identities
        /// </summary>
        /// <param name="cert">the certificate queried about</param>
        /// <returns>true if it is a server identity</returns>
        public virtual bool IsServerCertificateIncluded(X509Certificate cert)
        {
            if (this.IsServerWithNoCertificateAllowed)
            {
                return true;
            }

            return this.serverIdentitiesLookup.Contains(cert);
        }
    }
}