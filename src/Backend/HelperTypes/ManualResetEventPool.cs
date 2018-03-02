// <copyright file="ManualResetEventPool.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    public class ManualResetEventPool : PoolOf<ManualResetEvent>
    {
        public static ManualResetEventPool InstancePool { get; } = new ManualResetEventPool(50000);

        private ManualResetEventPool(int maxCachedElements)
            : base(maxCachedElements, CreateOne, Reset)
        {
        }

        private static ManualResetEvent CreateOne()
        {
            return new ManualResetEvent(false);
        }

        private static void Reset(ManualResetEvent e)
        {
            e.Reset();
        }

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
    }
}