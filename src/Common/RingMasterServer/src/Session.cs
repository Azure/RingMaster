// <copyright file="Session.cs" company="Microsoft">
//     Copyright ©  2016
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
        private readonly IConnection connection;
        private readonly Func<RequestInit, IRingMasterRequestHandler> onInitSession;
        private readonly ICommunicationProtocol protocol;
        private readonly IRingMasterServerInstrumentation instrumentation;

        private IRingMasterRequestHandler requestHandler;

        public Session(
            RingMasterServer server,
            ulong sessionId,
            Func<RequestInit, IRingMasterRequestHandler> onInitSession,
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

        public ulong Id => this.sessionId;

        public IConnection Connection => this.connection;

        public int Timeout { get; set; }

        public async Task OnPacketReceived(byte[] packet)
        {
            var connection = this.Connection;
            var timer = Stopwatch.StartNew();
            RequestCall call = this.protocol.DeserializeRequest(packet, packet.Length, connection.ProtocolVersion);
            RequestResponse response;
            try
            {
                RingMasterServerEventSource.Log.ProcessRequest(this.Id, call.CallId, (int)call.Request.RequestType, call.Request.Path, packet.Length, connection.ProtocolVersion);
                response = await this.ProcessRequest(call.Request);
                response.CallId = call.CallId;
            }
            catch (Exception ex)
            {
                RingMasterServerEventSource.Log.ProcessRequestFailed(this.Id, call.CallId, ex.ToString());
                response = new RequestResponse();
                response.CallId = call.CallId;
                response.ResultCode = (int)RingMasterException.Code.Systemerror;
            }

            byte[] responsePacket = this.protocol.SerializeResponse(response, connection.ProtocolVersion);
            connection.Send(responsePacket);
            timer.Stop();

            RingMasterServerEventSource.Log.ProcessRequestCompleted(this.Id, call.CallId, timer.ElapsedMilliseconds);
            this.instrumentation?.OnRequestCompleted(call.Request.RequestType, timer.Elapsed);
        }

        public void Close()
        {
            this.requestHandler?.Close();
        }

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

        private async Task<RequestResponse> ProcessRequest(IRingMasterRequest request)
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
                            Content = new string[] { initRequest.SessionId.ToString(), Guid.NewGuid().ToString() }
                        };

                        RingMasterServerEventSource.Log.ProcessSessionInit(this.Id, initResponse.ResultCode);
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

                RingMasterServerEventSource.Log.RedirectionSuggested(this.Id, redirect?.SuggestedConnectionString);
                return new RequestResponse()
                {
                    ResponsePath = request.Path,
                    ResultCode = (int)RingMasterException.Code.Sessionmoved,
                    Stat = default(Stat),
                    Content = redirect
                };
            }

            if (this.requestHandler != null)
            {
                return await this.requestHandler.Request(request);
            }

            throw new InvalidOperationException("Session has not been initialized");
        }

        private IWatcher MakeWatcher(IWatcher watcher)
        {
            if (watcher != null)
            {
                RingMasterServerEventSource.Log.MakeWatcher(this.sessionId, watcher.Id);
                this.instrumentation?.OnWatcherSet(this.sessionId);
                return new Watcher(watcher.Id, watcher.OneUse, this);
            }

            return null;
        }

        private sealed class Watcher : IWatcher
        {
            private readonly Session session;

            public Watcher(ulong id, bool oneUse, Session session)
            {
                this.Id = id;
                this.session = session;
                this.OneUse = oneUse;
            }

            public ulong Id { get; private set; }

            public bool OneUse { get; private set; }

            public void Process(WatchedEvent evt)
            {
                var watcherCall = new WatcherCall();
                watcherCall.WatcherId = this.Id;
                watcherCall.OneUse = this.OneUse;
                watcherCall.WatcherEvt = evt;

                this.session.SendWatcherNotification(watcherCall);
            }
        }
    }
}