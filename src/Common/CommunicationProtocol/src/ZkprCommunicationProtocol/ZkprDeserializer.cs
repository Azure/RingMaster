// <copyright file="ZkprDeserializer.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using RingMaster;
    using RingMaster.Data;
    using RingMaster.Requests;

    /// <summary>
    /// Helper class to Deserialize a <see cref="RequestCall"/> or
    /// a <see cref="RequestResponse"/>.
    /// </summary>
    public sealed class ZkprDeserializer : IDisposable
    {
        /// <summary>
        /// Stream that contains the serialized data.
        /// </summary>
        private MemoryStream memoryStream;

        /// <summary>
        /// BinaryReader that is used to read from the stream.
        /// </summary>
        private BinaryReader binaryReader;

        /// <summary>
        /// Formatter to use to read objects from their binary representation.
        /// </summary>
        private BinaryFormatter binaryFormatter = new BinaryFormatter();

        /// <summary>
        /// Version of the serialization format used.
        /// </summary>
        private uint serializationVersionUsed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZkprDeserializer"/> class.
        /// </summary>
        /// <param name="bytes">Bytes that contain serialized data</param>
        /// <param name="byteLength">Length of the bytes</param>
        /// <param name="versionUsed">Version of the serialization format used</param>
        public ZkprDeserializer(byte[] bytes, int byteLength, uint versionUsed)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if ((versionUsed < ZkprSerializationFormatVersions.MinimumSupportedVersion) || (versionUsed > ZkprSerializationFormatVersions.MaximumSupportedVersion))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(versionUsed),
                    string.Format("Version must be between {0} and {1}", ZkprSerializationFormatVersions.MinimumSupportedVersion, ZkprSerializationFormatVersions.MaximumSupportedVersion));
            }

            this.memoryStream = new MemoryStream(bytes, 0, byteLength);
            this.binaryReader = new BinaryReader(this.memoryStream);
            this.serializationVersionUsed = versionUsed;
        }

        /// <summary>
        /// Deserialize a <see cref="RequestCall"/>.
        /// </summary>
        /// <param name="sessionState">PerSession state</param>
        /// <returns>Deserialized <see cref="RequestCall"/></returns>
        public ProtocolRequestCall DeserializeRequest(ZkprPerSessionState sessionState)
        {
            int callId;
            ZooKeeperRequestType type = ZooKeeperRequestType.Notification;
            if (sessionState == null)
            {
                throw new ArgumentNullException("sessionState");
            }

            if (!sessionState.ConnectRecieved)
            {
                type = ZooKeeperRequestType.CreateSession;
                callId = 0;
            }
            else
            {
                callId = this.binaryReader.ReadInt32BE();
                type = (ZooKeeperRequestType)this.binaryReader.ReadInt32BE();
            }

            IRingMasterRequest ringMasterRequest;
            IZooKeeperRequest zkprRequest = this.DeserializeZooKeeperRequest(callId, type, sessionState, out ringMasterRequest);
            return new ProtocolRequestCall()
            {
                CallId = (ulong)callId,
                Request = ringMasterRequest,
                ProtocolRequest = zkprRequest
            };
        }

        public void Dispose()
        {
            this.memoryStream.Dispose();
            this.binaryReader.Dispose();
        }

        /// <summary>
        /// Deserializes a string that may be null
        /// </summary>
        /// <returns>the deserialized string</returns>
        private string DeserializeNullableString()
        {
            string readString = null;
            bool isNull = this.binaryReader.ReadBoolean();
            if (!isNull)
            {
                readString = this.binaryReader.ReadString();
            }

            return readString;
        }

        /// <summary>
        /// Deserialize <see cref="IZookeeperRequest"/>
        /// </summary>
        /// <param name="xid">the callid</param>
        /// <param name="type">type of the call</param>
        /// <param name="sessionState">The PerSession State</param>
        /// <param name="ringMasterRequest">The ring master request.</param>
        /// <exception cref="System.ArgumentException">unknown type  + type</exception>
        /// <returns>The Zookeeper Request</returns>
        private IZooKeeperRequest DeserializeZooKeeperRequest(int xid, ZooKeeperRequestType type, ZkprPerSessionState sessionState, out IRingMasterRequest ringMasterRequest)
        {
            ringMasterRequest = null;
            switch (type)
            {
                case ZooKeeperRequestType.Notification: // "0" for Createing a session
                    ringMasterRequest = null;
                    return this.DeserializeNotification(xid);
                case ZooKeeperRequestType.CreateSession:
                    ZkprProtocolMessages.CreateSession cs = this.DeserializeCreateSession();
                    ringMasterRequest = new RequestInit((ulong)cs.SessionId, cs.IsNullPassword ? string.Empty : cs.Password);
                    sessionState.ConnectRecieved = true;
                    return cs;
                case ZooKeeperRequestType.Exists:
                    ZkprProtocolMessages.Exists ex = this.DeserializeExists(xid);
                    ringMasterRequest = new RequestExists(ex.Path, ex.Watch == false ? null : new Watcher((ulong)xid, true));
                    return ex;
                case ZooKeeperRequestType.GetChildren:
                    ZkprProtocolMessages.GetChildren gc = this.DeserializeGetChildren(xid);
                    ringMasterRequest = new RequestGetChildren(gc.Path, gc.Watch == false ? null : new Watcher((ulong)xid, true), null);
                    return gc;
                case ZooKeeperRequestType.GetChildren2:
                    ZkprProtocolMessages.GetChildren2 gc2 = this.DeserializeGetChildren2(xid);
                    ringMasterRequest = new RequestGetChildren(gc2.Path, gc2.Watch == false ? null : new Watcher((ulong)xid, true), null);
                    return gc2;
                case ZooKeeperRequestType.GetData:
                    ZkprProtocolMessages.GetData gd = this.DeserializeGetData(xid);
                    ringMasterRequest = new RequestGetData(gd.Path, RequestGetData.GetDataOptions.None, gd.Watch == false ? null : new Watcher((ulong)xid, true));
                    return gd;
                case ZooKeeperRequestType.Create:
                    ZkprProtocolMessages.Create cr = this.DeserializeCreate(xid);
                    IReadOnlyList<Acl> acls = this.TranslateZkprAclListToRMAclList(cr.Acls);
                    CreateMode cm = this.TranslateZkprCreatFlagsToRmCreateMode(cr.Flags);
                    ringMasterRequest = new RequestCreate(cr.Path, cr.Data, acls, cm);
                    return cr;
                case ZooKeeperRequestType.Create2:
                    ZkprProtocolMessages.Create2 cr2 = this.DeserializeCreate2(xid);
                    IReadOnlyList<Acl> acls2 = this.TranslateZkprAclListToRMAclList(cr2.Acls);
                    CreateMode cm2 = this.TranslateZkprCreatFlagsToRmCreateMode(cr2.Flags);
                    ringMasterRequest = new RequestCreate(cr2.Path, cr2.Data, acls2, cm2);
                    return cr2;
                case ZooKeeperRequestType.SetData:
                    ZkprProtocolMessages.SetData sd = this.DeserializeSetData(xid);
                    ringMasterRequest = new RequestSetData(sd.Path, sd.Data, sd.Version);
                    return sd;
                case ZooKeeperRequestType.Delete:
                    ZkprProtocolMessages.Delete dl = this.DeserializeDelete(xid);
                    ringMasterRequest = new RequestDelete(dl.Path, dl.Version, false);
                    return dl;
                case ZooKeeperRequestType.Ping:
                    ringMasterRequest = null;
                    return this.DeserializePing(xid);
                case ZooKeeperRequestType.CloseSession:
                    ringMasterRequest = null;
                    sessionState.ConnectRecieved = false; // Renegotiate the CreateSession
                    return this.DeserializeCloseSession(xid);
                case ZooKeeperRequestType.GetACL:
                    ZkprProtocolMessages.GetACL ga = this.DeserializeGetACL(xid);
                    ringMasterRequest = new RequestGetAcl(ga.Path, null);
                    return ga;
                case ZooKeeperRequestType.SetACL:
                    ZkprProtocolMessages.SetACL sa = this.DeserializeSetACL(xid);
                    IReadOnlyList<Acl> sa_acls = this.TranslateZkprAclListToRMAclList(sa.Acls);
                    ringMasterRequest = new RequestSetAcl(sa.Path, sa_acls, sa.Version);
                    return sa;
                case ZooKeeperRequestType.Multi:
                    ZkprProtocolMessages.Multi mu = this.DeserializeMulti(xid);
                    IReadOnlyList<Op> rmOps = this.TranslateZkprOpsListToRmOpsList(mu.Ops);
                    ringMasterRequest = new RequestMulti(rmOps, false);
                    return mu;
                case ZooKeeperRequestType.Auth:
                    ZkprProtocolMessages.Auth au = this.DeserializeAuth(xid);
                    ringMasterRequest = new RequestSetAuth(au.RmAuthId);
                    return au;
                case ZooKeeperRequestType.Check:
                case ZooKeeperRequestType.Sync:
                case ZooKeeperRequestType.Reconfig:
                case ZooKeeperRequestType.SetWatches:
                case ZooKeeperRequestType.RemoveWatches:
                case ZooKeeperRequestType.CreateContainer:
                case ZooKeeperRequestType.DeleteContainer:
                case ZooKeeperRequestType.Sasl:
                case ZooKeeperRequestType.Error:
                default:
                    break;
            }

            return null;
        }

        private IReadOnlyList<Op> TranslateZkprOpsListToRmOpsList(IReadOnlyList<IZooKeeperRequest> zkprOps)
        {
            List<Op> rmOps = new List<Op>();

            foreach (IZooKeeperRequest zkReq in zkprOps)
            {
                switch (zkReq.RequestType)
                {
                    case ZooKeeperRequestType.Create:
                        ZkprProtocolMessages.Create zkCreate = zkReq as ZkprProtocolMessages.Create;
                        IReadOnlyList<Acl> acls = this.TranslateZkprAclListToRMAclList(zkCreate.Acls);
                        CreateMode cm = this.TranslateZkprCreatFlagsToRmCreateMode(zkCreate.Flags);

                        rmOps.Add(Op.Create(zkCreate.Path, zkCreate.Data, acls, cm));
                        break;
                    case ZooKeeperRequestType.Delete:
                        ZkprProtocolMessages.Delete zkDelete = zkReq as ZkprProtocolMessages.Delete;

                        rmOps.Add(Op.Delete(zkDelete.Path, zkDelete.Version, false));
                        break;
                    case ZooKeeperRequestType.SetData:
                        ZkprProtocolMessages.SetData zkSetData = zkReq as ZkprProtocolMessages.SetData;

                        rmOps.Add(Op.SetData(zkSetData.Path, zkSetData.Data, zkSetData.Version));
                        break;
                    case ZooKeeperRequestType.Check:
                        ZkprProtocolMessages.Check zkCheck = zkReq as ZkprProtocolMessages.Check;

                        rmOps.Add(Op.Check(zkCheck.Path, zkCheck.Version));
                        break;
                }
            }

            return rmOps;
        }

        private IReadOnlyList<Acl> TranslateZkprAclListToRMAclList(IReadOnlyList<Acl> zkprAcls)
        {
            if (zkprAcls == null || zkprAcls.Any<Acl>() == false)
            {
                return null;
            }

            List<Acl> rmAcls = new List<Acl>();
            foreach (Acl a in zkprAcls)
            {
                Acl rmAcl = this.TranslateZkprAclToRmAcl(a);
                rmAcls.Add(rmAcl);
            }

            return rmAcls as IReadOnlyList<Acl>;
        }

        private Acl TranslateZkprAclToRmAcl(Acl a)
        {
            Acl.Perm p = 0;
            ZkprProtocolMessages.Perms zp = (ZkprProtocolMessages.Perms)a.Perms;

            if ((zp & ZkprProtocolMessages.Perms.Read) == ZkprProtocolMessages.Perms.Read)
            {
                p |= Acl.Perm.READ;
            }

            if ((zp & ZkprProtocolMessages.Perms.Write) == ZkprProtocolMessages.Perms.Write)
            {
                p |= Acl.Perm.WRITE;
            }

            if ((zp & ZkprProtocolMessages.Perms.Delete) == ZkprProtocolMessages.Perms.Delete)
            {
                p |= Acl.Perm.DELETE;
            }

            if ((zp & ZkprProtocolMessages.Perms.Create) == ZkprProtocolMessages.Perms.Create)
            {
                p |= Acl.Perm.CREATE;
            }

            if ((zp & ZkprProtocolMessages.Perms.Admin) == ZkprProtocolMessages.Perms.Admin)
            {
                p |= Acl.Perm.ADMIN;
            }

            if ((zp & ZkprProtocolMessages.Perms.All) == ZkprProtocolMessages.Perms.All)
            {
                p |= Acl.Perm.ALL;
            }

            Id rmId = new Id(a.Id.Scheme, a.Id.Identifier.Equals("anyone") ? "world" : a.Id.Identifier);

            return new Acl((int)p, rmId);
        }

        private CreateMode TranslateZkprCreatFlagsToRmCreateMode(int zkprFlags)
        {
            CreateMode cm = CreateMode.Persistent;

            switch ((ZkprProtocolMessages.CreateMode)zkprFlags)
            {
                case ZkprProtocolMessages.CreateMode.Ephemeral:
                    cm = CreateMode.Ephemeral;
                    break;
                case ZkprProtocolMessages.CreateMode.EphemeralSequential:
                    cm = CreateMode.EphemeralSequential;
                    break;
                case ZkprProtocolMessages.CreateMode.PersistentSequential:
                    cm = CreateMode.PersistentSequential;
                    break;
                case ZkprProtocolMessages.CreateMode.Persistent:
                    break;
                case ZkprProtocolMessages.CreateMode.Container:
                    // We have no equivalent in RM.
                default:
                    break;
            }

            return cm;
        }

        private ZkprProtocolMessages.Multi DeserializeMulti(int xid)
        {
            // A Multi consists of a repeating pattern of MultiHeader
            // int OpType. (-1 or 0 is no-op)
            // byte if '1' if done or '0'
            // int error byte (set t0 -1 on request)
            // Seralized Op depending on Type
            bool done = false;
            List<IZooKeeperRequest> ops = new List<IZooKeeperRequest>();
            do
            {
                ZkprProtocolMessages.MultiHeader mh = this.DeserializeMultiHeader();
                if (mh.Done)
                {
                    done = true;
                }
                else
                {
                    switch (mh.OpType)
                    {
                        case ZooKeeperRequestType.Create:
                            ZkprProtocolMessages.Create cr = this.DeserializeCreate(xid);
                            ops.Add(cr);
                            break;
                        case ZooKeeperRequestType.Delete:
                            ZkprProtocolMessages.Delete dl = this.DeserializeDelete(xid);
                            ops.Add(dl);
                            break;
                        case ZooKeeperRequestType.SetData:
                            ZkprProtocolMessages.SetData sd = this.DeserializeSetData(xid);
                            ops.Add(sd);
                            break;
                        case ZooKeeperRequestType.Check:
                            ZkprProtocolMessages.Check ch = this.DeserializeCheck(xid);
                            ops.Add(ch);
                            break;
                    }
                }
            }
            while (!done);

            return ZkprProtocolMessages.Multi.CreateMulti(xid, ops);
        }

        private ZkprProtocolMessages.Check DeserializeCheck(int xid)
        {
            string path = this.binaryReader.ReadString32BitPrefixLengthBE();
            int version = this.binaryReader.ReadInt32BE();
            return ZkprProtocolMessages.Check.CreateCheck(xid, path, version);
        }

        private ZkprProtocolMessages.MultiHeader DeserializeMultiHeader()
        {
            int optype = this.binaryReader.ReadInt32BE();
            byte bdone = this.binaryReader.ReadByte();
            int err = this.binaryReader.ReadInt32BE();

            return ZkprProtocolMessages.MultiHeader.CreateMultiHeader(optype, bdone, err);
        }

        private ZkprProtocolMessages.GetACL DeserializeGetACL(int xid)
        {
            string path = this.binaryReader.ReadString32BitPrefixLengthBE();
            return ZkprProtocolMessages.GetACL.CreateGetACL(xid, path);
        }

        private ZkprProtocolMessages.SetACL DeserializeSetACL(int xid)
        {
            string path = this.binaryReader.ReadString32BitPrefixLengthBE();
            int numAcls = this.binaryReader.ReadInt32BE();
            List<Acl> aclList = new List<Acl>();

            for (int n = 0; n < numAcls; n++)
            {
                int perms = this.binaryReader.ReadInt32BE();
                string scheme = this.binaryReader.ReadString32BitPrefixLengthBE();
                string id = this.binaryReader.ReadString32BitPrefixLengthBE();
                Acl acl = new Acl(perms, new Id(scheme, id));
                aclList.Add(acl);
            }

            int version = this.binaryReader.ReadInt32BE();

            return ZkprProtocolMessages.SetACL.CreateSetACL(xid, path, aclList, version);
        }

        private ZkprProtocolMessages.Delete DeserializeDelete(int xid)
        {
            string path = this.binaryReader.ReadString32BitPrefixLengthBE();
            int version = this.binaryReader.ReadInt32BE();

            return ZkprProtocolMessages.Delete.CreateDelete(xid, path, version);
        }

        private ZkprProtocolMessages.Create DeserializeCreate(int xid)
        {
            string path = this.binaryReader.ReadString32BitPrefixLengthBE();
            byte[] data = this.binaryReader.ReadByteArray32BitPrefixLengthBE();
            int numAcls = this.binaryReader.ReadInt32BE();
            List<Acl> aclList;
            if ((numAcls == -1) || (numAcls == 0))
            {
                aclList = null;
            }
            else
            {
                aclList = new List<Acl>();
            }

            for (int n = 0; n < numAcls; n++)
            {
                int perms = this.binaryReader.ReadInt32BE();
                string scheme = this.binaryReader.ReadString32BitPrefixLengthBE();
                string id = this.binaryReader.ReadString32BitPrefixLengthBE();
                Acl acl = new Acl(perms, new Id(scheme, id));
                aclList.Add(acl);
            }

            int flags = this.binaryReader.ReadInt32BE();

            return ZkprProtocolMessages.Create.CreateCreate(xid, path, data, aclList, flags);
        }

        private ZkprProtocolMessages.Create2 DeserializeCreate2(int xid)
        {
            string path = this.binaryReader.ReadString32BitPrefixLengthBE();
            byte[] data = this.binaryReader.ReadByteArray32BitPrefixLengthBE();
            int numAcls = this.binaryReader.ReadInt32BE();
            List<Acl> aclList;
            if ((numAcls == -1) || (numAcls == 0))
            {
                aclList = null;
            }
            else
            {
                aclList = new List<Acl>();
            }

            for (int n = 0; n < numAcls; n++)
            {
                int perms = this.binaryReader.ReadInt32BE();
                string scheme = this.binaryReader.ReadString32BitPrefixLengthBE();
                string id = this.binaryReader.ReadString32BitPrefixLengthBE();
                Acl acl = new Acl(perms, new Id(scheme, id));
                aclList.Add(acl);
            }

            int flags = this.binaryReader.ReadInt32BE();

            return ZkprProtocolMessages.Create2.CreateCreate2(xid, path, data, aclList, flags);
        }

        private ZkprProtocolMessages.Exists DeserializeExists(int xid)
        {
            string path = this.binaryReader.ReadString32BitPrefixLengthBE();
            byte watchByte = this.binaryReader.ReadByte();
            bool watch = watchByte != 0;

            return ZkprProtocolMessages.Exists.CreateExists(xid, path, watch);
        }

        private bool IsNullZkprPassword(byte[] pwd)
        {
            if (pwd.Length == 0)
            {
                return true;
            }

            byte[] bNullPwd = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            return bNullPwd.SequenceEqual<byte>(pwd);
        }

        private ZkprProtocolMessages.CreateSession DeserializeCreateSession()
        {
            int version = this.binaryReader.ReadInt32BE();
            long lastZxidSeen = this.binaryReader.ReadInt64BE();
            int timeout = this.binaryReader.ReadInt32BE();
            long sessionId = this.binaryReader.ReadInt64BE();
            byte[] bPwd = this.binaryReader.ReadByteArray32BitPrefixLengthBE();
            bool isNullPwd = this.IsNullZkprPassword(bPwd);
            string pwd = System.Text.Encoding.ASCII.GetString(bPwd);

            return ZkprProtocolMessages.CreateSession.CreateRequest(version, lastZxidSeen, timeout, sessionId, pwd, isNullPwd);
        }

        private ZkprProtocolMessages.GetChildren DeserializeGetChildren(int xid)
        {
            string path = this.binaryReader.ReadString32BitPrefixLengthBE();
            byte watchByte = this.binaryReader.ReadByte();
            bool watch = watchByte != 0;

            return ZkprProtocolMessages.GetChildren.CreateGetChildren(xid, path, watch);
        }

        private ZkprProtocolMessages.GetChildren2 DeserializeGetChildren2(int xid)
        {
            string path = this.binaryReader.ReadString32BitPrefixLengthBE();
            byte watchByte = this.binaryReader.ReadByte();
            bool watch = watchByte != 0;

            return ZkprProtocolMessages.GetChildren2.CreateGetChildren2(xid, path, watch);
        }

        private ZkprProtocolMessages.GetData DeserializeGetData(int xid)
        {
            string path = this.binaryReader.ReadString32BitPrefixLengthBE();
            byte watchByte = this.binaryReader.ReadByte();
            bool watch = watchByte != 0;

            return ZkprProtocolMessages.GetData.CreateGetData(xid, path, watch);
        }

        private ZkprProtocolMessages.SetData DeserializeSetData(int xid)
        {
            string path = this.binaryReader.ReadString32BitPrefixLengthBE();
            byte[] data = this.binaryReader.ReadByteArray32BitPrefixLengthBE();
            int version = this.binaryReader.ReadInt32BE();

            return ZkprProtocolMessages.SetData.CreateSetData(xid, path, data, version);
        }

        private ZkprProtocolMessages.Auth DeserializeAuth(int xid)
        {
            int authType = this.binaryReader.ReadInt32BE();
            string scheme = this.binaryReader.ReadString32BitPrefixLengthBE();
            string id = this.binaryReader.ReadString32BitPrefixLengthBE();

            return ZkprProtocolMessages.Auth.CreateAuth(xid, authType, scheme, id);
        }

        private ZkprProtocolMessages.Ping DeserializePing(int xid)
        {
            return ZkprProtocolMessages.Ping.CreatePing(xid);
        }

        private ZkprProtocolMessages.CloseSession DeserializeCloseSession(int xid)
        {
            return ZkprProtocolMessages.CloseSession.CreateCloseSession(xid);
        }

        private ZkprProtocolMessages.Notification DeserializeNotification(int xid)
        {
            return ZkprProtocolMessages.Notification.CreateNotification(xid);
        }

        /// <summary>
        /// Placeholder watcher that remembers the deserialized watcher id.
        /// </summary>
        private class Watcher : IWatcher
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Watcher"/> class.
            /// </summary>
            /// <param name="id">Id of the watcher</param>
            /// <param name="oneUse"><c>true</c> if this watcher is a single use watcher</param>
            public Watcher(ulong id, bool oneUse)
            {
                this.Id = id;
                this.OneUse = oneUse;
            }

            /// <summary>
            /// Gets the Id of the watcher.
            /// </summary>
            public ulong Id { get; private set; }

            /// <summary>
            /// Gets a value indicating whether this watcher is single use.
            /// </summary>
            public bool OneUse { get; private set; }

            /// <summary>
            /// Process a watcher notification.
            /// </summary>
            /// <param name="evt">Watcher notification event</param>
            public void Process(WatchedEvent evt)
            {
                // This is a placeholder class that is just for remembering the
                // watcher id.
                throw new NotImplementedException();
            }
        }
    }
}