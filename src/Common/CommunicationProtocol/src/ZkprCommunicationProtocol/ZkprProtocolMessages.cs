// <copyright file="ZkprProtocolMessages.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Data;
    using RingMaster.Requests;

    public enum ZooKeeperErrorCodes
    {
        ZOK = 0,
        ZSYSTEMERROR = -1,
        ZRUNTIMEINCONSISTENCY = -2,
        ZDATAINCONSISTENCY = -3,
        ZCONNECTIONLOSS = -4,
        ZMARSHALLINGERROR = -5,
        ZUNIMPLEMENTED = -6,
        ZOPERATIONTIMEOUT = -7,
        ZBADARGUMENTS = -8,
        ZINVALIDSTATE = -9,
        ZAPIERROR = -100,
        ZNONODE = -101,
        ZNOAUTH = -102,
        ZBADVERSION = -103,
        ZNOCHILDRENFOREPHEMERALS = -108,
        ZNODEEXISTS = -110,
        ZNOTEMPTY = -111,
        ZSESSIONEXPIRED = -112,
        ZINVALIDCALLBACK = -113,
        ZINVALIDACL = -114,
        ZAUTHFAILED = -115,
        ZCLOSING = -116,
        ZNOTHING = -117,
        ZSESSIONMOVED = -118,
        ZNEWCONFIGNOQUORUM = -120,
        ZRECONFIGINPROGRESS = -121,
        ZEPHEMERALONLOCALSESSION = -122,
    }

    public class ZkprProtocolMessages
    {
        [Flags]
        public enum Perms
        {
            Read = 1 << 0,
            Write = 1 << 1,
            Create = 1 << 2,
            Delete = 1 << 3,
            Admin = 1 << 4,
            All = Read | Write | Create | Delete | Admin
        }

        public enum CreateMode
        {
            Persistent = 0,
            Ephemeral = 1,
            PersistentSequential = 2,
            EphemeralSequential = 3,

            // When last child of container is deleted, the container becomes eligible for deletion by server without any notification to client. It may happen at any time in the future.
            // client should be prepared to get NoNodeException when creating children inside this
            Container = 4
        }

        public class CreateSession : IZooKeeperRequest
        {
            private CreateSession(int version, long lastZxidSeen, int timeout, long sessionId, string password, bool isNullPassword)
            {
                this.Version = version;
                this.LastZxidSeen = lastZxidSeen;
                this.Timeout = timeout;
                this.SessionId = sessionId;
                this.Password = password;
                this.IsNullPassword = isNullPassword;
            }

            public int Version { get; }

            public long LastZxidSeen { get; }

            public int Timeout { get; }

            public long SessionId { get; }

            public string Password { get; }

            public int Xid { get; }

            public bool IsNullPassword { get; }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.CreateSession;
                }
            }

            public static CreateSession CreateRequest(int version, long lastZxidSeen, int timeout, long sessionId, string password, bool isNullPassword)
            {
                return new CreateSession(version, lastZxidSeen, timeout, sessionId, password, isNullPassword);
            }
        }

        public class Exists : IZooKeeperRequest
        {
            private Exists(int xid, string path, bool watch)
            {
                this.Xid = xid;
                this.Path = path;
                this.Watch = watch;
            }

            public int Xid { get; }

            public string Path { get; }

            public bool Watch { get; }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.Exists;
                }
            }

            public static Exists CreateExists(int xid, string path, bool watch)
            {
                return new Exists(xid, path, watch);
            }
        }

        public class Create : IZooKeeperRequest
        {
            private Create(int xid, string path, byte[] data, IReadOnlyList<Acl> acls, int flags)
            {
                this.Xid = xid;
                this.Path = path;
                this.Data = data;
                this.Acls = acls;
                this.Flags = flags;
            }

            public int Xid { get; }

            public string Path { get; }

            public byte[] Data { get; }

            public IReadOnlyList<Acl> Acls { get; }

            public int Flags { get; }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.Create;
                }
            }

            public static Create CreateCreate(int xid, string path, byte[] data, IReadOnlyList<Acl> acls, int flags)
            {
                return new Create(xid, path, data, acls, flags);
            }
        }

        public class Create2 : IZooKeeperRequest
        {
            private Create2(int xid, string path, byte[] data, IReadOnlyList<Acl> acls, int flags)
            {
                this.Xid = xid;
                this.Path = path;
                this.Data = data;
                this.Acls = acls;
                this.Flags = flags;
            }

            public int Xid { get; }

            public string Path { get; }

            public byte[] Data { get; }

            public IReadOnlyList<Acl> Acls { get; }

            public int Flags { get; }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.Create2;
                }
            }

            public static Create2 CreateCreate2(int xid, string path, byte[] data, IReadOnlyList<Acl> acls, int flags)
            {
                return new Create2(xid, path, data, acls, flags);
            }
        }

        public class Delete : IZooKeeperRequest
        {
            private Delete(int xid, string path, int version)
            {
                this.Xid = xid;
                this.Path = path;
                this.Version = version;
            }

            public int Xid { get; }

            public string Path { get; }

            public int Version { get; }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.Delete;
                }
            }

            public static Delete CreateDelete(int xid, string path, int version)
            {
                return new Delete(xid, path, version);
            }
        }

        public class GetData : IZooKeeperRequest
        {
            private GetData(int xid, string path, bool watch)
            {
                this.Xid = xid;
                this.Path = path;
                this.Watch = watch;
            }

            public int Xid { get; }

            public string Path { get; }

            public bool Watch { get; }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.GetData;
                }
            }

            public static GetData CreateGetData(int xid, string path, bool watch)
            {
                return new GetData(xid, path, watch);
            }
        }

        public class GetChildren : IZooKeeperRequest
        {
            private GetChildren(int xid, string path, bool watch)
            {
                this.Xid = xid;
                this.Path = path;
                this.Watch = watch;
            }

            public int Xid { get; }

            public string Path { get; }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.GetChildren;
                }
            }

            public bool Watch { get; }

            public static GetChildren CreateGetChildren(int xid, string path, bool watch)
            {
                return new GetChildren(xid, path, watch);
            }
        }

        public class GetChildren2 : IZooKeeperRequest
        {
            private GetChildren2(int xid, string path, bool watch)
            {
                this.Xid = xid;
                this.Path = path;
                this.Watch = watch;
            }

            public int Xid { get; }

            public string Path { get; }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.GetChildren2;
                }
            }

            public bool Watch { get; }

            public static GetChildren2 CreateGetChildren2(int xid, string path, bool watch)
            {
                return new GetChildren2(xid, path, watch);
            }
        }

        public class SetData : IZooKeeperRequest
        {
            private SetData(int xid, string path, byte[] data, int version)
            {
                this.Xid = xid;
                this.Path = path;
                this.Data = data;
                this.Version = version;
            }

            public int Xid { get; }

            public string Path { get; }

            public byte[] Data { get; }

            public int Version { get; }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.SetData;
                }
            }

            public static SetData CreateSetData(int xid, string path, byte[] data, int version)
            {
                return new SetData(xid, path, data, version);
            }
        }

        public class GetACL : IZooKeeperRequest
        {
            private GetACL(int xid, string path)
            {
                this.Xid = xid;
                this.Path = path;
            }

            public int Xid { get; }

            public string Path { get; }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.GetACL;
                }
            }

            public static GetACL CreateGetACL(int xid, string path)
            {
                return new GetACL(xid, path);
            }
        }

        public class SetACL : IZooKeeperRequest
        {
            private SetACL(int xid, string path, IReadOnlyList<Acl> acls, int version)
            {
                this.Xid = xid;
                this.Path = path;
                this.Acls = acls;
                this.Version = version;
            }

            public int Xid { get; }

            public string Path { get; }

            public IReadOnlyList<Acl> Acls { get; }

            public int Version { get; }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.SetACL;
                }
            }

            public static SetACL CreateSetACL(int xid, string path, IReadOnlyList<Acl> acls, int version)
            {
                return new SetACL(xid, path, acls, version);
            }
        }

        public class Ping : IZooKeeperRequest
        {
            private Ping(int xid)
            {
                this.Xid = xid;
            }

            public int Xid { get; }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.Ping;
                }
            }

            public static Ping CreatePing(int xid)
            {
                return new Ping(xid);
            }
        }

        public class CloseSession : IZooKeeperRequest
        {
            private CloseSession(int xid)
            {
                this.Xid = xid;
            }

            public int Xid { get; }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.CloseSession;
                }
            }

            public static CloseSession CreateCloseSession(int xid)
            {
                return new CloseSession(xid);
            }
        }

        public class Notification : IZooKeeperRequest
        {
            private Notification(int xid)
            {
                this.Xid = xid;
            }

            public int Xid { get; }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.CloseSession;
                }
            }

            public static Notification CreateNotification(int xid)
            {
                return new Notification(xid);
            }
        }

        public class Multi : IZooKeeperRequest
        {
            private Multi(int xid, IReadOnlyList<IZooKeeperRequest> ops)
            {
                this.Xid = xid;
                this.Ops = ops;
            }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.Multi;
                }
            }

            public int Xid { get; }

            public IReadOnlyList<IZooKeeperRequest> Ops { get; }

            public static Multi CreateMulti(int xid, IReadOnlyList<IZooKeeperRequest> ops)
            {
                return new Multi(xid, ops);
            }
        }

        public class MultiHeader
        {
            private MultiHeader(ZooKeeperRequestType optype, bool done, int err)
            {
                this.OpType = optype;
                this.Done = done;
                this.Error = err;
            }

            // int OpType. (-1 or 0 is no-op)
            // byte if '1' if done or '0'
            // int error byte (set t0 -1 on request)
            // Seralized Op depending on Type
            public ZooKeeperRequestType OpType { get; set; }

            public bool Done { get; set; }

            public int Error { get; set; }

            public static MultiHeader CreateMultiHeader(int optype, byte bytedone, int err)
            {
                return new MultiHeader((ZooKeeperRequestType)optype, (bytedone == 0) ? false : true, err);
            }
        }

        public class Check : IZooKeeperRequest
        {
            private Check(int xid, string path, int version)
            {
                this.Xid = xid;
                this.Path = path;
                this.Version = version;
            }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.Check;
                }
            }

            public int Xid { get; }

            public string Path { get; }

            public int Version { get; }

            public static Check CreateCheck(int xid, string path, int version)
            {
                return new Check(xid, path, version);
            }
        }

        public class Auth : IZooKeeperRequest
        {
            private Auth(int xid, int authType, string scheme, string id)
            {
                this.Xid = xid;
                this.AuthType = authType;
                this.Scheme = scheme;
                this.Id = id;
            }

            public ZooKeeperRequestType RequestType
            {
                get
                {
                    return ZooKeeperRequestType.Auth;
                }
            }

            public int Xid { get; }

            public int AuthType { get; }

            public string Scheme { get; }

            public string Id { get; }

            public string RmAuthId
            {
                get
                {
                    return $"{this.Scheme}:{this.Id}";
                }
            }

            public static Auth CreateAuth(int xid, int authType, string scheme, string id)
            {
                return new Auth(xid, authType, scheme, id);
            }
        }
    }
}
