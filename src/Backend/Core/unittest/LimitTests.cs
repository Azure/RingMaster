// <copyright file="LimitTests.cs" company="Microsoft">
//     Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterBackendCoreUnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using Backend;
    using Backend.Data;
    using Backend.HelperTypes;
    using Backend.KeeperException;
    using FluentAssertions;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.InMemory;
    using VisualStudio.TestTools.UnitTesting;

    using Code = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.RingMasterException.Code;
    using Perm = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.Acl.Perm;
    using RedirectionPolicy = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestInit.RedirectionPolicy;

    /// <summary>
    /// Tests for limit scenarios
    /// </summary>
    [TestClass]
    public sealed class LimitTests
    {
        /// <summary>
        /// Maximum node name length
        /// </summary>
        private const int MaxNodeNameLength = 128;

        /// <summary>
        /// Maximum node path length
        /// </summary>
        private const int MaxNodePathLength = 512;

        /// <summary>
        /// Maximum node data size
        /// </summary>
        private const int MaxNodeDataSize = 10240;

        /// <summary>
        /// Maximum amount of ACLs per node
        /// </summary>
        private const int MaxAclsPerNode = 5;

        /// <summary>
        /// Maximum ACL identifier length
        /// </summary>
        private const int MaxAclIdentiferLength = 128;

        /// <summary>
        /// Settings with limits
        /// </summary>
        private static readonly Dictionary<string, string> LimitTestSettings;

        /// <summary>
        /// Settings with no limits
        /// </summary>
        private static readonly Dictionary<string, string> NoLimitTestSettings;

        /// <summary>
        /// For generating random numbers
        /// </summary>
        private static readonly Random Random = new Random();

        /// <summary>
        /// Initializes static members of the <see cref="LimitTests"/> class.
        /// </summary>
        static LimitTests()
        {
            LimitTestSettings = new Dictionary<string, string>
            {
                { "RingMasterLimits.MaxNodeNameLength", MaxNodeNameLength.ToString() },
                { "RingMasterLimits.MaxNodePathLength", MaxNodePathLength.ToString() },
                { "RingMasterLimits.MaxNodeDataSize", MaxNodeDataSize.ToString() },
                { "RingMasterLimits.MaxAclsPerNode", MaxAclsPerNode.ToString() },
                { "RingMasterLimits.MaxAclIdentiferLength", MaxAclIdentiferLength.ToString() }
            };

            NoLimitTestSettings = new Dictionary<string, string>
            {
                { "RingMasterLimits.MaxNodeNameLength", "0" },
                { "RingMasterLimits.MaxNodePathLength", "0" },
                { "RingMasterLimits.MaxNodeDataSize", "0" },
                { "RingMasterLimits.MaxAclsPerNode", "0" },
                { "RingMasterLimits.MaxAclIdentiferLength", "0" }
            };
        }

        /// <summary>
        /// Tests name length and path length scenarios
        /// </summary>
        [TestMethod]
        public void TestNameAndPathLengthScenarios()
        {
            var backend = CreateBackend(GetLimitSetting);
            var session = CreateSession(backend);
            var rootSession = CreateSession(backend, RingMasterBackendCore.RootDigest);
            var noLimitBackend = CreateBackend(GetNoLimitSetting);
            var noLimitSession = CreateSession(noLimitBackend);

            // test max name length
            CreateNode(backend, session, 1, MaxNodeNameLength, Code.Ok);
            CreateNode(backend, session, 1, MaxNodeNameLength + 1, Code.Badarguments);
            CreateNode(noLimitBackend, noLimitSession, 1, MaxNodeNameLength + 1, Code.Ok);
            CreateNode(backend, rootSession, 1, MaxNodeNameLength + 1, Code.Ok);

            // test max path length
            var segmentsForLongPath = MaxNodePathLength / (MaxNodeNameLength + 1);
            var endingSegmentLength = MaxNodePathLength - (segmentsForLongPath * (MaxNodeNameLength + 1)) - 1;

            CreateNode(backend, session, segmentsForLongPath, MaxNodeNameLength, Code.Ok, endingSegmentLength, CreateMode.PersistentAllowPathCreation);
            CreateNode(backend, session, segmentsForLongPath, MaxNodeNameLength, Code.Badarguments, endingSegmentLength + 1, CreateMode.PersistentAllowPathCreation);
            CreateNode(noLimitBackend, noLimitSession, segmentsForLongPath, MaxNodeNameLength, Code.Ok, endingSegmentLength + 1, CreateMode.PersistentAllowPathCreation);
            CreateNode(backend, rootSession, segmentsForLongPath, MaxNodeNameLength, Code.Ok, endingSegmentLength + 1, CreateMode.PersistentAllowPathCreation);
        }

        /// <summary>
        /// Tests data length scenarios
        /// </summary>
        [TestMethod]
        public void TestDataLengthScenarios()
        {
            var backend = CreateBackend(GetLimitSetting);
            var session = CreateSession(backend);
            var rootSession = CreateSession(backend, RingMasterBackendCore.RootDigest);
            var noLimitBackend = CreateBackend(GetNoLimitSetting);
            var noLimitSession = CreateSession(noLimitBackend);

            var nodePath = "/DataLengthTest";

            CreateNode(backend, session, nodePath, new byte[MaxNodeDataSize], Code.Ok);
            DeleteNode(backend, session, nodePath);
            CreateNode(backend, session, nodePath, new byte[MaxNodeDataSize + 1], Code.Badarguments);
            CreateNode(backend, rootSession, nodePath, new byte[MaxNodeDataSize + 1], Code.Ok);
            CreateNode(noLimitBackend, noLimitSession, nodePath, new byte[MaxNodeDataSize + 1], Code.Ok);

            SetData(backend, session, nodePath, new byte[MaxNodeDataSize], Code.Ok);
            SetData(backend, session, nodePath, new byte[MaxNodeDataSize + 1], Code.Badarguments);
            SetData(backend, rootSession, nodePath, new byte[MaxNodeDataSize + 1], Code.Ok);
            SetData(noLimitBackend, noLimitSession, nodePath, new byte[MaxNodeDataSize + 1], Code.Ok);
        }

        /// <summary>
        /// Tests ACL scenarios
        /// </summary>
        [TestMethod]
        public void TestAclScenarios()
        {
            var sessionAuthIdentifer = CreateRandomString(MaxAclIdentiferLength);
            var backend = CreateBackend(GetLimitSetting);
            var session = CreateSession(backend, string.Format("{0}:{1}", AuthSchemes.Digest, sessionAuthIdentifer));
            var rootSession = CreateSession(backend, RingMasterBackendCore.RootDigest);
            var noLimitBackend = CreateBackend(GetNoLimitSetting);
            var noLimitSession = CreateSession(noLimitBackend, string.Format("{0}:{1}", AuthSchemes.Digest, sessionAuthIdentifer));

            // test maximum number of ACLs per node
            var nodePath = "/AclTest";

            Action<List<Acl>, List<Acl>> runAclLimitTestAction = (atLimitAclList, overLimitAclList) =>
            {
                CreateNode(backend, session, nodePath, null, Code.Ok, acls: atLimitAclList);
                DeleteNode(backend, session, nodePath);
                CreateNode(backend, session, nodePath, null, Code.Badarguments, acls: overLimitAclList);
                CreateNode(backend, rootSession, nodePath, null, Code.Ok, acls: overLimitAclList);
                CreateNode(noLimitBackend, noLimitSession, nodePath, null, Code.Ok, acls: overLimitAclList);

                SetAcl(backend, session, nodePath, atLimitAclList, Code.Ok);
                SetAcl(backend, session, nodePath, overLimitAclList, Code.Badarguments);
                SetAcl(backend, rootSession, nodePath, overLimitAclList, Code.Ok);
                SetAcl(noLimitBackend, noLimitSession, nodePath, overLimitAclList, Code.Ok);
                DeleteNode(backend, session, nodePath);
                DeleteNode(noLimitBackend, noLimitSession, nodePath);
            };

            // test maximum number of ACLs per node
            var numberAclsAtLimitAclList = new List<Acl>(MaxAclsPerNode);
            numberAclsAtLimitAclList.Add(new Acl((int)Perm.ALL, new Id(AuthSchemes.Digest, sessionAuthIdentifer)));
            for (int i = 0; i < MaxAclsPerNode - 1; ++i)
            {
                numberAclsAtLimitAclList.Add(new Acl((int)Perm.WRITE, new Id(AuthSchemes.Digest, Guid.NewGuid().ToString())));
            }

            var numberAclsOverLimitAclList = new List<Acl>(numberAclsAtLimitAclList);
            numberAclsOverLimitAclList.Add(new Acl((int)Perm.WRITE, new Id(AuthSchemes.Digest, Guid.NewGuid().ToString())));

            runAclLimitTestAction(numberAclsAtLimitAclList, numberAclsOverLimitAclList);

            // test ACL schema name too long
            var schemaLengthAtLimitAclList = new List<Acl>(2);
            schemaLengthAtLimitAclList.Add(new Acl((int)Perm.ALL, new Id(AuthSchemes.Digest, sessionAuthIdentifer)));
            schemaLengthAtLimitAclList.Add(new Acl((int)Perm.ALL, new Id(CreateRandomString(MaxAclIdentiferLength), sessionAuthIdentifer)));

            var schemaLengthOverLimitAclList = new List<Acl>(2);
            schemaLengthOverLimitAclList.Add(new Acl((int)Perm.ALL, new Id(AuthSchemes.Digest, sessionAuthIdentifer)));
            schemaLengthOverLimitAclList.Add(new Acl((int)Perm.ALL, new Id(CreateRandomString(MaxAclIdentiferLength + 1), sessionAuthIdentifer)));

            runAclLimitTestAction(schemaLengthAtLimitAclList, schemaLengthOverLimitAclList);

            // test ACL identifier too long
            var identifierLengthAtLimitAclList = new List<Acl>(2);
            identifierLengthAtLimitAclList.Add(new Acl((int)Perm.ALL, new Id(AuthSchemes.Digest, sessionAuthIdentifer)));
            identifierLengthAtLimitAclList.Add(new Acl((int)Perm.ALL, new Id(AuthSchemes.Digest, sessionAuthIdentifer)));

            var identifierLengthOverLimitAclList = new List<Acl>(2);
            identifierLengthOverLimitAclList.Add(new Acl((int)Perm.ALL, new Id(AuthSchemes.Digest, sessionAuthIdentifer)));
            identifierLengthOverLimitAclList.Add(new Acl((int)Perm.ALL, new Id(AuthSchemes.Digest, CreateRandomString(MaxAclIdentiferLength + 1))));

            runAclLimitTestAction(identifierLengthAtLimitAclList, identifierLengthOverLimitAclList);

            // test with ACL set to null
            var aclsWithNull = new List<Acl>
            {
                null
            };

            // note since this is just a general error, not a limit error, root cannot bypass
            CreateNode(backend, session, nodePath, null, Code.Badarguments, acls: aclsWithNull);
            CreateNode(backend, rootSession, nodePath, null, Code.Badarguments, acls: aclsWithNull);
            CreateNode(noLimitBackend, noLimitSession, nodePath, null, Code.Badarguments, acls: aclsWithNull);

            // test with ACL id set to null
            var aclsWithNullId = new List<Acl>
            {
                new Acl((int)Perm.ALL, null)
            };

            // note since this is just a general error, not a limit error, root cannot bypass
            CreateNode(backend, session, nodePath, null, Code.Badarguments, acls: aclsWithNullId);
            CreateNode(backend, rootSession, nodePath, null, Code.Badarguments, acls: aclsWithNullId);
            CreateNode(noLimitBackend, noLimitSession, nodePath, null, Code.Badarguments, acls: aclsWithNullId);
        }

        /// <summary>
        /// Creates a new backend with an in-memory store
        /// </summary>
        /// <param name="getSettingFunction">Function to get settings</param>
        /// <returns>Backend instance</returns>
        private static RingMasterBackendCore CreateBackend(RingMasterBackendCore.getSettingFunctionDelegate getSettingFunction)
        {
            var backendStarted = new ManualResetEventSlim();

            RingMasterBackendCore.GetSettingFunction = getSettingFunction;
            var backend = new RingMasterBackendCore(new InMemoryFactory());
            backend.StartService = (p1, p2) => { backendStarted.Set(); };
            backend.Start();
            backend.OnBecomePrimary();

            backendStarted.Wait(30000).Should().BeTrue();

            return backend;
        }

        /// <summary>
        /// Creates and initializes a new session
        /// </summary>
        /// <param name="backend">Backend to initialize the session for</param>
        /// <param name="authDigest">Optional authentication digest</param>
        /// <returns>The session</returns>
        private static ClientSession CreateSession(RingMasterBackendCore backend, string authDigest = null)
        {
            var session = new ClientSession((requestCall, clientSession, responseAction) =>
            {
                backend.ProcessMessage(requestCall.Request, clientSession, responseAction);
            });

            backend.ProcessSessionInitialization(
                new RequestCall
                {
                    CallId = 0,
                    Request = new RequestInit(0, Guid.NewGuid().ToString(), null, true, RedirectionPolicy.RedirectPreferred)
                },
                session).ResultCode.Should().Be((int)Code.Ok);

            if (authDigest != null)
            {
                backend.ProcessMessage(
                    new RequestSetAuth(authDigest, null),
                    session,
                    response => response.ResultCode.Should().Be((int)Code.Ok));
            }

            return session;
        }

        /// <summary>
        /// Creates a new node with random characters in the node path
        /// </summary>
        /// <param name="backend">The backend to create the node in</param>
        /// <param name="session">The session to use for the create</param>
        /// <param name="segments">Amount of path segments</param>
        /// <param name="segmentLength">Length of each path segment</param>
        /// <param name="expectedResponseCode">Expected response code for the create request</param>
        /// <param name="endingSegmentLength">If non-zero, will add an extra segment on the end with the specified length</param>
        /// <param name="createMode">Mode for creating the node</param>
        private static void CreateNode(RingMasterBackendCore backend, ClientSession session, int segments, int segmentLength, Code expectedResponseCode, int endingSegmentLength = 0, CreateMode createMode = CreateMode.Persistent)
        {
            var stringBuilder = new StringBuilder();
            for (int i = 0; i < segments; ++i)
            {
                stringBuilder.AppendFormat("/{0}", CreateRandomString(segmentLength));
            }

            if (endingSegmentLength > 0)
            {
                stringBuilder.AppendFormat("/{0}", CreateRandomString(endingSegmentLength));
            }

            CreateNode(backend, session, stringBuilder.ToString(), null, expectedResponseCode, createMode);
        }

        /// <summary>
        /// Creates a new node in the backend
        /// </summary>
        /// <param name="backend">Backend to create the node in</param>
        /// <param name="session">Session to use for creating the node</param>
        /// <param name="nodePath">Path of the node</param>
        /// <param name="data">Data for the node</param>
        /// <param name="expectedResponseCode">Expected response for the operation</param>
        /// <param name="createMode">Creation mode</param>
        /// <param name="acls">ACLs for the node</param>
        private static void CreateNode(RingMasterBackendCore backend, ClientSession session, string nodePath, byte[] data, Code expectedResponseCode, CreateMode createMode = CreateMode.Persistent, List<Acl> acls = null)
        {
            ProcessRequest(backend, session, new RequestCreate(nodePath, null, data, acls, createMode, null), expectedResponseCode);
        }

        /// <summary>
        /// Deletes a node in the backend
        /// </summary>
        /// <param name="backend">Backend to delete the node from</param>
        /// <param name="session">Session to use for the deletion</param>
        /// <param name="nodePath">Path of the node</param>
        private static void DeleteNode(RingMasterBackendCore backend, ClientSession session, string nodePath)
        {
            ProcessRequest(backend, session, new RequestDelete(nodePath, null, -1, null), Code.Ok);
        }

        /// <summary>
        /// Sets data on a node in the backend
        /// </summary>
        /// <param name="backend">Backend for the operation</param>
        /// <param name="session">Session for the operation</param>
        /// <param name="nodePath">Path of the node</param>
        /// <param name="data">Data to set</param>
        /// <param name="expectedResponseCode">Expected result code of the operation</param>
        private static void SetData(RingMasterBackendCore backend, ClientSession session, string nodePath, byte[] data, Code expectedResponseCode)
        {
            ProcessRequest(backend, session, new RequestSetData(nodePath, null, data, -1, null), expectedResponseCode);
        }

        /// <summary>
        /// Sets an ACL on a node in the backend
        /// </summary>
        /// <param name="backend">Backend for the operation</param>
        /// <param name="session">Session for the operation</param>
        /// <param name="nodePath">Path of the node</param>
        /// <param name="acls">Data to set</param>
        /// <param name="expectedResponseCode">Expected result code of the operation</param>
        private static void SetAcl(RingMasterBackendCore backend, ClientSession session, string nodePath, List<Acl> acls, Code expectedResponseCode)
        {
            ProcessRequest(backend, session, new RequestSetAcl(nodePath, null, acls, -1, null), expectedResponseCode);
        }

        /// <summary>
        /// Processes a request in the backend
        /// </summary>
        /// <param name="backend">Backend for the operation</param>
        /// <param name="session">Session for the operation</param>
        /// <param name="request">Request to execute</param>
        /// <param name="expectedResponseCode">Expected response code for the request</param>
        private static void ProcessRequest(RingMasterBackendCore backend, ClientSession session, IRingMasterBackendRequest request, Code expectedResponseCode)
        {
            var evt = ManualResetEventPool.InstancePool.GetOne();

            backend.ProcessMessage(
                request,
                session,
                response =>
                {
                    response.ResultCode.Should().Be((int)expectedResponseCode);
                    evt.Set();
                });

            ManualResetEventPool.InstancePool.WaitOneAndReturn(ref evt);
        }

        /// <summary>
        /// Gets a setting for the limit scenario
        /// </summary>
        /// <param name="settingName">Name of the setting</param>
        /// <returns>Parsed value of the setting or null if it is not found or parsing fails</returns>
        private static string GetLimitSetting(string settingName)
        {
            string settingValue;
            if (!LimitTestSettings.TryGetValue(settingName, out settingValue))
            {
                return null;
            }

            return settingValue;
        }

        /// <summary>
        /// Gets a setting for the no-limit scenario
        /// </summary>
        /// <param name="settingName">Name of the setting</param>
        /// <returns>Parsed value of the setting or null if it is not found or parsing fails</returns>
        private static string GetNoLimitSetting(string settingName)
        {
            string settingValue;
            if (!NoLimitTestSettings.TryGetValue(settingName, out settingValue))
            {
                return null;
            }

            return settingValue;
        }

        /// <summary>
        /// Creates a random string of letters
        /// </summary>
        /// <param name="length">Length of the string</param>
        /// <returns>The generated string</returns>
        private static string CreateRandomString(int length)
        {
            var chars = new char[length];

            for (int i = 0; i < length; ++i)
            {
                chars[i] = (char)Random.Next(97, 123);
            }

            return new string(chars);
        }
    }
}
