// <copyright file="CertAccessor.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CertificateRules
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Accessor to certificates metadata, for testing
    /// </summary>
    public class CertAccessor : IEqualityComparer<X509Certificate>
    {
        /// <summary>
        /// the instance for the certificate accessor, using CLR's standard classes.
        /// </summary>
        private static CertAccessor instance = new CertAccessor();

        /// <summary>
        /// Gets or sets a value indicating the global instance to use
        /// </summary>
        public static CertAccessor Instance
        {
            get
            {
                return instance;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                instance = value;
            }
        }

        /// <summary>
        /// Gets the name of the certs from thumb print or file.
        /// </summary>
        /// <param name="thumbprint">The thumbprints to look for.</param>
        /// <returns>array of certificates found</returns>
        /// <exception cref="System.Exception">
        /// two certs found in the same path
        /// or
        /// no cert found in path
        /// </exception>
        public virtual X509Certificate[] GetCertsFromThumbPrintOrFileName(string[] thumbprint)
        {
            if (thumbprint == null)
            {
                return null;
            }

            List<X509Certificate> certs = new List<X509Certificate>();

            for (int i = 0; i < thumbprint.Length; i++)
            {
                try
                {
                    string tp = thumbprint[i].ToUpper();
                    if (tp.StartsWith("FILE:"))
                    {
                        certs[i] = X509Certificate.CreateFromCertFile(tp.Substring("FILE:".Length));
                        continue;
                    }

                    StoreName name;
                    StoreLocation location;
                    string certThumbprint;

                    string[] pieces = tp.Split('/');
                    if (pieces.Length == 1)
                    {
                        // No store name in the cert. Use default
                        name = StoreName.My;
                        location = StoreLocation.LocalMachine;
                        certThumbprint = tp;
                    }
                    else
                    {
                        name = (StoreName)Enum.Parse(typeof(StoreName), pieces[0], true);
                        location = (StoreLocation)Enum.Parse(typeof(StoreLocation), pieces[1], true);
                        certThumbprint = pieces[2];
                    }

                    X509Store store = new X509Store(name, location);
                    try
                    {
                        store.Open(OpenFlags.ReadOnly);
                        X509Certificate2 found = null;
                        foreach (X509Certificate2 res in store.Certificates.Find(X509FindType.FindByThumbprint, certThumbprint, false))
                        {
                            if (found == null || found.Equals(res))
                            {
                                found = res;
                            }
                            else
                            {
                                throw new ArgumentException("two certs found in the same path " + tp);
                            }
                        }

                        if (found == null)
                        {
                            throw new KeyNotFoundException("no cert found in path " + tp);
                        }

                        certs.Add(found);
                    }
                    finally
                    {
                        store.Close();
                    }
                }
                catch (Exception e)
                {
                    CertificateRulesEventSource.Log.CertAccessor_GetCertsFromThumbprintOrFileNameFailed(e.ToString());
                }
            }

            return certs.ToArray();
        }

        /// <summary>
        /// returns the activation date
        /// </summary>
        /// <param name="cert">certificate queried</param>
        /// <returns>activation date</returns>
        public virtual DateTime NotBefore(X509Certificate cert)
        {
            X509Certificate2 certificate = this.AsV2(cert);
            if (certificate == null)
            {
                return default(DateTime);
            }

            return certificate.NotBefore;
        }

        /// <summary>
        /// returns the expiration date
        /// </summary>
        /// <param name="cert">certificate queried</param>
        /// <returns>expiration date</returns>
        public virtual DateTime NotAfter(X509Certificate cert)
        {
            X509Certificate2 certificate = this.AsV2(cert);
            if (certificate == null)
            {
                return default(DateTime);
            }

            return certificate.NotAfter;
        }

        /// <summary>
        /// returns the number of elements in the chain
        /// </summary>
        /// <param name="chain">the chain to explore</param>
        /// <returns>the number of elements, or 0 if null</returns>
        public virtual int ChainElementsCount(X509Chain chain)
        {
            if (chain == null)
            {
                return 0;
            }

            return chain.ChainElements.Count;
        }

        /// <summary>
        /// returns the array of chain status for the chain
        /// </summary>
        /// <param name="chain">the queried chain</param>
        /// <returns>the chain status array</returns>
        public virtual X509ChainStatus[] ChainStatus(X509Chain chain)
        {
            if (chain == null)
            {
                throw new ArgumentNullException("chain");
            }

            return chain.ChainStatus;
        }

        /// <summary>
        /// returns the certificate in the chain at the given position
        /// </summary>
        /// <param name="chain">the chain</param>
        /// <param name="i">the position</param>
        /// <returns>the certificate at such position</returns>
        public virtual X509Certificate ChainCertificateAtPosition(X509Chain chain, int i)
        {
            if (chain == null)
            {
                throw new ArgumentNullException("chain");
            }

            return chain.ChainElements[i].Certificate;
        }

        /// <summary>
        /// returns the serial number of the certificate
        /// </summary>
        /// <param name="certificate">certificate to explore</param>
        /// <returns>the serial number of the certificate</returns>
        public virtual string GetSerialNumberString(X509Certificate certificate)
        {
            if (certificate == null)
            {
                return string.Empty;
            }

            return certificate.GetSerialNumberString();
        }

        /// <summary>
        /// returns the issuer of the certificate
        /// </summary>
        /// <param name="certificate">certificate to explore</param>
        /// <returns>the issuer of the certificate</returns>
        public virtual string GetIssuer(X509Certificate certificate)
        {
            if (certificate == null)
            {
                return string.Empty;
            }

            return certificate.Issuer;
        }

        /// <summary>
        /// returns the subject of the certificate
        /// </summary>
        /// <param name="certificate">certificate to explore</param>
        /// <returns>the subject of the certificate</returns>
        public virtual string GetSubject(X509Certificate certificate)
        {
            if (certificate == null)
            {
                return string.Empty;
            }

            return certificate.Subject;
        }

        /// <summary>
        /// returns the thumbprint of the certificate
        /// </summary>
        /// <param name="cert">certificate to explore</param>
        /// <returns>the thumbprint of the certificate</returns>
        public virtual string GetThumbprint(X509Certificate cert)
        {
            X509Certificate2 certificate = this.AsV2(cert);
            if (certificate == null)
            {
                return string.Empty;
            }

            return certificate.Thumbprint;
        }

        /// <summary>
        /// Computes if the two certificates are equal
        /// </summary>
        /// <param name="x">first certificate</param>
        /// <param name="y">second certificate</param>
        /// <returns>true if both certs are equal</returns>
        public virtual bool Equals(X509Certificate x, X509Certificate y)
        {
            if (x == y)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return x.Equals(y);
        }

        /// <summary>
        /// Computes the hash code for the given object
        /// </summary>
        /// <param name="obj">the object to compute hash for</param>
        /// <returns>the hash code</returns>
        public virtual int GetHashCode(X509Certificate obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return obj.GetHashCode();
        }

        /// <summary>
        /// gets the v2 version of the certificate
        /// </summary>
        /// <param name="cert">v1 certificate</param>
        /// <returns>v2 certificate</returns>
        private X509Certificate2 AsV2(X509Certificate cert)
        {
            X509Certificate2 cert2 = cert as X509Certificate2;
            return cert2;
        }
    }
}