// <copyright file="ManualResetEventPool.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    /// <summary>
    /// Pool of <see cref="ManualResetEvent"/> objects
    /// </summary>
    public class ManualResetEventPool : PoolOf<ManualResetEvent>
    {
        private ManualResetEventPool(int maxCachedElements)
            : base(maxCachedElements, CreateOne, Reset)
        {
        }

        /// <summary>
        /// Gets the instance of <see cref="ManualResetEventPool"/> class
        /// </summary>
        public static ManualResetEventPool InstancePool { get; } = new ManualResetEventPool(50000);

        /// <summary>
        /// Sets the specified event
        /// </summary>
        /// <param name="ev">Event to be set</param>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Unnecessary")]
        public void Set(ManualResetEvent ev)
        {
            if (ev == null)
            {
                throw new ArgumentNullException(nameof(ev));
            }

            RingMasterThreadPool.Instance.RecordSet(ev);
            ev.Set();
        }

        /// <summary>
        /// Waits the specified event and returns it to the pool
        /// </summary>
        /// <param name="ev">Event to wait</param>
        /// <returns>If the event occurs</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Unnecessary")]
        public bool WaitOneAndReturn(ref ManualResetEvent ev)
        {
            if (ev == null)
            {
                throw new ArgumentNullException(nameof(ev));
            }

            RingMasterThreadPool.Instance.RecordWait(ev);
            bool res = ev.WaitOne();
            RingMasterThreadPool.Instance.RecordWaitDone(ev, !res);
            if (res)
            {
                this.ReturnOne(ref ev);
            }

            return res;
        }

        /// <summary>
        /// Waits the specified event and returns it to the pool
        /// </summary>
        /// <param name="timeout">Timeout in millisecond</param>
        /// <param name="ev">Event to wait</param>
        /// <returns>If the event occurs</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Unnecessary")]
        public bool WaitOneAndReturn(int timeout, ref ManualResetEvent ev)
        {
            if (ev == null)
            {
                throw new ArgumentNullException(nameof(ev));
            }

            RingMasterThreadPool.Instance.RecordWait(ev);

            bool res = ev.WaitOne(timeout);
            RingMasterThreadPool.Instance.RecordWaitDone(ev, !res);
            if (res)
            {
                this.ReturnOne(ref ev);
            }

            return res;
        }

        /// <summary>
        /// Waits the specified event and does not return it to the pool
        /// </summary>
        /// <param name="ev">Event to wait</param>
        /// <param name="timeoutInMillis">Timeout in millisecond</param>
        /// <returns>If the event occurs</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Unnecessary")]
        public bool WaitOneAndKeep(ManualResetEvent ev, int timeoutInMillis)
        {
            if (ev == null)
            {
                throw new ArgumentNullException(nameof(ev));
            }

            RingMasterThreadPool.Instance.RecordWait(ev);
            bool res = ev.WaitOne(timeoutInMillis);
            RingMasterThreadPool.Instance.RecordWaitDone(ev);
            return res;
        }

        /// <summary>
        /// Waits the specified event and does not return it to the pool
        /// </summary>
        /// <param name="ev">Event to wait</param>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Unnecessary")]
        public void WaitOneAndKeep(ManualResetEvent ev)
        {
            if (ev == null)
            {
                throw new ArgumentNullException(nameof(ev));
            }

            RingMasterThreadPool.Instance.RecordWait(ev);
            ev.WaitOne();
            RingMasterThreadPool.Instance.RecordWaitDone(ev);
        }

        /// <summary>
        /// Waits the specified event and does not return it to the pool
        /// </summary>
        /// <param name="timeout">Timeout in millisecond</param>
        /// <param name="ev">Event to wait</param>
        /// <returns>If the event occurs</returns>
        [SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Unnecessary")]
        public bool WaitOneAndKeep(int timeout, ManualResetEvent ev)
        {
            if (ev == null)
            {
                throw new ArgumentNullException(nameof(ev));
            }

            RingMasterThreadPool.Instance.RecordWait(ev);
            bool res = ev.WaitOne(timeout);
            RingMasterThreadPool.Instance.RecordWaitDone(ev, !res);
            return res;
        }

        private static ManualResetEvent CreateOne()
        {
            return new ManualResetEvent(false);
        }

        private static void Reset(ManualResetEvent e)
        {
            e.Reset();
        }
    }
}