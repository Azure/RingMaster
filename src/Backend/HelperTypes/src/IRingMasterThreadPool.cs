// <copyright file="IRingMasterThreadPool.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// Interface IRingMasterThreadPool
    /// it abstracts threadpools
    /// </summary>
    public interface IRingMasterThreadPool
    {
        /// <summary>
        /// Queues the user work item.
        /// </summary>
        /// <param name="cb">The cb.</param>
        /// <returns><c>true</c> if callback was enqueued, <c>false</c> otherwise.</returns>
        bool QueueUserWorkItem(WaitCallback cb);

        /// <summary>
        /// Queues the user work item.
        /// </summary>
        /// <param name="cb">The cb.</param>
        /// <param name="obj">The object.</param>
        /// <returns><c>true</c> if callback was enqueued, <c>false</c> otherwise.</returns>
        bool QueueUserWorkItem(WaitCallback cb, object obj);

        /// <summary>
        /// Creates the thread.
        /// </summary>
        /// <param name="cb">The cb.</param>
        /// <returns>Thread.</returns>
        Thread CreateThread(ThreadStart cb);

        /// <summary>
        /// registers a callback when the waitObject is signaled
        /// </summary>
        /// <param name="waitObject">Receives the signal</param>
        /// <param name="callBack">The callback to execute</param>
        /// <param name="state">The state</param>
        /// <param name="millisecondsTimeOutInterval">The timeout interval in milliseconds</param>
        /// <param name="executeOnlyOnce">Whether to execute only once</param>
        /// <returns>A registered wait handle</returns>
        RegisteredWaitHandle RegisterWaitForSingleObject(
            WaitHandle waitObject,
            WaitOrTimerCallback callBack,
            object state,
            int millisecondsTimeOutInterval,
            bool executeOnlyOnce);

        /// <summary>
        /// Not in use
        /// </summary>
        /// <param name="ev">not in use</param>
        void RecordWait(ManualResetEvent ev);

        /// <summary>
        /// Not in use
        /// </summary>
        /// <param name="ev">not in use</param>
        /// <param name="isTimeout">not in use 2</param>
        void RecordWaitDone(ManualResetEvent ev, bool isTimeout = false);

        /// <summary>
        /// Not in use
        /// </summary>
        /// <param name="ev">not in use</param>
        void RecordSet(ManualResetEvent ev);

        /// <summary>
        /// Creates the thread.
        /// </summary>
        /// <param name="cb">The cb.</param>
        /// <returns>Thread.</returns>
        Thread CreateThreadParam(ParameterizedThreadStart cb);

        /// <summary>
        /// Sets the unhandled exception.
        /// </summary>
        /// <param name="handler">The handler.</param>
        void SetUnhandledException(Action<Exception> handler);
    }
}