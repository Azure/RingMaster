// <copyright file="RingMasterRequestHandler.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// RingMasterRequestHandler - Manages connection with the server and handles sending IRingMasterRequest
    /// and receiving RingMasterResponse
    /// </summary>
    internal sealed class RingMasterRequestHandler : IRingMasterRequestHandler
    {
        /// <summary>
        /// Configuration settings.
        /// </summary>
        private readonly Configuration configuration;

        /// <summary>
        /// Interface to the protocol used to format requests and responses.
        /// </summary>
        private readonly ICommunicationProtocol communicationProtocol;

        /// <summary>
        /// Interface to the transport layer.
        /// </summary>
        private readonly ITransport transport;

        /// <summary>
        /// Instrumentation consumer.
        /// </summary>
        private readonly IInstrumentation instrumentation;

        /// <summary>
        /// Map of requestId to request.
        /// </summary>
        private readonly Dictionary<ulong, RequestWrapper> requestMap = new Dictionary<ulong, RequestWrapper>();

        /// <summary>
        /// Map of watcherId to watcher.
        /// </summary>
        private readonly Dictionary<ulong, WatcherWrapper> watcherMap = new Dictionary<ulong, WatcherWrapper>();

        /// <summary>
        /// Queue of outgoing requests.
        /// </summary>
        private readonly BlockingCollection<RequestWrapper> outgoingRequests;

        /// <summary>
        /// Semaphore used to signal that outgoing requests are available.
        /// </summary>
        private readonly SemaphoreSlim outgoingRequestsAvailable;

        /// <summary>
        /// Queue of incoming responses.
        /// </summary>
        private readonly BlockingCollection<ResponseWrapper> incomingResponses;

        /// <summary>
        /// Semaphore used to signal that responses are available.
        /// </summary>
        private readonly SemaphoreSlim responsesAvailable;

        /// <summary>
        /// Cancellation Token Source that provides the cancellation token.
        /// </summary>
        private readonly CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Task that manages requests.
        /// </summary>
        private readonly Task manageRequestsTask;

        /// <summary>
        /// Task that manages responses.
        /// </summary>
        private readonly Task manageResponsesTask;

        /// <summary>
        /// Monotonically increasing Id for requests sent by this client.
        /// </summary>
        private long requestId;

        /// <summary>
        /// Monotonically increasing Id for watchers set on this client.
        /// </summary>
        private long watcherId;

        /// <summary>
        /// Monotically increasing count of heartbeats.
        /// </summary>
        private ulong heartBeatCount;

        /// <summary>
        /// Oldest request that has not been received yet
        /// </summary>
        private RequestWrapper oldestPendingRequest;

        /// <summary>
        /// Newest request that has not been received yet
        /// </summary>
        private RequestWrapper newestPendingRequest;

        // Prevent the double disposal
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterRequestHandler"/> class.
        /// </summary>
        /// <param name="configuration">Configuration settings</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="communicationProtocol">Interface to the communication protocol</param>
        /// <param name="transport">Interface to the transport layer</param>
        /// <param name="cancellationToken">Token that will be observed for cancellation signal</param>
        public RingMasterRequestHandler(
            Configuration configuration,
            IInstrumentation instrumentation,
            ICommunicationProtocol communicationProtocol,
            ITransport transport,
            CancellationToken cancellationToken)
        {
            this.configuration = configuration;
            this.instrumentation = instrumentation;
            this.communicationProtocol = communicationProtocol;
            this.transport = transport;
            this.transport.OnNewConnection = this.OnNewConnection;
            this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            this.outgoingRequests = new BlockingCollection<RequestWrapper>(this.configuration.RequestQueueLength);
            this.outgoingRequestsAvailable = new SemaphoreSlim(0, this.configuration.RequestQueueLength);
            this.incomingResponses = new BlockingCollection<ResponseWrapper>(this.configuration.ResponseQueueLength);
            this.responsesAvailable = new SemaphoreSlim(0, this.configuration.ResponseQueueLength);
            this.manageRequestsTask = Task.Run(this.ManageRequestLifetime);
            this.manageResponsesTask = Task.Run(this.ManageResponses);
        }

#pragma warning disable SA1600 // TODO: add document later
        public interface IInstrumentation
        {
            void ConnectionCreated(ulong connectionId, EndPoint remoteEndPoint, string remoteIdentity);

            void ConnectionClosed(ulong connectionId, EndPoint remoteEndPoint, string remoteIdentity);

            void RequestQueued(ulong requestId, RingMasterRequestType requestType, int pendingRequestCount);

            void RequestQueueFull(ulong requestId, RingMasterRequestType requestType, int pendingRequestCount);

            void RequestSent(ulong requestId, RingMasterRequestType requestType, int requestLength);

            void RequestSendFailed(ulong requestId, RingMasterRequestType requestType);

            void ResponseQueued(int responseLength);

            void ResponseProcessed(ulong requestId, RingMasterRequestType requestType, int resultCode, TimeSpan elapsed);

            void RequestTimedOut(ulong requestId, RingMasterRequestType requestType, TimeSpan elapsed);

            void RequestAborted(ulong requestId, RingMasterRequestType requestType);

            void HeartBeatSent(ulong heartBeatId);

            void WatcherNotificationReceived(WatchedEvent.WatchedEventType eventType);

            void WatcherNotFound();

            void InvalidPacketReceived();

            void UnexpectedResponseReceived(ulong callId);

            void InvalidClientMessageReceived();
        }
#pragma warning restore

        /// <summary>
        /// Gets or sets the number of milliseconds to wait before a request is timed out.
        /// </summary>
        public int Timeout
        {
            get
            {
                return (int)this.configuration.DefaultTimeout.TotalMilliseconds;
            }

            set
            {
                this.configuration.DefaultTimeout = TimeSpan.FromMilliseconds(value);
            }
        }

        /// <summary>
        /// Enqueue a request.
        /// </summary>
        /// <param name="request">Request to enqueue</param>
        /// <returns>A task that resolves to the response sent by the server</returns>
        public async Task<RequestResponse> Request(IRingMasterRequest request)
        {
            // Register the request in the requestMap - the request map keeps track of the request
            // throughout its lifetime.
            RequestWrapper requestWrapper = this.RegisterRequest(request);

            // Attempt to add the request to the outgoing request queue.
            if (this.outgoingRequests.TryAdd(requestWrapper))
            {
                // Once the request is added to the outgoing request queue, it stays there until a connection
                // is available to send it.  If the request's timeout expires before a connection is available, then
                // the request is completed with OperationTimedout resultCode.
                this.instrumentation.RequestQueued(requestWrapper.CallId, requestWrapper.WrappedRequest.RequestType, this.outgoingRequests.Count);
                this.outgoingRequestsAvailable.Release();
            }
            else
            {
                // The outgoing request queue is full, so remove the request from the requestMap and throw a RequestQueueFull exception.
                lock (this.requestMap)
                {
                    this.RemoveRequestFromMap(requestWrapper.CallId);
                }

                int pendingRequestCount = this.outgoingRequestsAvailable.CurrentCount;
                RingMasterClientEventSource.Log.RequestQueueFull(requestWrapper.CallId, pendingRequestCount);
                this.instrumentation.RequestQueueFull(requestWrapper.CallId, requestWrapper.WrappedRequest.RequestType, pendingRequestCount);
                throw RingMasterClientException.RequestQueueFull(pendingRequestCount);
            }

            return await requestWrapper.TaskCompletionSource.Task;
        }

        /// <summary>
        /// Close this request handler.
        /// </summary>
        public void Close()
        {
            RingMasterClientEventSource.Log.CloseRequestHandler();
            this.outgoingRequests.CompleteAdding();

            // Don't accept any more response packets.
            this.incomingResponses.CompleteAdding();

            // Explicitly cancel the cancellation source to indicate that
            // the lifetime of this instance is over.
            this.cancellationTokenSource.Cancel();

            this.manageRequestsTask.Wait();
            this.manageResponsesTask.Wait();
        }

        /// <summary>
        /// Dispose this request handler.
        /// </summary>
        public void Dispose()
        {
            RingMasterClientEventSource.Log.DisposeRequestHandler();
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Examine the ResultCode in the given <paramref name="response"/> and if the ResultCode is not Ok, throw an exception that corresponds to the response.
        /// </summary>
        /// <param name="response">Response to check for error</param>
        private static void ThrowIfError(RequestResponse response)
        {
            if (response.ResultCode != (int)RingMasterException.Code.Ok)
            {
                Exception exception = RingMasterException.GetException(response);
                RingMasterClientEventSource.Log.CompleteWithException(exception.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Initializes the connection with the server.
        /// </summary>
        /// <param name="connection">Connection to initialize</param>
        /// <param name="sessionId">Id of the session</param>
        /// <param name="sessionPassword">Session Password</param>
        /// <returns>Task that tracks completion of this method</returns>
        private async Task Init(
            IConnection connection,
            ulong sessionId,
            string sessionPassword)
        {
            RequestInit.RedirectionPolicy redirectionPolicy = this.configuration.MustTransparentlyForwardRequests
                ? RequestInit.RedirectionPolicy.ForwardPreferred
                : RequestInit.RedirectionPolicy.RedirectPreferred;

            var initRequest = new RequestInit(
                sessionId,
                sessionPassword,
                readOnlyInterfaceRequiresLocks: this.configuration.RequireLockForReadOnlyOperations,
                redirection: redirectionPolicy);

            var requestWrapper = this.RegisterRequest(initRequest);

            RingMasterClientEventSource.Log.Init(requestWrapper.CallId, sessionId, sessionPassword);
            await this.SendRequest(connection, requestWrapper);

            RequestResponse response = await requestWrapper.TaskCompletionSource.Task;

            ThrowIfError(response);
        }

        /// <summary>
        /// Send a request through the given connection.
        /// </summary>
        /// <param name="connection">Connection through which the request must be sent</param>
        /// <param name="request">Request to send</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        private async Task SendRequest(IConnection connection, RequestWrapper request)
        {
            switch (request.WrappedRequest.RequestType)
            {
                case RingMasterRequestType.Exists:
                {
                    var existsRequest = (RequestExists)request.WrappedRequest;
                    request.AssociatedWatcher = this.RegisterWatcher(existsRequest.Watcher, existsRequest.Path);
                    existsRequest.Watcher = request.AssociatedWatcher;
                    break;
                }

                case RingMasterRequestType.GetData:
                {
                    var getDataRequest = (RequestGetData)request.WrappedRequest;
                    request.AssociatedWatcher = this.RegisterWatcher(getDataRequest.Watcher, getDataRequest.Path);
                    getDataRequest.Watcher = request.AssociatedWatcher;
                    break;
                }

                case RingMasterRequestType.GetChildren:
                {
                    var getChildrenRequest = (RequestGetChildren)request.WrappedRequest;
                    request.AssociatedWatcher = this.RegisterWatcher(getChildrenRequest.Watcher, getChildrenRequest.Path);
                    getChildrenRequest.Watcher = request.AssociatedWatcher;
                    break;
                }
            }

            RequestCall call = new RequestCall
            {
                CallId = request.CallId,
                Request = request.WrappedRequest,
            };

            try
            {
                this.cancellationTokenSource.Token.ThrowIfCancellationRequested();

                byte[] requestPacket = this.communicationProtocol.SerializeRequest(call, connection.ProtocolVersion);
                RingMasterClientEventSource.Log.Send(connection.Id, call.CallId, requestPacket.Length);

                await connection.SendAsync(requestPacket);
                this.instrumentation.RequestSent(call.CallId, call.Request.RequestType, requestPacket.Length);
            }
            catch (Exception ex)
            {
                RingMasterClientEventSource.Log.RequestSendFailed(connection.Id, request.CallId, ex.ToString());
                this.instrumentation.RequestSendFailed(request.CallId, request.WrappedRequest.RequestType);
                throw;
            }
        }

        /// <summary>
        /// Invoked by the transport layer when a new connection is established.
        /// </summary>
        /// <param name="connection">The new connection</param>
        private void OnNewConnection(IConnection connection)
        {
            connection.OnPacketReceived = packet => this.OnPacketReceived(connection, packet);

            CancellationTokenSource connectionLifetime = CancellationTokenSource.CreateLinkedTokenSource(this.cancellationTokenSource.Token);
            var connectionLifetimeTask = Task.Run(() => this.ManageConnectionLifetime(connection, connectionLifetime.Token));

            connection.OnConnectionLost = () =>
            {
                RingMasterClientEventSource.Log.ConnectionLost(connection.Id);
                connectionLifetime.Cancel();
                Task.WaitAny(connectionLifetimeTask);
                connectionLifetime.Dispose();
            };
        }

        /// <summary>
        /// Invoked by the currently active connection when a packet is received.
        /// </summary>
        /// <param name="connection">Connection through which the packet was received</param>
        /// <param name="responsePacket">The response packet that was received</param>
        private void OnPacketReceived(IConnection connection, byte[] responsePacket)
        {
            if (this.incomingResponses.IsAddingCompleted)
            {
                return;
            }

            RingMasterClientEventSource.Log.OnPacketReceived(connection.Id, responsePacket.Length);
            try
            {
                this.incomingResponses.Add(new ResponseWrapper
                {
                    ProtocolVersion = connection.ProtocolVersion,
                    SerializedResponse = responsePacket,
                });

                this.responsesAvailable.Release();
                this.instrumentation.ResponseQueued(responsePacket.Length);
            }
            catch (Exception ex)
            {
                RingMasterClientEventSource.Log.OnPacketReceivedFailed(connection.Id, ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Task that is responsible for managing request lifetime.
        /// </summary>
        /// <returns>A Task that tracks execution of this method</returns>
        private async Task ManageRequestLifetime()
        {
            CancellationToken cancellationToken = this.cancellationTokenSource.Token;

            try
            {
                // Request Lifetime
                // 1. Tracked by this.requestMap from the moment it is queued until it is completed or timed out.
                // 2. Stays in this.outgoingRequests from the moment it is queued until it is sent or timed out.
                while (!cancellationToken.IsCancellationRequested)
                {
                    TimeSpan nextExpiry = this.TimeoutOldestRequest();

                    await Task.Delay(nextExpiry, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                RingMasterClientEventSource.Log.ManageRequestLifetimeTaskCanceled();
            }
            finally
            {
                // Yield here to ensure that DrainPendingRequests and
                // DrainWatchers run in a separate callback.
                await Task.Yield();

                this.cancellationTokenSource.Cancel();

                this.DrainPendingRequests();
                this.DrainWatchers();
                RingMasterClientEventSource.Log.ManageRequestLifetimeTaskCompleted();
            }
        }

        /// <summary>
        /// Task that is responsible for managing connection lifetime.
        /// </summary>
        /// <param name="connection">Connection whose lifetime is represented by this task</param>
        /// <param name="lifetimeToken">Token that is observed for cancellation signal</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        private async Task ManageConnectionLifetime(IConnection connection, CancellationToken lifetimeToken)
        {
            try
            {
                this.instrumentation.ConnectionCreated(connection.Id, connection.RemoteEndPoint, connection.RemoteIdentity);
                await this.Init(connection, 0, string.Empty);

                RingMasterClientEventSource.Log.OnNewConnectionInitialized(connection.Id, connection.RemoteEndPoint.ToString(), connection.RemoteIdentity);

                while (!lifetimeToken.IsCancellationRequested)
                {
                    // During the lifetime of a connection, keep watching for new requests in outgoingReqests queue and send
                    // the requests.  If no new requests have been queued for the duration of a HeartBeatInterval, send a heart beat
                    // request to keep the connection alive.
                    if (await this.outgoingRequestsAvailable.WaitAsync(this.configuration.HeartBeatInterval, lifetimeToken))
                    {
                        RequestWrapper request = this.outgoingRequests.Take();
                        if (!request.TaskCompletionSource.Task.IsCompleted)
                        {
                            // Queue the request for send, no need to wait for the transport to actually
                            // send the request. If the transport fails to send the request, the error will be logged
                            // and the request will timeout.
                            Task unused = this.SendRequest(connection, request);
                        }
                    }
                    else
                    {
                        bool heartBeatResult = await this.SendHeartbeat(connection);
                        if (!heartBeatResult)
                        {
                            RingMasterClientEventSource.Log.HeartbeatFailure(connection.Id);
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                RingMasterClientEventSource.Log.ManageConnectionLifetimeTaskCanceled(connection.Id);
            }
            catch (Exception ex)
            {
                RingMasterClientEventSource.Log.ManageConnectionLifetimeTaskFailed(connection.Id, ex.ToString());
            }
            finally
            {
                // Yield here to ensure that connection.Disconnect runs in a different
                // callback.
                await Task.Yield();
                connection.Disconnect();

                // When a connection is terminated, drain any active watchers.  However, requests that have been
                // sent through this connection will remain in this.requestMap until their timeout expires.  This is
                // because they can receive a response through a different connection.
                this.DrainWatchers();
                RingMasterClientEventSource.Log.ManageConnectionLifetimeTaskCompleted(connection.Id, connection.RemoteEndPoint.ToString(), connection.RemoteIdentity);
                this.instrumentation.ConnectionClosed(connection.Id, connection.RemoteEndPoint, connection.RemoteIdentity);
            }
        }

        /// <summary>
        /// Task that is responsible for managing responses.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        private async Task ManageResponses()
        {
            CancellationToken lifetimeToken = this.cancellationTokenSource.Token;
            try
            {
                while (!lifetimeToken.IsCancellationRequested)
                {
                    await this.responsesAvailable.WaitAsync(lifetimeToken);
                    ResponseWrapper wrapper = this.incomingResponses.Take();
                    RequestResponse response = this.communicationProtocol.DeserializeResponse(wrapper.SerializedResponse, wrapper.ProtocolVersion);
                    this.ProcessResponse(response);
                }
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                {
                    RingMasterClientEventSource.Log.ManageResponsesTaskFailed(ex.ToString());
                    this.instrumentation.InvalidPacketReceived();
                    this.cancellationTokenSource.Cancel();
                }
            }

            RingMasterClientEventSource.Log.ManageResponsesTaskCompleted();
        }

        /// <summary>
        /// Process a response.
        /// </summary>
        /// <param name="response">Response to process</param>
        private void ProcessResponse(RequestResponse response)
        {
            try
            {
                if (response.CallId == ulong.MaxValue)
                {
                    // This is a message to the client and not a response to a previous
                    // request sent by this client.
                    this.ProcessMessageToClient(response);
                }
                else
                {
                    RequestWrapper request;
                    lock (this.requestMap)
                    {
                        request = this.RemoveRequestFromMap(response.CallId);
                    }

                    if (request != null)
                    {
                        this.ProcessRequestResponse(response.CallId, request, response);
                    }
                    else
                    {
                        RingMasterClientEventSource.Log.UnexpectedResponse(response.CallId);
                        this.instrumentation.UnexpectedResponseReceived(response.CallId);
                    }
                }
            }
            catch (Exception ex)
            {
                RingMasterClientEventSource.Log.ProcessResponseFailed(response.CallId, ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Process a message sent to this client.
        /// </summary>
        /// <param name="clientMessage">Message to process</param>
        private void ProcessMessageToClient(RequestResponse clientMessage)
        {
            WatcherCall watcherCall = clientMessage.Content as WatcherCall;

            if (watcherCall == null)
            {
                RingMasterClientEventSource.Log.ProcessMessageToClientFailedNotWatcherCall();
                this.instrumentation.InvalidClientMessageReceived();
                return;
            }

            lock (this.watcherMap)
            {
                WatcherWrapper watcher = null;
                if (this.watcherMap.TryGetValue(watcherCall.WatcherId, out watcher))
                {
                    watcher.Process(watcherCall.WatcherEvt);

                    // If it is a one use watcher, it must be removed from the watcher map as it won't receive any further
                    // notifications.   Similarly, if it is a multi use watcher and the current notification is a WatcherRemoved
                    // notification, it will not receive any further notifications.
                    if (watcher.Kind.HasFlag(WatcherKind.OneUse) ||
                        watcherCall.WatcherEvt.EventType == WatchedEvent.WatchedEventType.WatcherRemoved)
                    {
                        RingMasterClientEventSource.Log.UnregisterWatcher(watcher.Id);
                        this.watcherMap.Remove(watcher.Id);
                    }
                }
                else
                {
                    RingMasterClientEventSource.Log.ProcessMessageToClientFailedWatcherDoesNotExist(watcherCall.WatcherId);
                    this.instrumentation.WatcherNotFound();
                }
            }
        }

        /// <summary>
        /// Process a response to a request.
        /// </summary>
        /// <param name="callId">Id of the call associated with this response</param>
        /// <param name="request">The request associated with the response</param>
        /// <param name="response">The response that was received</param>
        private void ProcessRequestResponse(ulong callId, RequestWrapper request, RequestResponse response)
        {
            // If the message comes with a path, this means we need to assume
            // that path is the one the data relates to, so we will modify the
            // request to reflect that.
            if (response.ResponsePath != null)
            {
                request.WrappedRequest.Path = response.ResponsePath;
            }

            RingMasterClientEventSource.Log.ProcessResponse(callId, response.ResponsePath, response.ResultCode);
            this.instrumentation.ResponseProcessed(request.CallId, request.WrappedRequest.RequestType, response.ResultCode, TimeSpan.FromTicks(request.ElapsedInTicks));
            Task.Run(() =>
            {
                request.TaskCompletionSource.SetResult(response);

                // The response for the request that installed the watcher has been processed
                // Now, notifications for any watcher installed by the request can be dispatched.
                WatcherWrapper watcher = request.AssociatedWatcher;
                if (watcher != null)
                {
                    if (response.ResultCode == (int)RingMasterException.Code.Ok)
                    {
                        watcher.EnableDispatch();
                    }
                    else
                    {
                        // If the request that installed the watcher failed, then remove the watcher
                        lock (this.watcherMap)
                        {
                            this.watcherMap.Remove(watcher.Id);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Register a watcher to receive notifications.
        /// </summary>
        /// <param name="watcher">Watcher that must be notified</param>
        /// <param name="path">Path associated with the watcher</param>
        /// <returns>A wrapper that associates a unique id with the given watcher</returns>
        private WatcherWrapper RegisterWatcher(IWatcher watcher, string path)
        {
            if (watcher == null)
            {
                return null;
            }

            WatcherWrapper wrapper = null;
            ulong watcherId = (ulong)Interlocked.Increment(ref this.watcherId);
            RingMasterClientEventSource.Log.RegisterWatcher(watcherId, path);
            wrapper = new WatcherWrapper(this, watcherId, watcher, path);

            lock (this.watcherMap)
            {
                WatcherWrapper result = wrapper;
                this.watcherMap.Add(watcherId, wrapper);
                wrapper = null;
                return result;
            }
        }

        /// <summary>
        /// Drains pending requests by completing them with <c>ConnectionLoss</c> error.
        /// </summary>
        private void DrainPendingRequests()
        {
            lock (this.requestMap)
            {
                RingMasterClientEventSource.Log.DrainPendingRequests(this.requestMap.Count);
                while (this.requestMap.Values.Count > 0)
                {
                    var request = this.requestMap.Values.First();
                    this.RemoveRequestFromMap(request.CallId);
                    RingMasterClientEventSource.Log.NotifyConnectionLoss(request.CallId);
                    this.NotifyRequestFailed(request, RingMasterException.Code.Connectionloss);
                    this.instrumentation.RequestAborted(request.CallId, request.WrappedRequest.RequestType);
                }
            }
        }

        /// <summary>
        /// Drain currently installed watchers.
        /// </summary>
        private void DrainWatchers()
        {
            lock (this.watcherMap)
            {
                RingMasterClientEventSource.Log.DrainWatchers(this.watcherMap.Count);
                foreach (var pair in this.watcherMap)
                {
                    WatcherWrapper watcher = pair.Value;
                    Debug.Assert(watcher != null, "Watcher in watchers map is null");
                    RingMasterClientEventSource.Log.DrainWatcher(pair.Key, watcher.Path);
                    watcher.Process(
                        new WatchedEvent(
                            WatchedEvent.WatchedEventType.WatcherRemoved,
                            WatchedEvent.WatchedEventKeeperState.Disconnected,
                            watcher.Path));
                }

                this.watcherMap.Clear();
            }
        }

        /// <summary>
        /// Notifies the request task completion source that the task failed with the given <paramref name="code"/>
        /// </summary>
        /// <param name="request">Request to notify of the failure</param>
        /// <param name="code">The failure code to notify</param>
        private void NotifyRequestFailed(RequestWrapper request, RingMasterException.Code code)
        {
            var response = new RequestResponse()
            {
                ResultCode = (int)code,
            };

            this.ProcessRequestResponse(request.CallId, request, response);
        }

        /// <summary>
        /// Registers the request.
        /// </summary>
        /// <param name="request">Request to register</param>
        /// <returns>Wrapper that represents the request and its metadata</returns>
        private RequestWrapper RegisterRequest(IRingMasterRequest request)
        {
            RequestWrapper requestWrapper;
            lock (this.requestMap)
            {
                requestWrapper = this.AddRequestToMap(request);
            }

            return requestWrapper;
        }

        /// <summary>
        /// Adds the <paramref name="request"/> to <see cref="requestMap"/> with the given request ID
        /// </summary>
        /// <param name="request">Request to add</param>
        /// <returns>Wrapper for the request</returns>
        /// <remarks>This should be called while locking <see cref="requestMap"/></remarks>
        private RequestWrapper AddRequestToMap(IRingMasterRequest request)
        {
            ulong requestId = (ulong)Interlocked.Increment(ref this.requestId);

            RequestWrapper requestWrapper = new RequestWrapper(requestId, request);
            this.requestMap.Add(requestId, requestWrapper);

            if (this.newestPendingRequest != null)
            {
                requestWrapper.Previous = this.newestPendingRequest;
                this.newestPendingRequest.Next = requestWrapper;
            }
            else
            {
                // There are no pending requests so we should set oldestPendingRequest also
                this.oldestPendingRequest = requestWrapper;
            }

            this.newestPendingRequest = requestWrapper;

            return requestWrapper;
        }

        /// <summary>
        /// Tries to remove a request from <see cref="requestMap"/> for the given <paramref name="callId"/>
        /// </summary>
        /// <param name="callId">Id of the call</param>
        /// <returns>Wrapper for the request if the request is in the map and null otherwise.</returns>
        /// <remarks>This should be called while locking <see cref="requestMap"/></remarks>
        private RequestWrapper RemoveRequestFromMap(ulong callId)
        {
            RequestWrapper request;

            if (this.requestMap.TryGetValue(callId, out request))
            {
                this.requestMap.Remove(callId);

                if (request.Previous == null)
                {
                    // must be the oldest request
                    this.oldestPendingRequest = request.Next;
                }
                else
                {
                    request.Previous.Next = request.Next;
                }

                if (request.Next != null)
                {
                    request.Next.Previous = request.Previous;
                }
                else
                {
                    // must be the newest request
                    this.newestPendingRequest = request.Previous;
                }
            }

            return request;
        }

        /// <summary>
        /// Times out the oldest request if it has passed the time out limit.
        /// </summary>
        /// <returns>The minimum time to wait for next request expiry</returns>
        private TimeSpan TimeoutOldestRequest()
        {
            RequestWrapper request;
            lock (this.requestMap)
            {
                var timeoutInterval = TimeSpan.FromMilliseconds(this.Timeout);
                if (this.oldestPendingRequest == null)
                {
                    // There are no pending requests at this time, check again after
                    // the configured timeout interval.
                    return timeoutInterval;
                }

                TimeSpan oldestElapsed = TimeSpan.FromTicks(this.oldestPendingRequest.ElapsedInTicks);

                if (oldestElapsed < timeoutInterval)
                {
                    // The oldest request has not yet timed out.  Checkback when
                    // that request is due to expire.
                    return timeoutInterval - oldestElapsed;
                }

                // Note: no null check required for request as it will always be non-null since we are inside the lock
                request = this.RemoveRequestFromMap(this.oldestPendingRequest.CallId);
            }

            var elapsed = TimeSpan.FromTicks(request.ElapsedInTicks);
            RingMasterClientEventSource.Log.NotifyResponseTimeout(request.CallId, (long)elapsed.TotalMilliseconds);
            this.instrumentation.RequestTimedOut(request.CallId, request.WrappedRequest.RequestType, elapsed);
            this.NotifyRequestFailed(request, RingMasterException.Code.Operationtimeout);

            return TimeSpan.Zero;
        }

        /// <summary>
        /// Send a heart beat request to keep the connection alive.
        /// </summary>
        /// <param name="connection">Connection to keep alive</param>
        /// <returns>A Task that resolves to <c>true</c> if the heartbeat was sent successfully</returns>
        private async Task<bool> SendHeartbeat(IConnection connection)
        {
            ulong beatId = ++this.heartBeatCount;

            RingMasterClientEventSource.Log.SendHeartbeat(beatId);
            this.instrumentation.HeartBeatSent(beatId);
            var requestWrapper = this.RegisterRequest(new RequestExists("<Fail>", watcher: null, uid: beatId));

            // Send the heartbeat request and wait for the response to be received
            Func<Task<RequestResponse>> sendReceive = async () =>
            {
                await this.SendRequest(connection, requestWrapper);
                return await requestWrapper.TaskCompletionSource.Task;
            };

            var responseTask = sendReceive();

            // Wait until the response is received OR timeout
            await Task.WhenAny(responseTask, Task.Delay(this.Timeout));
            if (!responseTask.IsCompleted)
            {
                RingMasterClientEventSource.Log.SendHeartbeatFailed(
                    beatId,
                    $"Timeout waiting for heartbeat response from remote {connection.RemoteEndPoint} ConnectionId={connection.Id}");
                return false;
            }
            else
            {
                return responseTask.Result.ResultCode == (int)RingMasterException.Code.Nonode;
            }
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (!this.disposed)
                {
                    this.disposed = true;

                    this.Close();

                    this.cancellationTokenSource.Dispose();
                    this.outgoingRequests.Dispose();
                    this.outgoingRequestsAvailable.Dispose();
                    this.incomingResponses.Dispose();
                    this.responsesAvailable.Dispose();
                    this.transport.Dispose();
                }
            }
        }

        /// <summary>
        /// Represents a response packet that has not yet been processed.
        /// </summary>
        private struct ResponseWrapper
        {
            public byte[] SerializedResponse;
            public uint ProtocolVersion;
        }

        /// <summary>
        /// Configuration of the request handling
        /// </summary>
        public class Configuration
        {
            /// <summary>
            /// Gets or sets the timeout for requests.
            /// </summary>
            public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMilliseconds(10000);

            /// <summary>
            /// Gets or sets the interval after which a heartbeat message will be sent if the connection is idle.
            /// </summary>
            public TimeSpan HeartBeatInterval { get; set; } = TimeSpan.FromMilliseconds(30000);

            /// <summary>
            /// Gets or sets the maximum number of requests that can be enqueued in the request queue.
            /// </summary>
            public int RequestQueueLength { get; set; } = 1000;

            /// <summary>
            /// Gets or sets the maximum number of responses that can be in process.
            /// </summary>
            public int ResponseQueueLength { get; set; } = 1000;

            /// <summary>
            /// Gets or sets a value indicating whether the backend must use locking even for read-only operations.
            /// </summary>
            public bool RequireLockForReadOnlyOperations { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether the backend must transparently forward requests to the current primary if the
            /// target server is not the primary.
            /// </summary>
            public bool MustTransparentlyForwardRequests { get; set; } = true;
        }

        /// <summary>
        /// Wraps a <see cref="IRingMasterRequest"/> with a task completion source
        /// that is completed when a response is received.
        /// </summary>
        private class RequestWrapper
        {
            /// <summary>
            /// Time this <see cref="RequestWrapper"/> was created.
            /// </summary>
            private readonly long creationTimeTicks;

            /// <summary>
            /// Initializes a new instance of the <see cref="RequestWrapper"/> class.
            /// </summary>
            /// <param name="callId">Id of the call</param>
            /// <param name="wrappedRequest">Request to wrap</param>
            public RequestWrapper(ulong callId, IRingMasterRequest wrappedRequest)
            {
                this.WrappedRequest = wrappedRequest;
                this.TaskCompletionSource = new TaskCompletionSource<RequestResponse>();
                this.CallId = callId;
                this.creationTimeTicks = DateTime.UtcNow.Ticks;
            }

            /// <summary>
            /// Gets the Request that is wrapped by this wrapper.
            /// </summary>
            public IRingMasterRequest WrappedRequest { get; private set; }

            /// <summary>
            /// Gets the TaskCompletionSource that must be completed when a response is received.
            /// </summary>
            public TaskCompletionSource<RequestResponse> TaskCompletionSource { get; private set; }

            /// <summary>
            /// Gets the Id of the call we sent to the server for this request
            /// </summary>
            public ulong CallId { get; private set; }

            /// <summary>
            /// Gets the amount of ticks that have elapsed since the request wrapper was created
            /// </summary>
            public long ElapsedInTicks
            {
                get { return DateTime.UtcNow.Ticks - this.creationTimeTicks; }
            }

            /// <summary>
            /// Gets or sets the watcher installed by this request (if any).
            /// </summary>
            public WatcherWrapper AssociatedWatcher { get; set; }

            /// <summary>
            /// Gets or sets the next newest request
            /// </summary>
            public RequestWrapper Next { get; set; }

            /// <summary>
            /// Gets or sets the next oldest request
            /// </summary>
            public RequestWrapper Previous { get; set; }
        }

        /// <summary>
        /// Wraps a watcher with a unique id and ensures
        /// that single use watchers are not invoked multiple times.
        /// </summary>
        private sealed class WatcherWrapper : IWatcher
        {
            /// <summary>
            /// The request handler associated with this watcher.
            /// </summary>
            private readonly RingMasterRequestHandler handler;

            /// <summary>
            /// The watcher that is wrapped by this wrapper.
            /// </summary>
            private readonly IWatcher watcher;

            /// <summary>
            /// Queue of events received.
            /// </summary>
            private readonly Queue<WatchedEvent> eventQueue;

            /// <summary>
            /// TaskCompletionSource that will be signalled once the response for the request that installed the watcher is received.
            /// </summary>
            private readonly TaskCompletionSource<object> readyToDispatch = new TaskCompletionSource<object>();

            /// <summary>
            /// Initializes a new instance of the <see cref="WatcherWrapper"/> class.
            /// </summary>
            /// <param name="handler">The request handler associated with this watcher</param>
            /// <param name="watcherId">Unique Id of the watcher</param>
            /// <param name="watcher">Watcher that is wrapped by this wrapper</param>
            /// <param name="path">Path associated with the watcher</param>
            public WatcherWrapper(RingMasterRequestHandler handler, ulong watcherId, IWatcher watcher, string path)
            {
                this.handler = handler;
                this.watcher = watcher;
                this.Id = watcherId;
                this.Path = path;
                this.eventQueue = new Queue<WatchedEvent>();
            }

            /// <summary>
            /// Gets the unique id of this watcher.
            /// </summary>
            public ulong Id { get; private set; }

            /// <summary>
            /// Gets the path associated with the watcher.
            /// </summary>
            public string Path { get; private set; }

            /// <summary>
            /// Gets a value indicating whether the watcher is for a single use only.
            /// </summary>
            public bool OneUse => this.Kind.HasFlag(WatcherKind.OneUse);

            /// <summary>
            /// Gets the kind of the watcher, if it is for single use and if the data is included on notification
            /// </summary>
            public WatcherKind Kind
            {
                get
                {
                    return this.watcher.Kind;
                }
            }

            /// <summary>
            /// Enable dispatch for this watcher.
            /// </summary>
            public void EnableDispatch()
            {
                RingMasterClientEventSource.Log.EnableDispatch(this.Id, this.Path);
                this.readyToDispatch.SetResult(null);
            }

            /// <summary>
            /// Processes the specified event.
            /// </summary>
            /// <param name="evt">The event to process</param>
            public void Process(WatchedEvent evt)
            {
                if (evt == null)
                {
                    throw new ArgumentNullException(nameof(evt));
                }

                lock (this)
                {
                    this.handler.instrumentation.WatcherNotificationReceived(evt.EventType);
                    this.eventQueue.Enqueue(evt);
                }

                Task.Run(this.DispatchEvent);
            }

            private async Task DispatchEvent()
            {
                // Watcher notifications can be dispatched only after the
                // response for the request that installed the watcher is
                // processed.
                await this.readyToDispatch.Task;

                // Dispatch all events received so far in the order they were
                // received.
                lock (this)
                {
                    while (this.eventQueue.Count > 0)
                    {
                        WatchedEvent evt = this.eventQueue.Dequeue();
                        RingMasterClientEventSource.Log.DispatchWatcherNotification(this.Id, evt.Path, (int)evt.EventType);
                        this.watcher.Process(evt);
                    }
                }
            }
        }
    }
}
