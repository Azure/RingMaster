// <copyright file="ZooKeeperSession.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Server.ZooKeeper
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Represents a session established with the server.
    /// </summary>
    internal sealed class ZooKeeperSession
    {
        private readonly ZooKeeperServer server;
        private readonly ulong sessionId;
        private readonly IConnection connection;
        private readonly Func<RequestInit, IRingMasterRequestHandlerOverlapped> onInitSession;
        private readonly IZooKeeperCommunicationProtocol protocol;
        private readonly IZooKeeperServerInstrumentation instrumentation;

        private IRingMasterRequestHandlerOverlapped requestHandler;

        private ZkprPerSessionState sessionState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZooKeeperSession"/> class.
        /// </summary>
        /// <param name="server">ZooKeeper server object</param>
        /// <param name="sessionId">Session ID</param>
        /// <param name="onInitSession">Function to process init request and returns a request handler</param>
        /// <param name="connection">Secure transport connection object</param>
        /// <param name="protocol">Communication protocol for serialization and deserialization</param>
        /// <param name="instrumentation">Instrumentation object to get the notification of internal state change</param>
        public ZooKeeperSession(
            ZooKeeperServer server,
            ulong sessionId,
            Func<RequestInit, IRingMasterRequestHandlerOverlapped> onInitSession,
            IConnection connection,
            IZooKeeperCommunicationProtocol protocol,
            IZooKeeperServerInstrumentation instrumentation)
        {
            this.server = server;
            this.sessionId = sessionId;
            this.connection = connection;
            this.onInitSession = onInitSession;
            this.protocol = protocol;
            this.instrumentation = instrumentation;
            this.sessionState = new ZkprPerSessionState(this.sessionId);
        }

        /// <summary>
        /// Gets the session ID
        /// </summary>
        public ulong Id => this.sessionId;

        /// <summary>
        /// Gets the secure transport connection
        /// </summary>
        public IConnection Connection => this.connection;

        /// <summary>
        /// Gets or sets the timeout
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Callback method to process received packet from the secure transport connection
        /// </summary>
        /// <param name="packet">Received data packet</param>
        /// <returns>Async task that resolves to indicate the completion of packet processing</returns>
        public Task OnPacketReceived(byte[] packet)
        {
            var connection = this.Connection;
            var timer = Stopwatch.StartNew();

            ProtocolRequestCall call = this.protocol.DeserializeRequest(packet, packet.Length, connection.ProtocolVersion, this.sessionState);
            RequestResponse response = null;
            try
            {
                if (call.Request != null)
                {
                    string sState = this.sessionState.ToString();
                    ZooKeeperServerEventSource.Log.ProcessRequest(this.Id, call.CallId, (int)call.Request.RequestType, call.Request.Path, packet.Length, connection.ProtocolVersion, sState);
                    response = this.ProcessRequest(call.Request, call.ProtocolRequest as IZooKeeperRequest);
                    ZooKeeperServerEventSource.Log.ProcessRequestSucceeded(this.Id, call.CallId, (int)call.Request.RequestType, call.Request.Path, packet.Length, connection.ProtocolVersion, sState);
                    response.CallId = call.CallId;
                }
                else
                {
                    if (call.ProtocolRequest is ZkprProtocolMessages.Ping)
                    {
                        ZkprProtocolMessages.Ping pingRequest = call.ProtocolRequest as ZkprProtocolMessages.Ping;
                        response = new RequestResponse();
                        response.CallId = (ulong)pingRequest.Xid;
                        response.ResultCode = (int)RingMasterException.Code.Ok;
                    }
                }
            }
            catch (Exception ex)
            {
                ZooKeeperServerEventSource.Log.ProcessRequestFailed(this.Id, call == null ? ulong.MaxValue : call.CallId, ex.ToString());
                response = new RequestResponse();
                response.CallId = call.CallId;
                response.ResultCode = (int)RingMasterException.Code.Systemerror;
            }

            if (response != null)
            {
                byte[] responsePacket = this.protocol.SerializeResponse(response, connection.ProtocolVersion, call.ProtocolRequest as IZooKeeperRequest);
                if (responsePacket != null)
                {
                    connection.Send(responsePacket);
                }
                else
                {
                    ZooKeeperServerEventSource.Log.NullResponsePacket(
                        response.CallId,
                        call.ProtocolRequest == null ? "null" : (call.ProtocolRequest as IZooKeeperRequest).RequestType.ToString(),
                        call.Request == null ? "null" : call.Request.RequestType.ToString());
                }
            }

            timer.Stop();

            ZooKeeperServerEventSource.Log.ProcessRequestCompleted(this.Id, call == null ? ulong.MaxValue : call.CallId, timer.ElapsedMilliseconds);
            this.instrumentation?.OnRequestCompleted(call.Request == null ? RingMasterRequestType.None : call.Request.RequestType, timer.Elapsed);

            return Task.FromResult(0);
        }

        /// <summary>
        /// Closes this session
        /// </summary>
        public void Close()
        {
            this.requestHandler?.Close();
        }

        /// <summary>
        /// Sends the watcher notification back
        /// </summary>
        /// <param name="watcherCall">Watcher call from the backend core</param>
        internal void SendWatcherNotification(WatcherCall watcherCall)
        {
            ZooKeeperServerEventSource.Log.SendWatcherNotification(this.sessionId, watcherCall.WatcherId);
            var messageToClient = new RequestResponse();
            messageToClient.CallId = ulong.MaxValue;
            messageToClient.Content = watcherCall;
            messageToClient.ResultCode = (int)RingMasterException.Code.Ok;
            byte[] packet = this.protocol.SerializeWatcherResponse(messageToClient, this.connection.ProtocolVersion);
            this.connection.Send(packet);

            this.instrumentation?.OnWatcherNotified(this.sessionId);
        }

        private RequestResponse ProcessRequest(IRingMasterRequest request, IZooKeeperRequest zkprRequest)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (zkprRequest == null)
            {
                throw new ArgumentNullException(nameof(zkprRequest));
            }

            switch (request.RequestType)
            {
                case RingMasterRequestType.Init:
                    {
                        var initRequest = (RequestInit)request;
                        RequestResponse initResponse = null;

                        if (this.requestHandler == null)
                        {
                            this.requestHandler = this.onInitSession(initRequest);
                            if (((zkprRequest as ZkprProtocolMessages.CreateSession).SessionId != 0) || (zkprRequest as ZkprProtocolMessages.CreateSession).IsNullPassword == false)
                            {
                                initResponse = new RequestResponse
                                {
                                    ResultCode = (int)RingMasterException.Code.Authfailed,
                                    Content = new string[] { "0", string.Empty },
                                };
                            }
                        }

                        if (initResponse == null)
                        {
                            initResponse = (this.requestHandler as CoreRequestHandler).InitResponse;
                        }

                        ZooKeeperServerEventSource.Log.ProcessSessionInit(this.Id, initResponse.ResultCode);
                        return initResponse;
                    }

                case RingMasterRequestType.GetData:
                    {
                        var getDataRequest = (RequestGetData)request;
                        getDataRequest.Watcher = this.MakeWatcher(getDataRequest.Watcher);
                        break;
                    }

                case RingMasterRequestType.GetChildren:
                    {
                        var getChildrenRequest = (RequestGetChildren)request;
                        getChildrenRequest.Watcher = this.MakeWatcher(getChildrenRequest.Watcher);
                        break;
                    }

                case RingMasterRequestType.Exists:
                    {
                        var existsRequest = (RequestExists)request;
                        existsRequest.Watcher = this.MakeWatcher(existsRequest.Watcher);
                        break;
                    }
            }

            if (this.server.Redirect != null)
            {
                RedirectSuggested redirect = this.server.Redirect();

                ZooKeeperServerEventSource.Log.RedirectionSuggested(this.Id, redirect?.SuggestedConnectionString);
                return new RequestResponse()
                {
                    ResponsePath = request.Path,
                    ResultCode = (int)RingMasterException.Code.Sessionmoved,
                    Stat = default(Stat),
                    Content = redirect,
                };
            }

            if (this.requestHandler != null)
            {
                using (var completed = new AutoResetEvent(false))
                {
                    RequestResponse response = null;
                    this.requestHandler.RequestOverlapped(request, (r, e) =>
                    {
                        response = r;
                        completed.Set();
                    });

                    completed.WaitOne();
                    return response;
                }
            }

            throw new InvalidOperationException("Session has not been initialized");
        }

        private IWatcher MakeWatcher(IWatcher watcher)
        {
            if (watcher != null)
            {
                ZooKeeperServerEventSource.Log.MakeWatcher(this.sessionId, watcher.Id);
                this.instrumentation?.OnWatcherSet(this.sessionId);
                return new Watcher(watcher.Id, watcher.Kind, this);
            }

            return null;
        }

        private sealed class Watcher : IWatcher
        {
            private readonly ZooKeeperSession session;

            public Watcher(ulong id, WatcherKind kind, ZooKeeperSession session)
            {
                this.Id = id;
                this.session = session;
                this.Kind = kind;
            }

            public ulong Id { get; private set; }

            public bool OneUse => this.Kind.HasFlag(WatcherKind.OneUse);

            public WatcherKind Kind { get; private set; }

            public void Process(WatchedEvent evt)
            {
                var watcherCall = new WatcherCall();
                watcherCall.WatcherId = this.Id;
                watcherCall.Kind = this.Kind;
                watcherCall.WatcherEvt = evt;

                this.session.SendWatcherNotification(watcherCall);
                Trace.TraceInformation("Watcher Notification Sent WatcherId:{0}, Evt:{1}", watcherCall.WatcherId, watcherCall.WatcherEvt);
            }
        }
    }
}
