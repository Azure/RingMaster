// <copyright file="RingMasterBVT.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.BVT
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Base class for RingMasterBVT tests.
    /// </summary>
    [TestClass]
    public class RingMasterBVT
    {
        /// <summary>
        /// Process of the ring master backend tool
        /// </summary>
        private static Process backendProcess;

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
        /// Starts the ring master backend tool
        /// </summary>
        [AssemblyInitialize]
        public static void StartBackendTool(TestContext context)
        {
            // Only start the backend tool on CloudBuild. In other environment, start it manually.
            if (Environment.GetEnvironmentVariable("TestEnvironment") == "QTEST")
            {
                backendProcess = new Process();
                backendProcess.StartInfo.FileName = Path.Combine(
                    Environment.CurrentDirectory,
                    @"backendtool\Microsoft.RingMaster.RingMasterBackendTool.exe");
                backendProcess.StartInfo.Arguments = "99";
                backendProcess.StartInfo.RedirectStandardOutput = false;
                backendProcess.StartInfo.UseShellExecute = true;
                backendProcess.StartInfo.CreateNoWindow = false;
                backendProcess.Start();
            }
        }

        /// <summary>
        /// Stops the ring master backend tool
        /// </summary>
        [AssemblyCleanup]
        public static void StopBackendTool()
        {
            backendProcess?.Kill();
            backendProcess?.Dispose();
        }

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
