// <copyright file="TestClient.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterClientUnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Test;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify RingMasterClient class functionality.
    /// </summary>
    [TestClass]
    public class TestClient : RingMasterClientUnitTest
    {
        private readonly Random random = new Random();

        [TestMethod]
        [Timeout(30000)]
        public void TestConstructorWithServerSpec()
        {
            var serverSpec = new RingMasterClient.ServerSpec();

            serverSpec.Endpoints = new IPEndPoint[] { new IPEndPoint(IPAddress.Loopback, 0) };
            serverSpec.UseSecureConnection = true;
            serverSpec.ClientCertificate = GetLocalCertificates(1)[0];
            serverSpec.AcceptedServerCertificates = GetLocalCertificates(1);

            var configuration = new RingMasterClient.Configuration();
            configuration.DefaultTimeout = Timeout.InfiniteTimeSpan;
            configuration.HeartBeatInterval = Timeout.InfiniteTimeSpan;
            configuration.RequestQueueLength = int.MaxValue;
            configuration.BufferSize = int.MaxValue;

            var instrumentation = new RingMasterClientInstrumentation();

            try
            {
                using (var client = new RingMasterClient(serverSpec, configuration, instrumentation, new CancellationToken(true)))
                {
                    client.Sync("/").Wait();
                }
            }
            catch (Exception)
            {
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestConstructorWithConnectionString()
        {
            var configuration = new RingMasterClient.Configuration();
            configuration.DefaultTimeout = Timeout.InfiniteTimeSpan;
            configuration.HeartBeatInterval = Timeout.InfiniteTimeSpan;
            configuration.RequestQueueLength = int.MaxValue;
            configuration.BufferSize = int.MaxValue;

            var instrumentation = new RingMasterClientInstrumentation();

            try
            {
                using (var client = new RingMasterClient("127.0.0.1:0", configuration, instrumentation, new CancellationToken(true)))
                {
                    client.Sync("/").Wait();
                }
            }
            catch (Exception)
            {
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestConstructorWithConnectionStringNoCancellationToken()
        {
            var configuration = new RingMasterClient.Configuration();
            configuration.DefaultTimeout = Timeout.InfiniteTimeSpan;
            configuration.HeartBeatInterval = Timeout.InfiniteTimeSpan;
            configuration.RequestQueueLength = int.MaxValue;
            configuration.BufferSize = int.MaxValue;

            var instrumentation = new RingMasterClientInstrumentation();

            using (var client = new RingMasterClient("127.0.0.1:0", configuration, instrumentation))
            {
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestConstructorWithConnectionStringAndCertificates()
        {
            var clientCertificates = GetLocalCertificates(1);
            var serverCertificates = GetLocalCertificates(1);

            try
            {
                var watcher = new TestWatcher(0);
                using (var client = new RingMasterClient("127.0.0.1:0", clientCertificates, serverCertificates, 0, watcher))
                {
                    client.Sync("/").Wait();
                }
            }
            catch (Exception)
            {
            }
        }

        [TestMethod]
        [Timeout(30000)]
        public void TestConstructorWithConnectionStringAndThumbprints()
        {
            var clientCertificateThumbprints = GetLocalCertificates(1).Select(c => c.GetCertHashString()).ToArray();
            var serverCertificateThumbprints = GetLocalCertificates(1).Select(c => c.GetCertHashString()).ToArray();

            try
            {
                var watcher = new TestWatcher(0);
                using (var client = new RingMasterClient("127.0.0.1:0", clientCertificateThumbprints, serverCertificateThumbprints, 0, watcher))
                {
                    client.Sync("/").Wait();
                }
            }
            catch (Exception)
            {
            }
        }

        [TestMethod]
        [Timeout(10000)]
        public void TestNullServerSpec()
        {
            var configuration = new RingMasterClient.Configuration();
            var instrumentation = new RingMasterClientInstrumentation();

            try
            {
                using (var clientTransport = new SimpleTransport())
                using (var ringMaster = new RingMasterClient((RingMasterClient.ServerSpec)null, configuration, instrumentation, CancellationToken.None))
                {
                    Assert.Fail("ArgumentNullException should have been thrown");
                }
            }
            catch (ArgumentNullException)
            {
            }
        }

        [TestMethod]
        [Timeout(10000)]
        public void TestNullEndpoints()
        {
            var serverSpec = new RingMasterClient.ServerSpec();
            var configuration = new RingMasterClient.Configuration();
            var instrumentation = new RingMasterClientInstrumentation();

            try
            {
                serverSpec.Endpoints = null;
                serverSpec.UseSecureConnection = false;
                using (var clientTransport = new SimpleTransport())
                using (var ringMaster = new RingMasterClient(serverSpec, configuration, instrumentation, CancellationToken.None))
                {
                    Assert.Fail("ArgumentException should have been thrown");
                }
            }
            catch (ArgumentException)
            {
            }
        }

        [TestMethod]
        [Timeout(10000)]
        public void TestEmptyEndpoints()
        {
            var serverSpec = new RingMasterClient.ServerSpec();
            var configuration = new RingMasterClient.Configuration();
            var instrumentation = new RingMasterClientInstrumentation();

            try
            {
                serverSpec.Endpoints = new IPEndPoint[0];
                serverSpec.UseSecureConnection = false;
                using (var clientTransport = new SimpleTransport())
                using (var ringMaster = new RingMasterClient(serverSpec, configuration, instrumentation, CancellationToken.None))
                {
                    Assert.Fail("ArgumentException should have been thrown");
                }
            }
            catch (ArgumentException)
            {
            }
        }

        [TestMethod]
        [Timeout(10000)]
        public void TestNullConfiguration()
        {
            var instrumentation = new RingMasterClientInstrumentation();
            try
            {
                var serverSpec = new RingMasterClient.ServerSpec();
                using (var ringMaster = new RingMasterClient(serverSpec, null, instrumentation, CancellationToken.None))
                {
                    Assert.Fail("ArgumentNullException should have been thrown");
                }
            }
            catch (ArgumentNullException)
            {
            }

            var protocol = new RingMasterCommunicationProtocol();
            try
            {
                using (var clientTransport = new SimpleTransport())
                using (var ringMaster = new RingMasterClient(null, instrumentation, protocol, clientTransport, CancellationToken.None))
                {
                    Assert.Fail("ArgumentNullException should have been thrown");
                }
            }
            catch (ArgumentNullException)
            {
            }
        }

        [TestMethod]
        [Timeout(10000)]
        public void TestDefaultconfiguration()
        {
            var configuration = new RingMasterClient.Configuration();
            var protocol = new RingMasterCommunicationProtocol();

            using (var clientTransport = new SimpleTransport())
            using (var ringMaster = new RingMasterClient(protocol, clientTransport))
            {
                Assert.AreEqual((int)configuration.DefaultTimeout.TotalMilliseconds, ringMaster.Timeout);
            }
        }

        [TestMethod]
        [Timeout(10000)]
        public void TestTimeoutConfiguration()
        {
            int timespanInMilliseconds = 4536;
            var configuration = new RingMasterClient.Configuration() { DefaultTimeout = TimeSpan.FromMilliseconds(timespanInMilliseconds) };
            var protocol = new RingMasterCommunicationProtocol();

            using (var clientTransport = new SimpleTransport())
            using (var ringMaster = new RingMasterClient(configuration, null, protocol, clientTransport, CancellationToken.None))
            {
                Assert.AreEqual(timespanInMilliseconds, ringMaster.Timeout);

                ringMaster.Timeout = 10000;

                Assert.AreEqual(10000, ringMaster.Timeout);
            }
        }

        /// <summary>
        /// Verify that RingMasterClientException can be serialized/deserialized properly.
        /// </summary>
        [TestMethod]
        [Timeout(10000)]
        public void TestSerializeRingMasterClientException()
        {
            var exception = RingMasterClientException.RequestQueueFull(100);

            Assert.AreEqual(RingMasterClientException.Code.RequestQueueFull, exception.ErrorCode);

            var formatter = new BinaryFormatter();
            byte[] serializedData = null;
            using (var memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, exception);
                serializedData = memoryStream.GetBuffer();
            }

            using (var memoryStream = new MemoryStream(serializedData))
            {
                var deserializedException = (RingMasterClientException)formatter.Deserialize(memoryStream);
                Assert.AreEqual(RingMasterClientException.Code.RequestQueueFull, deserializedException.ErrorCode);
            }
        }

        /// <summary>
        /// Verifies that Create request correctly populates RequestCreate object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreate()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string path = this.GetRandomString();
                byte[] data = this.GetRandomData();
                var acl = new List<Acl>() { new Acl((int)Acl.Perm.ALL, new Id(AuthSchemes.Digest, this.GetRandomString())) };
                var createMode = CreateMode.Persistent;
                var expectedResult = this.GetRandomString();

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestCreate);
                    var createRequest = (RequestCreate)request;
                    Assert.AreEqual(path, createRequest.Path);
                    Assert.AreEqual(data, createRequest.Data);
                    Assert.AreEqual(acl.Count, createRequest.Acl.Count);
                    Assert.IsTrue(Acl.AreEqual(acl[0], createRequest.Acl[0]));
                    Assert.AreEqual(createMode, createRequest.CreateMode);

                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Ok,
                        Content = expectedResult
                    };
                };

                string result = client.Create(path, data, acl, createMode).Result;
                Assert.AreEqual(expectedResult, result);
            }
        }

        /// <summary>
        /// Verifies that Create request correctly populates RequestCreate object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateDontThrowIfNodeExists()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string path = this.GetRandomString();
                byte[] data = this.GetRandomData();
                var acl = new List<Acl>() { new Acl((int)Acl.Perm.ALL, new Id(AuthSchemes.Digest, this.GetRandomString())) };
                var createMode = CreateMode.Persistent;
                var expectedResult = this.GetRandomString();

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestCreate);
                    var createRequest = (RequestCreate)request;
                    Assert.AreEqual(path, createRequest.Path);
                    Assert.AreEqual(data, createRequest.Data);
                    Assert.AreEqual(acl.Count, createRequest.Acl.Count);
                    Assert.IsTrue(Acl.AreEqual(acl[0], createRequest.Acl[0]));
                    Assert.AreEqual(createMode, createRequest.CreateMode);

                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Nodeexists,
                        Content = expectedResult
                    };
                };

                client.Create(path, data, acl, createMode, throwIfNodeExists: false).Wait();
            }
        }

        /// <summary>
        /// Verifies that CreateAndGetStat request correctly populates RequestCreate object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCreateAndGetStat()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string path = this.GetRandomString();
                byte[] data = this.GetRandomData();
                var acl = new List<Acl>() { new Acl((int)Acl.Perm.ALL, new Id(AuthSchemes.Digest, this.GetRandomString())) };
                var createMode = CreateMode.Persistent;
                var expectedStat = new Stat() { Version = this.GetRandomInt() };

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestCreate);
                    var createRequest = (RequestCreate)request;
                    Assert.AreEqual(path, createRequest.Path);
                    Assert.AreEqual(data, createRequest.Data);
                    Assert.AreEqual(acl.Count, createRequest.Acl.Count);
                    Assert.IsTrue(Acl.AreEqual(acl[0], createRequest.Acl[0]));
                    Assert.AreEqual(createMode, createRequest.CreateMode);

                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Ok,
                        Stat = expectedStat
                    };
                };

                IStat stat = client.CreateAndGetStat(path, data, acl, createMode).Result;
                Assert.AreEqual(expectedStat.Version, stat.Version);
            }
        }

        /// <summary>
        /// Verifies that Move request correctly populates RequestMove object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestMove()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string pathSrc = this.GetRandomString();
                string pathDst = this.GetRandomString();
                var version = this.GetRandomInt();
                var moveMode = MoveMode.AllowPathCreationFlag;
                var expectedResult = this.GetRandomString();

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestMove);
                    var moveRequest = (RequestMove)request;
                    Assert.AreEqual(pathSrc, moveRequest.Path);
                    Assert.AreEqual(pathDst, moveRequest.PathDst);
                    Assert.AreEqual(version, moveRequest.Version);
                    Assert.AreEqual(moveMode, moveRequest.MoveMode);

                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Ok,
                        Content = expectedResult
                    };
                };

                string result = client.Move(pathSrc, version, pathDst, moveMode).Result;
                Assert.AreEqual(expectedResult, result);
            }
        }

        /// <summary>
        /// Verifies that Delete request correctly populates RequestDelete object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestDelete()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string path = this.GetRandomString();
                var version = this.GetRandomInt();

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestDelete);
                    var deleteRequest = (RequestDelete)request;
                    Assert.AreEqual(path, deleteRequest.Path);
                    Assert.AreEqual(version, deleteRequest.Version);

                    return new RequestResponse()
                    {
                        ResultCode = (deleteRequest.DeleteMode == DeleteMode.None) ? (int)RingMasterException.Code.Ok : (int)RingMasterException.Code.Nonode
                    };
                };

                bool result = client.Delete(path, version, isRecursive: false).Result;
                Assert.IsTrue(result);

                result = client.Delete(path, version, isRecursive: true).Result;
                Assert.IsFalse(result);
            }
        }

        /// <summary>
        /// Verifies that Exists request correctly populates RequestExists object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestExists()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string path = this.GetRandomString();
                var version = this.GetRandomInt();
                var watcher = new TestWatcher(this.GetRandomInt());
                var expectedStat = new Stat() { Version = this.GetRandomInt() };

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestExists);
                    var existsRequest = (RequestExists)request;
                    Assert.AreEqual(path, existsRequest.Path);
                    Assert.AreEqual(watcher.Id, existsRequest.Watcher.Id);

                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Ok,
                        Content = expectedStat
                    };
                };

                client.SetWatcher(watcher);

                IStat stat = client.Exists(path, watch: true).Result;
                Assert.IsNotNull(stat);
                Assert.AreEqual(expectedStat.Version, stat.Version);
            }
        }

        /// <summary>
        /// Verifies that GetChildren request correctly populates RequestGetChildren object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestGetChildren()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string path = this.GetRandomString();
                string retrievalCondition = this.GetRandomString();
                var watcher = new TestWatcher(this.GetRandomInt());
                IReadOnlyList<string> expectedChildren = new string[] { this.GetRandomString(), this.GetRandomString() };

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestGetChildren);
                    var getChildrenRequest = (RequestGetChildren)request;
                    Assert.AreEqual(path, getChildrenRequest.Path);
                    Assert.AreEqual(watcher.Id, getChildrenRequest.Watcher.Id);

                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Ok,
                        Content = expectedChildren
                    };
                };

                client.SetWatcher(watcher);

                var children = client.GetChildren(path, watch: true, retrievalCondition: retrievalCondition).Result;
                Assert.AreEqual(expectedChildren.Count, children.Count);
                for (int i = 0; i < expectedChildren.Count; i++)
                {
                    Assert.AreEqual(expectedChildren[i], children[i]);
                }
            }
        }

        /// <summary>
        /// Verifies that GetData request correctly populates RequestGetData object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestGetData()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string path = this.GetRandomString();
                var watcher = new TestWatcher(this.GetRandomInt());
                byte[] expectedData = this.GetRandomData();

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestGetData);
                    var getDataRequest = (RequestGetData)request;
                    Assert.AreEqual(path, getDataRequest.Path);
                    Assert.AreEqual(watcher.Id, getDataRequest.Watcher.Id);

                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Ok,
                        Content = expectedData
                    };
                };

                client.SetWatcher(watcher);

                var data = client.GetData(path, watch: true).Result;
                CollectionAssert.AreEqual(expectedData, data);
            }
        }

        /// <summary>
        /// Verifies that GetDataWithStat request correctly populates RequestGetData object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestGetDataWithStat()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string path = this.GetRandomString();
                var watcher = new TestWatcher(this.GetRandomInt());
                byte[] expectedData = this.GetRandomData();
                var expectedStat = new Stat() { Version = this.GetRandomInt() };

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestGetData);
                    var getDataRequest = (RequestGetData)request;
                    Assert.AreEqual(path, getDataRequest.Path);
                    Assert.AreEqual(watcher.Id, getDataRequest.Watcher.Id);

                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Ok,
                        Content = expectedData,
                        Stat = expectedStat
                    };
                };

                var dataAndStat = client.GetDataWithStat(path, watcher).Result;
                Assert.AreEqual(expectedStat.Version, dataAndStat.Item1.Version);
                CollectionAssert.AreEqual(expectedData, dataAndStat.Item2);
            }
        }

        /// <summary>
        /// Verifies that SetData request correctly populates RequestSetData object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestSetData()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string path = this.GetRandomString();
                int version = this.GetRandomInt();
                byte[] data = this.GetRandomData();
                var expectedStat = new Stat() { Version = this.GetRandomInt() };

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestSetData);
                    var setDataRequest = (RequestSetData)request;
                    Assert.AreEqual(path, setDataRequest.Path);
                    Assert.AreEqual(version, setDataRequest.Version);
                    CollectionAssert.AreEqual(data, setDataRequest.Data);

                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Ok,
                        Stat = expectedStat
                    };
                };

                var stat = client.SetData(path, data, version).Result;
                Assert.AreEqual(expectedStat.Version, stat.Version);
            }
        }

        /// <summary>
        /// Verifies that GetACL request correctly populates RequestGetAcl object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestGetACL()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string path = this.GetRandomString();
                var stat = new Stat() { Version = this.GetRandomInt() };
                var expectedAcl = new List<Acl>() { new Acl((int)Acl.Perm.ALL, new Id(AuthSchemes.Digest, this.GetRandomString())) };

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestGetAcl);
                    var getAclRequest = (RequestGetAcl)request;
                    Assert.AreEqual(path, getAclRequest.Path);
                    Assert.AreEqual(stat.Version, getAclRequest.Stat.Version);

                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Ok,
                        Content = expectedAcl
                    };
                };

                var resultAcl = client.GetACL(path, stat).Result;
                Assert.AreEqual(expectedAcl.Count, resultAcl.Count);
                Assert.IsTrue(Acl.AreEqual(expectedAcl[0], resultAcl[0]));
            }
        }

        /// <summary>
        /// Verifies that SetACL request correctly populates RequestSetAcl object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestSetACL()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string path = this.GetRandomString();
                var version = this.GetRandomInt();
                var acl = new List<Acl>() { new Acl((int)Acl.Perm.ALL, new Id(AuthSchemes.Digest, this.GetRandomString())) };
                var expectedStat = new Stat() { Version = this.GetRandomInt() };

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestSetAcl);
                    var setAclRequest = (RequestSetAcl)request;
                    Assert.AreEqual(path, setAclRequest.Path);
                    Assert.AreEqual(acl.Count, setAclRequest.Acl.Count);
                    Assert.IsTrue(Acl.AreEqual(acl[0], setAclRequest.Acl[0]));

                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Ok,
                        Stat = expectedStat
                    };
                };

                var stat = client.SetACL(path, acl, version).Result;
                Assert.AreEqual(expectedStat.Version, stat.Version);
            }
        }

        /// <summary>
        /// Verifies that Sync request correctly populates RequestSync object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestSync()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string path = this.GetRandomString();

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestSync);
                    var syncRequest = (RequestSync)request;
                    Assert.AreEqual(path, syncRequest.Path);

                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Ok
                    };
                };

                client.Sync(path).Wait();
            }
        }

        /// <summary>
        /// Verifies that Multi request correctly populates RequestMulti object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestMulti()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string path = this.GetRandomString();
                var version = this.GetRandomInt();
                var operations = new List<Op>();
                operations.Add(Op.Delete(path, version, recursive: false));

                var expectedResults = new List<OpResult>();
                expectedResults.Add(new OpResult.DeleteResult());

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestMulti);
                    var multiRequest = (RequestMulti)request;
                    Assert.IsTrue(multiRequest.CompleteSynchronously);
                    Assert.AreEqual(operations.Count, multiRequest.Requests.Count);
                    Assert.IsTrue(multiRequest.Requests[0] is RequestDelete);

                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Ok,
                        Content = expectedResults
                    };
                };

                var results = client.Multi(operations, mustCompleteSynchronously: true).Result;
                Assert.AreEqual(results.Count, expectedResults.Count);
                Assert.IsTrue(results[0] is OpResult.DeleteResult);

                try
                {
                    client.Multi(null, mustCompleteSynchronously: false).Wait();
                    Assert.Fail("Multi call must have thrown ArgumentNullException");
                }
                catch (ArgumentException)
                {
                }
            }
        }

        /// <summary>
        /// Verifies that Batch request correctly populates RequestBatch object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestBatch()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string path = this.GetRandomString();
                var version = this.GetRandomInt();
                var operations = new List<Op>();
                operations.Add(Op.Delete(path, version, recursive: false));

                var expectedResults = new List<OpResult>();
                expectedResults.Add(new OpResult.DeleteResult());

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestBatch);
                    var batchRequest = (RequestBatch)request;
                    Assert.IsTrue(batchRequest.CompleteSynchronously);
                    Assert.AreEqual(operations.Count, batchRequest.Requests.Count);
                    Assert.IsTrue(batchRequest.Requests[0] is RequestDelete);

                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Ok,
                        Content = expectedResults
                    };
                };

                var results = client.Batch(operations, mustCompleteSynchronously: true).Result;
                Assert.AreEqual(results.Count, expectedResults.Count);
                Assert.IsTrue(results[0] is OpResult.DeleteResult);

                try
                {
                    client.Batch(null, mustCompleteSynchronously: false).Wait();
                    Assert.Fail("Batch call must have thrown ArgumentNullException");
                }
                catch (ArgumentException)
                {
                }
            }
        }

        /// <summary>
        /// Verifies that SetAuth request correctly populates RequestSetAuth object and handles the response.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestSetAuth()
        {
            using (var requestHandler = new TestRequestHandler())
            using (var client = new RingMasterClient(requestHandler))
            {
                string path = this.GetRandomString();
                var id = new Id(AuthSchemes.Digest, this.GetRandomString());

                requestHandler.Implementation = request =>
                {
                    Assert.IsTrue(request is RequestSetAuth);
                    var setAuthRequest = (RequestSetAuth)request;
                    Assert.AreEqual($"digest:{id.Identifier}", setAuthRequest.ClientId);

                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Ok
                    };
                };

                client.SetAuth(id).Wait();
            }
        }

        /// <summary>
        /// Get the specified number of X509Certificates from the personal certificate store in the local machine.
        /// </summary>
        /// <param name="count">Number of certificates</param>
        /// <returns>An array with the specified number of certificates</returns>
        private static X509Certificate[] GetLocalCertificates(int count)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            var certificates = new List<X509Certificate>();
            try
            {
                store.Open(OpenFlags.ReadOnly);
                DateTime now = DateTime.Now;

                // Select certificates from the personal store in the local machine that were specifically
                // created for use by this unit test.
                foreach (var certificate in store.Certificates)
                {
                    certificates.Add(certificate);

                    if (certificates.Count >= count)
                    {
                        break;
                    }
                }

                return certificates.ToArray();
            }
            finally
            {
                store.Close();
            }
        }

        private string GetRandomString()
        {
            return Guid.NewGuid().ToString();
        }

        private byte[] GetRandomData()
        {
            return Guid.NewGuid().ToByteArray();
        }

        private int GetRandomInt()
        {
            return this.random.Next();
        }

        private sealed class TestWatcher : IWatcher
        {
            public TestWatcher(int id)
            {
                this.Id = (ulong)id;
                this.Kind = WatcherKind.OneUse;
            }

            public ulong Id { get; private set; }

            public bool OneUse => this.Kind.HasFlag(WatcherKind.OneUse);

            public WatcherKind Kind { get; private set; }

            public void Process(WatchedEvent evt)
            {
            }
        }

        private sealed class TestRequestHandler : IRingMasterRequestHandlerOverlapped
        {
            public int Timeout { get; set; }

            public Func<IRingMasterRequest, RequestResponse> Implementation { get; set; }

            public void Close()
            {
            }

            public void Dispose()
            {
                this.Close();
            }

            public Task<RequestResponse> Request(IRingMasterRequest request)
            {
                return Task.FromResult(this.Implementation(request));
            }

            public void RequestOverlapped(IRingMasterRequest request, Action<RequestResponse, Exception> onCompletion)
            {
                onCompletion?.Invoke(this.Implementation(request), null);
            }
        }
    }
}
