// <copyright file="Certificates.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport
{
    using System;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Helper methods to deal with certificates
    /// </summary>
    public static class Certificates
    {
        /// <summary>
        ///  Certificate encoding version
        /// </summary>
        private const int CertificateEncodingVersion = 1;

        /// <summary>
        /// Get certificate object stored in the given file
        /// </summary>
        /// <param name="filePath">Path to the certificate file</param>
        /// <returns>Certificate object</returns>
        public static X509Certificate2 GetCertificateFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException(string.Format("File path does not exist : {0} ", filePath), filePath);
            }

            return new X509Certificate2(filePath);
        }

        /// <summary>
        /// Get certificate object identified by a string of the form MY/LOCALMACHINE/hash
        /// </summary>
        /// <param name="certPathAndHash">Certificate path and hash</param>
        /// <returns>Certificate object</returns>
        public static X509Certificate2 GetCertificateByPathAndHash(string certPathAndHash)
        {
            if (string.IsNullOrWhiteSpace(certPathAndHash))
            {
                throw new ArgumentException("certPathAndHash cannot be null or whitespace", "certPathAndHash");
            }

            var parts = certPathAndHash.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                throw new FormatException(string.Format("cerrPathAndHash {0} is not in the right format", certPathAndHash));
            }

            StoreName storeName;
            if (!Enum.TryParse<StoreName>(parts[0], out storeName))
            {
                storeName = StoreName.My;
            }

            StoreLocation storeLocation;
            if (!Enum.TryParse<StoreLocation>(parts[1], out storeLocation))
            {
                storeLocation = StoreLocation.LocalMachine;
            }

            return GetCertificateByHash(parts[2], storeName, storeLocation);
        }

        /// <summary>
        /// Get certificate object from the localmachine\my store or from the specified store
        /// </summary>
        /// <param name="certHash">Hash that identifies the certificate</param>
        /// <param name="storeName">Name of the certificate store in which to look for the certificate</param>
        /// <param name="storeLocation">Location of the store</param>
        /// <returns>Certificate Object</returns>
        public static X509Certificate2 GetCertificateByHash(
            string certHash,
            StoreName storeName = StoreName.My,
            StoreLocation storeLocation = StoreLocation.LocalMachine)
        {
            if (string.IsNullOrWhiteSpace(certHash))
            {
                throw new ArgumentException("certhash cannot be null or empty");
            }

            using (X509Store store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);

                var desiredCert = store.Certificates.Find(X509FindType.FindByThumbprint, certHash, false);
                if (desiredCert.Count == 0)
                {
                    throw new ArgumentException("Unable to find cert with hash {0} ", certHash);
                }

                return desiredCert[0];
            }
        }

        /// <summary>
        /// Get the encoded certificate array
        /// </summary>
        /// <param name="certificates">List of certificates to be encoded</param>
        /// <returns>Certificate encoded as string</returns>
        public static string GetEncodedCertificates(X509Certificate2[] certificates)
        {
            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream();
                using (BinaryWriter bw = new BinaryWriter(ms, System.Text.Encoding.Default, leaveOpen: true))
                {
                    ms = null;

                    // Write version
                    bw.Write(CertificateEncodingVersion);

                    if (certificates == null)
                    {
                        bw.Write(0);
                    }
                    else
                    {
                        // no of certs
                        bw.Write(certificates.Length);

                        foreach (X509Certificate2 cert in certificates)
                        {
                            byte[] certData = cert.Export(X509ContentType.Cert);
                            bw.Write(certData.Length);
                            bw.Write(certData);
                        }
                    }
                }

                return Convert.ToBase64String(ms.ToArray());
            }
            finally
            {
                if (ms != null)
                {
                    ms.Dispose();
                }
            }
        }

        /// <summary>
        /// Decodes the certificates encoded using GetEncodedCertificates
        /// </summary>
        /// <param name="encodedCert">Encoded certificate</param>
        /// <returns>List of certificates</returns>
        public static X509Certificate2[] GetDecodedCertificates(string encodedCert)
        {
            if (string.IsNullOrEmpty(encodedCert))
            {
                return new X509Certificate2[0];
            }

            byte[] encodedCertArr = Convert.FromBase64String(encodedCert);
            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream(encodedCertArr);
                using (BinaryReader br = new BinaryReader(ms))
                {
                    ms = null;

                    // read version
                    int ver = br.ReadInt32();

                    // v1 only. accept all

                    // No of certs
                    int certCount = br.ReadInt32();

                    X509Certificate2[] certs = new X509Certificate2[certCount];
                    for (int i = 0; i < certCount; i++)
                    {
                        int certDataLength = br.ReadInt32();
                        byte[] certData = br.ReadBytes(certDataLength);
                        certs[i] = new X509Certificate2(certData);
                    }

                    return certs;
                }
            }
            finally
            {
                if (ms != null)
                {
                    ms.Dispose();
                }
            }
        }
    }
}
