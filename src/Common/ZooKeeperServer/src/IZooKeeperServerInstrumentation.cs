// <copyright file="IZooKeeperServerInstrumentation.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Server.ZooKeeper
{
    using System;
    using System.Net;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Interface that is used by <see cref="RingMasterServer"/> to report
    /// metrics.
    /// </summary>
    public interface IZooKeeperServerInstrumentation
    {
        /// <summary>
        /// A Request was completed.
        /// </summary>
        /// <param name="requestType">Type of request</param>
        /// <param name="elapsed">Time taken to complete the request</param>
        void OnRequestCompleted(RingMasterRequestType requestType, TimeSpan elapsed);

        /// <summary>
        /// A new session was created.
        /// </summary>
        /// <param name="sessionId">Unique Id of the session</param>
        /// <param name="client">Id of the client for which the session was created</param>
        void OnSessionCreated(ulong sessionId, string client);

        /// <summary>
        /// A session was closed.
        /// </summary>
        /// <param name="sessionId">Unique Id of the session</param>
        /// <param name="client">Id of the client</param>
        void OnSessionClosed(ulong sessionId, string client);

        /// <summary>
        /// A bad request was received.
        /// </summary>
        /// <param name="sessionId">Unique Id of the session that received the bad request</param>
        void OnBadRequest(ulong sessionId);

        /// <summary>
        /// A watcher was set.
        /// </summary>
        /// <param name="sessionId">Unique Id of the session in which a watcher was set</param>
        void OnWatcherSet(ulong sessionId);

        /// <summary>
        /// A watcher was notified.
        /// </summary>
        /// <param name="sessionId">Unique Id of the session to which the watcher notificationw as delivered</param>
        void OnWatcherNotified(ulong sessionId);
    }
}