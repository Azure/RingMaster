// <copyright file="Deserializer.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using RingMaster;
    using RingMaster.Data;
    using RingMaster.Requests;
    using static Requests.RequestGetData;

    /// <summary>
    /// Helper class to Deserialize a <see cref="RequestCall"/> or
    /// a <see cref="RequestResponse"/>.
    /// </summary>
    public sealed class Deserializer : IDisposable
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
        /// Initializes a new instance of the <see cref="Deserializer"/> class.
        /// </summary>
        /// <param name="bytes">Bytes that contain serialized data</param>
        /// <param name="byteLength">Length of the bytes</param>
        /// <param name="versionUsed">Version of the serialization format used</param>
        public Deserializer(byte[] bytes, int byteLength, uint versionUsed)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if ((versionUsed < SerializationFormatVersions.MinimumSupportedVersion)
            || (versionUsed > SerializationFormatVersions.MaximumSupportedVersion))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(versionUsed),
                    string.Format("Version must be between {0} and {1}", SerializationFormatVersions.MinimumSupportedVersion, SerializationFormatVersions.MaximumSupportedVersion));
            }

            this.memoryStream = new MemoryStream(bytes, 0, byteLength);
            this.binaryReader = new BinaryReader(this.memoryStream);
            this.serializationVersionUsed = versionUsed;
        }

        /// <summary>
        /// Deserialize a <see cref="RequestCall"/>.
        /// </summary>
        /// <returns>Deserialized <see cref="RequestCall"/></returns>
        public RequestCall DeserializeRequest()
        {
            uint type = this.binaryReader.ReadUInt32();

            if (type != RequestType.RequestCall)
            {
                throw new FormatException("not a request");
            }

            ulong callId = this.binaryReader.ReadUInt64();
            IRingMasterRequest ringMasterRequest;
            this.DeserializeRingMasterRequest(callId, out ringMasterRequest);

            return new RequestCall()
            {
                CallId = callId,
                Request = ringMasterRequest,
            };
        }

        /// <summary>
        /// Deserialize a <see cref="RequestResponse"/>.
        /// </summary>
        /// <returns>Deserialized <see cref="RequestResponse"/></returns>
        public RequestResponse DeserializeResponse()
        {
            uint type = this.binaryReader.ReadUInt32();
            if (type != RequestType.RequestResponse)
            {
                throw new FormatException($"not a RequestResponse, type={type}");
            }

            ulong callId = this.binaryReader.ReadUInt64();
            int resultcode = this.binaryReader.ReadInt32();
            string responsepath = this.DeserializeNullableString();

            Stat stat = this.DeserializeStat();
            object content = this.DeserializeContent();

            return new RequestResponse()
            {
                CallId = callId,
                ResultCode = resultcode,
                Content = content,
                Stat = stat,
                ResponsePath = responsepath,
            };
        }

        /// <summary>
        /// Disposes the object
        /// </summary>
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
        /// Deserialize response content based on content type.
        /// </summary>
        /// <returns>Content object</returns>
        /// <exception cref="System.FormatException">don't understand content type  + contentByte</exception>
        private object DeserializeContent()
        {
            byte contentByte = this.binaryReader.ReadByte();
            ContentType contentType = ContentType.Unknown;

            if (Enum.IsDefined(typeof(ContentType), contentByte))
            {
                contentType = (ContentType)contentByte;
            }

            switch (contentType)
            {
                case ContentType.ListOfString:
                    return this.DeserializeStringList();
                case ContentType.StringArray:
                    return this.DeserializeStringArray();
                case ContentType.String:
                    return this.binaryReader.ReadString();
                case ContentType.ByteArray:
                    return this.DeserializeByteArray();
                case ContentType.AclList:
                    return this.DeserializeAclList();
                case ContentType.OpResultList:
                    return this.DeserializeOpResultList();
                case ContentType.Stat:
                    return this.DeserializeStat();
                case ContentType.Redirect:
                    return this.DeserializeRedirectSuggested();
                case ContentType.AnyObject:
                    {
                        byte[] bytes = this.DeserializeByteArray();
                        return this.FromByteArray<object>(bytes);
                    }

                case ContentType.Request:
                    {
                        IRingMasterRequest ringMasterRequest;
                        this.DeserializeRingMasterRequest(ulong.MaxValue, out ringMasterRequest);
                        return ringMasterRequest;
                    }

                case ContentType.WatcherCall:
                    return this.DeserializeWatcherCall();
                case ContentType.Unknown:
                default:
                    throw new FormatException("don't understand content type " + contentByte);
            }
        }

        /// <summary>
        /// Deserialize <see cref="IRingMasterRequest"/>
        /// </summary>
        /// <param name="callid">Id of the call</param>
        /// <param name="ringMasterRequest">The ring master request.</param>
        /// <exception cref="System.ArgumentException">unknown type  + type</exception>
        private void DeserializeRingMasterRequest(ulong callid, out IRingMasterRequest ringMasterRequest)
        {
            RingMasterRequestType type = (RingMasterRequestType)this.binaryReader.ReadUInt16();

            ulong uid = this.binaryReader.ReadUInt64();
            string path = this.binaryReader.ReadString();

            ISessionAuth sessionAuth = this.DeserializeSessionAuth();
            IOperationOverrides operationOverrides = this.DeserializeOperationOverrides();
            Guid executionQueueId = Guid.Empty;
            int executionQueueTimeoutMilliseconds = 0;

            if (this.serializationVersionUsed >= SerializationFormatVersions.Version17)
            {
                bool hasExecutionQueueData = this.binaryReader.ReadBoolean();

                if (hasExecutionQueueData)
                {
                    executionQueueId = this.binaryReader.ReadGuid();
                    executionQueueTimeoutMilliseconds = this.binaryReader.ReadInt32();
                }
            }

            ulong timeStreamId = 0;
            if (this.serializationVersionUsed >= SerializationFormatVersions.Version21)
            {
                timeStreamId = this.binaryReader.ReadUInt64();
            }

            AbstractRingMasterRequest request = null;
            switch (type)
            {
                case RingMasterRequestType.Init:
                    request = this.DeserializeRequestInit();
                    break;
                case RingMasterRequestType.SetAuth:
                    request = this.DeserializeRequestSetAuth();
                    break;
                case RingMasterRequestType.Create:
                    request = this.DeserializeRequestCreate(uid, path);
                    break;
                case RingMasterRequestType.Move:
                    request = this.DeserializeRequestMove(uid, path);
                    break;
                case RingMasterRequestType.Multi:
                    request = this.DeserializeRequestMulti(callid, uid);
                    break;
                case RingMasterRequestType.Batch:
                    request = this.DeserializeRequestBatch(callid, uid);
                    break;
                case RingMasterRequestType.Delete:
                    request = this.DeserializeRequestDelete(uid, path);
                    break;
                case RingMasterRequestType.Check:
                    request = this.DeserializeRequestCheck(uid, path);
                    break;
                case RingMasterRequestType.Sync:
                    request = this.DeserializeRequestSync(uid, path);
                    break;
                case RingMasterRequestType.Exists:
                    request = this.DeserializeRequestExists(uid, path);
                    break;
                case RingMasterRequestType.GetAcl:
                    request = this.DeserializeRequestGetAcl(uid, path);
                    break;
                case RingMasterRequestType.GetData:
                    request = this.DeserializeRequestGetData(uid, path);
                    break;
                case RingMasterRequestType.GetChildren:
                    request = this.DeserializeRequestGetChildren(uid, path);
                    break;
                case RingMasterRequestType.SetData:
                    request = this.DeserializeRequestSetData(uid, path);
                    break;
                case RingMasterRequestType.SetAcl:
                    request = this.DeserializeRequestSetAcl(uid, path);
                    break;

                case RingMasterRequestType.None:
                default:
                    throw new ArgumentException("unknown type " + type);
            }

            if (request != null)
            {
                request.TimeStreamId = timeStreamId;
                request.Auth = sessionAuth;
                request.Overrides = operationOverrides;

                request.ExecutionQueueId = executionQueueId;
                request.ExecutionQueueTimeoutMillis = executionQueueTimeoutMilliseconds;
            }

            ringMasterRequest = request;
        }

        /// <summary>
        /// Deserialize a list of <see cref="Acl"/>s.
        /// </summary>
        /// <returns>List of <see cref="Acl"/>s</returns>
        private List<Acl> DeserializeAclList()
        {
            bool isNull = this.binaryReader.ReadBoolean();
            if (isNull)
            {
                return null;
            }
            else
            {
                var acls = new List<Acl>();

                int num = this.binaryReader.ReadInt32();
                for (int i = 0; i < num; i++)
                {
                    Acl acl = this.DeserializeAcl();
                    acls.Add(acl);
                }

                return acls;
            }
        }

        /// <summary>
        /// Deserialize <see cref="ISessionAuth"/>.
        /// </summary>
        /// <returns><see cref="ISessionAuth"/> object</returns>
        private ISessionAuth DeserializeSessionAuth()
        {
            bool isNull = this.binaryReader.ReadBoolean();

            if (isNull)
            {
                return null;
            }

            SessionAuth sessionAuth = new SessionAuth();

            sessionAuth.IsSuperSession = this.binaryReader.ReadBoolean();
            sessionAuth.ClientIP = this.binaryReader.ReadNullableString();
            sessionAuth.ClientDigest = this.binaryReader.ReadNullableString();
            sessionAuth.ClientIdentity = this.binaryReader.ReadNullableString();

            return sessionAuth;
        }

        /// <summary>
        /// Deserialize <see cref="IOperationOverrides"/>.
        /// </summary>
        /// <returns><see cref="IOperationOverrides"/></returns>
        private IOperationOverrides DeserializeOperationOverrides()
        {
            bool isNull = this.binaryReader.ReadBoolean();

            if (isNull)
            {
                return null;
            }

            IOperationOverrides operationOverrides = new OperationOverrides();

            operationOverrides.TxId = this.binaryReader.ReadUInt64();
            operationOverrides.TxTime = this.binaryReader.ReadUInt64();

            return operationOverrides;
        }

        /// <summary>
        /// Deserialize list of <see cref="OpResult"/>s.
        /// </summary>
        /// <returns>List of <see cref="OpResult"/>s</returns>
        private List<OpResult> DeserializeOpResultList()
        {
            bool isNull = this.binaryReader.ReadBoolean();
            if (isNull)
            {
                return null;
            }
            else
            {
                var operationResults = new List<OpResult>();

                int num = this.binaryReader.ReadInt32();
                for (int i = 0; i < num; i++)
                {
                    OpResult operationResult = this.DeserializeOpResult();
                    operationResults.Add(operationResult);
                }

                return operationResults;
            }
        }

        /// <summary>
        /// Deserialize a string array.
        /// </summary>
        /// <returns>Array of strings</returns>
        private string[] DeserializeStringArray()
        {
            int numberOfElements = this.binaryReader.ReadInt32();
            var stringArray = new string[numberOfElements];
            for (int i = 0; i < numberOfElements; i++)
            {
                stringArray[i] = this.binaryReader.ReadString();
            }

            return stringArray;
        }

        /// <summary>
        /// Deserialize a string list.
        /// </summary>
        /// <returns>A list of strings</returns>
        private List<string> DeserializeStringList()
        {
            int numberOfElements = this.binaryReader.ReadInt32();
            var stringList = new List<string>();
            for (int i = 0; i < numberOfElements; i++)
            {
                stringList.Add(this.binaryReader.ReadString());
            }

            return stringList;
        }

        /// <summary>
        /// Deserialize a <see cref="WatcherCall"/>
        /// </summary>
        /// <returns>A <see cref="WatcherCall"/></returns>
        private WatcherCall DeserializeWatcherCall()
        {
            WatcherCall watcherCall = new WatcherCall();
            watcherCall.WatcherId = this.binaryReader.ReadUInt64();

            if (this.serializationVersionUsed >= SerializationFormatVersions.Version23)
            {
                watcherCall.Kind = (WatcherKind)this.binaryReader.ReadByte();
            }
            else
            {
                watcherCall.Kind = this.binaryReader.ReadBoolean() ? WatcherKind.OneUse : default(WatcherKind);
            }

            bool nullwatcher = this.binaryReader.ReadBoolean();
            if (!nullwatcher)
            {
                WatchedEvent.WatchedEventType type = (WatchedEvent.WatchedEventType)this.binaryReader.ReadInt32();
                WatchedEvent.WatchedEventKeeperState state = (WatchedEvent.WatchedEventKeeperState)this.binaryReader.ReadInt32();
                string path = this.binaryReader.ReadString();

                byte[] data = null;
                IStat stat = null;
                if (this.serializationVersionUsed >= SerializationFormatVersions.Version23)
                {
                    data = this.DeserializeByteArray();
                    stat = this.DeserializeStat();
                }

                watcherCall.WatcherEvt = new WatchedEvent(type, state, path, data, stat);
            }
            else
            {
                watcherCall.WatcherEvt = null;
            }

            return watcherCall;
        }

        /// <summary>
        /// Deserialize an <see cref="Acl"/>.
        /// </summary>
        /// <returns>An <see cref="Acl"/></returns>
        private Acl DeserializeAcl()
        {
            bool isNull = this.binaryReader.ReadBoolean();
            if (isNull)
            {
                return null;
            }
            else
            {
                string scheme = this.binaryReader.ReadString();
                string id = this.binaryReader.ReadString();
                int perms = this.binaryReader.ReadInt32();

                return new Acl(perms, new Id(scheme, id));
            }
        }

        /// <summary>
        /// Deserialize an <see cref="OpResult"/>.
        /// </summary>
        /// <returns>An <see cref="OpResult"/></returns>
        private OpResult DeserializeOpResult()
        {
            OpCode opcode = (OpCode)this.binaryReader.ReadInt32();

            switch (opcode)
            {
                case OpCode.Check:
                    {
                        return new OpResult.CheckResult();
                    }

                case OpCode.GetData:
                    {
                        Stat stat = this.DeserializeStat();
                        byte[] bytes = this.DeserializeByteArray();
                        string path = string.Empty;
                        if (this.serializationVersionUsed >= SerializationFormatVersions.Version14)
                        {
                            path = this.binaryReader.ReadString();
                        }

                        return new OpResult.GetDataResult(stat, bytes, path);
                    }

                case OpCode.Delete:
                    {
                        return new OpResult.DeleteResult();
                    }

                case OpCode.Create:
                    {
                        Stat stat = this.DeserializeStat();
                        string path;

                        if (this.serializationVersionUsed >= SerializationFormatVersions.Version22)
                        {
                            path = this.DeserializeNullableString();
                        }
                        else
                        {
                            path = this.binaryReader.ReadString();
                        }

                        return new OpResult.CreateResult(stat, path);
                    }

                case OpCode.Move:
                    {
                        Stat stat = this.DeserializeStat();
                        string dstpath = this.binaryReader.ReadString();
                        return new OpResult.MoveResult(stat, dstpath);
                    }

                case OpCode.Error:
                    {
                        int err = this.binaryReader.ReadInt32();
                        return new OpResult.ErrorResult(err);
                    }

                case OpCode.SetData:
                    {
                        Stat stat = this.DeserializeStat();
                        return new OpResult.SetDataResult(stat);
                    }

                case OpCode.SetACL:
                    {
                        Stat stat = this.DeserializeStat();
                        return new OpResult.SetAclResult(stat);
                    }

                case OpCode.Multi:
                    {
                        List<OpResult> operationResults = this.DeserializeOpResultList();
                        return new OpResult.RunResult(operationResults);
                    }
            }

            throw new FormatException("bad byte array. unkown opcode " + opcode);
        }

        /// <summary>
        /// Deserialize <see cref="IWatcher"/>
        /// </summary>
        /// <returns>The deserialized watcher</returns>
        private IWatcher DeserializeWatcher()
        {
            ulong id = this.binaryReader.ReadUInt64();
            var kind = (WatcherKind)this.binaryReader.ReadByte();
            if (id != 0)
            {
                return new Watcher(id, kind);
            }

            return null;
        }

        /// <summary>
        /// Deserialize a byte array.
        /// </summary>
        /// <returns>A byte array</returns>
        private byte[] DeserializeByteArray()
        {
            bool isNull = this.binaryReader.ReadBoolean();
            if (isNull)
            {
                return null;
            }
            else
            {
                int length = this.binaryReader.ReadInt32();
                if (length == 0)
                {
                    return new byte[0];
                }
                else
                {
                    return this.binaryReader.ReadBytes(length);
                }
            }
        }

        /// <summary>
        /// Deserialize <see cref="RedirectSuggested"/>.
        /// </summary>
        /// <returns>A <see cref="RedirectSuggested"/> object</returns>
        private RedirectSuggested DeserializeRedirectSuggested()
        {
            bool isNull = this.binaryReader.ReadBoolean();

            if (isNull)
            {
                return null;
            }

            return new RedirectSuggested()
            {
                SuggestedConnectionString = this.binaryReader.ReadNullableString(),
            };
        }

        /// <summary>
        /// Deserialize <see cref="IGetDataOptionArgument"/>.
        /// </summary>
        /// <returns>A <see cref="IGetDataOptionArgument"/> object</returns>
        private RequestGetData.IGetDataOptionArgument DeserializeGetDataOptionArgument()
        {
            RequestGetData.IGetDataOptionArgument optionArgument;
            bool isNull = this.binaryReader.ReadBoolean();
            if (isNull)
            {
                optionArgument = null;
            }
            else
            {
                RequestGetData.GetDataOptions options = (RequestGetData.GetDataOptions)this.binaryReader.ReadByte();

                if (options == RequestGetData.GetDataOptions.FaultbackOnParentDataWithMatch)
                {
                    var condition = (RequestGetData.GetDataOptionArgumentForMatch.Comparison)this.binaryReader.ReadByte();
                    int pos = this.binaryReader.ReadInt32();
                    byte[] bytes = this.DeserializeByteArray();
                    optionArgument = new RequestGetData.GetDataOptionArgumentForMatch(bytes, pos, condition);
                }
                else
                {
                    optionArgument = null;
                }
            }

            return optionArgument;
        }

        /// <summary>
        /// Deserialize <see cref="Stat"/>.
        /// </summary>
        /// <returns>A <see cref="Stat"/></returns>
        private Stat DeserializeStat()
        {
            bool isNull = this.binaryReader.ReadBoolean();

            if (isNull)
            {
                return null;
            }

            var stat = new Stat();
            stat.Aversion = this.binaryReader.ReadInt32();
            stat.Cversion = this.binaryReader.ReadInt32();
            stat.Version = this.binaryReader.ReadInt32();
            stat.NumChildren = this.binaryReader.ReadInt32();
            stat.DataLength = this.binaryReader.ReadInt32();
            stat.EphemeralOwner = this.binaryReader.ReadInt64();
            stat.Czxid = this.binaryReader.ReadInt64();
            stat.Mzxid = this.binaryReader.ReadInt64();
            stat.Pzxid = this.binaryReader.ReadInt64();
            stat.Ctime = this.binaryReader.ReadInt64();
            stat.Mtime = this.binaryReader.ReadInt64();

            return stat;
        }

        /// <summary>
        /// Deserialize <see cref="RequestGetChildren"/>
        /// </summary>
        /// <param name="uid"><c>uid</c> associated with the request</param>
        /// <param name="path">Path associated with the request</param>
        /// <returns>The deserialized request</returns>
        private RequestGetChildren DeserializeRequestGetChildren(ulong uid, string path)
        {
            IWatcher watcher = this.DeserializeWatcher();
            string retrievalCondition = null;
            if (this.serializationVersionUsed >= SerializationFormatVersions.Version2)
            {
                if (this.binaryReader.ReadBoolean())
                {
                    retrievalCondition = this.binaryReader.ReadString();
                }
            }

            return new RequestGetChildren(path, watcher, retrievalCondition, uid);
        }

        /// <summary>
        /// Deserialize <see cref="RequestGetData"/>
        /// </summary>
        /// <param name="uid"><c>uid</c> associated with the request</param>
        /// <param name="path">Path associated with the request</param>
        /// <returns>The deserialized request</returns>
        private RequestGetData DeserializeRequestGetData(ulong uid, string path)
        {
            IWatcher watcher = this.DeserializeWatcher();

            RequestGetData.GetDataOptions options = RequestGetData.GetDataOptions.None;

            byte readByte = this.binaryReader.ReadByte();
            options = (RequestGetData.GetDataOptions)readByte;

            RequestGetData.IGetDataOptionArgument optionArgument = null;
            if (this.serializationVersionUsed >= SerializationFormatVersions.Version14)
            {
                optionArgument = this.DeserializeGetDataOptionArgument();
            }

            if (options == RequestGetData.GetDataOptions.FaultbackOnParentData)
            {
                // If the option was Faultback on parent data, we will use an argumentformatch to simulate it.
                optionArgument = new RequestGetData.GetDataOptionArgumentForMatch(null, 0, RequestGetData.GetDataOptionArgumentForMatch.Comparison.Different);
            }

            return new RequestGetData(path, options, optionArgument, watcher, uid);
        }

        /// <summary>
        /// Deserialize <see cref="RequestSetData"/>.
        /// </summary>
        /// <param name="uid"><c>uid</c> associated with the request</param>
        /// <param name="path">Path associated with the request</param>
        /// <returns>The deserialized request</returns>
        private RequestSetData DeserializeRequestSetData(ulong uid, string path)
        {
            int version = this.binaryReader.ReadInt32();
            byte[] bytes = this.DeserializeByteArray();
            bool isdatacommand = false;

            isdatacommand = this.binaryReader.ReadBoolean();

            return new RequestSetData(path, bytes, version, isdatacommand, uid);
        }

        /// <summary>
        /// Deserialize <see cref="RequestSetAcl"/>.
        /// </summary>
        /// <param name="uid"><c>uid</c> associated with the request</param>
        /// <param name="path">Path associated with the request</param>
        /// <returns>The deserialized request</returns>
        private RequestSetAcl DeserializeRequestSetAcl(ulong uid, string path)
        {
            int version = this.binaryReader.ReadInt32();
            List<Acl> acl = this.DeserializeAclList();

            return new RequestSetAcl(path, acl, version, uid);
        }

        /// <summary>
        /// Deserialize <see cref="RequestGetAcl"/>.
        /// </summary>
        /// <param name="uid"><c>uid</c> associated with the request</param>
        /// <param name="path">Path associated with the request</param>
        /// <returns>The deserialized request</returns>
        private RequestGetAcl DeserializeRequestGetAcl(ulong uid, string path)
        {
            Stat stat = this.DeserializeStat();

            return new RequestGetAcl(path, stat, uid);
        }

        /// <summary>
        /// Deserialize <see cref="RequestExists"/>.
        /// </summary>
        /// <param name="uid"><c>uid</c> associated with the request</param>
        /// <param name="path">Path associated with the request</param>
        /// <returns>The deserialized request</returns>
        private RequestExists DeserializeRequestExists(ulong uid, string path)
        {
            IWatcher watcher = this.DeserializeWatcher();

            return new RequestExists(path, watcher, uid);
        }

        /// <summary>
        /// Deserialize <see cref="RequestSync"/>
        /// </summary>
        /// <param name="uid"><c>uid</c> associated with the request</param>
        /// <param name="path">Path associated with the request</param>
        /// <returns>The deserialized request</returns>
        private RequestSync DeserializeRequestSync(ulong uid, string path)
        {
            return new RequestSync(path, uid);
        }

        /// <summary>
        /// Deserialize <see cref="RequestCheck"/>.
        /// </summary>
        /// <param name="uid"><c>uid</c> associated with the request</param>
        /// <param name="path">Path associated with the request</param>
        /// <returns>The deserialized request</returns>
        private RequestCheck DeserializeRequestCheck(ulong uid, string path)
        {
            int version = this.binaryReader.ReadInt32();
            int cversion = -1;
            int aversion = -1;

            Guid guid = Guid.Empty;
            RequestCheck.UniqueIncarnationIdType kind = (RequestCheck.UniqueIncarnationIdType)this.binaryReader.ReadByte();

            if (kind != RequestCheck.UniqueIncarnationIdType.None)
            {
                guid = this.binaryReader.ReadGuid();
            }

            if (this.serializationVersionUsed >= SerializationFormatVersions.Version16)
            {
                cversion = this.binaryReader.ReadInt32();
                aversion = this.binaryReader.ReadInt32();
            }

            return new RequestCheck(path, version, cversion, aversion, guid, kind, uid);
        }

        /// <summary>
        /// Deserialize <see cref="RequestDelete"/>.
        /// </summary>
        /// <param name="uid"><c>uid</c> associated with the request</param>
        /// <param name="path">Path associated with the request</param>
        /// <returns>The deserialized request</returns>
        private RequestDelete DeserializeRequestDelete(ulong uid, string path)
        {
            int version = this.binaryReader.ReadInt32();

            DeleteMode mode = DeleteMode.None;

            if (this.serializationVersionUsed >= SerializationFormatVersions.Version18)
            {
                ushort moderaw = this.binaryReader.ReadUInt16();

                mode = (DeleteMode)moderaw;
            }
            else
            {
                bool iscascade = this.binaryReader.ReadBoolean();

                if (iscascade)
                {
                    mode |= DeleteMode.CascadeDelete;
                }
            }

            return new RequestDelete(path, version, mode, uid);
        }

        /// <summary>
        /// Deserialize <see cref="RequestInit"/>.
        /// </summary>
        /// <returns>The deserialized request</returns>
        private RequestInit DeserializeRequestInit()
        {
            ulong sessionId = this.binaryReader.ReadUInt64();
            string sessionPwd = this.binaryReader.ReadString();
            bool readonlyInterfaceRequiresLocks = this.binaryReader.ReadBoolean();
            RequestInit.RedirectionPolicy red = (RequestInit.RedirectionPolicy)this.binaryReader.ReadByte();

            return new RequestInit(sessionId, sessionPwd, readonlyInterfaceRequiresLocks, red);
        }

        /// <summary>
        /// Deserialize <see cref="RequestSetAuth"/>.
        /// </summary>
        /// <returns>The deserialized request</returns>
        private RequestSetAuth DeserializeRequestSetAuth()
        {
            string clientId = this.binaryReader.ReadString();

            return new RequestSetAuth(clientId);
        }

        /// <summary>
        /// Deserialize <see cref="RequestCreate"/>.
        /// </summary>
        /// <param name="uid"><c>uid</c> associated with the request</param>
        /// <param name="path">Path associated with the request</param>
        /// <returns>The deserialized request</returns>
        private RequestCreate DeserializeRequestCreate(ulong uid, string path)
        {
            CreateMode createMode = (CreateMode)this.binaryReader.ReadUInt16();

            byte[] bytes = this.DeserializeByteArray();
            List<Acl> acls = this.DeserializeAclList();

            return new RequestCreate(path, bytes, acls, createMode, uid);
        }

        /// <summary>
        /// Deserialize <see cref="RequestMove"/>.
        /// </summary>
        /// <param name="uid"><c>uid</c> associated with the request</param>
        /// <param name="path">Path associated with the request</param>
        /// <returns>The deserialized request</returns>
        private RequestMove DeserializeRequestMove(ulong uid, string path)
        {
            if (this.serializationVersionUsed < SerializationFormatVersions.Version19)
            {
                throw new NotImplementedException(string.Format("The channel is in version {0} which doesn't support Move", this.serializationVersionUsed));
            }

            int version = this.binaryReader.ReadInt32();
            string pathDst = this.binaryReader.ReadString();
            MoveMode mode = (MoveMode)this.binaryReader.ReadUInt16();

            return new RequestMove(path, version, pathDst, mode, uid);
        }

        /// <summary>
        /// Deserialize <see cref="RequestMulti"/>.
        /// </summary>
        /// <param name="callid">Id of the call</param>
        /// <param name="uid"><c>uid</c> associated with the request</param>
        /// <returns>The deserialized request</returns>
        private RequestMulti DeserializeRequestMulti(ulong callid, ulong uid)
        {
            ushort numReq = this.binaryReader.ReadUInt16();

            IRingMasterRequest[] req = new IRingMasterRequest[numReq];

            for (int i = 0; i < req.Length; i++)
            {
                this.DeserializeRingMasterRequest(callid, out req[i]);
            }

            bool completeSynchronously = this.binaryReader.ReadBoolean();

            string scheduledName = null;

            if (this.serializationVersionUsed >= SerializationFormatVersions.Version20)
            {
                scheduledName = this.binaryReader.ReadNullableString();
            }

            return new RequestMulti(req, completeSynchronously, scheduledName, uid);
        }

        /// <summary>
        /// Deserialize <see cref="RequestBatch"/>.
        /// </summary>
        /// <param name="callid">Id of the call</param>
        /// <param name="uid"><c>uid</c> associated with the request</param>
        /// <returns>The deserialized request</returns>
        private RequestBatch DeserializeRequestBatch(ulong callid, ulong uid)
        {
            ushort numReq = this.binaryReader.ReadUInt16();

            IRingMasterRequest[] req = new IRingMasterRequest[numReq];

            for (int i = 0; i < req.Length; i++)
            {
                this.DeserializeRingMasterRequest(callid, out req[i]);
            }

            bool completeSynchronously = this.binaryReader.ReadBoolean();

            return new RequestBatch(req, completeSynchronously, uid);
        }

        /// <summary>
        /// Deserializes an object from the given byte array.
        /// </summary>
        /// <typeparam name="T">Type of the object to deserialize</typeparam>
        /// <param name="bytes">The bytes from which the object must be deserialize</param>
        /// <returns>The deserialized object</returns>
        private T FromByteArray<T>(byte[] bytes)
            where T : class
        {
            if (bytes == null || (bytes.Length == 1 && bytes[0] == 1))
            {
                return null;
            }

            using (MemoryStream ms = new MemoryStream(bytes))
            {
                int isNull = ms.ReadByte();
                Debug.Assert(isNull == 0, "IsNull is 0");
                return (T)this.binaryFormatter.Deserialize(ms);
            }
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
            /// <param name="kind">Kind of the watcher</param>
            public Watcher(ulong id, WatcherKind kind)
            {
                this.Id = id;
                this.Kind = kind;
            }

            /// <summary>
            /// Gets the Id of the watcher.
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
