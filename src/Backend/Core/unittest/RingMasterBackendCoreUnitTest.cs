// <copyright file="RingMasterBackendCoreUnitTest.cs" company="Microsoft">
//     Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterBackendCoreUnitTest
{
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.InMemory;
    using VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Base class for RingMaster backend core unittests.
    /// </summary>
    [TestClass]
    public class RingMasterBackendCoreUnitTest
    {
        private RingMasterBackendCore backend;
        private PseudoNodes ps;

        protected RingMasterBackendCoreUnitTest()
        {
            this.backend = CreateBackend(null);
        }

        /// <summary>
        /// Creates a new backend with an in-memory store
        /// </summary>
        /// <param name="auditConsumer">Interface to an object that can consume audit events</param>
        /// <returns>Backend instance</returns>
        protected static RingMasterBackendCore CreateBackend(IRingMasterAudit auditConsumer)
        {
            var backendStarted = new ManualResetEventSlim();
            Trace.TraceInformation("RingMasterBackendCoreUnitTest.CreateBackend");

            RingMasterBackendCore.GetSettingFunction = GetSetting;
            var backend = new RingMasterBackendCore(new InMemoryFactory(), auditConsumer);

            backend.StartService = (p1, p2) => { backendStarted.Set(); };
            backend.Start(CancellationToken.None);
            backend.OnBecomePrimary();
            backendStarted.Wait(30000).Should().BeTrue();
            Trace.TraceInformation("RingMasterBackendCoreUnitTest.BackendStarted");
            return backend;
        }

        protected IRingMasterRequestHandler ConnectToRingMaster()
        {
            return new CoreRequestHandler(this.backend);
        }

        protected void StartPseudoNodes()
        {
            PseudoNodes ps = new PseudoNodes(this.backend, GetSetting);

            ps.SetRuntimeMembersetFunction(() =>
            {
                return new Backend.Persistence.ClusterMember[] { new Backend.Persistence.ClusterMember("one", System.Net.IPAddress.Loopback) };
            });

            ps.SetupPseudoNodes(() =>
            {
                return new LoopbackRingMaster(this.backend);
            });
        }

        protected void StopPseudoNodes()
        {
            if (this.ps == null)
            {
                return;
            }

            this.ps.Stop();
            this.ps = null;
        }

        private static string GetSetting(string settingName)
        {
            if (settingName == "RingMaster.DontStartPseudoNodes")
            {
                return "false";
            }

            if (settingName == "RingMasterLimits.MaxGetChildrenEnumerationCount")
            {
                return "256";
            }

            return null;
        }
    }
}
