// <copyright file="Serializer.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Helper class to Serialize a <see cref="RequestCall"/> or <see cref="RequestResponse"/>.
    /// </summary>
    public sealed class Serializer : IDisposable
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
        /// Initializes a new instance of the <see cref="Serializer"/> class.
        /// </summary>
        /// <param name="versionToUse">Version of the serialization format to use</param>
        public Serializer(uint versionToUse)
        {
            if ((versionToUse < SerializationFormatVersions.MinimumSupportedVersion)
            || (versionToUse > SerializationFormatVersions.MaximumSupportedVersion))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(versionToUse),
                    $"Version must be between {SerializationFormatVersions.MinimumSupportedVersion} and {SerializationFormatVersions.MaximumSupportedVersion} versionToUse={versionToUse}");
            }

            this.versionToUse = versionToUse;
            this.memoryStream = new MemoryStream();
            this.binaryWriter = new BinaryWriter(this.memoryStream);
        }

        /// <summary>
        /// Serialize a <see cref="RequestCall"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        public void SerializeRequest(RequestCall request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            this.binaryWriter.Write((uint)RequestType.RequestCall);
            this.binaryWriter.Write((ulong)request.CallId);
            this.SerializeRingmasterRequest(request.Request);
        }

        /// <summary>
        /// Serialize a <see cref="RequestResponse"/>.
        /// </summary>
        /// <param name="response">Response to serialize</param>
        public void SerializeResponse(RequestResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }

            this.binaryWriter.Write((uint)RequestType.RequestResponse);

            this.binaryWriter.Write((ulong)response.CallId);
            this.binaryWriter.Write((int)response.ResultCode);

            this.SerializeNullableString(response.ResponsePath);

            this.SerializeStat(response.Stat);

            this.SerializeContent(response.Content);
        }

        /// <summary>
        /// Get a byte array that contains the serialized data.
        /// </summary>
        /// <returns>Byte array that contains serialized data</returns>
        public byte[] GetBytes()
        {
            return this.memoryStream.ToArray();
        }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public void Dispose()
        {
            this.memoryStream.Dispose();
            this.binaryWriter.Dispose();
        }

        /// <summary>
        /// Serializes the string, considering it may be null
        /// </summary>
        /// <param name="someString">the string (or null) to be serialized</param>
        private void SerializeNullableString(string someString)
        {
            bool isNull = someString == null;
            this.binaryWriter.Write((bool)isNull);
            if (!isNull)
            {
                this.binaryWriter.Write((string)someString);
            }
        }

        /// <summary>
        /// Serialize a <see cref="IRingMasterRequest"/>
        /// </summary>
        /// <param name="ringMasterRequest">Request to serialize</param>
        private void SerializeRingmasterRequest(IRingMasterRequest ringMasterRequest)
        {
            this.SerializeRequestPreamble(ringMasterRequest);

            switch (ringMasterRequest.RequestType)
            {
                case RingMasterRequestType.Init:
                    this.SerializeRequestBody((RequestInit)ringMasterRequest);
                    break;
                case RingMasterRequestType.SetAuth:
                    this.SerializeRequestSetAuth((RequestSetAuth)ringMasterRequest);
                    break;
                case RingMasterRequestType.Create:
                    this.SerializeRequestCreate((RequestCreate)ringMasterRequest);
                    break;
                case RingMasterRequestType.Multi:
                    this.SerializeRequestMulti((RequestMulti)ringMasterRequest);
                    break;
                case RingMasterRequestType.Move:
                    this.SerializeRequestMove((RequestMove)ringMasterRequest);
                    break;
                case RingMasterRequestType.Batch:
                    this.SerializeRequestBatch((RequestBatch)ringMasterRequest);
                    break;
                case RingMasterRequestType.Delete:
                    this.SerializeRequestDelete((RequestDelete)ringMasterRequest);
                    break;
                case RingMasterRequestType.Check:
                    this.SerializeRequestCheck((RequestCheck)ringMasterRequest);
                    break;
                case RingMasterRequestType.Sync:
                    // Sync request has no body, so there is nothing to serialize.
                    break;
                case RingMasterRequestType.Exists:
                    this.SerializeRequestExists((RequestExists)ringMasterRequest);
                    break;
                case RingMasterRequestType.GetAcl:
                    this.SerializeRequestGetAcl((RequestGetAcl)ringMasterRequest);
                    break;
                case RingMasterRequestType.GetData:
                    this.SerializeRequestGetData((RequestGetData)ringMasterRequest);
                    break;
                case RingMasterRequestType.GetChildren:
                    this.SerializeRequestGetChildren((RequestGetChildren)ringMasterRequest);
                    break;
                case RingMasterRequestType.SetAcl:
                    this.SerializeRequestSetAcl((RequestSetAcl)ringMasterRequest);
                    break;
                case RingMasterRequestType.SetData:
                    this.SerializeRequestSetData((RequestSetData)ringMasterRequest);
                    break;

                case RingMasterRequestType.None:
                default:
                    throw new ArgumentException("unknown type " + ringMasterRequest.GetType());
            }
        }

        /// <summary>
        /// Serialize a request preamble.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        private void SerializeRequestPreamble(IRingMasterRequest request)
        {
            this.binaryWriter.Write((ushort)request.RequestType);
            this.binaryWriter.Write((ulong)request.Uid);
            this.binaryWriter.Write((string)request.Path);
            this.SerializeSessionAuth(request.Auth);
            this.SerializeOperationOverrides(request.Overrides);

            if (this.versionToUse >= SerializationFormatVersions.Version17)
            {
                bool hasExecutionQueueData = request.ExecutionQueueId != Guid.Empty;
                this.binaryWriter.Write((bool)hasExecutionQueueData);
                if (hasExecutionQueueData)
                {
                    this.binaryWriter.Write((Guid)request.ExecutionQueueId);
                    this.binaryWriter.Write((int)request.ExecutionQueueTimeoutMillis);
                }
            }

            if (this.versionToUse >= SerializationFormatVersions.Version21)
            {
                this.binaryWriter.Write((ulong)request.TimeStreamId);
            }
        }

        /// <summary>
        /// Serialize <see cref="RequestGetChildren"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        private void SerializeRequestGetChildren(RequestGetChildren request)
        {
            this.SerializeWatcher(request.Watcher);
            bool hasValue = request.RetrievalCondition != null;
            this.binaryWriter.Write((bool)hasValue);
            if (hasValue)
            {
                this.binaryWriter.Write((string)request.RetrievalCondition);
            }
        }

        /// <summary>
        /// Serialize <see cref="RequestGetData"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        private void SerializeRequestGetData(RequestGetData request)
        {
            this.SerializeWatcher(request.Watcher);
            this.binaryWriter.Write((byte)request.Options);
            if (this.versionToUse >= SerializationFormatVersions.Version14)
            {
                this.SerializeGetDataOptionArgument(request.OptionArgument);
            }
        }

        /// <summary>
        /// Serialize <see cref="RequestGetAcl"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        private void SerializeRequestGetAcl(RequestGetAcl request)
        {
            this.SerializeStat(request.Stat);
        }

        /// <summary>
        /// Serialize <see cref="RequestExists"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        private void SerializeRequestExists(RequestExists request)
        {
            this.SerializeWatcher(request.Watcher);
        }

        /// <summary>
        /// Serialize <see cref="RequestDelete"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        private void SerializeRequestDelete(RequestDelete request)
        {
            this.binaryWriter.Write(request.Version);

            if (this.versionToUse >= SerializationFormatVersions.Version18)
            {
                this.binaryWriter.Write((ushort)request.DeleteMode);
            }
            else
            {
                if ((request.DeleteMode & ~DeleteMode.CascadeDelete) != DeleteMode.None)
                {
                    throw new NotSupportedException("Marshaller protocol version " + this.versionToUse + " doesn't support DeleteMode other than CascadeDelete");
                }

                this.binaryWriter.Write(request.IsCascade);
            }
        }

        /// <summary>
        /// Serialize <see cref="RequestDelete"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        private void SerializeRequestCheck(RequestCheck request)
        {
            this.binaryWriter.Write(request.Version);
            this.binaryWriter.Write((byte)request.UniqueIncarnationIdKind);
            if (request.UniqueIncarnationIdKind != RequestCheck.UniqueIncarnationIdType.None)
            {
                this.binaryWriter.Write((Guid)request.UniqueIncarnationId);
            }

            if (this.versionToUse >= SerializationFormatVersions.Version16)
            {
                this.binaryWriter.Write(request.CVersion);
                this.binaryWriter.Write(request.AVersion);
            }
        }

        /// <summary>
        /// Serialize <see cref="RequestCreate"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        private void SerializeRequestCreate(RequestCreate request)
        {
            this.binaryWriter.Write((ushort)request.CreateMode);
            this.SerializeData((byte[])request.Data);
            this.SerializeAclList(request.Acl);
        }

        /// <summary>
        /// Serialize <see cref="RequestMove"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        private void SerializeRequestMove(RequestMove request)
        {
            if (this.versionToUse < SerializationFormatVersions.Version19)
            {
                throw new NotImplementedException(string.Format("The channel is in version {0} which doesn't support Move", this.versionToUse));
            }

            this.binaryWriter.Write((int)request.Version);
            this.binaryWriter.Write((string)request.PathDst);
            this.binaryWriter.Write((ushort)request.MoveMode);
        }

        /// <summary>
        /// Serialize <see cref="RequestMulti"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        private void SerializeRequestMulti(RequestMulti request)
        {
            this.binaryWriter.Write((ushort)request.Requests.Count);
            foreach (IRingMasterRequest req in request.Requests)
            {
                this.SerializeRingmasterRequest(req);
            }

            this.binaryWriter.Write((bool)request.CompleteSynchronously);

            if (this.versionToUse >= SerializationFormatVersions.Version20)
            {
                this.binaryWriter.WriteNullableString(request.ScheduledName);
            }
        }

        /// <summary>
        /// Serialize <see cref="RequestBatch"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        private void SerializeRequestBatch(RequestBatch request)
        {
            if (this.versionToUse < SerializationFormatVersions.Version15)
            {
                throw new ArgumentException("This session doesn't support Batch Requests");
            }

            this.binaryWriter.Write((ushort)request.Requests.Count);
            foreach (IRingMasterRequest req in request.Requests)
            {
                this.SerializeRingmasterRequest(req);
            }

            this.binaryWriter.Write((bool)request.CompleteSynchronously);
        }

        /// <summary>
        /// Serialize <see cref="RequestInit"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        private void SerializeRequestBody(RequestInit request)
        {
            this.binaryWriter.Write((ulong)request.SessionId);
            this.binaryWriter.Write((string)request.SessionPwd);
            this.binaryWriter.Write((bool)request.ROInterfaceRequiresLocks);
            this.binaryWriter.Write((byte)request.Redirection);
        }

        /// <summary>
        /// Serialize <see cref="RequestSetAuth"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        private void SerializeRequestSetAuth(RequestSetAuth request)
        {
            this.binaryWriter.Write((string)request.ClientId);
        }

        /// <summary>
        /// Serialize <see cref="RequestSetData"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        private void SerializeRequestSetData(RequestSetData request)
        {
            this.binaryWriter.Write((int)request.Version);
            this.SerializeData((byte[])request.Data);
            this.binaryWriter.Write((bool)request.IsDataCommand);
        }

        /// <summary>
        /// Serialize <see cref="RequestSetAcl"/>.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        private void SerializeRequestSetAcl(RequestSetAcl request)
        {
            this.binaryWriter.Write((int)request.Version);
            this.SerializeAclList(request.Acl);
        }

        /// <summary>
        /// Serialize a list of <see cref="Acl"/>s.
        /// </summary>
        /// <param name="acls">List of <see cref="Acl"/>s to serialize</param>
        private void SerializeAclList(IReadOnlyList<Acl> acls)
        {
            bool isNull = acls == null;
            this.binaryWriter.Write((bool)isNull);
            if (!isNull)
            {
                this.binaryWriter.Write((int)acls.Count);
                foreach (Acl acl in acls)
                {
                    this.SerializeAcl(acl);
                }
            }
        }

        /// <summary>
        /// Serialize <see cref="ISessionAuth"/>.
        /// </summary>
        /// <param name="sessionAuth"><see cref="ISessionAuth"/> to serialize</param>
        private void SerializeSessionAuth(ISessionAuth sessionAuth)
        {
            bool isNull = sessionAuth == null;
            this.binaryWriter.Write((bool)isNull);

            if (!isNull)
            {
                this.binaryWriter.Write((bool)sessionAuth.IsSuperSession);
                this.binaryWriter.WriteNullableString(sessionAuth.ClientIP);
                this.binaryWriter.WriteNullableString(sessionAuth.ClientDigest);
                this.binaryWriter.WriteNullableString(sessionAuth.ClientIdentity);
            }
        }

        /// <summary>
        /// Serialize <see cref="IOperationOverrides"/>.
        /// </summary>
        /// <param name="operationOverrides"><see cref="IOperationOverrides"/> to serialize</param>
        private void SerializeOperationOverrides(IOperationOverrides operationOverrides)
        {
            bool isNull = operationOverrides == null;
            this.binaryWriter.Write((bool)isNull);

            if (!isNull)
            {
                this.binaryWriter.Write((ulong)operationOverrides.TxId);
                this.binaryWriter.Write((ulong)operationOverrides.TxTime);
            }
        }

        /// <summary>
        /// Serialize <see cref="RequestGetData.IGetDataOptionArgument"/>.
        /// </summary>
        /// <param name="optionArgument"><see cref="RequestGetData.IGetDataOptionArgument"/> to serialize</param>
        private void SerializeGetDataOptionArgument(RequestGetData.IGetDataOptionArgument optionArgument)
        {
            bool isNull = optionArgument == null;
            this.binaryWriter.Write((bool)isNull);

            if (!isNull)
            {
                this.binaryWriter.Write((byte)optionArgument.Option);
                if (optionArgument.Option == RequestGetData.GetDataOptions.FaultbackOnParentDataWithMatch)
                {
                    var m = (RequestGetData.GetDataOptionArgumentForMatch)optionArgument;
                    this.binaryWriter.Write((byte)m.Condition);
                    this.binaryWriter.Write((int)m.Position);
                    this.SerializeData((byte[])m.Bytes);
                }
            }
        }

        /// <summary>
        /// Serialize a list of <see cref="OpResult"/>s.
        /// </summary>
        /// <param name="operationResults">List of <see cref="OpResult"/>s to serialize</param>
        private void SerializeOpResultList(IReadOnlyList<OpResult> operationResults)
        {
            bool isNull = operationResults == null;
            this.binaryWriter.Write((bool)isNull);
            if (!isNull)
            {
                this.binaryWriter.Write((int)operationResults.Count);
                foreach (OpResult res in operationResults)
                {
                    this.binaryWriter.Write((int)res.ResultType);
                    this.SerializeOpResult(res);
                }
            }
        }

        /// <summary>
        /// Serialize <see cref="OpResult"/>.
        /// </summary>
        /// <param name="operationResult"><see cref="OpResult"/> to serialize</param>
        private void SerializeOpResult(OpResult operationResult)
        {
            switch (operationResult.ResultType)
            {
                case OpCode.Create:
                {
                    var createResult = (OpResult.CreateResult)operationResult;
                    this.SerializeStat(createResult.Stat);
                    if (this.versionToUse >= SerializationFormatVersions.Version22)
                    {
                        this.SerializeNullableString(createResult.Path);
                    }
                    else
                    {
                        if (createResult.Path == null)
                        {
                            // even though this is not what the contract says, if we don't do this, an exception will be thrown on the server.
                            this.binaryWriter.Write(string.Empty);
                        }
                        else
                        {
                            this.binaryWriter.Write(createResult.Path);
                        }
                    }

                    return;
                }

                case OpCode.Move:
                {
                    var moveResult = (OpResult.MoveResult)operationResult;
                    this.SerializeStat(moveResult.Stat);
                    this.binaryWriter.Write(moveResult.DstPath);
                    return;
                }

                case OpCode.GetData:
                {
                    var getDataResult = (OpResult.GetDataResult)operationResult;
                    this.SerializeStat(getDataResult.Stat);
                    this.SerializeData(getDataResult.Bytes);
                    if (this.versionToUse >= SerializationFormatVersions.Version14)
                    {
                        this.binaryWriter.Write((string)getDataResult.Path ?? string.Empty);
                    }

                    return;
                }

                case OpCode.SetData:
                {
                    var setDataResult = (OpResult.SetDataResult)operationResult;
                    this.SerializeStat(setDataResult.Stat);
                    return;
                }

                case OpCode.Error:
                {
                    var errorResult = (OpResult.ErrorResult)operationResult;
                    this.binaryWriter.Write(errorResult.ResultCode);
                    return;
                }

                case OpCode.SetACL:
                {
                    var setAclResult = (OpResult.SetAclResult)operationResult;
                    this.SerializeStat(setAclResult.Stat);
                    return;
                }

                case OpCode.Multi:
                {
                    var runResult = (OpResult.RunResult)operationResult;
                    this.SerializeOpResultList(runResult.Results);
                    return;
                }
            }
        }

        /// <summary>
        /// Serialize <see cref="Acl"/>.
        /// </summary>
        /// <param name="acl"><see cref="Acl"/> to serialize</param>
        private void SerializeAcl(Acl acl)
        {
            bool isNull = acl == null;
            this.binaryWriter.Write((bool)isNull);
            if (!isNull)
            {
                Id id = acl.Id;
                this.binaryWriter.Write(id.Scheme);
                this.binaryWriter.Write(id.Identifier);
                this.binaryWriter.Write((int)acl.Perms);
            }
        }

        /// <summary>
        /// Serialize a list of strings.
        /// </summary>
        /// <param name="stringList">String list to serialize</param>
        private void SerializeStringList(IReadOnlyList<string> stringList)
        {
            this.binaryWriter.Write((int)stringList.Count);
            for (int i = 0; i < stringList.Count; i++)
            {
                this.binaryWriter.Write(stringList[i]);
            }
        }

        /// <summary>
        /// Serialize <see cref="WatcherCall"/>.
        /// </summary>
        /// <param name="watcherCall"><see cref="WatcherCall"/> to serialize</param>
        private void SerializeWatcherCall(WatcherCall watcherCall)
        {
            Debug.Assert(watcherCall != null, "watcherCall parameter is null");

            // ReSharper disable once PossibleNullReferenceException
            this.binaryWriter.Write(watcherCall.WatcherId);
            this.binaryWriter.Write((byte)watcherCall.Kind);

            var evt = watcherCall.WatcherEvt;
            this.binaryWriter.Write(evt == null);
            if (evt != null)
            {
                this.binaryWriter.Write((int)evt.EventType);
                this.binaryWriter.Write((int)evt.KeeperState);
                this.binaryWriter.Write(evt.Path);

                if (this.versionToUse >= SerializationFormatVersions.Version23)
                {
                    this.SerializeData(evt.Data);
                    this.SerializeStat(evt.Stat);
                }
            }
        }

        /// <summary>
        /// Serialize <see cref="IWatcher"/>.
        /// </summary>
        /// <param name="watcher"><see cref="IWatcher"/> to serialize</param>
        private void SerializeWatcher(IWatcher watcher)
        {
            ulong id = (watcher == null) ? 0 : watcher.Id;

            this.binaryWriter.Write(id);
            if (watcher != null)
            {
                this.binaryWriter.Write((byte)watcher.Kind);
            }
            else
            {
                // OneUse watcher by default
                this.binaryWriter.Write((byte)WatcherKind.OneUse);
            }
        }

        /// <summary>
        /// Serialize Data which is an array of bytes.
        /// </summary>
        /// <param name="data">Data to serialize</param>
        private void SerializeData(byte[] data)
        {
            bool isNull = data == null;
            this.binaryWriter.Write((bool)isNull);
            if (!isNull)
            {
                this.binaryWriter.Write((int)data.Length);
                if (data.Length > 0)
                {
                    this.binaryWriter.Write((byte[])data);
                }
            }
        }

        /// <summary>
        /// Serialize <see cref="RedirectSuggested"/>.
        /// </summary>
        /// <param name="redirectSuggested"><see cref="RedirectSuggested"/> to serialize</param>
        private void SerializeRedirectSuggested(RedirectSuggested redirectSuggested)
        {
            bool isNull = redirectSuggested == null;

            this.binaryWriter.Write((bool)isNull);
            if (isNull)
            {
                return;
            }

            this.binaryWriter.WriteNullableString(redirectSuggested.SuggestedConnectionString);
        }

        /// <summary>
        /// Serialize <see cref="IStat"/>.
        /// </summary>
        /// <param name="stat"><see cref="IStat"/> to serialize</param>
        private void SerializeStat(IStat stat)
        {
            bool isNull = stat == null;

            this.binaryWriter.Write((bool)isNull);
            if (isNull)
            {
                return;
            }

            this.binaryWriter.Write((int)stat.Aversion);
            this.binaryWriter.Write((int)stat.Cversion);
            this.binaryWriter.Write((int)stat.Version);
            this.binaryWriter.Write((int)stat.NumChildren);
            this.binaryWriter.Write((int)stat.DataLength);
            this.binaryWriter.Write(0L); // EphemeralOwner legacy field is set to zero always
            this.binaryWriter.Write((long)stat.Czxid);
            this.binaryWriter.Write((long)stat.Mzxid);
            this.binaryWriter.Write((long)stat.Pzxid);
            this.binaryWriter.Write((long)stat.Ctime);
            this.binaryWriter.Write((long)stat.Mtime);
        }

        /// <summary>
        /// Serialize an object based on its type.
        /// </summary>
        /// <param name="content">Object to serialize</param>
        private void SerializeContent(object content)
        {
            ContentType contentType = ContentType.AnyObject;
            if (content is IRingMasterRequest)
            {
                contentType = ContentType.Request;
            }
            else if (content is WatcherCall)
            {
                contentType = ContentType.WatcherCall;
            }
            else if (content is string[])
            {
                contentType = ContentType.StringArray;
            }
            else if (content is List<string>)
            {
                contentType = ContentType.ListOfString;
            }
            else if (content is string)
            {
                contentType = ContentType.String;
            }
            else if (content is IStat)
            {
                contentType = ContentType.Stat;
            }
            else if (content is List<Acl>)
            {
                contentType = ContentType.AclList;
            }
            else if (content is byte[])
            {
                contentType = ContentType.ByteArray;
            }
            else if (content is IReadOnlyList<OpResult>)
            {
                contentType = ContentType.OpResultList;
            }
            else if (content is RedirectSuggested)
            {
                contentType = ContentType.Redirect;
            }
            else if (content is Func<object>)
            {
                this.SerializeContent(((Func<object>)content)());
                return;
            }

            this.binaryWriter.Write((byte)contentType);

            switch (contentType)
            {
                case ContentType.AnyObject:
                    if (content != null)
                    {
                        Trace.TraceInformation("at PrivateSerializeContent(object obj) --> {0}", content.GetType().Name);
                    }

                    byte[] bytes = this.ToByteArray(content);
                    this.SerializeData(bytes);
                    return;
                case ContentType.Request:
                    this.SerializeRingmasterRequest((IRingMasterRequest)content);
                    return;
                case ContentType.OpResultList:
                    this.SerializeOpResultList((IReadOnlyList<OpResult>)content);
                    return;
                case ContentType.String:
                    this.binaryWriter.Write((string)content);
                    return;
                case ContentType.AclList:
                    this.SerializeAclList((IReadOnlyList<Acl>)content);
                    return;
                case ContentType.ByteArray:
                    this.SerializeData((byte[])content);
                    return;
                case ContentType.Stat:
                    this.SerializeStat((IStat)content);
                    return;
                case ContentType.Redirect:
                    this.SerializeRedirectSuggested((RedirectSuggested)content);
                    return;
                case ContentType.ListOfString:
                case ContentType.StringArray:
                    this.SerializeStringList((IReadOnlyList<string>)content);
                    return;
                case ContentType.WatcherCall:
                    this.SerializeWatcherCall((WatcherCall)content);
                    return;
            }
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
