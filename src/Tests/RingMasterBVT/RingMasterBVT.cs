// <copyright file="RingMasterBVT.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.BVT
{
    using System.Configuration;
    using System.Diagnostics;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Base class for RingMasterBVT tests.
    /// </summary>
    [TestClass]
    public class RingMasterBVT
    {
        /// <summary>
        /// IP Address of the ringmaster server to be tested.
        /// </summary>
        private string ringMasterAddress;

        /// <summary>
        /// Thumbprints of client certificates to use for SSL connection
        /// </summary>
        private string[] clientCertificateThumbprints;

        /// <summary>
        /// Thumbprints of server certificates to use for SSL connection
        /// </summary>
        private string[] serverCertificateThumbprints;

        /// <summary>
        /// Loads the client and server thumbprints from the configuration file.
        /// </summary>
        [TestInitialize]
        public void SetupTest()
        {
            this.ringMasterAddress = ConfigurationManager.AppSettings["RingMasterAddress"];

            this.clientCertificateThumbprints = null;
            this.serverCertificateThumbprints = null;

            if (bool.Parse(ConfigurationManager.AppSettings["SSL.UseSSL"]))
            {
                this.clientCertificateThumbprints = ConfigurationManager.AppSettings["SSL.ClientCerts"].Split(new char[] { ';', ',' });
                this.serverCertificateThumbprints = ConfigurationManager.AppSettings["SSL.ServerCerts"].Split(new char[] { ';', ',' });
            }

            RingMasterClient.TraceLevel = TraceLevel.Verbose;
        }

        /// <summary>
        /// Establishes a connection to the ring master server that is being exercised
        /// by this test.
        /// </summary>
        /// <returns>A <see cref="RingMasterClient"/> object that represents the connection.</returns>
        protected virtual RingMasterClient ConnectToRingMaster()
        {
            return this.ConnectToRingMaster(null);
        }

        /// <summary>
        /// Establishes a connection to the ring master server that is being exercised
        /// by this test.
        /// </summary>
        /// <param name="watcher">the watcher</param>
        /// <returns>A <see cref="RingMasterClient"/> object that represents the connection.</returns>
        protected virtual RingMasterClient ConnectToRingMaster(IWatcher watcher)
        {
            RingMasterClient rm = new RingMasterClient(this.ringMasterAddress, this.clientCertificateThumbprints, this.serverCertificateThumbprints, 15000, watcher);

            return rm;
        }
    }
}