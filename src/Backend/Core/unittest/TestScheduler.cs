// <copyright file="TestScheduler.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterBackendCoreUnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Backend;
    using Backend.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Requests;

    /// <summary>
    /// Tests that verify scheduler functionality.
    /// </summary>
    [TestClass]
    public sealed class TestScheduler
    {
        internal enum EventNames
        {
            Close,
        }

        // [TestMethod]
        public void ValidateScheduler()
        {
            bool ok = false;

            TestEvents ev = new TestEvents();
            TestMarshaller marshaller = new TestMarshaller(ev);
            TestRingMaster testRM = new TestRingMaster(ev, marshaller);
            ScheduledCommand scheduler = null;
            try
            {
                foreach (string path in ScheduledCommand.GetPaths())
                {
                    testRM.Create(path, null, null, CreateMode.PersistentAllowPathCreation);
                }

                scheduler = new ScheduledCommand(() => { return testRM.IsPrimary; }, testRM, marshaller);
                scheduler.InternalOnAbandon = new Action<Exception>(ex =>
                {
                    Assert.Fail("Scheduler Abandoned: " + ex);
                });

                scheduler.Start();

                this.VerifySetup(ev, TimeSpan.FromSeconds(15));

                string res = testRM.Create("/$metadata/scheduler/commands/c1", null, null, CreateMode.Persistent);

                this.VerifyCommandTaken(ev, TimeSpan.FromSeconds(10), "c1");
                this.VerifyCommandFailed(ev, TimeSpan.FromSeconds(10), "c1");

                List<Op> ops = new List<Op>();
                ops.Add(Op.Create("/$metadata/scheduler/commands/c2", null, null, CreateMode.Persistent));
                ops.Add(Op.Create("/$metadata/scheduler/commands/c4", null, null, CreateMode.Persistent));
                ops.Add(Op.Create("/$metadata/scheduler/commands/c3", null, null, CreateMode.Persistent));

                IReadOnlyList<OpResult> resL = testRM.Multi(ops.AsReadOnly(), completeSynchronously: true, scheduledName: null);

                this.VerifyCommandTaken(ev, TimeSpan.FromSeconds(10), "c2");
                this.VerifyCommandTaken(ev, TimeSpan.FromSeconds(10), "c3");
                this.VerifyCommandTaken(ev, TimeSpan.FromSeconds(10), "c4");
                this.VerifyCommandFailed(ev, TimeSpan.FromSeconds(10), "c2", "c3", "c4");

                ops.Clear();
                ops.Add(Op.Create("/test1", null, null, CreateMode.Persistent));
                resL = testRM.Multi(ops.AsReadOnly(), completeSynchronously: true, scheduledName: "c5");

                this.VerifyCommandTaken(ev, TimeSpan.FromSeconds(10), "c5");
                this.VerifyCommandSucceeded(ev, TimeSpan.FromSeconds(10), "c5");
                this.VerifyCreated(ev, TimeSpan.FromSeconds(10), "/test1");

                Assert.IsTrue(testRM.Exists("/test1", false, false) != null);
                Assert.IsTrue(testRM.Exists("/$metadata/scheduler/commands/c5", false, false) == null);

                ok = true;
            }
            finally
            {
                if (!ok)
                {
                    testRM.DumpNodes();
                }

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    if (scheduler != null)
                    {
                        scheduler.Close();
                    }

                    testRM.Close();
                });
            }
        }

        private void VerifyCommandTaken(TestEvents ev, TimeSpan timeout, string command)
        {
            ev.AssertOccursInOrder(
                "Timeout (10s) waiting for scheduler to collect data for the new command",
                false,
                timeout,
                "Request[Type:Create, Path:/$metadata/scheduler/commands]",
                "Request[Type:Create, Path:/$metadata/scheduler/failures]",
                "Request[Type:Create, Path:/$metadata/scheduler/inflight]",
                "Request[Type:GetChildren, Path:/$metadata/scheduler/commands]",
                "<contains>Request[Type:Create, Path:/$metadata/scheduler/commands/" + command + "]",
                "WatchedEvent[Class:WatchedEvent,Path=/$metadata/scheduler/commands,EventType=NodeChildrenChanged,KeeperState=SyncConnected]",
                "Request[Type:GetChildren, Path:/$metadata/scheduler/commands]",
                "Request[Type:GetData, Path:/$metadata/scheduler/commands/" + command + "]",
                "<contains>Request[Type:Move, Path:/$metadata/scheduler/commands/" + command + ", PathDst:/$metadata/scheduler/");
        }

        private void VerifyCreated(TestEvents ev, TimeSpan timeout, string path)
        {
            ev.AssertOccursInOrder(
                "Timeout (10s) waiting for path to be created",
                false,
                TimeSpan.FromSeconds(10),
                "<contains>Request[Type:Create, Path:" + path + "]");
        }

        private void VerifyCommandSucceeded(TestEvents ev, TimeSpan timeout, string command)
        {
            ev.AssertOccursInOrder(
                "Timeout (10s) waiting for scheduler to complete successfully the new command",
                false,
                TimeSpan.FromSeconds(10),
                "Request[Type:Create, Path:/$metadata/scheduler/commands]",
                "Request[Type:Create, Path:/$metadata/scheduler/failures]",
                "Request[Type:Create, Path:/$metadata/scheduler/inflight]",
                "Request[Type:GetChildren, Path:/$metadata/scheduler/commands]",
                "<contains>Request[Type:Create, Path:/$metadata/scheduler/commands/" + command + "]",
                "WatchedEvent[Class:WatchedEvent,Path=/$metadata/scheduler/commands,EventType=NodeChildrenChanged,KeeperState=SyncConnected]",
                "Request[Type:GetChildren, Path:/$metadata/scheduler/commands]",
                "Request[Type:GetData, Path:/$metadata/scheduler/commands/" + command + "]",
                "<contains>Request[Type:Move, Path:/$metadata/scheduler/commands/" + command + ", PathDst:/$metadata/scheduler/inflight]",
                "<contains>Request[Type:Delete, Path:/$metadata/scheduler/inflight/" + command + "]");
        }

        private void VerifyCommandFailed(TestEvents ev, TimeSpan timeout, params string[] commands)
        {
            List<string> expected = new List<string>();

            expected.Add("Request[Type:Create, Path:/$metadata/scheduler/commands]");
            expected.Add("Request[Type:Create, Path:/$metadata/scheduler/failures]");
            expected.Add("Request[Type:Create, Path:/$metadata/scheduler/inflight]");
            expected.Add("Request[Type:GetChildren, Path:/$metadata/scheduler/commands]");
            foreach (string command in commands)
            {
                expected.Add("<contains>Request[Type:Create, Path:/$metadata/scheduler/commands/" + command + "]");
            }

            expected.Add("WatchedEvent[Class:WatchedEvent,Path=/$metadata/scheduler/commands,EventType=NodeChildrenChanged,KeeperState=SyncConnected]");
            expected.Add("Request[Type:GetChildren, Path:/$metadata/scheduler/commands]");
            foreach (string command in commands)
            {
                expected.Add("Request[Type:GetData, Path:/$metadata/scheduler/commands/" + command + "]");
                expected.Add("Request[Type:Multi, Requests:Request[Type:Create, Path:/$metadata/scheduler/failures/" + command + "];Request[Type:Move, Path:/$metadata/scheduler/commands/" + command + ", PathDst:/$metadata/scheduler/failures/" + command + "];Request[Type:Create, Path:/$metadata/scheduler/failures/" + command + "/ResultData]]");
            }

            string[] expectedArray = expected.ToArray();

            ev.AssertOccursInOrder(
                "Timeout (10s) waiting for scheduler to close command as failed",
                false,
                TimeSpan.FromSeconds(10),
                expectedArray);
        }

        private void VerifySetup(TestEvents ev, TimeSpan timeout)
        {
            ev.AssertOccursInOrder(
                "Timeout (10s) waiting for scheduler to learn about baseline commands",
                false,
                TimeSpan.FromSeconds(10),
                "Request[Type:Create, Path:/$metadata/scheduler/commands]",
                "Request[Type:Create, Path:/$metadata/scheduler/failures]",
                "Request[Type:GetChildren, Path:/$metadata/scheduler/commands]");
        }

        internal class TestEvents
        {
            private List<object> events = new List<object>();

            public int Count
            {
                get
                {
                    lock (this.events)
                    {
                        return this.events.Count;
                    }
                }
            }

            public void Clear()
            {
                lock (this.events)
                {
                    this.events.Clear();
                }
            }

            public void PushEvent(object op)
            {
                lock (this.events)
                {
                    this.events.Add(op);
                }
            }

            public void AssertOccursInOrder(string message, bool inPack, TimeSpan timeout, params object[] sequence)
            {
                if (!this.OccursInOrder(inPack, timeout, sequence))
                {
                    Assert.Fail("AssertOccursInOrder: " + message + "\nExpected:\n" + string.Join(Environment.NewLine, sequence) + "\nFound:\n" + string.Join(Environment.NewLine, this.events));
                }
            }

            public bool OccursInOrder(bool inPack, TimeSpan timeout, params object[] sequence)
            {
                Stopwatch sw = Stopwatch.StartNew();

                while (true)
                {
                    if (this.OccursInOrder(inPack, sequence))
                    {
                        break;
                    }

                    if (sw.ElapsedMilliseconds > timeout.TotalMilliseconds)
                    {
                        return false;
                    }

                    Thread.Sleep(500);
                }

                return true;
            }

            public bool OccursInOrder(bool inPack, params object[] sequence)
            {
                if (sequence == null || sequence.Length == 0)
                {
                    return true;
                }

                lock (this.events)
                {
                    int i = 0;

                    for (int j = 0; j < this.events.Count;)
                    {
                        if (i == sequence.Length)
                        {
                            break;
                        }

                        string s_i = sequence[i] as string;
                        string s_j = this.events[j] as string;

                        if (s_j != null && s_i != null && s_i.StartsWith("<contains>"))
                        {
                            s_i = s_i.Substring("<contains>".Length);

                            if (s_j.Contains(s_i))
                            {
                                i++;

                                // note we don't increment j here
                                continue;
                            }
                        }
                        else if (this.AreEqual(this.events[j], sequence[i]))
                        {
                            i++;
                            j++;
                            continue;
                        }

                        if (inPack && i > 0)
                        {
                            return false;
                        }

                        j++;
                    }

                    return i == sequence.Length;
                }
            }

            protected virtual bool AreEqual(object v1, object v2)
            {
                if (v1 == v2)
                {
                    return true;
                }

                if (v1 != null)
                {
                    return v1.Equals(v2);
                }

                return false;
            }
        }

        private class TestNode
        {
            public TestNode(string path)
            {
                this.Path = path;
                this.Children = new List<string>();
            }

            internal string Path { get; private set; }

            internal IReadOnlyList<string> Children { get; private set; }

            internal byte[] Data { get; set; }

            internal IStat Stat
            {
                get
                {
                    return new FirstStat(1, 1, this.Data == null ? 0 : this.Data.Length);
                }
            }

            public void AddChild(string child)
            {
                List<string> children = (List<string>)this.Children;

                if (children.Contains(child))
                {
                    throw new ArgumentException("key already there " + child);
                }

                children.Add(child);
                children.Sort();
            }

            public bool RemoveChild(string child)
            {
                List<string> children = (List<string>)this.Children;

                return children.Remove(child);
            }
        }

        private class TestRingMaster : AbstractRingMaster
        {
            private TestEvents ev;
            private TestMarshaller marshaller;
            private Dictionary<string, TestNode> dataByPath = new Dictionary<string, TestNode>();
            private Dictionary<string, IWatcher> watchers = new Dictionary<string, IWatcher>();

            public TestRingMaster(TestEvents ev, TestMarshaller marshaller)
                : base(string.Empty, 10, null)
            {
                this.ev = ev;
                this.marshaller = marshaller;
            }

            public bool IsPrimary
            {
                get; set;
            }

            public override int RequestTimeout
            {
                get
                {
                    return 10;
                }
            }

            public override int SessionTimeout
            {
                get
                {
                    return 10;
                }
            }

            public override void Close()
            {
                this.ev.PushEvent(EventNames.Close);
            }

            public override ISetDataOperationHelper GetSetDataOperationHelper()
            {
                throw new NotImplementedException();
            }

            public override void Initialize(int sessionTimeout, int requestTimeout)
            {
            }

            public override void Send(IRingMasterBackendRequest req)
            {
                if (req == null)
                {
                    return;
                }

                List<Action> actions = new List<Action>();

                RequestResponse resp;

                if (req.RequestType == RingMasterRequestType.Multi && ((Backend.RequestMulti)req).ScheduledName != null)
                {
                    string scheduledName = ((Backend.RequestMulti)req).ScheduledName;
                    ((Backend.RequestMulti)req).ScheduledName = null;

                    Requests.RequestCreate crReq = new Requests.RequestCreate("/$metadata/scheduler/commands/" + scheduledName, ScheduledCommand.GetBytes(req, this.marshaller), null, CreateMode.Persistent);
                    RequestResponse aux = this.ProcessT(crReq, actions);

                    this.ev.PushEvent(this.ToString(crReq));

                    resp = new RequestResponse()
                    {
                        CallId = 0,
                        Content = new List<OpResult>() { OpResult.GetOpResult(RingMasterRequestType.Create, aux) }.AsReadOnly(),
                        ResponsePath = string.Empty,
                        Stat = null,
                        ResultCode = aux.ResultCode
                    };
                }
                else
                {
                    resp = this.Process(req.WrappedRequest, actions);
                    this.ev.PushEvent(this.ToString(req.WrappedRequest));
                }

                foreach (Action action in actions)
                {
                    action();
                }

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    req.NotifyComplete(resp.ResultCode, resp.Content, resp.Stat, resp.ResponsePath);
                });
            }

            internal void DumpNodes()
            {
                Console.WriteLine("Nodes:\n" + string.Join(Environment.NewLine, this.dataByPath.Keys));
            }

            protected override void OnComplete(IRingMasterBackendRequest req, int resultcode, double timeInMillis)
            {
            }

            private RequestResponse Process(IRingMasterRequest req, List<Action> actions)
            {
                IRingMasterBackendRequest breq = req as IRingMasterBackendRequest;

                if (breq != null)
                {
                    req = breq.WrappedRequest;
                }

                switch (req.RequestType)
                {
                    case RingMasterRequestType.Check:
                        return this.ProcessT((Requests.RequestCheck)req, actions);
                    case RingMasterRequestType.Create:
                        return this.ProcessT((Requests.RequestCreate)req, actions);
                    case RingMasterRequestType.GetChildren:
                        return this.ProcessT((Requests.RequestGetChildren)req, actions);
                    case RingMasterRequestType.GetData:
                        return this.ProcessT((Requests.RequestGetData)req, actions);
                    case RingMasterRequestType.Delete:
                        return this.ProcessT((Requests.RequestDelete)req, actions);
                    case RingMasterRequestType.Exists:
                        return this.ProcessT((Requests.RequestExists)req, actions);
                    case RingMasterRequestType.Multi:
                        return this.ProcessT((Requests.RequestMulti)req, actions);
                    case RingMasterRequestType.Move:
                        return this.ProcessT((Requests.RequestMove)req, actions);
                }

                return new RequestResponse()
                {
                    ResultCode = (int)RingMasterException.Code.Unimplemented
                };
            }

            private RequestResponse ProcessT(Requests.RequestGetChildren req, List<Action> actions)
            {
                TestNode node;

                if (this.dataByPath.TryGetValue(req.Path, out node))
                {
                    actions.Add(new Action(() =>
                    {
                        this.TriggerWatcher(req.Path, req.Watcher);
                    }));

                    IReadOnlyList<string> children = new List<string>(node.Children).AsReadOnly();
                    this.ev.PushEvent("-->GetChildren(" + req.Path + ")={" + string.Join(",", children) + "}");
                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Ok,
                        ResponsePath = req.Path,
                        Content = children,
                        Stat = node.Stat,
                    };
                }

                return new RequestResponse()
                {
                    ResultCode = (int)RingMasterException.Code.Nonode,
                    ResponsePath = req.Path,
                    Content = null,
                    Stat = null,
                };
            }

            private RequestResponse ProcessT(Requests.RequestCheck req, List<Action> actions)
            {
                return new RequestResponse()
                {
                    ResultCode = (int)RingMasterException.Code.Ok,
                    ResponsePath = req.Path,
                    Content = null,
                    Stat = null,
                };
            }

            private RequestResponse ProcessT(Requests.RequestCreate req, List<Action> actions)
            {
                string parent = req.Path.Substring(0, req.Path.LastIndexOf('/'));

                if (parent.Length == 0)
                {
                    parent = "/";
                }

                TestNode newNode = new TestNode(req.Path);
                newNode.Data = req.Data;
                TestNode parentNode;

                if (!this.dataByPath.TryGetValue(parent, out parentNode))
                {
                    if (req.CreateMode == CreateMode.PersistentAllowPathCreation || req.CreateMode == CreateMode.PersistentSequentialAllowPathCreation)
                    {
                        parentNode = new TestNode(parent);
                        this.dataByPath[parent] = parentNode;

                        string pa = parent;
                        TestNode ch = parentNode;

                        while (pa != "/")
                        {
                            string name = pa.Substring(pa.LastIndexOf('/') + 1);

                            pa = pa.Substring(0, pa.LastIndexOf('/'));

                            if (pa.Length == 0)
                            {
                                pa = "/";
                            }

                            if (this.dataByPath.TryGetValue(pa, out ch))
                            {
                                break;
                            }

                            ch = new TestNode(pa);

                            ch.AddChild(name);
                            this.dataByPath[pa] = ch;
                        }
                    }
                    else
                    {
                        return new RequestResponse()
                        {
                            ResultCode = (int)RingMasterException.Code.Nonode,
                            ResponsePath = req.Path,
                            Content = null,
                            Stat = null,
                        };
                    }
                }

                parentNode.AddChild(req.Path.Substring(req.Path.LastIndexOf('/') + 1));
                this.dataByPath[req.Path] = newNode;

                actions.Add(new Action(() =>
                {
                    this.TriggerWatcher(parent, null, WatchedEvent.WatchedEventType.NodeChildrenChanged);
                }));

                return new RequestResponse()
                {
                    ResultCode = (int)RingMasterException.Code.Ok,
                    ResponsePath = req.Path,
                    Content = req.Path,
                    Stat = newNode.Stat
                };
            }

            private RequestResponse ProcessT(Requests.RequestDelete req, List<Action> actions)
            {
                string parent = req.Path.Substring(0, req.Path.LastIndexOf('/'));

                TestNode parentNode;
                TestNode node;

                if (!this.dataByPath.TryGetValue(req.Path, out node) ||
                    !this.dataByPath.TryGetValue(parent, out parentNode))
                {
                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Nonode,
                        ResponsePath = req.Path,
                        Content = null,
                        Stat = null,
                    };
                }

                parentNode.RemoveChild(req.Path.Substring(req.Path.LastIndexOf('/') + 1));
                this.dataByPath.Remove(req.Path);

                actions.Add(new Action(() =>
                {
                    this.TriggerWatcher(parent, null, WatchedEvent.WatchedEventType.NodeChildrenChanged);
                }));

                return new RequestResponse()
                {
                    ResultCode = (int)RingMasterException.Code.Ok,
                    ResponsePath = req.Path,
                    Content = req.Path,
                    Stat = parentNode.Stat
                };
            }

            private RequestResponse ProcessT(Requests.RequestMove req, List<Action> actions)
            {
                TestNode srcNode;

                string srcParentPath = req.Path.Substring(0, req.Path.LastIndexOf('/'));
                string srcNodename = req.Path.Substring(req.Path.LastIndexOf('/') + 1);

                if (!this.dataByPath.TryGetValue(srcParentPath, out srcNode))
                {
                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Nonode,
                        ResponsePath = req.Path,
                        Content = null,
                        Stat = null,
                    };
                }

                TestNode dstParentNode;

                if (!this.dataByPath.TryGetValue(req.PathDst, out dstParentNode))
                {
                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Nonode,
                        ResponsePath = req.PathDst,
                        Content = null,
                        Stat = null,
                    };
                }

                string dstParentPath = req.PathDst + "/" + srcNodename;
                srcNode.RemoveChild(srcNodename);
                dstParentNode.AddChild(srcNodename);
                this.dataByPath.Remove(req.Path);
                this.dataByPath[dstParentPath] = srcNode;

                actions.Add(new Action(() =>
                {
                    this.TriggerWatcher(srcParentPath, null, WatchedEvent.WatchedEventType.NodeChildrenChanged);
                }));

                actions.Add(new Action(() =>
                {
                    this.TriggerWatcher(dstParentPath, null, WatchedEvent.WatchedEventType.NodeChildrenChanged);
                }));

                return new RequestResponse()
                {
                    ResultCode = (int)RingMasterException.Code.Ok,
                    ResponsePath = req.PathDst,
                    Content = req.PathDst,
                    Stat = srcNode.Stat,
                };
            }

            private RequestResponse ProcessT(Requests.RequestExists req, List<Action> actions)
            {
                TestNode node;

                if (!this.dataByPath.TryGetValue(req.Path, out node))
                {
                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Nonode,
                        ResponsePath = req.Path,
                        Content = null,
                        Stat = null,
                    };
                }

                actions.Add(new Action(() =>
                {
                    this.TriggerWatcher(req.Path, req.Watcher);
                }));

                return new RequestResponse()
                {
                    ResultCode = (int)RingMasterException.Code.Ok,
                    ResponsePath = req.Path,
                    Content = node.Stat,
                    Stat = node.Stat,
                };
            }

            private RequestResponse ProcessT(Requests.RequestGetData req, List<Action> actions)
            {
                TestNode node;

                if (!this.dataByPath.TryGetValue(req.Path, out node))
                {
                    return new RequestResponse()
                    {
                        ResultCode = (int)RingMasterException.Code.Nonode,
                        ResponsePath = req.Path,
                        Content = null,
                        Stat = null,
                    };
                }

                actions.Add(new Action(() =>
                {
                    this.TriggerWatcher(req.Path, req.Watcher);
                }));

                return new RequestResponse()
                {
                    ResultCode = (int)RingMasterException.Code.Ok,
                    ResponsePath = req.Path,
                    Content = node.Data,
                    Stat = node.Stat,
                };
            }

            private RequestResponse ProcessT(Requests.RequestMulti req, List<Action> actions)
            {
                List<OpResult> results = new List<OpResult>();

                foreach (IRingMasterRequest r in req.Requests)
                {
                    RequestResponse res = this.Process(r, actions);
                    results.Add(OpResult.GetOpResult(r.RequestType, res));

                    if (res.ResultCode != (int)RingMasterException.Code.Ok)
                    {
                        Assert.Fail("some multi operation failed");
                    }
                }

                return new RequestResponse()
                {
                    ResultCode = (int)RingMasterException.Code.Ok,
                    ResponsePath = req.Path,
                    Content = results,
                    Stat = null,
                };
            }

            private void TriggerWatcher(string path, IWatcher newWatcher, WatchedEvent.WatchedEventType type = WatchedEvent.WatchedEventType.WatcherRemoved)
            {
                IWatcher watcher;

                if (this.watchers.TryGetValue(path, out watcher))
                {
                    WatchedEvent evt = new WatchedEvent(type, WatchedEvent.WatchedEventKeeperState.SyncConnected, path);

                    this.ev.PushEvent(this.ToString(evt));

                    if (watcher.Kind.HasFlag(WatcherKind.OneUse))
                    {
                        this.watchers.Remove(path);
                    }

                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        watcher.Process(evt);
                    });
                }

                if (newWatcher != null)
                {
                    this.watchers[path] = newWatcher;
                }
            }

            private string ToString(WatchedEvent evt)
            {
                return string.Format("WatchedEvent[Class:{0},Path={1},EventType={2},KeeperState={3}]", evt.GetType().Name, evt.Path, evt.EventType, evt.KeeperState);
            }

            private string ToString(IRingMasterRequest req)
            {
                IRingMasterBackendRequest breq = req as IRingMasterBackendRequest;

                if (breq != null)
                {
                    req = breq.WrappedRequest;
                }

                if (req.RequestType == RingMasterRequestType.Multi)
                {
                    return string.Format("Request[Type:{0}, Requests:{1}]", req.RequestType, string.Join(";", ((Requests.RequestMulti)req).Requests.Select(c => this.ToString(c)).ToArray()));
                }

                if (req.RequestType == RingMasterRequestType.Move)
                {
                    return string.Format("Request[Type:{0}, Path:{1}, PathDst:{2}]", req.RequestType, req.Path, ((Requests.RequestMove)req).PathDst);
                }

                return string.Format("Request[Type:{0}, Path:{1}]", req.RequestType, req.Path);
            }
        }

        private class TestMarshaller : Backend.IByteArrayMarshaller
        {
            private TestEvents ev;

            private Dictionary<byte[], object> objectsByBytes = new Dictionary<byte[], object>();

            public TestMarshaller(TestEvents ev)
            {
                this.ev = ev;
            }

            public Backend.RequestCall DeserializeRequestFromBytes(byte[] requestBytes)
            {
                if (requestBytes == null)
                {
                    return null;
                }

                return (Backend.RequestCall)this.objectsByBytes[requestBytes];
            }

            public RequestResponse DeserializeResponseFromBytes(byte[] responseBytes)
            {
                if (responseBytes == null)
                {
                    return null;
                }

                return (RequestResponse)this.objectsByBytes[responseBytes];
            }

            public byte[] SerializeRequestAsBytes(Backend.RequestCall request)
            {
                if (request == null)
                {
                    return null;
                }

                byte[] resp = new byte[0];

                this.objectsByBytes[resp] = request;

                return resp;
            }

            public byte[] SerializeResponseAsBytes(RequestResponse response)
            {
                if (response == null)
                {
                    return null;
                }

                byte[] resp = new byte[0];

                this.objectsByBytes[resp] = response;

                return resp;
            }
        }
    }
}