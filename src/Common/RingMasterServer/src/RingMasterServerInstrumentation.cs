// <copyright file="RingMasterServerInstrumentation.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Server
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// Default implementation of <see cref="IRingMasterServerInstrumentation"/> interface.
    /// </summary>
    public sealed class RingMasterServerInstrumentation : IRingMasterServerInstrumentation
    {
        private readonly IMetric1D sessionCreated;
        private readonly IMetric1D sessionClosed;
        private readonly IMetric1D responseTime;
        private readonly IMetric0D badRequests;
        private readonly IMetric0D watcherSet;
        private readonly IMetric0D watcherNotified;

        public RingMasterServerInstrumentation(IMetricsFactory metricsFactory)
        {
            if (metricsFactory == null)
            {
                throw new ArgumentNullException(nameof(metricsFactory));
            }

            this.sessionCreated = metricsFactory.Create1D(nameof(this.sessionCreated), "client");
            this.sessionClosed = metricsFactory.Create1D(nameof(this.sessionClosed), "client");
            this.responseTime = metricsFactory.Create1D(nameof(this.responseTime), "requestType");
            this.badRequests = metricsFactory.Create0D(nameof(this.badRequests));
            this.watcherSet = metricsFactory.Create0D(nameof(this.watcherSet));
            this.watcherNotified = metricsFactory.Create0D(nameof(this.watcherNotified));
        }

        /// <summary>
        /// A Request was completed.
        /// </summary>
        /// <param name="requestType">Type of request</param>
        /// <param name="elapsed">Time taken to complete the request</param>
        public void OnRequestCompleted(RingMasterRequestType requestType, TimeSpan elapsed)
        {
            this.responseTime.LogValue((long)elapsed.TotalMilliseconds, Enum.GetName(typeof(RingMasterRequestType), requestType));
        }

        /// <summary>
        /// A new session was created.
        /// </summary>
        /// <param name="sessionId">Unique Id of the session</param>
        /// <param name="client">Id of the client for which the session was created</param>
        public void OnSessionCreated(ulong sessionId, string client)
        {
            this.sessionCreated.LogValue(1, client);
        }

        /// <summary>
        /// A session was closed.
        /// </summary>
        /// <param name="sessionId">Unique Id of the session</param>
        /// <param name="client">Id of the client</param>
        public void OnSessionClosed(ulong sessionId, string client)
        {
            this.sessionClosed.LogValue(1, client);
        }

        /// <summary>
        /// A bad request was received.
        /// </summary>
        /// <param name="sessionId">Unique Id of the session that received the bad request</param>
        public void OnBadRequest(ulong sessionId)
        {
            this.badRequests.LogValue(1);
        }

        /// <summary>
        /// A watcher was set.
        /// </summary>
        /// <param name="sessionId">Unique Id of the session in which a watcher was set</param>
        public void OnWatcherSet(ulong sessionId)
        {
            this.watcherSet.LogValue(1);
        }

        /// <summary>
        /// A watcher was notified.
        /// </summary>
        /// <param name="sessionId">Unique Id of the session to which the watcher notificationw as delivered</param>
        public void OnWatcherNotified(ulong sessionId)
        {
            this.watcherNotified.LogValue(1);
        }
    }
}
