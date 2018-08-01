// <copyright file="CountDownEvent.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Threading;

    /// <summary>
    /// Count down event
    /// </summary>
    public sealed class CountdownEvent : IDisposable
    {
        private readonly ManualResetEvent e;
        private int pending;

        /// <summary>
        /// Initializes a new instance of the <see cref="CountdownEvent"/> class.
        /// </summary>
        /// <param name="initialCount">Initial count of the event</param>
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

        /// <summary>
        /// Decrements and sets the event if reach to zero
        /// </summary>
        /// <returns>If pending count is greater than 0</returns>
        public bool DecrementAndSetIfZero()
        {
            if (this.e != null && Interlocked.Decrement(ref this.pending) == 0)
            {
                ManualResetEventPool.InstancePool.Set(this.e);
                return true;
            }

            return this.e == null;
        }

        /// <summary>
        /// Sets the pending count to zero and sets the event
        /// </summary>
        public void SetToZeroAndSignal()
        {
            if (this.e != null)
            {
                this.pending = 0;
                ManualResetEventPool.InstancePool.Set(this.e);
            }
        }

        /// <summary>
        /// Gets the wait handle of the event
        /// </summary>
        /// <returns>wait handle</returns>
        public WaitHandle GetWaitHandle()
        {
            return this.e;
        }

        /// <summary>
        /// Waits if the event exists and keeps it (not returning to the pool)
        /// </summary>
        public void WaitOne()
        {
            if (this.e != null)
            {
                ManualResetEventPool.InstancePool.WaitOneAndKeep(this.e);
            }
        }

        /// <summary>
        /// Waits if the event exists and keeps it (not returning to the pool)
        /// </summary>
        /// <param name="timeout">Timeout in millisecond</param>
        /// <returns>true if no pending</returns>
        public bool WaitOne(int timeout)
        {
            if (this.e != null)
            {
                return ManualResetEventPool.InstancePool.WaitOneAndKeep(timeout, this.e);
            }

            return true;
        }

        /// <inheritdoc />
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