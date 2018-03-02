// <copyright file="BasicStateMachine.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RSLValidation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading;
    using ManagedRSLib;
    using VisualStudio.TestTools.UnitTesting;

    public class BasicStateMachine : ManagedRSLStateMachine
    {
        private static System.Text.Encoding encoding = new System.Text.UTF8Encoding();

        private List<Command> commands = new List<Command>();
        private Queue<Command> inflight = new Queue<Command>();
        private object replicationLock = new object();
        private bool needsUnloading = false;
        private ManualResetEvent isBootStrapped = new ManualResetEvent(true);

        public BasicStateMachine(TestTracer tracer, ushort port, ushort learnport)
        {
            this.Tracer = tracer;

            this.SelfNode = CreateNode(port, learnport);

            this.Log(TestTracer.EventType.CreateStateMachine, "starting SM in AppDomain " + AppDomain.CurrentDomain.FriendlyName);

            this.SMReference = new Reference(this);
        }

        public Reference SMReference { get; set; }

        public bool IsPrimary { get; private set; }

        public ManagedRSLNode SelfNode { get; private set; }

        public bool CanBP { get; set; }

        internal TestTracer Tracer { get; set; }

        public static ManagedRSLNode CreateNode(ushort port, ushort learnport)
        {
            ManagedRSLNode node = new ManagedRSLNode();

            try
            {
                node.Ip = IPAddress.Loopback;
                node.HostName = node.Ip.ToString();
                node.MemberId = "instance_" + port;
                node.RslPort = port;
                node.RslLearnPort = learnport;
            }
            catch (Exception)
            {
                if (node != null)
                {
                    node.Dispose();
                }

                throw;
            }

            return node;
        }

        public override void AbortChangeConfiguration(RSLResponse status, object gc_cookie)
        {
            this.UnloadAsynchronously("AbortChangeConfiguration");
        }

        public override void AbortRequest(RSLResponse status, object gc_cookie)
        {
            this.UnloadAsynchronously("AbortRequest");
        }

        public override void ExecuteFastReadRequest(byte[] gc_request, object gc_cookie)
        {
            this.UnloadAsynchronously("ExecuteFastReadRequest");
        }

        public override void NotifyConfigurationChanged(object gc_cookie)
        {
            this.Log(TestTracer.EventType.NotifyConfigurationChangedCalled, " NotifyConfigurationChanged was called. cookie: {0}", gc_cookie);
        }

        public override void NotifyPrimaryRecovered()
        {
            this.Log(TestTracer.EventType.NotifyPrimaryRecoveredCalled, " NotifyPrimaryRecovered was called");
        }

        public override void StateCopied(ulong seqNo, string fileName, object gc_cookie)
        {
            this.Log(TestTracer.EventType.StateCopiedCalled, " StateCopied was called: seqNo = {0}", seqNo);
        }

        public override void StateSaved(ulong seqNo, string fileName)
        {
            this.Log(TestTracer.EventType.StateSavedCalled, " StateSaved was called: seqNo = {0}", seqNo);
        }

        public override void ShutDown(RSLResponse status)
        {
            this.Log(TestTracer.EventType.ShutDownCalled, " was asked ShutDown");
        }

        public override bool AcceptMessageFromReplica(ManagedRSLNode node, byte[] data)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            this.Log(TestTracer.EventType.AcceptMessageFromReplicaCalled, " AcceptMessageFromReplica was called: node = {0}", node.MemberId);
            return true;
        }

        public override bool CanBecomePrimary(ref byte[] data)
        {
            bool canBP = this.CanBP;
            this.Log(TestTracer.EventType.CanBecomePrimaryCalled, " was asked CanBP? --> {0} ({1}/{2})", canBP, this.GetCurrentSequenceNumber(), this.GetHighestPassedSequenceNumber());

            return canBP;
        }

        public override void NotifyStatus(bool isPrimary)
        {
            this.Log(isPrimary ? TestTracer.EventType.PrimaryCommandExecuted : TestTracer.EventType.NotifyStatusSecondaryCalled, " we are {0}", isPrimary ? "Primary" : "Secondary");

            if (isPrimary)
            {
                this.ReplicateCommand(new PrimaryCommand(() =>
                {
                    this.Log(TestTracer.EventType.PrimaryCommandExecuted, string.Empty);
                    this.IsPrimary = isPrimary;
                }));
            }
            else
            {
                this.IsPrimary = isPrimary;
            }
        }

        public override void ExecuteReplicatedRequest(byte[] gc_request, object gc_cookie, ref bool saveState)
        {
            this.Log(TestTracer.EventType.ExecuteReplicatedRequestCalled, " ExRR is invoked. {0} {1}", gc_request, gc_cookie);

            Command cmd;

            if (gc_request == null)
            {
                throw new ArgumentNullException("gc_request");
            }

            MemoryStream ms = new MemoryStream(gc_request);
            try
            {
                using (BinaryReader br = new BinaryReader(ms, encoding))
                {
                    ms = null;
                    cmd = Command.Deserialize(br);

                    if (cmd == null)
                    {
                        Assert.Fail("command not deserialized!");
                    }
                }
            }
            finally
            {
                if (ms != null)
                {
                    ms.Dispose();
                }
            }

            bool fromMyself = false;

            if (gc_cookie != null)
            {
                fromMyself = true;

                Command cmd2 = (Command)gc_cookie;

                if (!cmd.AreSame(cmd2))
                {
                    Assert.Fail("commands don't match");
                }

                // use the cookie instead of the deserialization
                cmd = cmd2;
            }

            if (fromMyself)
            {
                lock (this.replicationLock)
                {
                    cmd.ExecuteInPrimary(this, ref saveState);
                }
            }
            else
            {
                if (this.IsPrimary)
                {
                    Debugger.Launch();
                    throw new InvalidOperationException("a primary should never be executing commands from another replica");
                }

                // no lock needed, since there is no race condition here.
                cmd.ExecuteInSecondary(this, ref saveState);
            }
        }

        public override bool LoadState(ManagedRSLCheckpointStream gc_reader)
        {
            this.Log(TestTracer.EventType.LoadStateCalled, " LoadState was called: gc_reader = {0}", gc_reader);

            // if we are here, that is because Initialize was called.
            this.needsUnloading = true;

            if (gc_reader == null)
            {
                return true;
            }

            try
            {
                this.commands.Clear();

                using (BinaryReader br = new BinaryReader(gc_reader, encoding, true))
                {
                    while (true)
                    {
                        Command cmd = Command.Deserialize(br);

                        if (cmd == null)
                        {
                            break;
                        }

                        bool ignore = false;
                        cmd.ExecuteInSecondary(this, ref ignore);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return false;
        }

        public override void SaveState(ManagedRSLCheckpointStream gc_writer)
        {
            this.Log(TestTracer.EventType.SaveStateCalled, " SaveState was called: gc_writer = {0}", gc_writer);

            if (gc_writer == null)
            {
                throw new ArgumentNullException("gc_writer");
            }

            using (BinaryWriter bw = new BinaryWriter(gc_writer, encoding, true))
            {
                foreach (Command cmd in this.commands)
                {
                    cmd.Serialize(bw);
                }
            }
        }

        public void Log(string format, params object[] args)
        {
            this.Log(TestTracer.EventType.Generic, format, args);
        }

        public void Log(TestTracer.EventType evt, string format, params object[] args)
        {
            TestTracer tr = this.Tracer;

            if (tr == null)
            {
                Console.WriteLine("replica {0} {1} {2}", this.SelfNode.MemberId, evt, string.Format(format, args));
            }
            else
            {
                tr.OnEvent("replica " + this.SelfNode.MemberId, evt, string.Format(format, args));
            }
        }

        public void InitiateBootstrap(ManagedRSLNode[] nodes, int timeoutInSeconds, Action<RSLResponse> onComplete = null)
        {
            this.isBootStrapped.Reset();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                using (ManagedRSLMemberSet ms = new ManagedRSLMemberSet(nodes, new byte[] { 0 }, 0, 1))
                {
                    RSLResponse resp = this.Bootstrap(ms, timeoutInSeconds);

                    this.Log(TestTracer.EventType.BootstrapFinished, "bootstrap instance finished = " + resp);

                    this.isBootStrapped.Set();

                    if (onComplete != null)
                    {
                        onComplete(resp);
                    }
                }
            });
        }

        public void WaitForPrimary(int timeoutMillis)
        {
            Stopwatch w = Stopwatch.StartNew();

            while (!this.IsPrimary)
            {
                Thread.Sleep(Math.Min(100, timeoutMillis / 10));

                if (w.ElapsedMilliseconds > timeoutMillis)
                {
                    throw new TimeoutException("didn't find a primary in " + timeoutMillis + "ms");
                }
            }
        }

        public void ReplicateCommand(Command cmd, bool synchronous = false)
        {
            this.Log(TestTracer.EventType.ReplicateCommandCalled, " ReplicateCommand was called: cmd = {0}", cmd);

            if (cmd == null)
            {
                throw new ArgumentNullException(nameof(cmd));
            }

            ManualResetEvent e = null;

            if (synchronous)
            {
                e = new ManualResetEvent(false);

                cmd.OnCompletion = () =>
                {
                    e.Set();
                };
            }

            MemoryStream ms = new MemoryStream();
            try
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    ms = null;
                    cmd.Serialize(bw);
                    bw.BaseStream.Flush();

                    lock (this.replicationLock)
                    {
                        RSLResponse response = this.ReplicateRequest(((MemoryStream)bw.BaseStream).GetBuffer(), cmd);

                        if (response != RSLResponse.RSLSuccess)
                        {
                            throw new InvalidOperationException("cannot replicate command: " + response);
                        }

                        this.inflight.Enqueue(cmd);
                    }
                }
            }
            finally
            {
                if (ms != null)
                {
                    ms.Dispose();
                }
            }

            if (e != null)
            {
                e.WaitOne();
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (this.needsUnloading)
            {
                this.needsUnloading = false;
                this.Log(TestTracer.EventType.DisposeStateMachine, "stopping SM in AppDomain " + AppDomain.CurrentDomain.FriendlyName);
                this.isBootStrapped.WaitOne();
                this.UnloadThisOne();
                this.isBootStrapped.Dispose();
                this.isBootStrapped = null;
            }

            base.Dispose(isDisposing);
        }

        private void UnloadAsynchronously(string msg)
        {
            this.Log(TestTracer.EventType.UnloadAppDomain, msg);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                this.Log(TestTracer.EventType.UnloadAppDomain, "unloading now");
                AppDomain.Unload(AppDomain.CurrentDomain);
            });
        }

        [Serializable]
        public abstract class Command : Command.ICommandSerializer
        {
            [NonSerialized]
            private static ICommandSerializer[] serializers = InitializeSerializers(typeof(Command).Namespace);

            public interface ICommandSerializer
            {
                int CommandType { get; }

                Command DeserializeCommand(BinaryReader str);

                void SerializeCommand(BinaryWriter str);
            }

            public abstract int CommandType { get; }

            public Action OnCompletion { get; set; }

            public abstract Command DeserializeCommand(BinaryReader str);

            public abstract void SerializeCommand(BinaryWriter str);

            public abstract bool AreSame(Command obj);

            public virtual void ExecuteInPrimary(BasicStateMachine basicStateMachine, ref bool saveState)
            {
                if (basicStateMachine == null)
                {
                    throw new ArgumentNullException(nameof(basicStateMachine));
                }

                saveState = false;

                Command firstInqueue = basicStateMachine.inflight.Dequeue();

                if (firstInqueue != this)
                {
                    throw new ArgumentException("first in queue should be this command");
                }

                basicStateMachine.commands.Add(this);

                if (this.OnCompletion != null)
                {
                    this.OnCompletion();
                }
            }

            public virtual void ExecuteInSecondary(BasicStateMachine basicStateMachine, ref bool saveState)
            {
                if (basicStateMachine == null)
                {
                    throw new ArgumentNullException(nameof(basicStateMachine));
                }

                saveState = false;

                basicStateMachine.commands.Add(this);
            }

            internal static Command Deserialize(BinaryReader str)
            {
                int commandType = str.ReadByte();

                return serializers[commandType].DeserializeCommand(str);
            }

            internal void Serialize(BinaryWriter str)
            {
                str.BaseStream.WriteByte((byte)this.CommandType);
                this.SerializeCommand(str);
            }

            private static ICommandSerializer[] InitializeSerializers(string selectNamespace)
            {
                List<ICommandSerializer> list = new List<ICommandSerializer>();

                foreach (Type t in Assembly.GetExecutingAssembly().GetTypes())
                {
                    if (!t.Namespace.StartsWith(selectNamespace))
                    {
                        continue;
                    }

                    if (!t.IsAbstract && t.IsClass && typeof(ICommandSerializer).IsAssignableFrom(t))
                    {
                        ICommandSerializer obj = (ICommandSerializer)t.GetTypeInfo().GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance).First(c => c.GetParameters().Length == 0).Invoke(new object[0]);
                        list.Add(obj);
                    }
                }

                return InitializeSerializers(list.ToArray());
            }

            private static ICommandSerializer[] InitializeSerializers(params ICommandSerializer[] serializers)
            {
                int max = 0;
                foreach (ICommandSerializer s in serializers)
                {
                    if (s.CommandType > max)
                    {
                        max = s.CommandType;
                    }
                }

                ICommandSerializer[] res = new ICommandSerializer[max + 1];

                foreach (ICommandSerializer s in serializers)
                {
                    if (res[s.CommandType] != null && res[s.CommandType] != s)
                    {
                        throw new ArgumentException("two serializers with the same commandType!!");
                    }

                    res[s.CommandType] = s;
                }

                return res;
            }
        }

        [Serializable]
        public class CommandA : Command
        {
            public CommandA(int id)
            {
                this.Id = id;
            }

            private CommandA()
            {
            }

            public int Id { get; set; }

            public override int CommandType
            {
                get
                {
                    return 0;
                }
            }

            public override Command DeserializeCommand(BinaryReader str)
            {
                if (str == null)
                {
                    throw new ArgumentNullException("str");
                }

                CommandA c = new CommandA();
                c.Id = str.ReadInt32();

                return c;
            }

            public override void SerializeCommand(BinaryWriter str)
            {
                if (str == null)
                {
                    throw new ArgumentNullException("str");
                }

                str.Write((int)this.Id);
            }

            public override string ToString()
            {
                return string.Format("CommandA: " + this.Id);
            }

            public override bool AreSame(Command obj)
            {
                CommandA other = obj as CommandA;
                if (other == null)
                {
                    return false;
                }

                return other.Id == this.Id;
            }
        }

        [Serializable]
        public class CommandB : Command
        {
            public CommandB(string name)
            {
                this.Name = name;
            }

            private CommandB()
            {
            }

            public override int CommandType
            {
                get
                {
                    return 1;
                }
            }

            public string Name { get; set; }

            public override Command DeserializeCommand(BinaryReader str)
            {
                if (str == null)
                {
                    throw new ArgumentNullException("str");
                }

                CommandB c = new CommandB();
                bool isNull = str.ReadBoolean();
                if (isNull)
                {
                    c.Name = null;
                }
                else
                {
                    c.Name = str.ReadString();
                }

                return c;
            }

            public override void SerializeCommand(BinaryWriter str)
            {
                if (str == null)
                {
                    throw new ArgumentNullException("str");
                }

                string name = this.Name;
                bool isNull = name == null;

                str.Write((bool)isNull);

                if (!isNull)
                {
                    str.Write((string)name);
                }
            }

            public override string ToString()
            {
                return string.Format("CommandB: " + this.Name);
            }

            public override bool AreSame(Command obj)
            {
                CommandB other = obj as CommandB;
                if (other == null)
                {
                    return false;
                }

                return other.Name == this.Name;
            }
        }

        public class PrimaryCommand : Command
        {
            private int id;
            private Action action;

            public PrimaryCommand(Action action)
            {
                if (action == null)
                {
                    throw new ArgumentNullException(nameof(action));
                }

                this.action = action;
                this.id = action == null ? 0 : action.GetHashCode();
            }

            private PrimaryCommand()
            {
            }

            public override int CommandType
            {
                get
                {
                    return 3;
                }
            }

            public override Command DeserializeCommand(BinaryReader str)
            {
                if (str == null)
                {
                    throw new ArgumentNullException("str");
                }

                PrimaryCommand c = new PrimaryCommand();
                c.id = str.ReadInt32();

                return c;
            }

            public override void SerializeCommand(BinaryWriter str)
            {
                if (str == null)
                {
                    throw new ArgumentNullException("str");
                }

                str.Write((int)this.id);
            }

            public override string ToString()
            {
                return string.Format("PrimaryCommand: " + this.id);
            }

            public override bool AreSame(Command obj)
            {
                PrimaryCommand other = obj as PrimaryCommand;
                if (other == null)
                {
                    return false;
                }

                return other.id == this.id;
            }

            public override void ExecuteInPrimary(BasicStateMachine basicStateMachine, ref bool saveState)
            {
                if (basicStateMachine == null)
                {
                    throw new ArgumentNullException(nameof(basicStateMachine));
                }

                base.ExecuteInPrimary(basicStateMachine, ref saveState);

                this.action();
            }

            public override void ExecuteInSecondary(BasicStateMachine basicStateMachine, ref bool saveState)
            {
                base.ExecuteInSecondary(basicStateMachine, ref saveState);
            }
        }

        public class Reference : MarshalByRefObject
        {
            private BasicStateMachine sm;

            public Reference(BasicStateMachine sm)
            {
                this.sm = sm;
            }

            public bool CanBP
            {
                get
                {
                    return this.sm.CanBP;
                }

                set
                {
                    this.sm.CanBP = value;
                }
            }

            public bool IsPrimary
            {
                get
                {
                    return this.sm.IsPrimary;
                }
            }

            public string MemberId
            {
                get
                {
                    return this.sm.SelfNode.MemberId;
                }
            }

            public void ReplicateCommand(Command cmd, bool synchronous = false)
            {
                this.sm.ReplicateCommand(cmd, synchronous);
            }

            public void DisposeThisStateMachine()
            {
                this.sm.Dispose();
            }

            public void WaitForPrimary(int timeoutMillis)
            {
                this.sm.WaitForPrimary(timeoutMillis);
            }
        }
    }
}