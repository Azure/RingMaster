// <copyright file="TestSerializeDeserialize.cs" company="Microsoft">
//     Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocolUnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Verifies that requests are able to be serialized and deserialized properly.
    /// </summary>
    [TestClass]
    public class TestSerializeDeserialize
    {
        /// <summary>
        /// Random value generator used by the tests.
        /// </summary>
        private static readonly Random RandomValueGenerator = new Random();

        /// <summary>
        /// Verify serialization and deserialization works for <see cref="RequestInit"/> requests.
        /// </summary>
        [TestMethod]
        public void TestRequestInit()
        {
            var originalRequests = new RequestInit[]
            {
                new RequestInit(0, string.Empty),
                PopulateCommonFields(new RequestInit(0, string.Empty)),
                new RequestInit(RandomUlongValue(), string.Empty, readOnlyInterfaceRequiresLocks: false),
                new RequestInit(0, RandomString(), readOnlyInterfaceRequiresLocks: false, redirection: RequestInit.RedirectionPolicy.ForwardPreferred),
                new RequestInit(0, string.Empty, readOnlyInterfaceRequiresLocks: false, redirection: RequestInit.RedirectionPolicy.RedirectPreferred),
            };

            VerifyRequestSerializationAndDeserialization(
                (expected, actual, protocolVersion) =>
                {
                    Assert.AreEqual(RingMasterRequestType.Init, actual.RequestType);
                    Assert.AreEqual(expected.ROInterfaceRequiresLocks, actual.ROInterfaceRequiresLocks);
                    Assert.AreEqual(expected.Redirection, actual.Redirection);
                    Assert.AreEqual(expected.SessionId, actual.SessionId);
                    Assert.AreEqual(expected.SessionPwd, actual.SessionPwd);
                },
                originalRequests);
        }

        /// <summary>
        /// Verify serialization and deserialization works for <see cref="RequestSetAuth"/> requests.
        /// </summary>
        [TestMethod]
        public void TestRequestSetAuth()
        {
            var originalRequests = new RequestSetAuth[]
            {
                new RequestSetAuth(string.Empty),
                PopulateCommonFields(new RequestSetAuth(string.Empty)),
                new RequestSetAuth(RandomString())
            };

            VerifyRequestSerializationAndDeserialization(
                (expected, actual, protocolVersion) =>
                {
                    Assert.AreEqual(RingMasterRequestType.SetAuth, actual.RequestType);
                    Assert.AreEqual(expected.ClientId, actual.ClientId);
                },
                originalRequests);
        }

        /// <summary>
        /// Verify serialization and deserialization works for <see cref="RequestCreate"/> requests.
        /// </summary>
        [TestMethod]
        public void TestRequestCreate()
        {
            var originalRequests = new RequestCreate[]
            {
                new RequestCreate("/create", data: null, acl: null, createMode: CreateMode.Persistent, uid: 0),
                PopulateCommonFields(new RequestCreate("/create", data: null, acl: null, createMode: CreateMode.Persistent, uid: 0)),
                new RequestCreate("/create/persistent", data: null, acl: null, createMode: CreateMode.Persistent, uid: 0),
                new RequestCreate("/create/persistent/allowpathcreation", data: null, acl: null, createMode: CreateMode.PersistentAllowPathCreation, uid: 0),
                new RequestCreate("/create/persistent/sequential", data: null, acl: null, createMode: CreateMode.PersistentSequential, uid: 0),
                new RequestCreate("/create/persistent/sequential/allowpathcreation", data: null, acl: null, createMode: CreateMode.PersistentSequentialAllowPathCreation, uid: 0),
                new RequestCreate("/create/persistent/sequential/allowpathcreationAndSuccessIfExists", data: null, acl: null, createMode: CreateMode.PersistentSequentialAllowPathCreation | CreateMode.SuccessEvenIfNodeExistsFlag, uid: 0),
                new RequestCreate("/create/ephemeral", data: null, acl: null, createMode: CreateMode.Ephemeral, uid: 0),
                new RequestCreate("/create/ephemeral/sequential", data: null, acl: null, createMode: CreateMode.EphemeralSequential, uid: 0),
                new RequestCreate("/create/data", data: RandomData(), acl: null, createMode: CreateMode.Persistent, uid: 0),
                new RequestCreate("/create/acl", data: null, acl: RandomAclList(), createMode: CreateMode.Persistent, uid: 0),
                new RequestCreate("/create/uid", data: null, acl: null, createMode: CreateMode.Persistent, uid: RandomUlongValue()),
            };

            VerifyRequestSerializationAndDeserialization(
                (expected, actual, protocolVersion) =>
                {
                    Assert.AreEqual(RingMasterRequestType.Create, actual.RequestType);
                    VerifyData(expected.Data, actual.Data);
                    VerifyAclLists(expected.Acl, actual.Acl);
                    Assert.AreEqual(expected.CreateMode, actual.CreateMode);
                },
                originalRequests);
        }

        /// <summary>
        /// Verify serialization and deserialization works for <see cref="RequestMulti"/> requests.
        /// </summary>
        [TestMethod]
        public void TestRequestMulti()
        {
            var noOps = new Op[0];
            RequestMulti[] originalRequests = new RequestMulti[]
            {
                new RequestMulti(operations: noOps, completeSynchronously: false, uid: 0),
                PopulateCommonFields(new RequestMulti(operations: noOps, completeSynchronously: false, uid: 0)),
                new RequestMulti(operations: noOps, completeSynchronously: true, uid: 0),
                new RequestMulti(operations: noOps, completeSynchronously: false, uid: RandomUlongValue()),
                new RequestMulti(operations: RandomList(RandomOp), completeSynchronously: false, uid: 0)
            };

            VerifyRequestSerializationAndDeserialization(
                (expected, actual, protocolVersion) =>
                {
                    Assert.AreEqual(RingMasterRequestType.Multi, actual.RequestType);
                    Assert.AreEqual(expected.CompleteSynchronously, actual.CompleteSynchronously);
                    VerifyRequests(expected.Requests, actual.Requests);
                },
                (proto, m) => 
                {
                    if (m == null || m.Requests == null)
                    {
                        return true;
                    }

                    for (int j = 0; j < m.Requests.Count; j++)
                    {
                        IRingMasterRequest r = m.Requests[j];

                        if (r is RequestMove && proto < 19)
                        {
                            return false;
                        }
                    }

                    return true;
                },
                originalRequests);
        }

        /// <summary>
        /// Verify serialization and deserialization works for <see cref="RequestBatch"/> requests.
        /// </summary>
        [TestMethod]
        public void TestRequestBatch()
        {
            var noOps = new Op[0];
            var originalRequests = new RequestBatch[]
            {
                new RequestBatch(operations: noOps, completeSynchronously: false, uid: 0),
                PopulateCommonFields(new RequestBatch(operations: noOps, completeSynchronously: false, uid: 0)),
                new RequestBatch(operations: noOps, completeSynchronously: true, uid: 0),
                new RequestBatch(operations: noOps, completeSynchronously: false, uid: RandomUlongValue()),
                new RequestBatch(operations: RandomList(RandomOp), completeSynchronously: false, uid: 0)
            };

            try
            {
                VerifyRequestSerializationAndDeserialization(
                    (expected, actual, protocolVersion) =>
                    {
                        Assert.AreEqual(RingMasterRequestType.Batch, actual.RequestType);
                        Assert.AreEqual(expected.CompleteSynchronously, actual.CompleteSynchronously);
                        VerifyRequests(expected.Requests, actual.Requests);
                    },
                    (proto, m) =>
                    {
                        if (proto < 15)
                        {
                            return false;
                        }

                        if (m == null || m.Requests == null)
                        {
                            return true;
                        }

                        for (int j = 0; j < m.Requests.Count; j++)
                        {
                            IRingMasterRequest r = m.Requests[j];

                            if (r is RequestMove && proto < 19)
                            {
                                return false;
                            }
                        }

                        return true;
                    },
                    originalRequests);
            }
            catch (ArgumentException ex)
            {
                Assert.AreEqual(ex.Message, "This session doesn't support Batch Requests");
            }
        }

        /// <summary>
        /// Verify serialization and deserialization works for <see cref="RequestDelete"/> requests.
        /// </summary>
        [TestMethod]
        public void TestRequestDelete()
        {
            var originalRequests = new RequestDelete[]
            {
                new RequestDelete("/delete", version: -1, cascade: false),
                PopulateCommonFields(new RequestDelete("/delete", version: -1, cascade: false)),
                new RequestDelete("/delete/specificVersion", version: RandomIntValue(), cascade: false),
                new RequestDelete("/delete/cascade", version: 0, cascade: true),
                new RequestDelete("/delete/uid", version: 0, cascade: false, uid: RandomUlongValue()),
                new RequestDelete("/delete", version: -1, deletemode: DeleteMode.FastDelete),
                new RequestDelete("/delete", version: -1, deletemode: DeleteMode.FastDelete | DeleteMode.CascadeDelete),
            };

            VerifyRequestSerializationAndDeserialization(
                (expected, actual, protocolVersion) =>
                {
                    Assert.AreEqual(RingMasterRequestType.Delete, actual.RequestType);
                    Assert.AreEqual(expected.IsCascade, actual.IsCascade);
                    Assert.AreEqual(expected.IsCascade, (actual.DeleteMode & DeleteMode.CascadeDelete) == DeleteMode.CascadeDelete);
                    if (protocolVersion >= SerializationFormatVersions.Version18)
                    {
                        Assert.AreEqual(expected.DeleteMode, actual.DeleteMode);
                    }
                },
                (proto, m) =>
                {
                    if (m == null)
                    {
                        return true;
                    }

                    if (proto <= 17 && (m.DeleteMode != DeleteMode.None || m.DeleteMode != DeleteMode.CascadeDelete))
                    {
                        return false;
                    }

                    return true;
                },
                originalRequests);

            originalRequests = new RequestDelete[]
            {
                new RequestDelete("/delete", version: -1, cascade: false),
                PopulateCommonFields(new RequestDelete("/delete", version: -1, cascade: false)),
                new RequestDelete("/delete/specificVersion", version: RandomIntValue(), cascade: false),
                new RequestDelete("/delete/cascade", version: 0, cascade: true),
                new RequestDelete("/delete/ignoreNoNode", version: 0, deletemode: DeleteMode.SuccessEvenIfNodeDoesntExist),
                new RequestDelete("/delete/cascadeAndIgnoreNoNode", version: 0, deletemode: DeleteMode.SuccessEvenIfNodeDoesntExist | DeleteMode.CascadeDelete),
                new RequestDelete("/delete/uid", version: 0, cascade: false, uid: RandomUlongValue()),
                new RequestDelete("/delete", version: -1, deletemode: DeleteMode.FastDelete | DeleteMode.SuccessEvenIfNodeDoesntExist),
                new RequestDelete("/delete", version: -1, deletemode: DeleteMode.FastDelete | DeleteMode.SuccessEvenIfNodeDoesntExist | DeleteMode.CascadeDelete)
            };

            VerifyRequestSerializationAndDeserialization<RequestDelete>(
                (expected, actual, protocolVersion) =>
                {
                    Assert.AreEqual(RingMasterRequestType.Delete, actual.RequestType);
                    Assert.AreEqual(expected.IsCascade, actual.IsCascade);
                    Assert.AreEqual(expected.IsCascade, (actual.DeleteMode & DeleteMode.CascadeDelete) == DeleteMode.CascadeDelete);
                    if (protocolVersion >= SerializationFormatVersions.Version18)
                    {
                        Assert.AreEqual(expected.DeleteMode, actual.DeleteMode);
                    }
                },
                (proto, m) =>
                {
                    if (m == null)
                    {
                        return true;
                    }

                    if (proto <= 17 && (m.DeleteMode != DeleteMode.None || m.DeleteMode != DeleteMode.CascadeDelete))
                    {
                        return false;
                    }

                    if (proto <= 18 && ((m.DeleteMode & DeleteMode.FastDelete) == DeleteMode.FastDelete))
                    {
                        return false;
                    }

                    return true;
                },
                originalRequests);
        }

        /// <summary>
        /// Verify serialization and deserialization works for <see cref="RequestMove"/> requests.
        /// </summary>
        [TestMethod]
        public void TestRequestMove()
        {
            var originalRequests = new RequestMove[]
            {
                new RequestMove("/move", version: -1, pathDst: "/move2", moveMode: MoveMode.None),
                new RequestMove("/move2", version: 1, pathDst: "/move3", moveMode: MoveMode.None),
                new RequestMove("/move3", version: 0, pathDst: "/move4", moveMode: MoveMode.None),
                new RequestMove("/move4", version: -1, pathDst: "/move5", moveMode: MoveMode.None),
                new RequestMove("/move2", version: 1, pathDst: "/move3", moveMode: MoveMode.AllowPathCreationFlag),
                new RequestMove("/move3", version: 0, pathDst: "/move4", moveMode: MoveMode.OnlyIfSourcePathIsEmpty),
                new RequestMove("/move4", version: -1, pathDst: "/move5", moveMode: MoveMode.AllowPathCreationFlag | MoveMode.OnlyIfSourcePathIsEmpty),
            };
            VerifyRequestSerializationAndDeserialization(
                (expected, actual, protocolVersion) =>
                {
                    Assert.AreEqual(RingMasterRequestType.Move, actual.RequestType);
                    Assert.AreEqual(expected.Path, actual.Path);
                    Assert.AreEqual(expected.MoveMode, actual.MoveMode);
                    Assert.AreEqual(expected.PathDst, actual.PathDst);
                    Assert.AreEqual(expected.Version, actual.Version);
                },
                (prot) => { return prot >= 19; },
                originalRequests);
        }

        /// <summary>
        /// Verify serialization and deserialization works for <see cref="RequestCheck"/> requests.
        /// </summary>
        [TestMethod]
        public void TestRequestCheck()
        {
            var originalRequests = new RequestCheck[]
            {
                new RequestCheck(path: "/check", version: 0, cversion: 0, aversion: 0, uniqueIncarnation: Guid.Empty, uniqueIncarnationIdKind: RequestCheck.UniqueIncarnationIdType.None, uid: 0),
                PopulateCommonFields(new RequestCheck(path: "/check", version: 0, cversion: 0, aversion: 0, uniqueIncarnation: Guid.Empty, uniqueIncarnationIdKind: RequestCheck.UniqueIncarnationIdType.None, uid: 0)),
                new RequestCheck(path: "/check/version", version: RandomIntValue(), cversion: RandomIntValue(), aversion: RandomIntValue(), uniqueIncarnation: Guid.Empty, uniqueIncarnationIdKind: RequestCheck.UniqueIncarnationIdType.None, uid: 0),
                new RequestCheck(path: "/check/uniqueIncarnationIdSimple", version: 0, cversion: 0, aversion: 0, uniqueIncarnation: Guid.NewGuid(), uniqueIncarnationIdKind: RequestCheck.UniqueIncarnationIdType.Simple, uid: 0),
                new RequestCheck(path: "/check/uniqueIncarnationIdExtended", version: 0, cversion: 0, aversion: 0, uniqueIncarnation: Guid.NewGuid(), uniqueIncarnationIdKind: RequestCheck.UniqueIncarnationIdType.Extended, uid: 0),
                new RequestCheck(path: "/check/uid", version: 0, cversion: 0, aversion: 0, uniqueIncarnation: Guid.Empty, uniqueIncarnationIdKind: RequestCheck.UniqueIncarnationIdType.None, uid: RandomUlongValue()),
            };

            VerifyRequestSerializationAndDeserialization(
                (expected, actual, protocolVersion) =>
                {
                    Assert.AreEqual(RingMasterRequestType.Check, actual.RequestType);
                    Assert.AreEqual(expected.UniqueIncarnationId, actual.UniqueIncarnationId);
                    if (protocolVersion >= 16)
                    {
                        Assert.AreEqual(expected.CVersion, actual.CVersion);
                        Assert.AreEqual(expected.AVersion, actual.AVersion);
                    }
                    else
                    {
                        Assert.AreEqual(-1, actual.CVersion);
                        Assert.AreEqual(-1, actual.AVersion);
                    }

                    Assert.AreEqual(expected.UniqueIncarnationIdKind, actual.UniqueIncarnationIdKind);
                },
                originalRequests);
        }

        /// <summary>
        /// Verify serialization and deserialization works for <see cref="RequestSync"/> requests.
        /// </summary>
        [TestMethod]
        public void TestRequestSync()
        {
            var originalRequests = new RequestSync[]
            {
                new RequestSync("/sync", uid: 0),
                PopulateCommonFields(new RequestSync("/sync", uid: 0)),
                new RequestSync("/sync/uid", uid: RandomUlongValue())
            };

            VerifyRequestSerializationAndDeserialization(
                (expected, actual, protocolVersion) =>
                {
                    Assert.AreEqual(RingMasterRequestType.Sync, actual.RequestType);
                },
                originalRequests);
        }

        /// <summary>
        /// Verify serialization and deserialization works for <see cref="RequestExists"/> requests.
        /// </summary>
        [TestMethod]
        public void TestRequestExists()
        {
            var originalRequests = new RequestExists[]
            {
                new RequestExists("/exists", watcher: null, uid: 0),
                PopulateCommonFields(new RequestExists("/exists", watcher: null, uid: 0)),
                new RequestExists("/exists/watcher", watcher: RandomWatcher(), uid: 0),
                new RequestExists("/exists/uid", watcher: null, uid: RandomUlongValue())
            };

            VerifyRequestSerializationAndDeserialization(
                (expected, actual, protocolVersion) =>
                {
                    Assert.AreEqual(RingMasterRequestType.Exists, actual.RequestType);
                    VerifyWatcher(expected.Watcher, actual.Watcher);
                },
                originalRequests);
        }

        /// <summary>
        /// Verify serialization and deserialization works for <see cref="RequestGetAcl"/> requests.
        /// </summary>
        [TestMethod]
        public void TestRequestGetAcl()
        {
            var originalRequests = new RequestGetAcl[]
            {
                new RequestGetAcl("/getacl", stat: null, uid: 0),
                PopulateCommonFields(new RequestGetAcl("/getacl", stat: null, uid: 0)),
                new RequestGetAcl("/getacl/stat", stat: RandomStat(), uid: 0),
                new RequestGetAcl("/getacl/uid", stat: null, uid: RandomUlongValue()),
            };

            VerifyRequestSerializationAndDeserialization(
                (expected, actual, protocolVersion) =>
                {
                    Assert.AreEqual(RingMasterRequestType.GetAcl, actual.RequestType);
                    VerifyStat(expected.Stat, actual.Stat);
                },
                originalRequests);
        }

        /// <summary>
        /// Verify serialization and deserialization works for <see cref="RequestGetData"/> requests.
        /// </summary>
        [TestMethod]
        public void TestRequestGetData()
        {
            var originalRequests = new RequestGetData[]
            {
                new RequestGetData("/getdata", options: RequestGetData.GetDataOptions.None, watcher: null, uid: 0),
                PopulateCommonFields(new RequestGetData("/getdata", options: RequestGetData.GetDataOptions.None, watcher: null, uid: 0)),
                new RequestGetData("/getdata/watcher", options: RequestGetData.GetDataOptions.None, watcher: RandomWatcher(), uid: 0),
                new RequestGetData("/getdata/uid", options: RequestGetData.GetDataOptions.None, watcher: null, uid: RandomUlongValue()),
                new RequestGetData("/getdata/faultbackonparentdata", options: RequestGetData.GetDataOptions.FaultbackOnParentData, watcher: null, uid: 0),
                new RequestGetData("/getdata/nostatrequired", options: RequestGetData.GetDataOptions.NoStatRequired, watcher: null, uid: 0),
                new RequestGetData("/getdata/nowildcardsforpath", options: RequestGetData.GetDataOptions.NoWildcardsForPath, watcher: null, uid: 0),
                new RequestGetData("/getdata/faultbackonparentdatawithmatch", options: RequestGetData.GetDataOptions.FaultbackOnParentDataWithMatch, optionArgument: new RequestGetData.GetDataOptionArgumentForMatch(RandomData(), RandomIntValue(), RequestGetData.GetDataOptionArgumentForMatch.Comparison.Smaller), watcher: null, uid: 0)
            };

            VerifyRequestSerializationAndDeserialization(
                (expected, actual, protocolVersion) =>
                {
                    Assert.AreEqual(RingMasterRequestType.GetData, actual.RequestType);
                    VerifyWatcher(expected.Watcher, actual.Watcher);
                    Assert.AreEqual(expected.FaultbackOnParentData, actual.FaultbackOnParentData);
                    Assert.AreEqual(expected.NoStatRequired, actual.NoStatRequired);
                    Assert.AreEqual(expected.NoWildcardsForPath, actual.NoWildcardsForPath);
                    Assert.AreEqual(expected.Options, actual.Options);
                    if (expected.OptionArgument != null)
                    {
                        if (protocolVersion >= 14)
                        {
                            Assert.IsNotNull(actual.OptionArgument);
                            Assert.AreEqual(expected.OptionArgument.Option, actual.OptionArgument.Option);

                            if (expected.OptionArgument is RequestGetData.GetDataOptionArgumentForMatch)
                            {
                                Assert.IsTrue(actual.OptionArgument is RequestGetData.GetDataOptionArgumentForMatch);
                                var expectedOptionArgument = (RequestGetData.GetDataOptionArgumentForMatch)expected.OptionArgument;
                                var actualOptionArgument = (RequestGetData.GetDataOptionArgumentForMatch)actual.OptionArgument;
                                VerifyData(expectedOptionArgument.Bytes, actualOptionArgument.Bytes);
                                Assert.AreEqual(expectedOptionArgument.Position, actualOptionArgument.Position);
                                Assert.AreEqual(expectedOptionArgument.Condition, actualOptionArgument.Condition);
                            }
                        }
                        else
                        {
                            Assert.IsNull(actual.OptionArgument);
                        }
                    }
                    else
                    {
                        if (actual.Options == RequestGetData.GetDataOptions.FaultbackOnParentData)
                        {
                            Assert.IsNotNull(actual.OptionArgument);
                            Assert.IsTrue(actual.OptionArgument is RequestGetData.GetDataOptionArgumentForMatch);
                            var actualOptionArgument = (RequestGetData.GetDataOptionArgumentForMatch)actual.OptionArgument;
                            Assert.IsNull(actualOptionArgument.Bytes);
                            Assert.AreEqual(0, actualOptionArgument.Position);
                            Assert.AreEqual(RequestGetData.GetDataOptionArgumentForMatch.Comparison.Different, actualOptionArgument.Condition);
                        }
                        else
                        {
                            Assert.IsNull(actual.OptionArgument);
                        }
                    }
                },
                originalRequests);
        }

        /// <summary>
        /// Verify serialization and deserialization works for <see cref="RequestGetChildren"/> requests.
        /// </summary>
        [TestMethod]
        public void TestRequestGetChildren()
        {
            var originalRequests = new RequestGetChildren[]
            {
                new RequestGetChildren("/getchildren", watcher: null, retrievalCondition: null, uid: 0),
                PopulateCommonFields(new RequestGetChildren("/getchildren", watcher: null, retrievalCondition: null, uid: 0)),
                new RequestGetChildren("/getchildren/watcher", watcher: RandomWatcher(), retrievalCondition: null, uid: 0),
                new RequestGetChildren("/getchildren/retrievalCondition", watcher: null, retrievalCondition: RandomString(), uid: 0),
                new RequestGetChildren("/getchildren/uid", watcher: null, retrievalCondition: null, uid: RandomUlongValue()),
            };

            VerifyRequestSerializationAndDeserialization(
                (expected, actual, protocolVersion) =>
                {
                    Assert.AreEqual(RingMasterRequestType.GetChildren, actual.RequestType);
                    VerifyWatcher(expected.Watcher, actual.Watcher);
                    Assert.AreEqual(expected.RetrievalCondition, actual.RetrievalCondition);
                },
                originalRequests);
        }

        /// <summary>
        /// Verify serialization and deserialization works for <see cref="RequestSetData"/> requests.
        /// </summary>
        [TestMethod]
        public void TestRequestSetData()
        {
            var originalRequests = new RequestSetData[]
            {
                new RequestSetData("/setdata", data: null, version: 0, dataCommand: false, uid: 0),
                PopulateCommonFields(new RequestSetData("/setdata", data: null, version: 0, dataCommand: false, uid: 0)),
                new RequestSetData("/setdata/data", data: RandomData(), version: 0, dataCommand: false, uid: 0),
                new RequestSetData("/setdata/version", data: null, version: RandomIntValue(), dataCommand: false, uid: 0),
                new RequestSetData("/setdata/datacommand", data: null, version: 0, dataCommand: true, uid: 0),
                new RequestSetData("/setdata/uid", data: null, version: 0, dataCommand: false, uid: RandomUlongValue()),
            };

            VerifyRequestSerializationAndDeserialization(
                (expected, actual, protocolVersion) =>
                {
                    Assert.AreEqual(RingMasterRequestType.SetData, actual.RequestType);
                    VerifyData(expected.Data, actual.Data);
                    Assert.AreEqual(expected.Version, actual.Version);
                    Assert.AreEqual(expected.IsDataCommand, actual.IsDataCommand);
                },
                originalRequests);
        }

        /// <summary>
        /// Verify serialization and deserialization works for <see cref="RequestSetAcl"/> requests.
        /// </summary>
        [TestMethod]
        public void TestRequestSetAcl()
        {
            var originalRequests = new RequestSetAcl[]
            {
                new RequestSetAcl("/setacl", acl: null, version: 0, uid: 0),
                PopulateCommonFields(new RequestSetAcl("/setacl", acl: null, version: 0, uid: 0)),
                new RequestSetAcl("/setacl/acl", acl: RandomAclList(), version: 0, uid: 0),
                new RequestSetAcl("/setacl/version", acl: null, version: RandomIntValue(), uid: 0),
                new RequestSetAcl("/setacl/uid", acl: null, version: 0, uid: RandomUlongValue()),
            };

            VerifyRequestSerializationAndDeserialization(
                (expected, actual, protocolVersion) =>
                {
                    Assert.AreEqual(RingMasterRequestType.SetAcl, actual.RequestType);
                    VerifyAclLists(expected.Acl, actual.Acl);
                    Assert.AreEqual(expected.Version, actual.Version);
                },
                originalRequests);
        }

        /// <summary>
        /// Verify serialization and deserialization for <see cref="RequestResponse"/> objects.
        /// </summary>
        [TestMethod]
        public void TestRequestResponse()
        {
            var originalResponses = new RequestResponse[]
            {
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = null, Stat = null, ResponsePath = null },
                new RequestResponse() { CallId = RandomUlongValue(), ResultCode = 0, Content = null, Stat = null, ResponsePath = null },
                new RequestResponse() { CallId = 0, ResultCode = RandomIntValue(), Content = null, Stat = null, ResponsePath = null },
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = null, Stat = RandomStat(), ResponsePath = null },
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = null, Stat = null, ResponsePath = RandomString() },
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = RandomRingMasterRequest(), Stat = null, ResponsePath = null },
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = new WatcherCall() { Watcher = RandomWatcher(), Kind = WatcherKind.OneUse, WatcherId = RandomUlongValue(), WatcherEvt = new WatchedEvent(WatchedEvent.WatchedEventType.None, WatchedEvent.WatchedEventKeeperState.Disconnected, path: RandomString()) }, Stat = null, ResponsePath = null },
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = new WatcherCall() { Watcher = RandomWatcher(), Kind = default(WatcherKind), WatcherId = RandomUlongValue(), WatcherEvt = new WatchedEvent(WatchedEvent.WatchedEventType.None, WatchedEvent.WatchedEventKeeperState.Disconnected, path: RandomString(),  data : new byte[] { 1, 2, 3 }), }, Stat = null, ResponsePath = null },
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = RandomList(RandomString).ToArray(), Stat = null, ResponsePath = null },
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = RandomList(RandomString), Stat = null, ResponsePath = null },
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = RandomString(), Stat = null, ResponsePath = null },
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = RandomStat(), Stat = null, ResponsePath = null },
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = RandomAclList(), Stat = null, ResponsePath = null },
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = RandomData(), Stat = null, ResponsePath = null },
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = RandomList(RandomOpResult), Stat = null, ResponsePath = null },
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = new RedirectSuggested() { SuggestedConnectionString = RandomString() }, Stat = null, ResponsePath = null },
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = (Func<object>)(() => RandomList(RandomString)), Stat = null, ResponsePath = null },
                new RequestResponse() { CallId = 0, ResultCode = 0, Content = new Uri("http://example.com"), Stat = null, ResponsePath = null },
            };

            VerifyResponseSerializationAndDeserialization(
                (p, exp) =>
                {
                    if (p < 19)
                    {
                        IRingMasterRequest req = exp.Content as RequestMove;
                        if (req != null)
                        {
                            return false;
                        }
                    }

                    return true;
                },
                originalResponses);
        }

        /// <summary>
        /// Populate fields common to all <see cref="AbstractRingMasterRequest"/> objects with random values.
        /// </summary>
        /// <typeparam name="T">A type that implements <see cref="IRingMasteRequest"/></typeparam>
        /// <param name="request">The request whose fields must be populated</param>
        /// <returns>The given request with common fields populated with random values</returns>
        private static T PopulateCommonFields<T>(T request) where T : AbstractRingMasterRequest
        {
            request.TimeStreamId = RandomUlongValue();
            request.ExecutionQueueId = Guid.NewGuid();
            request.ExecutionQueueTimeoutMillis = RandomIntValue();
            request.Overrides = new OperationOverrides()
            {
                TxId = RandomUlongValue(),
                TxTime = RandomUlongValue()
            };

            try
            {
                PopulateAuth(request);
                Assert.IsFalse(request.RequestType == RingMasterRequestType.Init || request.RequestType == RingMasterRequestType.SetAuth);
            }
            catch (NotImplementedException)
            {
                Assert.IsTrue(request.RequestType == RingMasterRequestType.Init || request.RequestType == RingMasterRequestType.SetAuth);
            }

            return request;
        }

        /// <summary>
        /// Populate <c>Auth</c> field of the given request with random values.
        /// </summary>
        /// <typeparam name="T">A type that implements <see cref="AbstractRingMasterRequest"/></typeparam>
        /// <param name="request">The request whose <c>Auth</c> field must be populated</param>
        /// <returns>The given request with <c>Auth</c> field populated with random values</returns>
        private static T PopulateAuth<T>(T request) where T : AbstractRingMasterRequest
        {
            request.Auth = new SessionAuth()
            {
                ClientDigest = Guid.NewGuid().ToString(),
                ClientIdentity = Guid.NewGuid().ToString(),
                ClientIP = Guid.NewGuid().ToString()
            };

            return request;
        }

        /// <summary>
        /// Generate a random integer value.
        /// </summary>
        /// <returns>A random integer value</returns>
        private static int RandomIntValue()
        {
            return RandomValueGenerator.Next(int.MinValue, int.MaxValue);
        }

        /// <summary>
        /// Generate a random enumerable value.
        /// </summary>
        /// <typeparam name="T">the type of the enumerable</typeparam>
        /// <returns>A random T value, being T an enumerable</returns>
        private static T RandomEnum<T>()
        {
            Array arr = Enum.GetValues(typeof(T));
            return (T)arr.GetValue(RandomValueGenerator.Next(0, arr.Length - 1));
        }

        /// <summary>
        /// Generate a random <c>ulong</c> value.
        /// </summary>
        /// <returns>A random <c>ulong</c> value</returns>
        private static ulong RandomUlongValue()
        {
            return (ulong)RandomValueGenerator.Next(0, int.MaxValue) * (ulong)RandomValueGenerator.Next(0, int.MaxValue);
        }

        /// <summary>
        /// Generate a random <c>long</c> value.
        /// </summary>
        /// <returns>A random <c>long</c> value</returns>
        private static long RandomLongValue()
        {
            return (long)RandomValueGenerator.Next(int.MinValue, int.MaxValue) * (long)RandomValueGenerator.Next(int.MinValue, int.MaxValue);
        }

        /// <summary>
        /// Generate a random string value.
        /// </summary>
        /// <returns>A random string value</returns>
        private static string RandomString()
        {
            int length = RandomValueGenerator.Next(0, 1024);
            var builder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                int codePoint = 0;

                do
                {
                    codePoint = RandomValueGenerator.Next(0, 0x10FFFF);
                }
                while ((codePoint >= 0xD800) && (codePoint <= 0xDFFF));

                builder.Append(char.ConvertFromUtf32(codePoint));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Generate a data buffer of random length filled with random data.
        /// </summary>
        /// <returns>Data buffer with random contents</returns>
        private static byte[] RandomData()
        {
            byte[] buffer = new byte[RandomValueGenerator.Next(0, 10240)];
            RandomValueGenerator.NextBytes(buffer);
            return buffer;
        }

        /// <summary>
        /// Generate a <see cref="Acl"/> list with random entries.
        /// </summary>
        /// <returns><see cref="Acl"/> list with random entries</returns>
        private static IReadOnlyList<Acl> RandomAclList()
        {
            var aclList = new List<Acl>();
            int numEntries = RandomValueGenerator.Next(0, 16);
            for (int i = 0; i < numEntries; i++)
            {
                var identifier = new Id(AuthSchemes.Digest, RandomString());
                var acl = new Acl((int)Acl.Perm.ALL, identifier);
                aclList.Add(acl);
            }

            return aclList;
        }

        /// <summary>
        /// Generate a <see cref="IWatcher"/> with random id.
        /// </summary>
        /// <returns>A <see cref="IWatcher"/> with random id</returns>
        private static IWatcher RandomWatcher()
        {
            return new Watcher(id: RandomUlongValue(), kind: WatcherKind.OneUse);
        }

        /// <summary>
        /// Generate a <see cref="IStat"/> object with random values.
        /// </summary>
        /// <returns>A <see cref="IStat"/> object with random values</returns>
        private static IStat RandomStat()
        {
            return new Stat()
            {
                Czxid = RandomLongValue(),
                Mzxid = RandomLongValue(),
                Pzxid = RandomLongValue(),
                Ctime = RandomLongValue(),
                Mtime = RandomLongValue(),
                Version = RandomIntValue(),
                Cversion = RandomIntValue(),
                Aversion = RandomIntValue(),
                DataLength = RandomIntValue(),
                NumChildren = RandomIntValue()
            };
        }

        /// <summary>
        /// Generate a random <see cref="IRingMasterRequest"/>
        /// </summary>
        /// <returns>A random <see cref="IRingMasterRequest"/></returns>
        private static IRingMasterRequest RandomRingMasterRequest()
        {
            int requestType = RandomValueGenerator.Next(0, 13);
            switch (requestType)
            {
                case 0:
                    return new RequestInit(sessionId: RandomUlongValue(), sessionPwd: RandomString());
                case 1:
                    return new RequestSetAuth(clientId: RandomString());
                case 2:
                    return new RequestCreate(path: RandomString(), data: RandomData(), acl: RandomAclList(), createMode: CreateMode.Persistent, uid: RandomUlongValue());
                case 3:
                    return new RequestDelete(path: RandomString(), version: RandomIntValue(), cascade: false, uid: RandomUlongValue());
                case 4:
                    return new RequestCheck(path: RandomString(), version: RandomIntValue(), uid: RandomUlongValue());
                case 5:
                    return new RequestSync(path: RandomString(), uid: RandomUlongValue());
                case 6:
                    return new RequestExists(path: RandomString(), watcher: RandomWatcher(), uid: RandomUlongValue());
                case 7:
                    return new RequestGetAcl(path: RandomString(), stat: RandomStat(), uid: RandomUlongValue());
                case 8:
                    return new RequestGetData(path: RandomString(), options: RequestGetData.GetDataOptions.None, watcher: RandomWatcher(), uid: RandomUlongValue());
                case 9:
                    return new RequestGetChildren(path: RandomString(), watcher: RandomWatcher(), retrievalCondition: RandomString(), uid: RandomUlongValue());
                case 10:
                    return new RequestSetAcl(path: RandomString(), acl: RandomAclList(), version: RandomIntValue(), uid: RandomUlongValue());
                case 11:
                    return new RequestSetData(path: RandomString(), data: RandomData(), version: RandomIntValue(), dataCommand: false, uid: RandomUlongValue());
                case 12:
                    return new RequestMove(pathSrc: RandomString(), version: RandomIntValue(), pathDst: RandomString(), moveMode: RandomEnum<MoveMode>());
                default:
                    return new RequestSync("/sync", uid: RandomUlongValue());
            }
        }

        /// <summary>
        /// Generate a random <see cref="Op"/>
        /// </summary>
        /// <returns>A random <see cref="Op"/></returns>
        private static Op RandomOp()
        {
            // Op Check will be created with higher probability
            int type = RandomValueGenerator.Next(0, 11);
            switch (type)
            {
                case 0:
                    return Op.Create(path: RandomString(), data: RandomData(), acl: RandomAclList(), mode: CreateMode.Persistent);
                case 1:
                    return Op.Run(RandomRingMasterRequest());
                case 2:
                    return Op.Check(path: RandomString(), uniqueIncarnationId: Guid.NewGuid(), isExtended: false);
                case 3:
                    return Op.GetData(path: RandomString(), options: RequestGetData.GetDataOptions.None, checkUsedForThisPath: null);
                case 4:
                    return Op.Delete(path: RandomString(), version: RandomIntValue(), recursive: false);
                case 5:
                    return Op.SetData(path: RandomString(), data: RandomData(), version: RandomIntValue());
                case 6:
                    return Op.SetAcl(path: RandomString(), acl: RandomAclList(), version: RandomIntValue());
                case 7:
                    return Op.Move(path: RandomString(), version: RandomIntValue(), pathDst: RandomString(), mode: RandomEnum<MoveMode>());
                default:
                    return Op.Check(path: RandomString(), version: RandomIntValue());
            }
        }

        /// <summary>
        /// Generate a random <see cref="OpResult"/>.
        /// </summary>
        /// <returns>A random <see cref="OpResult"/></returns>
        private static OpResult RandomOpResult()
        {
            // ErrorResult will be created with higher probability
            int type = RandomValueGenerator.Next(0, 10);
            switch (type)
            {
                case 0:
                    return new OpResult.CheckResult();
                case 1:
                    return new OpResult.GetDataResult(stat: RandomStat(), bytes: RandomData(), path: RandomString());
                case 2:
                    return new OpResult.CreateResult(stat: RandomStat(), path: RandomString());
                case 3:
                    return new OpResult.DeleteResult();
                case 4:
                    return new OpResult.SetDataResult(stat: RandomStat());
                case 5:
                    return new OpResult.RunResult(new List<OpResult>());
                case 6:
                    return new OpResult.SetAclResult(stat: RandomStat());
                default:
                    return new OpResult.ErrorResult(resultCode: RandomIntValue());
            }
        }

        /// <summary>
        /// Generate a list of objects.
        /// </summary>
        /// <typeparam name="T">Type of objects in the list</typeparam>
        /// <param name="generator">Function that can be used to generate entries in the list</param>
        /// <param name="maxEntries">Maximum number of entries in the list.</param>
        /// <returns>A list of objects generated by the given generator</returns>
        private static List<T> RandomList<T>(Func<T> generator, int maxEntries = 1000)
        {
            var list = new List<T>();
            int numEntries = RandomValueGenerator.Next(0, maxEntries);
            for (int i = 0; i < numEntries; i++)
            {
                list.Add(generator());
            }

            return list;
        }

        /// <summary>
        /// Verify that all the given requests can be serialized and deserialized correctly using all supported versions.
        /// </summary>
        /// <typeparam name="T">A type that implements <see cref="IRingMasteRequest"/></typeparam>
        /// <param name="verifyTypeSpecificFields">Callback that is invoked to compare the type specific fields of the original request with the deserialized request</param>
        /// <param name="requests">Requests to verify</param>
        private static void VerifyRequestSerializationAndDeserialization<T>(Action<T, T, uint> verifyTypeSpecificFields, params T[] requests) where T : IRingMasterRequest
        {
            VerifyRequestSerializationAndDeserialization(verifyTypeSpecificFields, (p, r) => true, requests);
        }

        /// <summary>
        /// Verify that all the given requests can be serialized and deserialized correctly using all supported versions.
        /// </summary>
        /// <typeparam name="T">A type that implements <see cref="IRingMasteRequest"/></typeparam>
        /// <param name="verifyTypeSpecificFields">Callback that is invoked to compare the type specific fields of the original request with the deserialized request</param>
        /// <param name="tryprotocol">tells if the protocol can be tried</param>
        /// <param name="requests">Requests to verify</param>
        private static void VerifyRequestSerializationAndDeserialization<T>(Action<T, T, uint> verifyTypeSpecificFields, Func<uint, bool> tryprotocol, params T[] requests) where T : IRingMasterRequest
        {
            VerifyRequestSerializationAndDeserialization(verifyTypeSpecificFields, (p, r) => tryprotocol(p), requests);
        }

        /// <summary>
        /// Verify that all the given requests can be serialized and deserialized correctly using all supported versions.
        /// </summary>
        /// <typeparam name="T">A type that implements <see cref="IRingMasteRequest"/></typeparam>
        /// <param name="verifyTypeSpecificFields">Callback that is invoked to compare the type specific fields of the original request with the deserialized request</param>
        /// <param name="tryprotocol">tells if the protocol can be tried</param>
        /// <param name="requests">Requests to verify</param>
        private static void VerifyRequestSerializationAndDeserialization<T>(Action<T, T, uint> verifyTypeSpecificFields, Func<uint, T, bool> tryprotocol, params T[] requests) where T : IRingMasterRequest
        {
            for (int i = 0; i < requests.Length; i++)
            {
                T expected = requests[i];
                Trace.TraceInformation("Verifying Serialization/Deserialization for request {0} of {1}", i, requests.Length);
                ulong callId = 0;
                for (uint protocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion;
                    protocolVersion >= RingMasterCommunicationProtocol.MinimumSupportedVersion;
                    protocolVersion--)
                {
                    try
                    {
                        if (!tryprotocol(protocolVersion, expected))
                        {
                            Trace.TraceInformation("Skipping version={0}", protocolVersion);
                            continue;
                        }

                        Trace.TraceInformation("Protocol version={0}", protocolVersion);
                        var requestCall = new RequestCall() { CallId = callId, Request = expected };
                        RequestCall deserializedRequest = SerializeAndDeserialize(requestCall, protocolVersion);
                        Assert.AreEqual(requestCall.CallId, deserializedRequest.CallId);
                        Assert.IsTrue(deserializedRequest.Request is T);

                        T actual = (T)deserializedRequest.Request;
                        VerifyCommonFields(expected, actual);
                        VerifyTimeStreamId(expected, actual, protocolVersion);
                        VerifyExecutionQueue(expected, actual, protocolVersion);

                        verifyTypeSpecificFields(expected, actual, protocolVersion);
                        callId++;
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Failed verification for request " + i + " protocol " + protocolVersion + ": " + e.Message, e);
                    }
                }
            }
        }

        /// <summary>
        /// Verify that all the given responses can be serialized and deserialized correctly using all supported versions.
        /// </summary>
        /// /// <param name="responses">Responses to verify</param>
        private static void VerifyResponseSerializationAndDeserialization(params RequestResponse[] responses)
        {
            VerifyResponseSerializationAndDeserialization((p, r) => { return true; }, responses);
        }

        /// <summary>
        /// Verify that all the given responses can be serialized and deserialized correctly using all supported versions.
        /// </summary>
        /// <param name="tryProtocol">the function to check if the protocol needs to be tested</param>
        /// <param name="responses">Responses to verify</param>
        /// <exception cref="System.Exception">Exception while validating response  + i +  protocol  + protocolVersion + :  + e.Message</exception>
        private static void VerifyResponseSerializationAndDeserialization(Func<uint, RequestResponse, bool> tryProtocol, params RequestResponse[] responses)
        {
            for (int i = 0; i < responses.Length; i++)
            {
                RequestResponse expected = responses[i];
                Trace.TraceInformation("Verifying Serialization/Deserialization for response {0} of {1}", i, responses.Length);
                for (uint protocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion;
                    protocolVersion >= RingMasterCommunicationProtocol.MinimumSupportedVersion;
                    protocolVersion--)
                {
                    try
                    {
                        if (!tryProtocol(protocolVersion, expected))
                        {
                            Trace.TraceInformation("Skipping protocol version={0}", protocolVersion);
                            continue;
                        }

                        Trace.TraceInformation("Protocol version={0}", protocolVersion);
                        var serializer = new Serializer(protocolVersion);
                        serializer.SerializeResponse(expected);

                        byte[] serializedBytes = serializer.GetBytes();

                        var deserializer = new Deserializer(serializedBytes, serializedBytes.Length, protocolVersion);

                        RequestResponse actual = deserializer.DeserializeResponse();
                        Assert.AreEqual(expected.CallId, actual.CallId);
                        Assert.AreEqual(expected.ResultCode, actual.ResultCode);
                        VerifyStat(expected.Stat, actual.Stat);
                        Assert.AreEqual(expected.ResponsePath, actual.ResponsePath);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Exception while validating response " + i + " protocol " + protocolVersion + ": " + e.Message, e);
                    }
                }
            }
        }

        /// <summary>
        /// Verify that fields common to all <see cref="IRingMasterRequest"/> objects are equal.
        /// </summary>
        /// <param name="expected">Request object with expected values</param>
        /// <param name="actual">Request object whose common fields must be compared to <paramref name="expected"/></param>
        private static void VerifyCommonFields(IRingMasterRequest expected, IRingMasterRequest actual)
        {
            Assert.AreEqual(expected.RequestType, actual.RequestType);
            Assert.AreEqual(expected.Uid, actual.Uid);
            Assert.AreEqual(expected.Path, actual.Path);
            VerifyAuth(expected.Auth, actual.Auth);
            VerifyOperationOverrides(expected.Overrides, actual.Overrides);
            Assert.AreEqual(expected.IsReadOnly(), actual.IsReadOnly());
        }

        /// <summary>
        /// Verify that the given <see cref="ISessionAuth"/> objects are equal.
        /// </summary>
        /// <param name="expected"><see cref="ISessionAuth"/> object with expected values</param>
        /// <param name="actual"><see cref="ISessionAuth"/> object whose fields must be compared to <paramref name="expected"/></param>
        private static void VerifyAuth(ISessionAuth expected, ISessionAuth actual)
        {
            if (expected != null)
            {
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.IsSuperSession, actual.IsSuperSession);
                Assert.AreEqual(expected.ClientDigest, actual.ClientDigest);
                Assert.AreEqual(expected.ClientIdentity, actual.ClientIdentity);
                Assert.AreEqual(expected.ClientIP, actual.ClientIP);
            }
            else
            {
                Assert.IsNull(actual);
            }
        }

        /// <summary>
        /// Verify that the time series Id of the given <see cref="IRingMasterRequest"/> objects are equal
        /// </summary>
        /// <param name="expected">Object with expected values</param>
        /// <param name="actual">Object whose fields must be compared to <paramref name="expected"/></param>
        /// <param name="protocolVersionUsed">Communication protocol that was used to serialize and deserialize</param>
        private static void VerifyTimeStreamId(IRingMasterRequest expected, IRingMasterRequest actual, uint protocolVersionUsed)
        {
            if (protocolVersionUsed >= 21)
            {
                Assert.AreEqual(expected.TimeStreamId, actual.TimeStreamId);
            }
            else
            {
                Assert.AreEqual(0ul, actual.TimeStreamId);
            }
        }
        
        /// <summary>
        /// Verify that the execution queue fields of the given <see cref="IRingMasterRequest"/> objects are equal.
        /// </summary>
        /// <param name="expected">Object with expected values</param>
        /// <param name="actual">Object whose fields must be compared to <paramref name="expected"/></param>
        /// <param name="protocolVersionUsed">Communication protocol that was used to serialize and deserialize</param>
        private static void VerifyExecutionQueue(IRingMasterRequest expected, IRingMasterRequest actual, uint protocolVersionUsed)
        {
            if (protocolVersionUsed >= 17)
            {
                Assert.AreEqual(expected.ExecutionQueueId, actual.ExecutionQueueId);
                Assert.AreEqual(expected.ExecutionQueueTimeoutMillis, actual.ExecutionQueueTimeoutMillis);
            }
            else
            {
                Assert.AreEqual(Guid.Empty, actual.ExecutionQueueId);
                Assert.AreEqual(0, actual.ExecutionQueueTimeoutMillis);
            }
        }

        /// <summary>
        /// Verify that the given <see cref="IOperationOverrides"/> objects are equal.
        /// </summary>
        /// <param name="expected"><see cref="IOperationOverrides"/> object with expected values</param>
        /// <param name="actual"><see cref="IOperationOverrides"/> object whose fields must be compared to <paramref name="expected"/></param>
        private static void VerifyOperationOverrides(IOperationOverrides expected, IOperationOverrides actual)
        {
            if (expected != null)
            {
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.TxId, actual.TxId);
                Assert.AreEqual(expected.TxTime, actual.TxTime);
            }
            else
            {
                Assert.IsNull(actual);
            }
        }

        /// <summary>
        /// Verify that the given buffers have exactly the same data.
        /// </summary>
        /// <param name="expected">Buffer with expected data</param>
        /// <param name="actual">Buffer that must be compared to <paramref name="expected"/></param>
        private static void VerifyData(byte[] expected, byte[] actual)
        {
            if (expected != null)
            {
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Length, actual.Length);
                for (int i = 0; i < expected.Length; i++)
                {
                    Assert.AreEqual(expected[i], actual[i]);
                }
            }
            else
            {
                Assert.IsNull(actual);
            }
        }

        /// <summary>
        /// Verify that the given <see cref="Acl"/> lists are equal.
        /// </summary>
        /// <param name="expected">Expected <see cref="Acl"/> list</param>
        /// <param name="actual">Actual <see cref="Acl"/> list</param>
        private static void VerifyAclLists(IReadOnlyList<Acl> expected, IReadOnlyList<Acl> actual)
        {
            if (expected != null)
            {
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Count, actual.Count);
                for (int i = 0; i < expected.Count; i++)
                {
                    Acl expectedAcl = expected[i];
                    Acl actualAcl = actual[i];
                    Assert.AreEqual(expectedAcl.Id.Scheme, actualAcl.Id.Scheme);
                    Assert.AreEqual(expectedAcl.Id.Identifier, actualAcl.Id.Identifier);
                    Assert.AreEqual(expectedAcl.Perms, actualAcl.Perms);
                }
            }
            else
            {
                Assert.IsNull(actual);
            }
        }

        /// <summary>
        /// Verify that the given <see cref="IWatcher"/>s are equal.
        /// </summary>
        /// <param name="expected">Expected <see cref="IWatcher"/></param>
        /// <param name="actual">Actual <see cref="IWatcher"/></param>
        private static void VerifyWatcher(IWatcher expected, IWatcher actual)
        {
            if (expected != null)
            {
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.Kind, actual.Kind);
            }
            else
            {
                Assert.IsNull(actual);
            }
        }

        /// <summary>
        /// Verify that the given <see cref="IStat"/>s are equal.
        /// </summary>
        /// <param name="expected">Expected <see cref="IStat"/></param>
        /// <param name="actual">Actual <see cref="IStat"/></param>
        private static void VerifyStat(IStat expected, IStat actual)
        {
            if (expected != null)
            {
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Czxid, actual.Czxid);
                Assert.AreEqual(expected.Mzxid, actual.Mzxid);
                Assert.AreEqual(expected.Pzxid, actual.Pzxid);
                Assert.AreEqual(expected.Ctime, actual.Ctime);
                Assert.AreEqual(expected.Mtime, actual.Mtime);
                Assert.AreEqual(expected.Version, actual.Version);
                Assert.AreEqual(expected.Cversion, actual.Cversion);
                Assert.AreEqual(expected.Aversion, actual.Aversion);
                Assert.AreEqual(expected.DataLength, actual.DataLength);
                Assert.AreEqual(expected.NumChildren, actual.NumChildren);
            }
            else
            {
                Assert.IsNull(actual);
            }
        }

        /// <summary>
        /// Verify that the given <see cref="IRingMasterRequest"/> lists are equal.
        /// </summary>
        /// <param name="expected">Expected <see cref="IRingMasterRequest"/> list</param>
        /// <param name="actual">Actual <see cref="IRingMasterRequest"/> list</param>
        private static void VerifyRequests(IReadOnlyList<IRingMasterRequest> expected, IReadOnlyList<IRingMasterRequest> actual)
        {
            if (expected != null)
            {
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.Count, actual.Count);
                for (int i = 0; i < expected.Count; i++)
                {
                    Assert.AreEqual(expected[i].RequestType, actual[i].RequestType);
                }
            }
            else
            {
                Assert.IsNull(actual);
            }
        }

        /// <summary>
        /// Serialize the given <paramref name="request"/> using protocol version <paramref name="serializerVersion"/>
        /// and then Deserialize it using protocol version <paramref name="deserializerVersion"/>.
        /// </summary>
        /// <param name="request">The request to serializer</param>
        /// <param name="protocolVersion">Version of communication protocol that must be used to serialize and deserialize</param>
        /// <returns>The deserialized request</returns>
        private static RequestCall SerializeAndDeserialize(RequestCall request, uint protocolVersion)
        {
            var serializer = new Serializer(protocolVersion);
            serializer.SerializeRequest(request);

            byte[] serializedBytes = serializer.GetBytes();

            var deserializer = new Deserializer(serializedBytes, serializedBytes.Length, protocolVersion);

            return deserializer.DeserializeRequest();
        }

        /// <summary>
        /// A simple watcher implementation only used for testing serialization/deserialization.
        /// </summary>
        private class Watcher : IWatcher
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Watcher"/> class.
            /// </summary>
            /// <param name="id">Id of the watcher</param>
            /// <param name="kind">Kind of the watcher</param>
            public Watcher(ulong id, WatcherKind kind)
            {
                this.Id = id;
                this.Kind = kind;
            }

            /// <summary>
            /// Gets the unique id of this watcher.
            /// </summary>
            public ulong Id { get; private set; }

            /// <summary>
            /// Gets a value indicating whether the watcher is for a single use only.
            /// </summary>
            public bool OneUse => this.Kind.HasFlag(WatcherKind.OneUse);

            /// <summary>
            /// Gets the kind of the watcher, if it is for single use and if the data is included on notification
            /// </summary>
            public WatcherKind Kind { get; private set; }

            /// <summary>
            /// Processes the specified event.
            /// </summary>
            /// <param name="evt">Event to process</param>
            public void Process(WatchedEvent evt)
            {
            }
        }
    }
}
