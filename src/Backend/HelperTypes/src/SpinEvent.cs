// <copyright file="SpinEvent.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Threading;

    /// <summary>
    /// Struct SpinEvent
    /// </summary>
    public class SpinEvent
    {
        /// <summary>
        /// The spinning factor
        /// </summary>
        private const int SpinningFactor = 100;

        /// <summary>
        /// The sleep one frequency
        /// </summary>
        private const int SleepOneFrequency = 40;

        /// <summary>
        /// The sleep zero frequency
        /// </summary>
        private const int SleepZeroFrequency = 10;

        /// <summary>
        /// The lock identifier disable mask
        /// </summary>
        private const int LockIdDisableMask = -2147483648;

        /// <summary>
        /// The lock anonymous owned
        /// </summary>
        private const int LockAnonymousOwned = 1;

        /// <summary>
        /// The waiters mask
        /// </summary>
        private const int WaitersMask = 2147483646;

        /// <summary>
        /// The lock unowned
        /// </summary>
        private const int LockUnowned = 0;

        /// <summary>
        /// The _maximum waiters
        /// </summary>
        private static readonly int MaximumWaiters = 2147483646;

        /// <summary>
        /// The _m owner
        /// </summary>
        private int mOwner;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpinEvent"/> class.
        /// </summary>
        public SpinEvent()
        {
            this.mOwner = LockIdDisableMask;
        }

        /// <summary>
        /// Gets a value indicating whether the lock is currently held by any thread.
        /// </summary>
        /// <value><c>true</c> if this instance is held; otherwise, <c>false</c>.</value>
        public bool IsHeld => (this.mOwner & LockAnonymousOwned) != LockUnowned;

        /// <summary>
        /// Acquires the lock in a reliable manner, such that even if an exception occurs within the method call, lockTaken can be examined reliably to determine whether the lock was acquired.
        /// </summary>
        /// <exception cref="T:System.ArgumentException">The lockTaken argument must be initialized to false prior to calling Enter.</exception>
        /// <exception cref="T:System.Threading.LockRecursionException">Thread ownership tracking is enabled, and the current thread has already acquired this lock.</exception>
        public void WaitOne()
        {
            bool lockTaken = false;
            this.ContinueTryEnter(-1, ref lockTaken);
        }

        /// <summary>
        /// Attempts to acquire the lock in a reliable manner, such that even if an exception occurs within the method call, lockTaken can be examined reliably to determine whether the lock was acquired.
        /// </summary>
        /// <returns><c>true</c> if could enter the lock, <c>false</c> otherwise.</returns>
        /// <exception cref="T:System.ArgumentException">The lockTaken argument must be initialized to false prior to calling TryEnter.</exception>
        /// <exception cref="T:System.Threading.LockRecursionException">Thread ownership tracking is enabled, and the current thread has already acquired this lock.</exception>
        public bool TryEnter()
        {
            return this.TryWaitOne(0);
        }

        /// <summary>
        /// Attempts to acquire the lock in a reliable manner, such that even if an exception occurs within the method call, lockTaken can be examined reliably to determine whether the lock was acquired.
        /// </summary>
        /// <param name="timeout">A <see cref="T:System.TimeSpan" /> that represents the number of milliseconds to wait, or a <see cref="T:System.TimeSpan" /> that represents -1 milliseconds to wait indefinitely.</param>
        /// <returns><c>true</c> if could enter the lock within the timeout, <c>false</c> otherwise.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">timeout;timeout must be -1 or &lt;=int.MaxValue</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout" /> is a negative number other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater than <see cref="F:System.Int32.MaxValue" /> milliseconds.</exception>
        /// <exception cref="T:System.ArgumentException">The lockTaken argument must be initialized to false prior to calling TryEnter.</exception>
        /// <exception cref="T:System.Threading.LockRecursionException">Thread ownership tracking is enabled, and the current thread has already acquired this lock.</exception>
        public bool TryEnter(TimeSpan timeout)
        {
            long num = (long)timeout.TotalMilliseconds;
            if (num < -1L || num > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "timeout must be -1 or <=int.MaxValue");
            }

            return this.TryWaitOne((int)timeout.TotalMilliseconds);
        }

        /// <summary>
        /// Attempts to acquire the lock in a reliable manner, such that even if an exception occurs within the method call, lockTaken can be examined reliably to determine whether the lock was acquired.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see cref="F:System.Threading.Timeout.Infinite" /> (-1) to wait indefinitely.</param>
        /// <returns><c>true</c> if could acquire within the timeout, <c>false</c> otherwise.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">millisecondsTimeout;millisecondsTimeout cannot be &lt;-1</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="millisecondsTimeout" /> is a negative number other than -1, which represents an infinite time-out.</exception>
        /// <exception cref="T:System.ArgumentException">The lockTaken argument must be initialized to false prior to calling TryEnter.</exception>
        /// <exception cref="T:System.Threading.LockRecursionException">Thread ownership tracking is enabled, and the current thread has already acquired this lock.</exception>
        public bool TryWaitOne(int millisecondsTimeout)
        {
            bool lockTaken = false;

            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout), millisecondsTimeout, "millisecondsTimeout cannot be <-1");
            }

            this.ContinueTryEnter(millisecondsTimeout, ref lockTaken);

            return lockTaken;
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">lock is not held</exception>
        /// <exception cref="T:System.Threading.SynchronizationLockException">Thread ownership tracking is enabled, and the current thread is not the owner of this lock.</exception>
        public void Release()
        {
            Thread.BeginCriticalRegion();
            if ((this.mOwner & LockAnonymousOwned) == LockUnowned)
            {
                Thread.EndCriticalRegion();
                throw new InvalidOperationException("lock is not held");
            }

            Interlocked.Decrement(ref this.mOwner);

            Thread.EndCriticalRegion();
        }

        /// <summary>
        /// Continues the try enter.
        /// </summary>
        /// <param name="millisecondsTimeout">The milliseconds timeout.</param>
        /// <param name="lockTaken">if set to <c>true</c> [lock taken].</param>
        private void ContinueTryEnter(int millisecondsTimeout, ref bool lockTaken)
        {
            uint startTime = 0u;
            if (millisecondsTimeout != -1 && millisecondsTimeout != 0)
            {
                startTime = TimeoutHelper.GetTime();
            }

            int numWaiters = int.MaxValue;
            int owner = this.mOwner;

            if ((owner & LockAnonymousOwned) == LockUnowned)
            {
                Thread.BeginCriticalRegion();
                if (Interlocked.CompareExchange(ref this.mOwner, owner | LockAnonymousOwned, owner) == owner)
                {
                    lockTaken = true;
                    return;
                }

                Thread.EndCriticalRegion();
            }
            else if ((owner & WaitersMask) != MaximumWaiters)
            {
                numWaiters = (Interlocked.Add(ref this.mOwner, 2) & WaitersMask) >> 1;
            }

            if (millisecondsTimeout == 0 || (millisecondsTimeout != -1 && TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) <= 0))
            {
                this.DecrementWaiters();
                return;
            }

            int processorCount = PlatformHelper.ProcessorCount;
            if (numWaiters < processorCount)
            {
                int multiplier = 1;
                for (int i = 1; i <= numWaiters * SpinningFactor; i++)
                {
                    Thread.SpinWait((numWaiters + i) * SpinningFactor * multiplier);
                    if (multiplier < processorCount)
                    {
                        multiplier++;
                    }

                    owner = this.mOwner;
                    if ((owner & LockAnonymousOwned) == LockUnowned)
                    {
                        Thread.BeginCriticalRegion();
                        int value = ((owner & WaitersMask) == 0) ? (owner | LockAnonymousOwned) : (owner - 2 | LockAnonymousOwned);
                        if (Interlocked.CompareExchange(ref this.mOwner, value, owner) == owner)
                        {
                            lockTaken = true;
                            return;
                        }

                        Thread.EndCriticalRegion();
                    }
                }
            }

            if (millisecondsTimeout != -1 && TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) <= 0)
            {
                this.DecrementWaiters();
                return;
            }

            int iterations = 0;
            while (true)
            {
                iterations++;

                owner = this.mOwner;
                if ((owner & LockAnonymousOwned) == LockUnowned)
                {
                    Thread.BeginCriticalRegion();
                    int value2 = ((owner & WaitersMask) == 0) ? (owner | LockAnonymousOwned) : (owner - 2 | LockAnonymousOwned);
                    if (Interlocked.CompareExchange(ref this.mOwner, value2, owner) == owner)
                    {
                        lockTaken = true;
                        break;
                    }

                    Thread.EndCriticalRegion();
                }

                if (iterations % SleepOneFrequency == 0)
                {
                    Thread.Sleep(1);
                }
                else if (iterations % SleepZeroFrequency == 0)
                {
                    Thread.Sleep(0);
                }
                else
                {
                    Thread.Yield();
                }

                if (iterations % SleepZeroFrequency == 0 && millisecondsTimeout != -1 && TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) <= 0)
                {
                    this.DecrementWaiters();
                    return;
                }
            }
        }

        /// <summary>
        /// Decrements the waiters.
        /// </summary>
        private void DecrementWaiters()
        {
            SpinWait spinWait = default(SpinWait);
            while (true)
            {
                int owner = this.mOwner;
                if ((owner & WaitersMask) == 0)
                {
                    break;
                }

                if (Interlocked.CompareExchange(ref this.mOwner, owner - 2, owner) == owner)
                {
                    return;
                }

                spinWait.SpinOnce();
            }
        }

        /// <summary>
        /// Continues the try enter with thread tracking.
        /// </summary>
        /// <param name="millisecondsTimeout">The milliseconds timeout.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="lockTaken">if set to <c>true</c> [lock taken].</param>
        /// <exception cref="System.Threading.LockRecursionException">attempt to acquire the lock recursively</exception>
        private void ContinueTryEnterWithThreadTracking(int millisecondsTimeout, uint startTime, ref bool lockTaken)
        {
            int num = 0;
            int managedThreadId = Thread.CurrentThread.ManagedThreadId;
            if (this.mOwner == managedThreadId)
            {
                throw new LockRecursionException("attempt to acquire the lock recursively");
            }

            SpinWait spinWait = default(SpinWait);
            while (true)
            {
                spinWait.SpinOnce();
                if (this.mOwner == num)
                {
                    Thread.BeginCriticalRegion();
                    if (Interlocked.CompareExchange(ref this.mOwner, managedThreadId, num) == num)
                    {
                        lockTaken = true;
                        break;
                    }

                    Thread.EndCriticalRegion();
                }

                if (millisecondsTimeout == 0 || (millisecondsTimeout != -1 && spinWait.NextSpinWillYield && TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) <= 0))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Class TimeoutHelper.
        /// </summary>
        internal static class TimeoutHelper
        {
            /// <summary>
            /// Gets the time.
            /// </summary>
            /// <returns>System.UInt32.</returns>
            public static uint GetTime()
            {
                return (uint)Environment.TickCount;
            }

            /// <summary>
            /// Updates the time out.
            /// </summary>
            /// <param name="startTime">The start time.</param>
            /// <param name="originalWaitMillisecondsTimeout">The original wait milliseconds timeout.</param>
            /// <returns>System.Int32.</returns>
            public static int UpdateTimeOut(uint startTime, int originalWaitMillisecondsTimeout)
            {
                uint num = GetTime() - startTime;
                if (num > (uint)int.MaxValue)
                {
                    return 0;
                }

                int num2 = originalWaitMillisecondsTimeout - (int)num;
                if (num2 <= 0)
                {
                    return 0;
                }

                return num2;
            }
        }
    }
}