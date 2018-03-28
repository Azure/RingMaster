// <copyright file="MarshallerChannel.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using RequestResponse = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestResponse;
    using RingMasterRequestType = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RingMasterRequestType;
    using WatcherCall = Microsoft.Azure.Networking.Infrastructure.RingMaster.WatcherCall;

    /// <summary>
    /// Channel through which requests are sent and responses are received.
    /// </summary>
    internal class MarshallerChannel : IByteArrayMarshaller, IDisposable
    {
        /// <summary>
        /// Current marshal version
        /// </summary>
        public const uint CurrentMarshalVersion = SerializationFormatVersions.MaximumSupportedVersion;

        private static readonly uint SProposedVersion;

        private readonly RingMasterCommunicationProtocol protocol = new RingMasterCommunicationProtocol();
        private readonly ObjectTracker<IWatcher> watchers = new ObjectTracker<IWatcher>();

        private BinaryReader reader;
        private BinaryWriter writer;
        private ReaderWriterLockSlim writerLock;
        private Action<RequestResponse> enqueueResponse;

        private uint otherMarshallerVersion;
        private bool isDisposed;

        /// <summary>
        /// Initializes static members of the <see cref="MarshallerChannel"/> class.
        /// Sets the version to propose, based on the version of this marshaller, and any possible
        /// overrides from appconfig
        /// </summary>
        static MarshallerChannel()
        {
            uint versionToPropose = CurrentMarshalVersion;

            try
            {
                uint downVersion;
                string downVersionStr = ConfigurationManager.AppSettings["Marshaller.DownScaleToVersion"];
                if (uint.TryParse(downVersionStr, out downVersion))
                {
                    versionToPropose = downVersion;
                }
            }
            catch
            {
                // ignore
            }

            SProposedVersion = versionToPropose;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarshallerChannel"/> class.
        /// </summary>
        /// <param name="ns">The ns.</param>
        public MarshallerChannel(Stream ns)
            : this(ns, ns)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarshallerChannel"/> class.
        /// </summary>
        /// <param name="strInput">The string input.</param>
        /// <param name="strOutput">The string output.</param>
        public MarshallerChannel(Stream strInput, Stream strOutput)
        {
            Guid connectionId = FakeGuid.NewGuid();

            bool ign;
            this.SetStream(strInput, strOutput, out ign);

            this.FriendlyName = connectionId.ToString();
        }

        /// <summary>
        /// Gets or sets the name of the friendly.
        /// </summary>
        /// <value>The name of the friendly.</value>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the version agreed as minimum between the two of us.
        /// </summary>
        public uint UsedMarshalVersion { get; protected set; }

        /// <summary>
        /// resets the stream for the marshaller.
        /// </summary>
        /// <param name="strInput">the new input stream to use</param>
        /// <param name="strOutput">the new output stream to use</param>
        /// <param name="marshalVersionChanged">indicates if the marshalling versions changed with the new stream</param>
        public void SetStream(Stream strInput, Stream strOutput, out bool marshalVersionChanged)
        {
            if (strOutput != null)
            {
                this.writer = new BinaryWriter(strOutput);
            }

            if (strInput != null)
            {
                this.reader = new BinaryReader(strInput);
            }

            uint previousVersion = this.UsedMarshalVersion;

            this.UsedMarshalVersion = SProposedVersion;

            // For writing and reading marshalling version we need to set a timeout
            // on the stream to avoid the listener thread getting stuck in a receive
            // and blocking all connections. Afterwards we restore the original timeout
            // (probably default of Infinite) as rest of requests have other timeout mechanisms.
            const int versionTimeoutMilliseconds = 5000;

            if (this.writer != null)
            {
                // In case of NetworkStream it does support the timeout but being this is a generic method
                // that works accepts non-NetworkStream instances we check just in case to avoid exception
                int originalWriteTimeout = 0;
                if (this.writer.BaseStream.CanTimeout)
                {
                    originalWriteTimeout = this.writer.BaseStream.WriteTimeout;
                    this.writer.BaseStream.WriteTimeout = versionTimeoutMilliseconds;
                }

                this.writerLock = new ReaderWriterLockSlim();
                this.SerializeVersion();
                this.writer.Flush();

                if (this.writer.BaseStream.CanTimeout)
                {
                    this.writer.BaseStream.WriteTimeout = originalWriteTimeout;
                }
            }

            if (this.reader != null)
            {
                int originalReadTimeout = 0;
                if (this.reader.BaseStream.CanTimeout)
                {
                    originalReadTimeout = this.reader.BaseStream.ReadTimeout;
                    this.reader.BaseStream.ReadTimeout = versionTimeoutMilliseconds;
                }

                this.ValidateVersion();
                this.writer.Flush();

                if (this.reader.BaseStream.CanTimeout)
                {
                    this.reader.BaseStream.ReadTimeout = originalReadTimeout;
                }
            }

            if (this.writer != null && this.reader != null)
            {
                Trace.WriteLine(
                    $"Marshaller: proposed {SProposedVersion} otherproposed "
                    + $"{this.otherMarshallerVersion} used {this.UsedMarshalVersion}");
            }

            marshalVersionChanged = previousVersion != this.UsedMarshalVersion;
        }

        /// <summary>
        /// Send a request packet to the server with no concurrency control
        /// </summary>
        /// <param name="requestPacket">Serialized request packet</param>
        /// <param name="flush">if true, it will flush the writer</param>
        public void SendRequestPacket(byte[] requestPacket, bool flush)
        {
            if (requestPacket == null)
            {
                throw new ArgumentNullException(nameof(requestPacket));
            }

            this.writer.Write((int)requestPacket.Length);
            this.writer.Write(requestPacket);

            if (flush)
            {
                this.writer.Flush();
            }
        }

        /// <summary>
        /// Send a response packet to the client with no concurrency control
        /// </summary>
        /// <param name="responsePacket">Serialized response packet</param>
        /// <param name="flush">if true the stream will be flushed</param>
        public void SendResponsePacket(byte[] responsePacket, bool flush)
        {
            if (responsePacket == null)
            {
                throw new ArgumentNullException(nameof(responsePacket));
            }

            this.writer.Write((int)responsePacket.Length);
            this.writer.Write(responsePacket);

            if (flush)
            {
                this.writer.Flush();
            }
        }

        /// <summary>
        /// Receive a request packet from the client.
        /// </summary>
        /// <returns>Serialized request packet</returns>
        public byte[] ReceiveRequestPacket()
        {
            int length = this.reader.ReadInt32();
            return this.reader.ReadBytes(length);
        }

        /// <summary>
        /// Receive a response packet from the server with no concurrency control on the stream
        /// </summary>
        /// <returns>Serialized response packet</returns>
        public byte[] ReceiveResponsePacket()
        {
            uint length = this.reader.ReadUInt32();
            return this.reader.ReadBytes((int)length);
        }

        /// <summary>
        /// Creates the on-wire representation of the given request.
        /// </summary>
        /// <param name="request">Request to serialize</param>
        /// <returns>Serialized data</returns>
        public byte[] SerializeRequestAsBytes(RequestCall request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Before serializing, check if the request has a watcher. If it does, add an entry to the tracking data strucuture
            // When a watchercall is received from the other side, this data structure will be used to get the correct watcher
            // by id and that watcher will be invoked.
            this.RegisterWatcher(request.Request.WrappedRequest, this.RegisterWrapperWatcher);

            RequestDefinitions.RequestCall call = new RequestDefinitions.RequestCall()
            {
                CallId = request.CallId,
                Request = request.Request.WrappedRequest,
            };

            return this.protocol.SerializeRequest(call, this.UsedMarshalVersion);
        }

        /// <summary>
        /// Recreates a request from its on-wire representation.
        /// </summary>
        /// <param name="requestBytes">Data to deserialize</param>
        /// <returns>Deserialized request</returns>
        public RequestCall DeserializeRequestFromBytes(byte[] requestBytes)
        {
            if (requestBytes == null)
            {
                throw new ArgumentNullException(nameof(requestBytes));
            }

            // Before deserializing, check if the request specifies a watcher.  If it does, create a proxy watcher and add an entry to
            // the tracking data structure.  When proxyWatcher.Process is called by the backend, the corresponding message with WatcherCall
            // will be sent to the other side.
            RequestDefinitions.RequestCall call = this.protocol.DeserializeRequest(requestBytes, requestBytes.Length, this.UsedMarshalVersion);

            this.RegisterWatcher(call.Request, this.RegisterProxyWatcher);

            return new RequestCall()
            {
                CallId = call.CallId,
                Request = BackendRequest.Wrap(call.Request),
            };
        }

        /// <summary>
        /// Creates the on-wire representation of the given response.
        /// </summary>
        /// <param name="response">Response to serialize</param>
        /// <returns>Serialized data</returns>
        public byte[] SerializeResponseAsBytes(RequestResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            return this.protocol.SerializeResponse(response, this.UsedMarshalVersion);
        }

        /// <summary>
        /// Recreates a response from its on-wire representation.
        /// </summary>
        /// <param name="responseBytes">Data to deserialize</param>
        /// <returns>Deserialized response</returns>
        public RequestResponse DeserializeResponseFromBytes(byte[] responseBytes)
        {
            if (responseBytes == null)
            {
                throw new ArgumentNullException(nameof(responseBytes));
            }

            RequestResponse response = this.protocol.DeserializeResponse(responseBytes, this.UsedMarshalVersion);

            // If the response is a message to the client with WatcherCall as the content then
            // set the registered watcher corresponding to the watcher id in the watcher call.
            if (response.CallId == ulong.MaxValue)
            {
                WatcherCall watcherCall = response.Content as WatcherCall;
                if (watcherCall != null)
                {
                    watcherCall.Watcher = this.watchers.GetObjectForId(watcherCall.WatcherId, watcherCall.Kind.HasFlag(WatcherKind.OneUse));
                }
            }

            return response;
        }

        /// <summary>
        /// Set the response to enqueue
        /// </summary>
        /// <param name="enqueue">Response to set</param>
        public void SetResponseQueue(Action<RequestResponse> enqueue)
        {
            this.enqueueResponse = enqueue;
        }

        /// <summary>
        /// Flushes the writer
        /// </summary>
        public void Flush()
        {
            this.EnterWriter();
            try
            {
                this.writer.Flush();
            }
            finally
            {
                this.ExitWriter();
            }
        }

        /// <summary>
        /// notifies all watchers about the disconnection
        /// </summary>
        public void NotifyAllWatchers()
        {
            lock (this.watchers)
            {
                if (this.isDisposed)
                {
                    return;
                }

                // the channel is gone! notify all pending watchers
                foreach (IWatcher watcher in this.watchers.GetAll())
                {
                    try
                    {
                        string path = string.Empty;
                        WrapperWatcher wrapperWatcher = watcher as WrapperWatcher;
                        if (wrapperWatcher != null)
                        {
                            path = wrapperWatcher.Path;
                        }
                        else if (watcher is FakeProxyWatcher)
                        {
                            path = ((FakeProxyWatcher)watcher).Path;
                        }

                        watcher.Process(new WatchedEvent(WatchedEvent.WatchedEventType.WatcherRemoved, WatchedEvent.WatchedEventKeeperState.Disconnected, path));
                    }
                    catch (Exception e)
                    {
                        Trace.TraceWarning("MarshallerChannel.NotifyAllWAtchers-Failed exception={0}", e);
                    }
                }

                this.watchers.Clear();
            }
        }

        /// <summary>
        /// Determines whether this instance is valid.
        /// </summary>
        /// <returns><c>true</c> if this instance is valid; otherwise, <c>false</c>.</returns>
        public bool IsValid()
        {
            BinaryReader br = this.reader;
            BinaryWriter bw = this.writer;
            return br != null && bw != null;
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public virtual void Close()
        {
            BinaryWriter bw = this.writer;
            if (bw != null)
            {
                lock (bw)
                {
                    if (this.writer == bw)
                    {
                        this.EnterWriter();
                        try
                        {
                            try
                            {
                                this.writer.Close();
                            }
                            catch (Exception)
                            {
                                // ignore
                            }

                            this.writer = null;
                        }
                        finally
                        {
                            this.ExitWriter();
                        }
                    }
                }
            }

            BinaryReader br = this.reader;
            if (br != null)
            {
                lock (br)
                {
                    if (this.reader == br)
                    {
                        this.EnterReader();
                        try
                        {
                            this.reader.Close();
                        }
                        catch (Exception)
                        {
                            // ignore
                        }
                        finally
                        {
                            this.ExitReader();
                            this.reader = null;
                        }
                    }
                }
            }

            this.NotifyAllWatchers();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Keeps track of any watchers that are set by the request.
        /// </summary>
        /// <param name="request">Request to track</param>
        /// <returns>The request call</returns>
        internal RequestCall LocalRequest(RequestDefinitions.IRingMasterRequest request)
        {
            return new RequestCall()
            {
                Request = BackendRequest.Wrap(request),
            };
        }

        /// <summary>
        /// Validates the version.
        /// </summary>
        /// <exception cref="System.FormatException">Invalid version in stream</exception>
        private void ValidateVersion()
        {
            this.EnterReader();
            try
            {
                uint version = this.reader.ReadUInt32();
                if (version < RingMasterCommunicationProtocol.MinimumSupportedVersion)
                {
                    throw new FormatException("Invalid version in stream (" + version + ")");
                }

                this.otherMarshallerVersion = version;
                this.UsedMarshalVersion = Math.Min(version, this.UsedMarshalVersion);
            }
            finally
            {
                this.ExitReader();
            }
        }

        /// <summary>
        /// Serializes the version.
        /// </summary>
        private void SerializeVersion()
        {
            this.writer.Write((uint)this.UsedMarshalVersion);
        }

        private void EnterReader()
        {
            Monitor.Enter(this.reader);
        }

        /// <summary>
        /// exits the lock for the writer
        /// </summary>
        private void ExitReader()
        {
            Monitor.Exit(this.reader);
        }

        /// <summary>
        /// enters the lock for the writer.
        /// </summary>
        private void EnterWriter()
        {
            this.writerLock.EnterWriteLock();
        }

        /// <summary>
        /// exits the lock for the writer
        /// </summary>
        private void ExitWriter()
        {
            this.writerLock.ExitWriteLock();
        }

        private void RegisterWatcher(RequestDefinitions.IRingMasterRequest request, Func<string, IWatcher, IWatcher> registerWatcher)
        {
            switch (request.RequestType)
            {
                case RingMasterRequestType.Exists:
                    RequestDefinitions.RequestExists existsRequest = (RequestDefinitions.RequestExists)request;
                    if (existsRequest.Watcher != null)
                    {
                        existsRequest.Watcher = registerWatcher(existsRequest.Path, existsRequest.Watcher);
                    }

                    break;
                case RingMasterRequestType.GetData:
                    RequestDefinitions.RequestGetData getDataRequest = (RequestDefinitions.RequestGetData)request;
                    if (getDataRequest.Watcher != null)
                    {
                        getDataRequest.Watcher = registerWatcher(getDataRequest.Path, getDataRequest.Watcher);
                    }

                    break;
                case RingMasterRequestType.GetChildren:
                    RequestDefinitions.RequestGetChildren getChildrenRequest = (RequestDefinitions.RequestGetChildren)request;
                    if (getChildrenRequest.Watcher != null)
                    {
                        getChildrenRequest.Watcher = registerWatcher(getChildrenRequest.Path, getChildrenRequest.Watcher);
                    }

                    break;
            }
        }

        private IWatcher RegisterWrapperWatcher(string path, IWatcher watcher)
        {
            var wrapper = new WrapperWatcher(path, watcher);
            wrapper.Id = this.watchers.CreateNewIdForObject(wrapper);
            return wrapper;
        }

        private IWatcher RegisterProxyWatcher(string path, IWatcher watcher)
        {
            if (watcher.Id != ObjectTracker<IWatcher>.IdForNull)
            {
                return new ProxyWatcher(this, watcher.Id, watcher.Kind, path);
            }

            return null;
        }

        private void SerializeResponse(RequestResponse response)
        {
            if (this.enqueueResponse == null)
            {
                // queue == null is a corner case, so we can safely send and flush here.
                byte[] bytes = this.SerializeResponseAsBytes(response);
                this.SendResponsePacket(bytes, true);
            }
            else
            {
                this.enqueueResponse(response);
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                this.isDisposed = true;

                this.Close();

                this.watchers.Dispose();

                if (this.writerLock != null)
                {
                    this.writerLock.Dispose();
                    this.writerLock = null;
                }
            }
        }

        /// <summary>
        /// Class ProxyWatcher.
        /// </summary>
        internal class ProxyWatcher : IWatcher
        {
            private readonly MarshallerChannel marshaller;
            private string toString;

            private int count;

            /// <summary>
            /// Initializes a new instance of the <see cref="ProxyWatcher"/> class.
            /// </summary>
            /// <param name="marshaller">The marshaller.</param>
            /// <param name="id">The identifier.</param>
            /// <param name="kind">Kind of the watcher.</param>
            /// <param name="path">Path associated with the watcher</param>
            public ProxyWatcher(MarshallerChannel marshaller, ulong id, WatcherKind kind, string path)
            {
                this.marshaller = marshaller;
                this.Id = id;
                this.Kind = kind;
                this.Path = path;
            }

            /// <summary>
            /// Gets or sets the watcher ID
            /// </summary>
            public ulong Id { get; set; }

            /// <summary>
            /// Gets a value indicating whether the watcher is for a single use only.
            /// </summary>
            public bool OneUse => this.Kind.HasFlag(WatcherKind.OneUse);

            /// <summary>
            /// Gets or sets the callback on process
            /// </summary>
            public Action<WatchedEvent> OnProcess { get; set; } = null;

            /// <summary>
            /// Gets or sets the kind of the watcher, if it is for single use and if the data is included on notification
            /// </summary>
            public WatcherKind Kind { get; set; }

            /// <summary>
            /// Gets the path of the watcher
            /// </summary>
            public string Path { get; }

            /// <inheritdoc />
            public override string ToString()
            {
                if (this.toString == null)
                {
                    return this.marshaller + " " + this.Id;
                }

                return this.toString;
            }

            /// <inheritdoc />
            public virtual void Process(WatchedEvent evt)
            {
                try
                {
                    this.count++;
                    this.marshaller.SerializeResponse(
                        new RequestResponse()
                        {
                            CallId = ulong.MaxValue,
                            ResultCode = (int)RingMasterException.Code.Ok,
                            Content = new WatcherCall()
                            {
                                WatcherId = this.Id,
                                WatcherEvt = evt,
                                Kind = this.Kind,
                            },
                        });
                }
                catch (Exception e)
                {
                    Trace.TraceWarning("MarshallerChannel.ProxyWatcher.Process-Failed watchdrId={0}, path={1}, exception={2}", this.Id, this.Path, e.Message);

                    // log, close marshaller, and move on
                    this.marshaller.Close();
                }
                finally
                {
                    this.OnProcess?.Invoke(evt);
                }
            }

            /// <summary>
            /// Processes the watcher once the session that created it is already gone.
            /// </summary>
            /// <param name="sendMessage">if false there is no channel back to the client, and this is a cleanup operation. If true, this needs a notification to the client.</param>
            internal void ProcessAndAbandon(bool sendMessage)
            {
                WatchedEvent evt = new WatchedEvent(WatchedEvent.WatchedEventType.WatcherRemoved, WatchedEvent.WatchedEventKeeperState.SyncConnected, this.Path);

                if (!sendMessage)
                {
                    // there is no need to use the marshaller here, as this is called only when the session is gone.
                    this.OnProcess?.Invoke(evt);
                }
                else
                {
                    this.Process(evt);
                }
            }

            /// <summary>
            /// Sets the to string for later used by <see cref="ToString"/>
            /// </summary>
            /// <param name="v">Value to be set</param>
            internal void SetToString(string v)
            {
                this.toString = v;
            }
        }

        /// <summary>
        /// Class FakeProxyWatcher. It allows the wrapping of a Watcher for consumption by the loopback.
        /// </summary>
        internal class FakeProxyWatcher : ProxyWatcher
        {
            /// <summary>
            /// The watcher to invoke
            /// </summary>
            private readonly IWatcher reqWatcher;

            /// <summary>
            /// Initializes a new instance of the <see cref="FakeProxyWatcher"/> class.
            /// </summary>
            /// <param name="reqWatcher">The watcher to invoke.</param>
            /// <param name="path">Path associated with the watcher</param>
            public FakeProxyWatcher(IWatcher reqWatcher, string path)
                : base(null, 0, reqWatcher == null ? default(WatcherKind) : reqWatcher.Kind, path)
            {
                this.reqWatcher = reqWatcher ?? throw new ArgumentNullException(nameof(reqWatcher));
            }

            /// <summary>
            /// Processes the specified event.
            /// </summary>
            /// <param name="evt">The evt.</param>
            public override void Process(WatchedEvent evt)
            {
                try
                {
                    this.reqWatcher.Process(evt);
                }
                finally
                {
                    this.OnProcess?.Invoke(evt);
                }
            }
        }

        private static class FakeGuid
        {
            private static int num = 1;

            public static Guid NewGuid()
            {
                int intValue = Interlocked.Increment(ref num);

                byte[] intBytes = BitConverter.GetBytes(intValue);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(intBytes);
                }

                byte[] result = new byte[16];
                for (int i = result.Length - 1, j = intBytes.Length - 1; i >= 0 && j >= 0; i--, j--)
                {
                    result[i] = intBytes[j];
                }

                return new Guid(result);
            }
        }

        /// <summary>
        /// Class WrapperWatcher.
        /// </summary>
        private class WrapperWatcher : IWatcher
        {
            /// <summary>
            /// The w
            /// </summary>
            private readonly IWatcher w;

            /// <summary>
            /// Initializes a new instance of the <see cref="WrapperWatcher"/> class.
            /// </summary>
            /// <param name="path">The path.</param>
            /// <param name="w">The w.</param>
            public WrapperWatcher(string path, IWatcher w)
            {
                this.Path = path;
                this.w = w;
            }

            public ulong Id { get; set; }

            /// <summary>
            /// Gets a value indicating whether the watcher is for a single use only.
            /// </summary>
            public bool OneUse => this.Kind.HasFlag(WatcherKind.OneUse);

            /// <summary>
            /// Gets the path associated with the watcher.
            /// </summary>
            public string Path { get; }

            /// <summary>
            /// Gets the kind of the watcher, if it is for single use and if the data is included on notification
            /// </summary>
            public WatcherKind Kind => this.w.Kind;

            /// <summary>
            /// Processes the specified evt.
            /// </summary>
            /// <param name="evt">The evt.</param>
            public void Process(WatchedEvent evt)
            {
                this.w.Process(evt);
            }
        }
    }
}
