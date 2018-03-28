// <copyright file="Session.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Server
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Represents a session established with the server.
    /// </summary>
    internal sealed class Session
    {
        private readonly RingMasterServer server;
        private readonly ulong sessionId;

        /// <summary>
        /// Ensures the requests in the same session are started to process in the same order as they are received
        /// </summary>
        private readonly SemaphoreSlim requestOrdering = new SemaphoreSlim(1, 1);

        private readonly IConnection connection;
        private readonly Func<RequestInit, IRingMasterRequestHandlerOverlapped> onInitSession;
        private readonly ICommunicationProtocol protocol;
        private readonly IRingMasterServerInstrumentation instrumentation;

        private IRingMasterRequestHandlerOverlapped requestHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="Session"/> class.
        /// </summary>
        /// <param name="server">Ring Master server object</param>
        /// <param name="sessionId">Session ID</param>
        /// <param name="onInitSession">Callback when the session is initialized</param>
        /// <param name="connection">Secure Transport connection object</param>
        /// <param name="protocol">Protocol object for sending/receiving responses</param>
        /// <param name="instrumentation">Instrumentation object</param>
        public Session(
            RingMasterServer server,
            ulong sessionId,
            Func<RequestInit, IRingMasterRequestHandlerOverlapped> onInitSession,
            IConnection connection,
            ICommunicationProtocol protocol,
            IRingMasterServerInstrumentation instrumentation)
        {
            this.server = server;
            this.sessionId = sessionId;
            this.connection = connection;
            this.onInitSession = onInitSession;
            this.protocol = protocol;
            this.instrumentation = instrumentation;
        }

        /// <summary>
        /// Gets the session ID
        /// </summary>
        public ulong Id => this.sessionId;

        /// <summary>
        /// Gets the secure transport connection object
        /// </summary>
        public IConnection Connection => this.connection;

        /// <summary>
        /// Gets or sets the session timeout
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Callback to be used by Connection.OnPacketReceived in secure transport
        /// </summary>
        /// <param name="packet">Packet received from the TCP connection</param>
        public void OnPacketReceived(byte[] packet)
        {
            var connection = this.Connection;
            var timer = Stopwatch.StartNew();

            RequestCall call = this.protocol.DeserializeRequest(packet, packet.Length, connection.ProtocolVersion);

            try
            {
                RingMasterServerEventSource.Log.ProcessRequest(
                    this.Id,
                    call.CallId,
                    (int)call.Request.RequestType,
                    call.Request.Path,
                    packet.Length,
                    connection.ProtocolVersion);

                // Wait until the previous request is started then let this one go.
                this.requestOrdering.Wait();

                this.ProcessRequest(
                    call.Request,
                    (response, exception) =>
                    {
                        if (exception == null)
                        {
                            response.CallId = call.CallId;
                        }
                        else
                        {
                            RingMasterServerEventSource.Log.ProcessRequestFailed(this.Id, call.CallId, exception.ToString());
                            response = new RequestResponse
                            {
                                CallId = call.CallId,
                                ResultCode = (int)RingMasterException.Code.Systemerror,
                            };
                        }

                        byte[] responsePacket = this.protocol.SerializeResponse(response, connection.ProtocolVersion);
                        connection.Send(responsePacket);
                        timer.Stop();

                        RingMasterServerEventSource.Log.ProcessRequestCompleted(this.Id, call.CallId, timer.ElapsedMilliseconds);
                        this.instrumentation?.OnRequestCompleted(call.Request.RequestType, timer.Elapsed);
                    });
            }
            catch (Exception ex)
            {
                RingMasterServerEventSource.Log.ProcessRequestFailed(this.Id, call.CallId, ex.ToString());
            }
        }

        /// <summary>
        /// Closes this session
        /// </summary>
        public void Close()
        {
            this.requestHandler?.Close();
        }

        /// <summary>
        /// Sends the watcher notification
        /// </summary>
        /// <param name="watcherCall">Watcher call</param>
        internal void SendWatcherNotification(WatcherCall watcherCall)
        {
            RingMasterServerEventSource.Log.SendWatcherNotification(this.sessionId, watcherCall.WatcherId);
            var messageToClient = new RequestResponse();
            messageToClient.CallId = ulong.MaxValue;
            messageToClient.Content = watcherCall;

            byte[] packet = this.protocol.SerializeResponse(messageToClient, this.connection.ProtocolVersion);
            this.connection.Send(packet);

            this.instrumentation?.OnWatcherNotified(this.sessionId);
        }

        private void ProcessRequest(IRingMasterRequest request, Action<RequestResponse, Exception> onCompletion)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            switch (request.RequestType)
            {
                case RingMasterRequestType.Init:
                    {
                        var initRequest = (RequestInit)request;

                        this.requestHandler = this.onInitSession(initRequest);

                        var initResponse = new RequestResponse
                        {
                            ResultCode = (int)RingMasterException.Code.Ok,
                            Content = new string[] { initRequest.SessionId.ToString(), Guid.NewGuid().ToString() },
                        };

                        this.requestOrdering.Release();

                        RingMasterServerEventSource.Log.ProcessSessionInit(this.Id, initResponse.ResultCode);
                        onCompletion?.Invoke(initResponse, null);
                        return;
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

                this.requestOrdering.Release();

                RingMasterServerEventSource.Log.RedirectionSuggested(this.Id, redirect?.SuggestedConnectionString);
                onCompletion?.Invoke(
                    new RequestResponse()
                    {
                        ResponsePath = request.Path,
                        ResultCode = (int)RingMasterException.Code.Sessionmoved,
                        Stat = default(Stat),
                        Content = redirect,
                    },
                    null);

                return;
            }

            if (this.requestHandler != null)
            {
                QueuedWorkItemPool.Default.Queue(
                    () =>
                    {
                        // Give a signal that the next request can be started. For read requests in the same session,
                        // they may be processed concurrently. For write requests, they will be ordered by locking.
                        this.requestOrdering.Release();

                        this.requestHandler.RequestOverlapped(request, onCompletion);
                    });
                return;
            }

            throw new InvalidOperationException("Session has not been initialized");
        }

        private IWatcher MakeWatcher(IWatcher watcher)
        {
            if (watcher != null)
            {
                RingMasterServerEventSource.Log.MakeWatcher(this.sessionId, watcher.Id);
                this.instrumentation?.OnWatcherSet(this.sessionId);
                return new Watcher(watcher.Id, watcher.Kind, this);
            }

            return null;
        }

        private sealed class Watcher : IWatcher
        {
            private readonly Session session;

            public Watcher(ulong id, WatcherKind kind, Session session)
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
            }
        }
    }
}
