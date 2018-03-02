// <copyright file="AuditTests.cs" company="Microsoft">
//     Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterBackendCoreUnitTest
{
    using System;
    using System.Text;
    using System.Threading;
    using Backend;
    using Backend.KeeperException;
    using FluentAssertions;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.InMemory;
    using VisualStudio.TestTools.UnitTesting;

    using Code = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.RingMasterException.Code;
    using RedirectionPolicy = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestInit.RedirectionPolicy;

    /// <summary>
    /// Tests for verifying that audit events are generated.
    /// </summary>
    [TestClass]
    public sealed class AuditTests
    {
        /// <summary>
        /// Verifies that session initialization is audited.
        /// </summary>
        [TestMethod]
        public void TestSessionInitialization()
        {
            var auditConsumer = new AuditConsumer();
            var backend = CreateBackend(auditConsumer);

            for (int i = 0; i < 5; i++)
            {
                var session = CreateSession(backend);

                auditConsumer.InitializeSessionCount.Should().Be(i + 1);
                auditConsumer.LastSessionId.Should().Be(session.SessionId);
            }
        }

        /// <summary>
        /// Verifies that an audit event is generated when a session's <c>Auth</c> is changed.
        /// </summary>
        [TestMethod]
        public void TestSetAuth()
        {
            var auditConsumer = new AuditConsumer();
            var backend = CreateBackend(auditConsumer);

            var session = CreateSession(backend);

            auditConsumer.InitializeSessionCount.Should().Be(1);
            auditConsumer.SetAuthCount.Should().Be(0);

            SetAuth(backend, session, "digest:root");
            auditConsumer.InitializeSessionCount.Should().Be(1);
            auditConsumer.SetAuthCount.Should().Be(1);
            auditConsumer.LastSessionId.Should().Be(session.SessionId);
            auditConsumer.LastClientDigest.Should().Be("digest:root");
            auditConsumer.LastIsSuperSession.Should().BeTrue();

            SetAuth(backend, session, "commander");
            auditConsumer.InitializeSessionCount.Should().Be(1);
            auditConsumer.SetAuthCount.Should().Be(2);
            auditConsumer.LastSessionId.Should().Be(session.SessionId);
            auditConsumer.LastClientDigest.Should().Be("commander");
            auditConsumer.LastIsSuperSession.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that an audit event is generated when a command is issued by creating a (virtual) node.
        /// </summary>
        [TestMethod]
        public void TestCreateCommandNode()
        {
            var auditConsumer = new AuditConsumer();
            var backend = CreateBackend(auditConsumer);

            var session = CreateSession(backend);
            SetAuth(backend, session, "digest:commander");

            auditConsumer.InitializeSessionCount.Should().Be(1);
            auditConsumer.SetAuthCount.Should().Be(1);
            auditConsumer.LastSessionId.Should().Be(session.SessionId);
            auditConsumer.LastClientDigest.Should().Be("digest:commander");

            backend.ProcessMessage(
                new RequestCreate("$/gc", context: null, data: null, acl: null, createMode: CreateMode.Persistent, callback: (rc, path, ctx, name) => { }),
                session,
                response => response.ResultCode.Should().Be((int)Code.Ok));

            auditConsumer.RequestCommandCount.Should().Be(1);
            auditConsumer.LastSessionId.Should().Be(session.SessionId);
            auditConsumer.LastCommandPath.Should().Be("$/gc");
            auditConsumer.LastCommand.Should().Be("gc");
        }

        /// <summary>
        /// Verifies that an audit event is generated when a command is issued by setting data on a command node.
        /// </summary>
        [TestMethod]
        public void TestSetCommandNodeData()
        {
            var auditConsumer = new AuditConsumer();
            var backend = CreateBackend(auditConsumer);

            var session = CreateSession(backend);
            SetAuth(backend, session, "digest:commander");

            auditConsumer.InitializeSessionCount.Should().Be(1);
            auditConsumer.SetAuthCount.Should().Be(1);
            auditConsumer.LastSessionId.Should().Be(session.SessionId);
            auditConsumer.LastClientDigest.Should().Be("digest:commander");

            backend.ProcessMessage(
                new RequestSetData("/$metadata/commands/$$<all>", context: null, data: Encoding.UTF8.GetBytes("$/gc"), version: -1, callback: (rc, path, ctx, name) => { }),
                session,
                response => response.ResultCode.Should().Be((int)Code.Ok));

            auditConsumer.RequestCommandCount.Should().Be(1);
            auditConsumer.LastSessionId.Should().Be(session.SessionId);
            auditConsumer.LastCommandPath.Should().Be("/$metadata/commands/$$<all>");
            ////auditConsumer.LastCommand.Should().Be("gc");
        }

        /// <summary>
        /// Creates a new backend with an in-memory store
        /// </summary>
        /// <param name="auditConsumer">Interface to an object that can consume audit events</param>
        /// <returns>Backend instance</returns>
        private static RingMasterBackendCore CreateBackend(IRingMasterAudit auditConsumer)
        {
            var backendStarted = new ManualResetEventSlim();
            RingMasterBackendCore.GetSettingFunction = settingName => null;
            var backend = new RingMasterBackendCore(new InMemoryFactory(), auditConsumer);
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
        /// <returns>The session</returns>
        private static ClientSession CreateSession(RingMasterBackendCore backend)
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

            return session;
        }

        /// <summary>
        /// Changes <c>Auth</c> for the given session.
        /// </summary>
        /// <param name="backend">Backend associated with the session</param>
        /// <param name="session">Session for which the <c>Auth</c> must be changed</param>
        /// <param name="authDigest">Digest string</param>
        private static void SetAuth(RingMasterBackendCore backend, ClientSession session, string authDigest)
        {
            backend.ProcessMessage(
                new RequestSetAuth(authDigest, null),
                session,
                response => response.ResultCode.Should().Be((int)Code.Ok));
        }

        /// <summary>
        /// Implementation of IRingMasterAudit interface for use in this unit test.
        /// </summary>
        private class AuditConsumer : IRingMasterAudit
        {
            /// <summary>
            /// Gets the number of times OnInitializeSession audit event was received.
            /// </summary>
            public int InitializeSessionCount { get; private set; }

            /// <summary>
            /// Gets the number of times <c>OnSetAuth</c> audit event was received.
            /// </summary>
            public int SetAuthCount { get; private set; }

            /// <summary>
            /// Gets the number of times <c>RequestCommand</c> audit event was received.
            /// </summary>
            public int RequestCommandCount { get; private set; }

            /// <summary>
            /// Gets the number of times <c>RunCommand</c> audit event was received.
            /// </summary>
            public int RunCommandCount { get; private set; }

            /// <summary>
            /// Gets the id of the last session that generated an audit event.
            /// </summary>
            public ulong LastSessionId { get; private set; }

            /// <summary>
            /// Gets the client digest associated with the last <c>SetAuth</c> request.
            /// </summary>
            public string LastClientDigest { get; private set; }

            /// <summary>
            /// Gets a value indicating whether the last <c>SetAuth</c> changed the session into a super session.
            /// </summary>
            public bool LastIsSuperSession { get; private set; }

            /// <summary>
            /// Gets the path associated with the last <c>RequestCommand</c> audit event.
            /// </summary>
            public string LastCommandPath { get; private set; }

            /// <summary>
            /// Gets the command associated with the last <c>RunCommand</c> audit event.
            /// </summary>
            public string LastCommand { get; private set; }

            /// <inheritdoc/>
            public void OnInitializeSession(ulong sessionId, string clientIP, string clientIdentity)
            {
                this.InitializeSessionCount++;
                this.LastSessionId = sessionId;
            }

            /// <inheritdoc/>
            public void OnRequestCommand(ulong sessionId, string clientIP, string clientIdentity, string path)
            {
                this.RequestCommandCount++;
                this.LastSessionId = sessionId;
                this.LastCommandPath = path;
            }

            /// <inheritdoc/>
            public void OnRunCommand(string command)
            {
                this.RunCommandCount++;
                this.LastCommand = command;
            }

            /// <inheritdoc/>
            public void OnSetAuth(ulong sessionId, string clientIP, string clientIdentity, string clientDigest, bool isSuperSession)
            {
                this.SetAuthCount++;
                this.LastSessionId = sessionId;
                this.LastClientDigest = clientDigest;
                this.LastIsSuperSession = isSuperSession;
            }
        }
    }
}
