// <copyright file="RingMasterThreadPool.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// Class RingMasterThreadPool.
    /// </summary>
    public class RingMasterThreadPool : IRingMasterThreadPool
    {
        /// <summary>
        /// The _handler
        /// </summary>
        private Action<Exception> handler = DefaultHandler;

        /// <summary>
        /// Initializes static members of the <see cref="RingMasterThreadPool"/> class.
        /// </summary>
        static RingMasterThreadPool()
        {
            int wth;
            int cpth;

            ThreadPool.GetMaxThreads(out wth, out cpth);
            Debug.WriteLine("max {0} {1}", wth, cpth);

            ThreadPool.GetMinThreads(out wth, out cpth);
            Debug.WriteLine("min {0} {1}", wth, cpth);

            Default = new RingMasterThreadPool();
            Instance = Default;
        }

        /// <summary>
        /// Gets the default
        /// </summary>
        public static IRingMasterThreadPool Default { get; }

        /// <summary>
        /// Gets or sets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static IRingMasterThreadPool Instance { get; set; }

        /// <summary>
        /// Creates the thread.
        /// </summary>
        /// <param name="cb">The cb.</param>
        /// <returns>Thread.</returns>
        public Thread CreateThread(ThreadStart cb)
        {
            return new Thread(this.WrapInHandler(cb));
        }

        /// <summary>
        /// Creates the thread.
        /// </summary>
        /// <param name="cb">The cb.</param>
        /// <returns>Thread.</returns>
        public Thread CreateThreadParam(ParameterizedThreadStart cb)
        {
            return new Thread(this.WrapInHandler(cb));
        }

        /// <summary>
        /// Queues the user work item.
        /// </summary>
        /// <param name="cb">The cb.</param>
        /// <returns><c>true</c> if callback was enqueued, <c>false</c> otherwise.</returns>
        public bool QueueUserWorkItem(WaitCallback cb)
        {
            if (!ThreadPool.QueueUserWorkItem(this.WrapInHandler(cb)))
            {
                WaitCallback v = this.WrapInHandler(cb);
                Thread t = new Thread(o => { v(o); });
                t.Start();
            }

            return true;
        }

        /// <summary>
        /// Queues the user work item.
        /// </summary>
        /// <param name="cb">The cb.</param>
        /// <param name="obj">The object.</param>
        /// <returns><c>true</c> if callback was enqueued, <c>false</c> otherwise.</returns>
        public bool QueueUserWorkItem(WaitCallback cb, object obj)
        {
            return this.QueueUserWorkItem(_ => cb(obj));
        }

        /// <inheritdoc />
        public RegisteredWaitHandle RegisterWaitForSingleObject(
            WaitHandle waitObject,
            WaitOrTimerCallback callBack,
            object state,
            int millisecondsTimeOutInterval,
            bool executeOnlyOnce)
        {
            return ThreadPool.RegisterWaitForSingleObject(
                waitObject,
                this.WrapInHandler(callBack),
                state,
                millisecondsTimeOutInterval,
                executeOnlyOnce);
        }

        /// <inheritdoc />
        public void RecordWait(ManualResetEvent ev)
        {
        }

        /// <inheritdoc />
        public void RecordSet(ManualResetEvent ev)
        {
        }

        /// <inheritdoc />
        public void RecordWaitDone(ManualResetEvent ev, bool isTimeout)
        {
        }

        /// <summary>
        /// Wraps the in handler.
        /// </summary>
        /// <param name="cb">The cb.</param>
        /// <returns>ThreadStart.</returns>
        public ThreadStart WrapInHandler(ThreadStart cb)
        {
            Action<Exception> h = this.handler;
            if (h == null)
            {
                return cb;
            }

            return () =>
            {
                try
                {
                    cb();
                }
                catch (Exception e)
                {
                    h(e);
                }
            };
        }

        /// <summary>
        /// Gets a callback wrapping the specified callback with try catch
        /// </summary>
        /// <param name="cb">callback to wrap</param>
        /// <returns>Wrapped callback</returns>
        public WaitOrTimerCallback WrapInHandler(WaitOrTimerCallback cb)
        {
            Action<Exception> h = this.handler;
            if (h == null)
            {
                return cb;
            }

            return (ign, to) =>
            {
                try
                {
                    cb(ign, to);
                }
                catch (Exception e)
                {
                    h(e);
                }
            };
        }

        /// <summary>
        /// Wraps the in parameterized handler.
        /// </summary>
        /// <param name="cb">The cb.</param>
        /// <returns>ParameterizedThreadStart.</returns>
        public ParameterizedThreadStart WrapInHandler(ParameterizedThreadStart cb)
        {
            Action<Exception> h = this.handler;
            if (h == null)
            {
                return cb;
            }

            return ign =>
            {
                try
                {
                    cb(ign);
                }
                catch (Exception e)
                {
                    h(e);
                }
            };
        }

        /// <summary>
        /// Wraps the in handler.
        /// </summary>
        /// <param name="cb">The cb.</param>
        /// <returns>WaitCallback.</returns>
        public WaitCallback WrapInHandler(WaitCallback cb)
        {
            Action<Exception> h = this.handler;
            if (h == null)
            {
                return cb;
            }

            return ign =>
            {
                try
                {
                    cb(ign);
                }
                catch (Exception e)
                {
                    h(e);
                }
            };
        }

        /// <summary>
        /// Sets the unhandled exception.
        /// </summary>
        /// <param name="handler">The handler.</param>
        public void SetUnhandledException(Action<Exception> handler)
        {
            this.handler = handler;
        }

        private static void DefaultHandler(Exception e)
        {
            Trace.TraceError("Exception happened " + e);
        }
    }
}
