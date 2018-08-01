// <copyright file="ExecutionQueue.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Class ExecutionQueue.
    /// </summary>
    public class ExecutionQueue
    {
        /// <summary>
        /// The _threads
        /// </summary>
        private readonly List<Thread> threads = new List<Thread>();

        /// <summary>
        /// The _actions
        /// </summary>
        private readonly Queue<IRunnable> actions = new Queue<IRunnable>();

        /// <summary>
        /// The _empty ev
        /// </summary>
        private readonly SpinEvent emptyEv = new SpinEvent();

        /// <summary>
        /// The MaxCachedElements threads
        /// </summary>
        private int maxThreads;

        /// <summary>
        /// The _gate
        /// </summary>
        private Func<IRunnable, bool> gate;

        /// <summary>
        /// The _on empty
        /// </summary>
        private Action onEmpty;

        /// <summary>
        /// The _in flight
        /// </summary>
        private int inFlight;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionQueue"/> class.
        /// </summary>
        /// <param name="maxThreads">The maximum threads.</param>
        public ExecutionQueue(int maxThreads)
        {
            this.maxThreads = maxThreads;
        }

        /// <summary>
        /// Enum DrainMode
        /// </summary>
        public enum DrainMode
        {
            /// <summary>
            /// The disallow all further enqueues
            /// </summary>
            DisallowAllFurtherEnqueues,

            /// <summary>
            /// The disallow all further enqueues, and removes all elements from the queue. Waits for the inflight to finish.
            /// </summary>
            DisallowAllFurtherEnqueuesAndRemoveAllElements,

            /// <summary>
            /// The block enqueues until drained
            /// </summary>
            BlockEnqueuesUntilDrained,

            /// <summary>
            /// The allow enqueues after drain point
            /// </summary>
            AllowEnqueuesAfterDrainPoint,

            /// <summary>
            /// The allow enqueues and wait for full drain
            /// </summary>
            AllowEnqueuesAndWaitForFullDrain,
        }

        /// <summary>
        /// Interface IRunnable
        /// </summary>
        public interface IRunnable
        {
            /// <summary>
            /// Runs this instance.
            /// </summary>
            void Run();
        }

        /// <summary>
        /// Gets the in flight count.
        /// </summary>
        /// <value>The in flight count.</value>
        public int InFlightCount
        {
            get
            {
                lock (this.actions)
                {
                    return this.inFlight;
                }
            }
        }

        /// <summary>
        /// Gets the pending count.
        /// </summary>
        /// <value>The pending count.</value>
        public int PendingCount
        {
            get
            {
                lock (this.actions)
                {
                    return this.inFlight + this.actions.Count;
                }
            }
        }

        /// <summary>
        /// sets the new max number of processor thread
        /// </summary>
        /// <param name="maxThreads">new max number of threads</param>
        public void SetMaxThreads(int maxThreads)
        {
            lock (this.actions)
            {
                this.maxThreads = maxThreads;
            }
        }

        /// <summary>
        /// sets the event gate for each enqueue. Enqueue will call e.WaitOne before proceeding to the enqueing
        /// </summary>
        /// <param name="gate">The gate.</param>
        public void SetGateEvent(Func<IRunnable, bool> gate)
        {
            this.gate = gate;
        }

        /// <summary>
        /// runs the action only once, next time there is no element in the queue (potentially now).
        /// </summary>
        /// <param name="onEmpty">The on empty.</param>
        public void RunOnNextEmpty(Action onEmpty)
        {
            lock (this.actions)
            {
                if (onEmpty == null)
                {
                    this.onEmpty = null;
                    return;
                }

                if (this.actions.Count == 0 && this.inFlight == 0)
                {
                    onEmpty();
                }
                else
                {
                    this.onEmpty = onEmpty;
                }
            }
        }

        /// <summary>
        /// Enqueues the specified action.
        /// </summary>
        /// <typeparam name="T">The parameter type.</typeparam>
        /// <param name="action">The action.</param>
        /// <param name="obj">The object.</param>
        public void Enqueue<T>(Action<T> action, T obj)
        {
            this.Enqueue(new ArgRunnable<T>(action, obj));
        }

        /// <summary>
        /// Enqueues the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="obj">The object.</param>
        public void Enqueue(WaitCallback action, object obj)
        {
            this.Enqueue(new ArgRunnableWaitCallback(action, obj));
        }

        /// <summary>
        /// Enqueues the specified action.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <typeparam name="T2">The type of the t2.</typeparam>
        /// <param name="action">The action.</param>
        /// <param name="obj">The object.</param>
        /// <param name="obj2">The obj2.</param>
        public void Enqueue<T1, T2>(Action<T1, T2> action, T1 obj, T2 obj2)
        {
            this.Enqueue(new ArgRunnable<T1, T2>(action, obj, obj2));
        }

        /// <summary>
        /// Enqueues the specified action.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <typeparam name="T2">The type of the t2.</typeparam>
        /// <typeparam name="T3">The type of the t3.</typeparam>
        /// <param name="action">The action.</param>
        /// <param name="obj">The object.</param>
        /// <param name="obj2">The obj2.</param>
        /// <param name="obj3">The obj3.</param>
        public void Enqueue<T1, T2, T3>(Action<T1, T2, T3> action, T1 obj, T2 obj2, T3 obj3)
        {
            this.Enqueue(new ArgRunnable<T1, T2, T3>(action, obj, obj2, obj3));
        }

        /// <summary>
        /// Enqueues the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        public void Enqueue(Action action)
        {
            this.Enqueue(new NoArgRunnable(action));
        }

        /// <summary>
        /// drains the queue (i.e. waits for the current elements to complete execution before returning
        /// </summary>
        /// <param name="mode">DrainageMode
        /// if DisallowAllFurtherEnqueues, disables more additions to the queue until ReEnableAfterDrain is invoked.
        /// If BlockEnqueuesUntilDrained, new additions will be blocked until drain is done.
        /// If AllowEnqueuesAfterDrainPoint, further enqueues are allowed, and drain will return when all CURRENTLY enqueued elements are consumed.
        /// If AllowEnqueuesAndWaitForFullDrain, further enqueues are allowed, and drain will return only when all enqueued elements (CURRENTLY present or enqueued during the drainage) are consumed.</param>
        public void Drain(DrainMode mode)
        {
            switch (mode)
            {
                case DrainMode.AllowEnqueuesAndWaitForFullDrain:
                {
                    ManualResetEvent e = ManualResetEventPool.InstancePool.GetOne();
                    this.RunOnNextEmpty(() =>
                    {
                        ManualResetEventPool.InstancePool.Set(e);
                    });
                    ManualResetEventPool.InstancePool.WaitOneAndReturn(ref e);

                    break;
                }

                case DrainMode.DisallowAllFurtherEnqueues:
                {
                    ManualResetEvent e = ManualResetEventPool.InstancePool.GetOne();
                    this.SetGateEvent(r =>
                    {
                        throw new InvalidOperationException("the queue has been disabled");
                    });
                    this.RunOnNextEmpty(() =>
                    {
                        ManualResetEventPool.InstancePool.Set(e);
                    });
                    ManualResetEventPool.InstancePool.WaitOneAndReturn(ref e);

                    break;
                }

                case DrainMode.DisallowAllFurtherEnqueuesAndRemoveAllElements:
                {
                    ManualResetEvent e = ManualResetEventPool.InstancePool.GetOne();
                    this.SetGateEvent(r =>
                    {
                        throw new InvalidOperationException("the queue has been disabled");
                    });
                    lock (this.actions)
                    {
                        this.actions.Clear();
                        this.Enqueue(
                            new NoArgRunnable(() =>
                            {
                                this.RunOnNextEmpty(() =>
                                {
                                    ManualResetEventPool.InstancePool.Set(e);
                                });
                            }), true);
                    }

                    ManualResetEventPool.InstancePool.WaitOneAndReturn(ref e);
                    break;
                }

                case DrainMode.BlockEnqueuesUntilDrained:
                {
                    ManualResetEvent e = ManualResetEventPool.InstancePool.GetOne();
                    this.SetGateEvent(r =>
                    {
                        e.WaitOne();
                        return true;
                    });
                    this.RunOnNextEmpty(() =>
                    {
                        ManualResetEventPool.InstancePool.Set(e);
                    });
                    ManualResetEventPool.InstancePool.WaitOneAndReturn(ref e);
                    this.SetGateEvent(null);
                    break;
                }

                case DrainMode.AllowEnqueuesAfterDrainPoint:
                {
                    ManualResetEvent e = ManualResetEventPool.InstancePool.GetOne();
                    Queue<IRunnable> tempqueue = new Queue<IRunnable>();
                    bool done = false;
                    this.SetGateEvent(r =>
                    {
                        lock (tempqueue)
                        {
                            if (!done)
                            {
                                tempqueue.Enqueue(r);
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                    });
                    this.RunOnNextEmpty(() =>
                    {
                        ManualResetEventPool.InstancePool.Set(e);
                    });
                    ManualResetEventPool.InstancePool.WaitOneAndReturn(ref e);
                    lock (this.actions)
                    {
                        lock (tempqueue)
                        {
                            done = true;
                            foreach (IRunnable r in tempqueue)
                            {
                                this.actions.Enqueue(r);
                            }
                        }

                        this.SetGateEvent(null);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Reenables the after drain.
        /// </summary>
        public void ReenableAfterDrain()
        {
            this.SetGateEvent(null);
        }

        /// <summary>
        /// Enqueues the specified runnable.
        /// </summary>
        /// <param name="runnable">The runnable.</param>
        /// <param name="skipgate">If true, the gate will not be executed</param>
        private void Enqueue(IRunnable runnable, bool skipgate = false)
        {
            Func<IRunnable, bool> e = this.gate;

            if (e != null && !skipgate)
            {
                if (!e(runnable))
                {
                    return;
                }
            }

            lock (this.actions)
            {
                this.actions.Enqueue(runnable);
                if (this.actions.Count <= this.maxThreads)
                {
                    this.MaybeCreateThread();
                }
            }
        }

        /// <summary>
        /// Maybes the create thread.
        /// </summary>
        private void MaybeCreateThread()
        {
            lock (this.threads)
            {
                if (this.threads.Count < this.maxThreads)
                {
                    Thread th = RingMasterThreadPool.Instance.CreateThread(this.RunInThread);
                    this.threads.Add(th);
                    th.Start();
                }

                lock (this.actions)
                {
                    if (this.emptyEv.IsHeld)
                    {
                        this.emptyEv.Release();
                    }
                }
            }
        }

        /// <summary>
        /// Runs the in thread.
        /// </summary>
        private void RunInThread()
        {
            DateTime lastRun = DateTime.UtcNow;

            while (true)
            {
                Monitor.Enter(this.actions);
                if (this.actions.Count > 0)
                {
                    IRunnable torun = this.actions.Dequeue();
                    this.inFlight++;
                    Monitor.Exit(this.actions);
                    torun.Run();
                    lastRun = DateTime.UtcNow;
                    lock (this.actions)
                    {
                        this.inFlight--;

                        if (this.onEmpty != null && this.actions.Count == 0 && this.inFlight == 0)
                        {
                            this.onEmpty();
                            this.onEmpty = null;
                        }
                    }
                }
                else
                {
                    Monitor.Exit(this.actions);

                    bool taken = false;

                    int toWait = 5000 - (int)(DateTime.UtcNow - lastRun).TotalMilliseconds;

                    if (toWait > 0)
                    {
                        taken = this.emptyEv.TryWaitOne(toWait);
                    }

                    if (!taken)
                    {
                        lock (this.threads)
                        {
                            this.threads.Remove(Thread.CurrentThread);
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Class NoArgRunnable.
        /// </summary>
        public class NoArgRunnable : IRunnable
        {
            /// <summary>
            /// The _action
            /// </summary>
            private readonly Action action;

            /// <summary>
            /// Initializes a new instance of the <see cref="NoArgRunnable"/> class.
            /// </summary>
            /// <param name="action">The action.</param>
            public NoArgRunnable(Action action)
            {
                this.action = action;
            }

            /// <summary>
            /// Runs this instance.
            /// </summary>
            public void Run()
            {
                this.action();
            }
        }

        /// <summary>
        /// Callback runnable with single argument
        /// </summary>
        public class ArgRunnableWaitCallback : IRunnable
        {
            /// <summary>
            /// The _action
            /// </summary>
            private readonly WaitCallback action;

            /// <summary>
            /// The _obj
            /// </summary>
            private readonly object obj;

            /// <summary>
            /// Initializes a new instance of the <see cref="ArgRunnableWaitCallback"/> class.
            /// </summary>
            /// <param name="action">The action.</param>
            /// <param name="obj">The object.</param>
            public ArgRunnableWaitCallback(WaitCallback action, object obj)
            {
                this.action = action;
                this.obj = obj;
            }

            /// <summary>
            /// Runs this instance.
            /// </summary>
            public void Run()
            {
                this.action(this.obj);
            }
        }

        /// <summary>
        /// Class ArgRunnable.
        /// </summary>
        /// <typeparam name="T">The parameter type.</typeparam>
        public class ArgRunnable<T> : IRunnable
        {
            /// <summary>
            /// The _action
            /// </summary>
            private readonly Action<T> action;

            /// <summary>
            /// The _obj
            /// </summary>
            private readonly T obj;

            /// <summary>
            /// Initializes a new instance of the <see cref="ArgRunnable{T}"/> class.
            /// </summary>
            /// <param name="action">The action.</param>
            /// <param name="obj">The object.</param>
            public ArgRunnable(Action<T> action, T obj)
            {
                this.action = action;
                this.obj = obj;
            }

            /// <summary>
            /// Runs this instance.
            /// </summary>
            public void Run()
            {
                this.action(this.obj);
            }
        }

        /// <summary>
        /// Class ArgRunnable.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <typeparam name="T2">The type of the t2.</typeparam>
        public class ArgRunnable<T1, T2> : IRunnable
        {
            /// <summary>
            /// The _action
            /// </summary>
            private readonly Action<T1, T2> action;

            /// <summary>
            /// The _obj
            /// </summary>
            private readonly T1 obj;

            /// <summary>
            /// The _obj2
            /// </summary>
            private readonly T2 obj2;

            /// <summary>
            /// Initializes a new instance of the <see cref="ArgRunnable{T1, T2}"/> class.
            /// </summary>
            /// <param name="action">The action.</param>
            /// <param name="obj">The object.</param>
            /// <param name="obj2">The obj2.</param>
            public ArgRunnable(Action<T1, T2> action, T1 obj, T2 obj2)
            {
                this.action = action;
                this.obj = obj;
                this.obj2 = obj2;
            }

            /// <summary>
            /// Runs this instance.
            /// </summary>
            public void Run()
            {
                this.action(this.obj, this.obj2);
            }
        }

        /// <summary>
        /// Class ArgRunnable.
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <typeparam name="T2">The type of the t2.</typeparam>
        /// <typeparam name="T3">The type of the t3.</typeparam>
        public class ArgRunnable<T1, T2, T3> : IRunnable
        {
            /// <summary>
            /// The _action
            /// </summary>
            private readonly Action<T1, T2, T3> action;

            /// <summary>
            /// The _obj
            /// </summary>
            private readonly T1 obj;

            /// <summary>
            /// The _obj2
            /// </summary>
            private readonly T2 obj2;

            /// <summary>
            /// The _obj3
            /// </summary>
            private readonly T3 obj3;

            /// <summary>
            /// Initializes a new instance of the <see cref="ArgRunnable{T1, T2, T3}"/> class.
            /// </summary>
            /// <param name="action">The action.</param>
            /// <param name="obj">The object.</param>
            /// <param name="obj2">The obj2.</param>
            /// <param name="obj3">The obj3.</param>
            public ArgRunnable(Action<T1, T2, T3> action, T1 obj, T2 obj2, T3 obj3)
            {
                this.action = action;
                this.obj = obj;
                this.obj2 = obj2;
                this.obj3 = obj3;
            }

            /// <summary>
            /// Runs this instance.
            /// </summary>
            public void Run()
            {
                this.action(this.obj, this.obj2, this.obj3);
            }
        }
    }
}