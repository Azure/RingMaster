// <copyright file="ILoadBehavior.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    /// <summary>
    /// this interface abstracts a client connection load limiter
    /// </summary>
    public interface ILoadBehavior
    {
        /// <summary>
        /// called when a new request arrives
        /// </summary>
        /// <returns>allow request or fail request, depending on the limiter policies</returns>
        BehaviorAction OnRequestArrived();

        /// <summary>
        /// called when a request is completed, indicating the completion time
        /// </summary>
        /// <param name="timeInTicks">if bigger than zero, the time in ticks ellapsed</param>
        void OnRequestCompleted(long timeInTicks);

        /// <summary>
        /// called when we need to reset the limiter
        /// </summary>
        void Reset();
    }
}