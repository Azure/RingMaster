// <copyright file="ZkprSerializer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Helper class to Serialize a <see cref="RequestCall"/> or <see cref="RequestResponse"/>.
    /// </summary>
    public sealed class ZkprSerializer : IDisposable
    {
        /// <summary>
        /// A byte array that indicates no data.
        /// </summary>
        private static readonly byte[] NullByteArray = new byte[] { 1 };

        /// <summary>
        /// Formatter to use to get the binary representation of objects.
        /// </summary>
        private static BinaryFormatter binaryFormatter = new BinaryFormatter();

        /// <summary>
        /// Watcher has special id.
        /// </summary>
        private readonly int watcherNotificationId = -1;

        /// <summary>
        /// Stream in which serialized data must be stored.
        /// </summary>
        private MemoryStream memoryStream;

        /// <summary>
        /// BinaryWriter that is used to write to the stream.
        /// </summary>
        private BinaryWriter binaryWriter;

        /// <summary>
        /// Version of the serialization format to use.
        /// </summary>
        private uint versionToUse;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZkprSerializer"/> class.
        /// </summary>
        /// <param name="versionToUse">Version of the serialization format to use</param>
        public ZkprSerializer(uint versionToUse)
        {
            if ((versionToUse < ZkprSerializationFormatVersions.MinimumSupportedVersion) || (versionToUse > ZkprSerializationFormatVersions.MaximumSupportedVersion))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(versionToUse),
                    $"Version must be between {ZkprSerializationFormatVersions.MinimumSupportedVersion} and {ZkprSerializationFormatVersions.MaximumSupportedVersion} versionToUse={versionToUse}");
            }

            this.versionToUse = versionToUse;
            this.memoryStream = new MemoryStream();
            this.binaryWriter = new BinaryWriter(this.memoryStream);
        }

        /// <summary>
        /// Serializes a response object
        /// </summary>
        /// <param name="response">Response object</param>
        public void SerializeWatcherResponse(RequestResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }

            this.SerializeReplyHeader(response.Stat == null ? 0L : response.Stat.Mzxid, response.ResultCode, (int)this.watcherNotificationId); // Watcher has 'special id'
            WatcherCall wc = response.Content as WatcherCall;
            this.binaryWriter.WriteBE((int)wc.WatcherEvt.EventType);
            this.binaryWriter.WriteBE((int)wc.WatcherEvt.KeeperState);
            this.binaryWriter.WriteString32BitPrefixLengthBE(wc.WatcherEvt.Path);
        }

        /// <summary>
        /// Serialize a <see cref="RequestResponse"/>.
        /// </summary>
        /// <param name="response">Response to serialize</param>
        /// <param name="zkprRequest">The Per call zookeeper request</param>
        public void SerializeResponse(RequestResponse response, IZooKeeperRequest zkprRequest)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }

            if (zkprRequest == null)
            {
                throw new ArgumentNullException("zkprRequest");
            }

            switch (zkprRequest.RequestType)
            {
                case ZooKeeperRequestType.CreateSession:
                    this.SerializeResponseCreateSession(response, zkprRequest);
                    break;
                case ZooKeeperRequestType.GetChildren:
                case ZooKeeperRequestType.GetChildren2:
                    this.SerializeResponseGetChildren(response, zkprRequest);
                    break;
                case ZooKeeperRequestType.Create:
                case ZooKeeperRequestType.Create2:
                    this.SerializeResponseCreate(response, zkprRequest);
                    break;
                case ZooKeeperRequestType.Delete:
                    this.SerializeResponseDelete(response, zkprRequest);
                    break;
                case ZooKeeperRequestType.Exists:
                    this.SerializeResponseExists(response, zkprRequest);
                    break;
                case ZooKeeperRequestType.GetData:
                    this.SerializeResponseGetData(response, zkprRequest);
                    break;
                case ZooKeeperRequestType.SetData:
                    this.SerializeResponseSetData(response, zkprRequest);
                    break;
                case ZooKeeperRequestType.GetACL:
                    this.SerializeResponseGetACL(response, zkprRequest);
                    break;
                case ZooKeeperRequestType.SetACL:
                    this.SerializeResponseSetACL(response, zkprRequest);
                    break;
                case ZooKeeperRequestType.Multi:
                    this.SerializeResponseMulti(response, zkprRequest);
                    break;
                case ZooKeeperRequestType.Auth:
                    this.SerializeResponseAuth(response, zkprRequest);
                    break;
                case ZooKeeperRequestType.Sync:
                    break;
                case ZooKeeperRequestType.Check: // Apparently this is only supported in a multi?
                    break;
                case ZooKeeperRequestType.Reconfig:
                    break;
                case ZooKeeperRequestType.CheckWatches:
                    break;
                case ZooKeeperRequestType.RemoveWatches:
                    break;
                case ZooKeeperRequestType.CreateContainer:
                    break;
                case ZooKeeperRequestType.DeleteContainer:
                    break;
                case ZooKeeperRequestType.SetWatches:
                    break;
                case ZooKeeperRequestType.Sasl:
                    break;
                case ZooKeeperRequestType.Ping:
                    this.SerializeResponsePing(response, zkprRequest);
                    break;
                case ZooKeeperRequestType.Error:
                case ZooKeeperRequestType.CloseSession:
                case ZooKeeperRequestType.Notification:
                default:
                    break;
            }

            return;
        }

        /// <summary>
        /// Get a byte array that contains the serialized data.
        /// </summary>
        /// <returns>Byte array that contains serialized data</returns>
        public byte[] GetBytes()
        {
            if (this.memoryStream.Length == 0)
            {
                return null; // Indicate nothing to send. (otherwise things like Ping will send a response and cause connection to be closed);
            }

            return this.memoryStream.ToArray();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.memoryStream.Dispose();
            this.binaryWriter.Dispose();
        }

        private void SerializeResponseCreateSession(RequestResponse response, IZooKeeperRequest request)
        {
            if (response.ResultCode == (int)Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.RingMasterException.Code.Ok)
            {
                int protocolVersion = ((ZkprProtocolMessages.CreateSession)request).Version;
                this.binaryWriter.WriteBE(protocolVersion);
                this.binaryWriter.WriteBE(((ZkprProtocolMessages.CreateSession)request).Timeout);
                ulong sessionId;
                sessionId = (ulong)((ZkprProtocolMessages.CreateSession)request).SessionId;
                if (sessionId == 0)
                {
                    sessionId = Convert.ToUInt64(((string[])response.Content)[0]);
                }

                this.binaryWriter.WriteBE(sessionId);
                string sPassword;
                if (((ZkprProtocolMessages.CreateSession)request).IsNullPassword)
                {
                    sPassword = ((string[])response.Content)[1];
                }
                else
                {
                    sPassword = ((ZkprProtocolMessages.CreateSession)request).Password;
                }

                this.binaryWriter.WriteString32BitPrefixLengthBE(sPassword);
            }
            else
            {
                // Dont know what to do in case of error return error?
            }
        }

        private void SerializeResponseExists(RequestResponse response, IZooKeeperRequest request)
        {
            Stat stat = (Stat)response.Content;

            this.SerializeReplyHeader(stat == null ? 0L : stat.Mzxid, response.ResultCode, request.Xid);

            this.SerializeStat(stat);
        }

        private void SerializeResponseGetACL(RequestResponse response, IZooKeeperRequest request)
        {
            this.SerializeReplyHeader(response.Stat == null ? 0L : response.Stat.Mzxid, response.ResultCode, request.Xid);
            IEnumerable<Acl> theAcls = response.Content as IEnumerable<Acl>;
            IReadOnlyList<Acl> zkprAcls = this.TranslateRingMasterAclListToZkprAclList(theAcls);

            if (zkprAcls == null)
            {
                zkprAcls = new List<Acl>();
            }

            // Write the count of Acls
            this.binaryWriter.WriteBE((int)zkprAcls.Count);
            foreach (Acl a in zkprAcls)
            {
                this.binaryWriter.WriteBE((int)a.Perms);
                this.binaryWriter.WriteString32BitPrefixLengthBE(a.Id.Scheme);
                this.binaryWriter.WriteString32BitPrefixLengthBE(a.Id.Identifier);
            }

            this.SerializeStat(response.Stat);
        }

        private void SerializeResponseSetACL(RequestResponse response, IZooKeeperRequest request)
        {
            this.SerializeReplyHeader(response.Stat == null ? 0L : response.Stat.Mzxid, response.ResultCode, request.Xid);
            this.SerializeStat(response.Stat);
        }

        private void SerializeResponseCreate(RequestResponse response, IZooKeeperRequest request)
        {
            this.SerializeReplyHeader(response.Stat == null ? 0L : response.Stat.Mzxid, response.ResultCode, request.Xid);
            if (((response.ResultCode == (int)RingMasterException.Code.Ok) || (response.ResultCode == (int)RingMasterException.Code.Nodeexists)) && (response.Content != null))
            {
                this.binaryWriter.WriteString32BitPrefixLengthBE(response.Content as string);
                if (request.RequestType == ZooKeeperRequestType.Create2)
                {
                    this.SerializeStat(response.Stat);
                }
            }
            else
            {
                this.binaryWriter.WriteString32BitPrefixLengthBE(string.Empty);
            }
        }

        private void SerializeResponseDelete(RequestResponse response, IZooKeeperRequest request)
        {
            this.SerializeReplyHeader(response.Stat == null ? 0L : response.Stat.Mzxid, response.ResultCode, request.Xid);
        }

        private void SerializeResponseGetChildren(RequestResponse response, IZooKeeperRequest request)
        {
            this.SerializeReplyHeader(response.Stat == null ? 0L : response.Stat.Mzxid, response.ResultCode, request.Xid);

            if (response.Content != null)
            {
                IEnumerable<string> children = response.Content as IEnumerable<string>;
                if (children.Any<string>())
                {
                    this.binaryWriter.WriteBE((int)children.Count<string>());

                    foreach (string c in children)
                    {
                        this.binaryWriter.WriteString32BitPrefixLengthBE(c);
                    }
                }
                else
                {
                    this.binaryWriter.WriteBE((int)-1);
                }

                if (request.RequestType == ZooKeeperRequestType.GetChildren2)
                {
                    this.SerializeStat(response.Stat);
                }
            }
            else
            {
                this.binaryWriter.WriteString32BitPrefixLengthBE(string.Empty);
            }
        }

        private void SerializeResponseGetData(RequestResponse response, IZooKeeperRequest request)
        {
            this.SerializeReplyHeader(response.Stat == null ? 0L : response.Stat.Mzxid, response.ResultCode, request.Xid);
            this.binaryWriter.WriteByteArray32BitPrefixLengthBE((byte[])response.Content);
            this.SerializeStat(response.Stat);
        }

        private void SerializeResponseSetData(RequestResponse response, IZooKeeperRequest request)
        {
            this.SerializeReplyHeader(response.Stat == null ? 0L : response.Stat.Mzxid, response.ResultCode, request.Xid);
            this.SerializeStat(response.Stat);
        }

        private void SerializeResponseMulti(RequestResponse response, IZooKeeperRequest request)
        {
            this.SerializeReplyHeader(response.Stat == null ? 0L : response.Stat.Mzxid, response.ResultCode, request.Xid);
            List<OpResult> results = response.Content as List<OpResult>;
            foreach (OpResult or in results)
            {
                this.SerializeMultiHeader(this.TranslateRmOpResultTypeToZkOpType(or.ResultType), false, this.TranslateRingMasterResultCodeToZkprErrorCode((int)or.ErrCode));
                RequestResponse rr = OpResult.ToResponse(or);
                switch (or.ResultType)
                {
                    case OpCode.Create:
                        this.binaryWriter.WriteString32BitPrefixLengthBE(rr.Content as string);
                        break;
                    case OpCode.Delete:
                        break;
                    case OpCode.SetData:
                        this.SerializeStat(rr.Stat);
                        break;
                    case OpCode.Check:
                        break;
                }
            }

            this.SerializeMultiHeader(-1, true, -1);
        }

        private int TranslateRmOpResultTypeToZkOpType(OpCode opcode)
        {
            int op = -1;

            switch (opcode)
            {
                case OpCode.Check:
                    op = (int)ZooKeeperRequestType.Check;
                    break;
                case OpCode.Create:
                    op = (int)ZooKeeperRequestType.Create;
                    break;
                case OpCode.Delete:
                    op = (int)ZooKeeperRequestType.Delete;
                    break;
                case OpCode.Error:
                    op = (int)ZooKeeperRequestType.Error;
                    break;
                case OpCode.SetData:
                    op = (int)ZooKeeperRequestType.SetData;
                    break;
            }

            return op;
        }

        private void SerializeMultiHeader(int opType, bool done, int err)
        {
            this.binaryWriter.WriteBE((int)opType);
            this.binaryWriter.Write((byte)(done ? 1 : 0));
            this.binaryWriter.WriteBE(err);
        }

        private void SerializeResponseAuth(RequestResponse response, IZooKeeperRequest request)
        {
            Stat stat;
            stat = new Stat() { Czxid = -1 };
            this.SerializeReplyHeader(stat.Mzxid, response.ResultCode, request.Xid);
        }

        private void SerializeResponsePing(RequestResponse response, IZooKeeperRequest request)
        {
            Stat stat;
            stat = new Stat() { Czxid = -1 };
            this.SerializeReplyHeader(stat.Mzxid, response.ResultCode, request.Xid);
        }

        private void SerializeReplyHeader(long mzxId, int resultCode, int xid)
        {
            this.binaryWriter.WriteBE(xid);
            this.binaryWriter.WriteBE(mzxId);
            int errorCode = this.TranslateRingMasterResultCodeToZkprErrorCode(resultCode);
            this.binaryWriter.WriteBE(errorCode);
        }

        private int TranslateRingMasterResultCodeToZkprErrorCode(int resultCode)
        {
            ZooKeeperErrorCodes eCode = ZooKeeperErrorCodes.ZOK;
            switch (resultCode)
            {
                // An API was not used correctly.
                case (int)RingMasterException.Code.Apierror:
                    eCode = ZooKeeperErrorCodes.ZAPIERROR;
                    break;

                // Client authentication failed.
                case (int)RingMasterException.Code.Authfailed:
                    eCode = ZooKeeperErrorCodes.ZAUTHFAILED;
                    break;

                // Invalid arguments.
                case (int)RingMasterException.Code.Badarguments:
                    eCode = ZooKeeperErrorCodes.ZBADARGUMENTS;
                    break;

                // Version conflict.
                case (int)RingMasterException.Code.Badversion:
                    eCode = ZooKeeperErrorCodes.ZBADVERSION;
                    break;

                // Connection to the server has been lost.
                case (int)RingMasterException.Code.Connectionloss:
                    eCode = ZooKeeperErrorCodes.ZCONNECTIONLOSS;
                    break;

                // A data inconsistency was found.
                case (int)RingMasterException.Code.Datainconsistency:
                    eCode = ZooKeeperErrorCodes.ZDATAINCONSISTENCY;
                    break;

                // Invalid <see cref="Acl"/> was specified.
                case (int)RingMasterException.Code.Invalidacl:
                    eCode = ZooKeeperErrorCodes.ZINVALIDACL;
                    break;

                // Invalid callback specified
                case (int)RingMasterException.Code.Invalidcallback:
                    eCode = ZooKeeperErrorCodes.ZINVALIDCALLBACK;
                    break;

                // Error while marshaling or un-marshaling data.
                case (int)RingMasterException.Code.Marshallingerror:
                    eCode = ZooKeeperErrorCodes.ZMARSHALLINGERROR;
                    break;

                // Not authenticated.
                case (int)RingMasterException.Code.Noauth:
                    eCode = ZooKeeperErrorCodes.ZNOAUTH;
                    break;

                // Ephemeral nodes are not allowed to have children.
                case (int)RingMasterException.Code.Nochildrenforephemerals:
                    eCode = ZooKeeperErrorCodes.ZNOCHILDRENFOREPHEMERALS;
                    break;

                // The node already exists.
                case (int)RingMasterException.Code.Nodeexists:
                    eCode = ZooKeeperErrorCodes.ZNODEEXISTS;
                    break;

                // Node does not exist.
                case (int)RingMasterException.Code.Nonode:
                    eCode = ZooKeeperErrorCodes.ZNONODE;
                    break;

                // The node has children.
                case (int)RingMasterException.Code.Notempty:
                    eCode = ZooKeeperErrorCodes.ZNOTEMPTY;
                    break;

                // Everything is OK.
                case (int)RingMasterException.Code.Ok:
                    eCode = ZooKeeperErrorCodes.ZOK;
                    break;

                // Operation timeout.
                case (int)RingMasterException.Code.Operationtimeout:
                    eCode = ZooKeeperErrorCodes.ZOPERATIONTIMEOUT;
                    break;

                // A runtime inconsistency was found.
                case (int)RingMasterException.Code.Runtimeinconsistency:
                    eCode = ZooKeeperErrorCodes.ZRUNTIMEINCONSISTENCY;
                    break;

                // The session has been expired by the server.
                case (int)RingMasterException.Code.Sessionexpired:
                    eCode = ZooKeeperErrorCodes.ZSESSIONEXPIRED;
                    break;

                // Session moved to another server, so operation is ignored.
                case (int)RingMasterException.Code.Sessionmoved:
                    eCode = ZooKeeperErrorCodes.ZSESSIONMOVED;
                    break;

                // Operation is unimplemented.
                case (int)RingMasterException.Code.Unimplemented:
                    eCode = ZooKeeperErrorCodes.ZUNIMPLEMENTED;
                    break;

                // System and server-side errors.
                case (int)RingMasterException.Code.Systemerror:
                // Unknown error.
                case (int)RingMasterException.Code.Unknown:
                // Participants did not agree on the transaction.
                case (int)RingMasterException.Code.TransactionNotAgreed:
                // Operation timeout on server (the request comes with a max timeout for the execution queue at the server that was not met).
                case (int)RingMasterException.Code.Waitqueuetimeoutonserver:
                // The server is in lockdown
                case (int)RingMasterException.Code.InLockDown:
                // The requested node has too many children to be enumerated in a single request.
                case (int)RingMasterException.Code.TooManyChildren:
                // The operation was cancelled.
                case (int)RingMasterException.Code.OperationCancelled:
                default:
                    eCode = ZooKeeperErrorCodes.ZSYSTEMERROR;
                    break;
            }

            return (int)eCode;
        }

        private IReadOnlyList<Acl> TranslateRingMasterAclListToZkprAclList(IEnumerable<Acl> rmAcls)
        {
            if (rmAcls == null || rmAcls.Any<Acl>() == false)
            {
                return null;
            }

            List<Acl> aclList = new List<Acl>();
            foreach (Acl rmAcl in rmAcls)
            {
                Acl zAcl = this.TranslateRingMasterAclToZkprAcl(rmAcl);
                aclList.Add(zAcl);
            }

            return aclList;
        }

        private Acl TranslateRingMasterAclToZkprAcl(Acl rmAcl)
        {
            Acl.Perm p = (Acl.Perm)rmAcl.Perms;
            ZkprProtocolMessages.Perms zp = 0;

            if ((p & Acl.Perm.READ) == Acl.Perm.READ)
            {
                zp |= ZkprProtocolMessages.Perms.Read;
            }

            if ((p & Acl.Perm.WRITE) == Acl.Perm.WRITE)
            {
                zp |= ZkprProtocolMessages.Perms.Read;
            }

            if ((p & Acl.Perm.DELETE) == Acl.Perm.DELETE)
            {
                zp |= ZkprProtocolMessages.Perms.Delete;
            }

            if ((p & Acl.Perm.CREATE) == Acl.Perm.CREATE)
            {
                zp |= ZkprProtocolMessages.Perms.Create;
            }

            if ((p & Acl.Perm.ADMIN) == Acl.Perm.ADMIN)
            {
                zp |= ZkprProtocolMessages.Perms.Admin;
            }

            if ((p & Acl.Perm.ALL) == Acl.Perm.ALL)
            {
                zp |= ZkprProtocolMessages.Perms.All;
            }

            Id zkprId = new Id(rmAcl.Id.Scheme, rmAcl.Id.Identifier.Equals("world") ? "anyone" : rmAcl.Id.Identifier);
            Acl zkprAcl = new Acl((int)zp, zkprId);
            return zkprAcl;
        }

        /// <summary>
        /// Serialize <see cref="IStat"/>.
        /// </summary>
        /// <param name="stat"><see cref="IStat"/> to serialize</param>
        private void SerializeStat(IStat stat)
        {
            IStat s = stat;
            if (stat == null)
            {
                s = new Stat() { Czxid = -1 };
            }

            // Note: Order is important because Zkpr assumes an order
            this.binaryWriter.WriteBE(s.Czxid);
            this.binaryWriter.WriteBE(s.Mzxid);
            this.binaryWriter.WriteBE(s.Ctime);
            this.binaryWriter.WriteBE(s.Mtime);
            this.binaryWriter.WriteBE(s.Version);
            this.binaryWriter.WriteBE(s.Cversion);
            this.binaryWriter.WriteBE(s.Aversion);
            this.binaryWriter.WriteBE(0L);  // EphemeralOwner legacy field is set to zero always
            this.binaryWriter.WriteBE(s.DataLength);
            this.binaryWriter.WriteBE(s.NumChildren);
            this.binaryWriter.WriteBE(s.Pzxid);
        }

        /// <summary>
        /// To the byte array.
        /// </summary>
        /// <param name="o">The object to serialize</param>
        /// <returns>Array of bytes that contains the serialized representation of the given object</returns>
        private byte[] ToByteArray(object o)
        {
            if (o == null)
            {
                return NullByteArray;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                // 0 means not null
                ms.WriteByte(0);
                binaryFormatter.Serialize(ms, o);
                ms.Flush();
                return ms.ToArray();
            }
        }
    }
}
