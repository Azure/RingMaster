// <copyright file="IRingMasterClientInstrumentation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Net;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Interface that is used by <see cref="RingMasterClient"/> to report
    /// metrics.
    /// </summary>
    public interface IRingMasterClientInstrumentation
    {
        /// <summary>
        /// A new connection was created.
        /// </summary>
        /// <param name="connectionId">Unique Id of the connection</param>
        /// <param name="remoteEndPoint">The remote endpoint</param>
        /// <param name="remoteIdentity">Identity of the remote endpoint</param>
        void ConnectionCreated(ulong connectionId, EndPoint remoteEndPoint, string remoteIdentity);

        /// <summary>
        /// An existing connection was closed.
        /// </summary>
        /// <param name="connectionId">Unique Id of the connection</param>
        /// <param name="remoteEndPoint">The remote endpoint</param>
        /// <param name="remoteIdentity">Identity of the remote endpoint</param>
        void ConnectionClosed(ulong connectionId, EndPoint remoteEndPoint, string remoteIdentity);

        /// <summary>
        /// A Request was queued.
        /// </summary>
        /// <param name="requestId">Unique Id of the request</param>
        /// <param name="requestType">Type of the request</param>
        /// <param name="pendingRequestCount">Number of requests that are pending in the queue</param>
        void RequestQueued(ulong requestId, RingMasterRequestType requestType, int pendingRequestCount);

        /// <summary>
        /// A Request was not queued because the number of unsent requests in the queue exceeded configured limit.
        /// </summary>
        /// <param name="requestId">Unique Id of the request</param>
        /// <param name="requestType">Type of the request</param>
        /// <param name="pendingRequestCount">Number of pending requests</param>
        void RequestQueueFull(ulong requestId, RingMasterRequestType requestType, int pendingRequestCount);

        /// <summary>
        /// A Request was sent.
        /// </summary>
        /// <param name="requestId">Unique Id of the request</param>
        /// <param name="requestType">Type of the request</param>
        /// <param name="requestLength">Serialized length of the request</param>
        void RequestSent(ulong requestId, RingMasterRequestType requestType, int requestLength);

        /// <summary>
        /// A Request send failed.
        /// </summary>
        /// <param name="requestId">Unique Id of the request</param>
        /// <param name="requestType">Type of the request</param>
        void RequestSendFailed(ulong requestId, RingMasterRequestType requestType);

        /// <summary>
        /// A Response has been queued for processing.
        /// </summary>
        /// <param name="responseLength">Length of the serialized response</param>
        void ResponseQueued(int responseLength);

        /// <summary>
        /// A Response received for a request was processed.
        /// </summary>
        /// <param name="requestId">Unique Id of the request</param>
        /// <param name="requestType">Type of the request</param>
        /// <param name="resultCode">Result code</param>
        /// <param name="elapsed">Time that has elapsed since the request was queued</param>
        void ResponseProcessed(ulong requestId, RingMasterRequestType requestType, int resultCode, TimeSpan elapsed);

        /// <summary>
        /// A Request timed out.
        /// </summary>
        /// <param name="requestId">Unique Id of the request</param>
        /// <param name="requestType">Type of the request</param>
        /// <param name="elapsed">Time that has elapsed since the request was queued</param>
        void RequestTimedOut(ulong requestId, RingMasterRequestType requestType, TimeSpan elapsed);

        /// <summary>
        /// A Request has been aborted.
        /// </summary>
        /// <param name="requestId">Unique Id of the request</param>
        /// <param name="requestType">Type of the request</param>
        void RequestAborted(ulong requestId, RingMasterRequestType requestType);

        /// <summary>
        /// A Heart beat message was sent.
        /// </summary>
        /// <param name="heartBeatId">Id of the heart beat</param>
        void HeartBeatSent(ulong heartBeatId);

        /// <summary>
        /// A Watcher notification was received.
        /// </summary>
        /// <param name="eventType">Type of the notification event</param>
        void WatcherNotificationReceived(WatchedEvent.WatchedEventType eventType);

        /// <summary>
        /// A notification was received for a watcher that was not present.
        /// </summary>
        void WatcherNotFound();

        /// <summary>
        /// An invalid packet was received as response.
        /// </summary>
        void InvalidPacketReceived();

        /// <summary>
        /// A Response received was not correlated with any pending request.
        /// </summary>
        /// <param name="callId">Id of the request for which response was received</param>
        void UnexpectedResponseReceived(ulong callId);

        /// <summary>
        /// An invalid client message was received.
        /// </summary>
        void InvalidClientMessageReceived();
    }
}