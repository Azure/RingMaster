// <copyright file="CountDownEvent.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Threading;

    public sealed class CountdownEvent : IDisposable
    {
        private int pending;
        private readonly ManualResetEvent e;

        public CountdownEvent(int initialCount)
        {
            this.pending = initialCount;
            this.e = this.pending > 0 ? ManualResetEventPool.InstancePool.GetOne() : null;
        }

        /// <summary>
        /// Adds the specified number to the counter.
        /// the method assumes that the counter is > 0 at the invocation moment.
        /// </summary>
        /// <param name="number">The number to add.</param>
        /// <returns><c>true</c> if the number was added, <c>false</c> otherwise.</returns>
        public bool Add(uint number)
        {
            if (this.e != null)
            {
                Interlocked.Add(ref this.pending, (int)number);
                return true;
            }

            return false;
        }

        public bool DecrementAndSetIfZero()
        {
            if (this.e != null && Interlocked.Decrement(ref this.pending) == 0)
            {
                ManualResetEventPool.InstancePool.Set(this.e);
                return true;
            }

            return this.e == null;
        }

        public void SetToZeroAndSignal()
        {
            if (this.e != null)
            {
                this.pending = 0;
                ManualResetEventPool.InstancePool.Set(this.e);
            }
        }

        public WaitHandle GetWaitHandle()
        {
            return this.e;
        }

        public void WaitOne()
        {
            if (this.e != null)
            {
                ManualResetEventPool.InstancePool.WaitOneAndKeep(this.e);
            }
        }

        public bool WaitOne(int timeout)
        {
            if (this.e != null)
            {
                return ManualResetEventPool.InstancePool.WaitOneAndKeep(timeout, this.e);
            }

            return true;
        }

        public void Dispose()
        {
            if (this.e != null)
            {
                ManualResetEvent ev = this.e;
                ManualResetEventPool.InstancePool.Set(ev);

                ManualResetEventPool.InstancePool.ReturnOne(ref ev);
            }

            GC.SuppressFinalize(this);
        }
    }
}