// <copyright file="ClientSession.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    using IOperationOverrides = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.IOperationOverrides;
    using ISessionAuth = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.ISessionAuth;
    using RedirectionPolicy = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestInit.RedirectionPolicy;

    /// <summary>
    /// Class ClientSession.
    /// </summary>
    public class ClientSession : IClientSession
    {
        /// <summary>
        /// Provider for unique session Ids.
        /// </summary>
        private static readonly UIdProvider SsessionIdProvider = new UIdProvider();

        /// <summary>
        /// Collection of active bulk watchers.
        /// </summary>
        private static readonly WatcherCollection SbulkWatchers = new WatcherCollection();

        /// <summary>
        /// Maximum amount of time a session can be idle for
        /// </summary>
        private static readonly TimeSpan MaxSessionIdleTime = TimeSpan.FromSeconds(60);

        /// <summary>
        /// do we want to use ROlocks?
        /// </summary>
        private static bool useROLocks = true;

        /// <summary>
        /// do we want to drain requests upon session termination
        /// </summary>
        private static bool drainRequestsOnTermination = false;

        /// <summary>
        /// Delegate to be invoked with received messages.
        /// </summary>
        private readonly ProcessMessageDelegate processMessage;

        /// <summary>
        /// Authentication info for this session
        /// </summary>
        private readonly SessionAuth authenticationInfo = new SessionAuth();

        /// <summary>
        /// Lock that guards the table of terminate actions.
        /// </summary>
        private readonly ReaderWriterLockSlim actionsOnTerminateLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// The friendly name.
        /// </summary>
        private string friendlyName;

        /// <summary>
        /// The inflightthe number of in flight requests
        /// </summary>
        private int inflight = 0;

        /// <summary>
        /// If not null, the action to run next time there is no request in flight.
        /// </summary>
        private Action runOnFlush = null;

        /// <summary>
        /// Table of actions to execute when the connection is terminated.
        /// </summary>
        private Dictionary<string, Action<bool>> actionsOnTerminate = new Dictionary<string, Action<bool>>();

        /// <summary>
        /// if true, the read only interface will require locks
        /// </summary>
        private bool roInterfaceRequiresLocks = true;

        /// <summary>
        /// The last time a request or response was seen in this session
        /// </summary>
        private DateTime lastSessionActivity = DateTime.UtcNow;

        /// <summary>
        /// Timer that kills the session if it has been idle for too long
        /// </summary>
        private Timer timeoutIdleSessionTimer;

        /// <summary>
        /// Object that protects access to the idle session timer
        /// </summary>
        private object timeoutIdleSessionTimerLockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSession"/> class.
        /// </summary>
        /// <param name="processMessage">Delegate to invoke to process received messages</param>
        public ClientSession(ProcessMessageDelegate processMessage)
        {
            this.processMessage = processMessage;
            this.SessionId = SsessionIdProvider.NextUniqueId();
            this.FriendlyName = "sid-" + this.SessionId;
        }

        /// <summary>
        /// Signature of the delegate invoked to process received messages.
        /// </summary>
        /// <param name="call">The call.</param>
        /// <param name="session">The session.</param>
        /// <param name="onCompleted">Action to invoke on completed</param>
        public delegate void ProcessMessageDelegate(RequestCall call, ClientSession session, Action<RequestResponse, Exception> onCompleted);

        /// <summary>
        /// Enum SessionState
        /// </summary>
        public enum SessionState
        {
            /// <summary>
            /// The session has not been initialized yet
            /// </summary>
            Uninitialized = 0,

            /// <summary>
            /// The session has failed
            /// </summary>
            Failed,

            /// <summary>
            /// The session has been initialized
            /// </summary>
            Initialized,

            /// <summary>
            /// The session is terminating
            /// </summary>
            Terminating,

            /// <summary>
            /// The session has been closed
            /// </summary>
            Closed,
        }

        /// <summary>
        /// Gets redirection policy for write operations addressing a non-primary or non-master
        /// </summary>
        public RedirectionPolicy Redirection { get; internal set; } = RedirectionPolicy.ServerDefault;

        /// <summary>
        /// Gets a value indicating whether this session allow any writes
        /// </summary>
        public bool WritesAllowed { get; internal set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the read only operations will require to acquire a RO lock. If false, they don't.
        /// </summary>
        public bool ROInterfaceRequiresLocks
        {
            get { return this.roInterfaceRequiresLocks; }
            set { this.roInterfaceRequiresLocks = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the session can run any write operations, if true write operations can only be done on ephemeral nodes
        /// </summary>
        public bool OnlyEphemeralChangesAllowed { get; set; }

        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        /// <value>The session identifier.</value>
        public ulong SessionId { get; set; }

        /// <summary>
        /// Gets or sets the friendly name of the client.
        /// </summary>
        /// <value>The name of the friendly.</value>
        public string FriendlyName
        {
            get
            {
                return this.friendlyName;
            }

            set
            {
                RmAssert.IsTrue(value != null);
                this.friendlyName = value;
            }
        }

        /// <summary>
        /// Gets the authentication info for this session
        /// </summary>
        public ISessionAuth Auth
        {
            get { return this.authenticationInfo; }
        }

        /// <summary>
        /// Gets a value indicating whether there is a terminate action for this session
        /// </summary>
        public bool HasOnTerminateActions
        {
            get
            {
                if (this.actionsOnTerminate == null)
                {
                    return false;
                }

                this.actionsOnTerminateLock.EnterReadLock();
                try
                {
                    if (this.actionsOnTerminate == null)
                    {
                        return false;
                    }

                    return this.actionsOnTerminate.Count != 0;
                }
                finally
                {
                    this.actionsOnTerminateLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>The state.</value>
        public SessionState State { get; internal set; }

        /// <summary>
        /// Gets or sets action that must be executed when the session is terminated.
        /// </summary>
        internal Action OnTerminated { get; set; }

        /// <summary>
        /// this will close some sessions that are expired (not used for a long time)
        /// </summary>
        public static void CloseSomeExpiredSessions()
        {
            // for now, it is a no-op
        }

        /// <summary>
        /// Adds an action to execute when this session terminates.
        /// </summary>
        /// <param name="name">Name of the action</param>
        /// <param name="action">Action to execute. the argument is true if the caller's session is being terminated</param>
        public void AddOnTerminateAction(string name, Action<bool> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            bool runAction = false;

            this.actionsOnTerminateLock.EnterWriteLock();
            try
            {
                if (this.actionsOnTerminate == null)
                {
                    runAction = true;
                    return;
                }

                RingMasterEventSource.Log.AddOnTerminateAction(this.SessionId, name);
                this.actionsOnTerminate.Add(name, action);
            }
            finally
            {
                this.actionsOnTerminateLock.ExitWriteLock();

                if (runAction)
                {
                    action(true);
                }
            }
        }

        /// <summary>
        /// Tries to remove a terminate action.
        /// </summary>
        /// <param name="name">Name of the action to remove</param>
        /// <param name="action">The removed action.</param>
        /// <returns><c>true</c> If element was present (and hence removed), <c>false</c> otherwise.</returns>
        public bool TryRemoveOnTerminateAction(string name, out Action<bool> action)
        {
            this.actionsOnTerminateLock.EnterWriteLock();
            try
            {
                if (this.actionsOnTerminate == null)
                {
                    action = null;
                    return false;
                }

                if (this.actionsOnTerminate.TryGetValue(name, out action))
                {
                    RingMasterEventSource.Log.TryRemoveOnTerminateAction(this.SessionId, name, true);
                    this.actionsOnTerminate.Remove(name);
                    return true;
                }

                RingMasterEventSource.Log.TryRemoveOnTerminateAction(this.SessionId, name, false);
                return false;
            }
            finally
            {
                this.actionsOnTerminateLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes the on terminate action.
        /// </summary>
        /// <param name="name">Name of the action to remove</param>
        public void RemoveOnTerminateAction(string name)
        {
            this.actionsOnTerminateLock.EnterWriteLock();
            try
            {
                if (this.actionsOnTerminate != null)
                {
                    RingMasterEventSource.Log.RemoveOnTerminateAction(this.SessionId, name);
                    this.actionsOnTerminate.Remove(name);
                }
            }
            finally
            {
                this.actionsOnTerminateLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Drains all requests
        /// </summary>
        public void DrainRequests()
        {
            if (drainRequestsOnTermination)
            {
                this.SetRunOnFlush(this.CompleteTermination);
            }
            else
            {
                this.CompleteTermination();
            }
        }

        /// <summary>
        /// Closes the session
        /// </summary>
        public virtual void Close()
        {
            this.CompleteTermination();
        }

        /// <summary>
        /// Sets the client information
        /// </summary>
        /// <param name="clientIP">IP address of the client</param>
        /// <param name="clientIdentity">Identity of the client</param>
        public void SetClientInfo(string clientIP, string clientIdentity)
        {
            this.authenticationInfo.ClientIP = clientIP;
            this.authenticationInfo.ClientIdentity = clientIdentity;
        }

        /// <summary>
        /// Starts the idle session timeout timer
        /// </summary>
        public void StartTimeoutIdleSessionTimer()
        {
            this.timeoutIdleSessionTimer = new Timer(this.TimeoutIdleSession, null, MaxSessionIdleTime, Timeout.InfiniteTimeSpan);

            this.AddOnTerminateAction("RemoveTimeoutIdleSessionTimer", _ =>
            {
                try
                {
                    lock (this.timeoutIdleSessionTimerLockObject)
                    {
                        if (this.timeoutIdleSessionTimer != null)
                        {
                            this.timeoutIdleSessionTimer.Dispose();
                            this.timeoutIdleSessionTimer = null;
                        }
                    }
                }
                catch (Exception)
                {
                }
            });
        }

        /// <summary>
        /// Is there any bulkwatcher?
        /// </summary>
        /// <returns><c>true</c> if any bulkwatcher exist, <c>false</c> otherwise.</returns>
        internal static bool AnyBulkWatcher()
        {
            return SbulkWatchers.IsAnyWatcherPresent;
        }

        /// <summary>
        /// Indicates if there is any bulkwatcher that applies to the given path or under it
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if there is any bulkwatcher that applies to this or any subpacth, <c>false</c> otherwise.</returns>
        internal static bool AnyBulkWatcher(string path)
        {
            return SbulkWatchers.IsWatcherPresent(path);
        }

        /// <summary>
        /// Gets the bulk watchers.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IEnumerable&lt;IWatcher&gt;.</returns>
        internal static IEnumerable<IWatcher> GetBulkWatchers(string path)
        {
            return SbulkWatchers.EnumerateApplicableWatchers(path);
        }

        /// <summary>
        /// Adds the bulk watcher.
        /// </summary>
        /// <param name="sessionId">Id of the session that is adding the watcher</param>
        /// <param name="path">Path to watch</param>
        /// <param name="watcher">Watcher to add</param>
        internal static void AddBulkWatcher(ulong sessionId, string path, MarshallerChannel.ProxyWatcher watcher)
        {
            int count = SbulkWatchers.AddWatcher(sessionId, path, watcher);
            RingMasterServerInstrumentation.Instance.UpdateBulkWatcherCount(count);
        }

        /// <summary>
        /// Removes the bulk watcher.
        /// </summary>
        /// <param name="sessionId">Id of the session that is removing the watcher</param>
        /// <param name="path">Path associated with the watcher</param>
        internal static void RemoveBulkWatcher(ulong sessionId, string path)
        {
            int count = SbulkWatchers.RemoveWatcher(sessionId, path);
            RingMasterServerInstrumentation.Instance.UpdateBulkWatcherCount(count);
        }

        /// <summary>
        /// Removes all bulk watchers in the session
        /// </summary>
        /// <param name="sessionId">Session ID that is removing the watchers</param>
        internal static void RemoveAllBulkWatchers(ulong sessionId)
        {
            int count = SbulkWatchers.RemoveAllWatchersForSession(sessionId);
            RingMasterServerInstrumentation.Instance.UpdateBulkWatcherCount(count);
        }

        /// <summary>
        /// gets or creates a locklist
        /// </summary>
        /// <param name="req">the request this operation is for</param>
        /// <param name="lockDownPaths">if not null, this is a list of paths that are in RW lockdown mode.</param>
        /// <returns>the locklist for this thread</returns>
        internal ILockListTransaction GetOrCreateLockList(IRingMasterBackendRequest req, LockDownSet lockDownPaths)
        {
            IOperationOverrides over = req.Overrides;

            ILockListTransaction ll;

            ISessionAuth auth = req.Auth;

            if (auth == null)
            {
                auth = this.authenticationInfo;
            }

            if (this.ROInterfaceRequiresLocks || this.WritesAllowed || !useROLocks)
            {
                ll = new LockListForRW(lockDownPaths, this.OnlyEphemeralChangesAllowed);
            }
            else
            {
                ll = new LockListForRO();
            }

            try
            {
                ll.Initialize(auth, over);
            }
            catch (Exception)
            {
                ll.Dispose();
                throw;
            }

            return ll;
        }

        /// <summary>
        /// unlocks and close changes if needed (committing or aborting those)
        /// </summary>
        /// <param name="ll">the locklist to release</param>
        /// <param name="task">async task to indicate the completion of lock list replication</param>
        internal void IfNeededUnlockAllAndCloseChanges(ILockListTransaction ll, out Task task)
        {
            task = null;
            if (ll != null && ll.Complete(out task))
            {
                ll.Dispose();
            }
        }

        /// <summary>
        /// Records an invocation has started.
        /// </summary>
        internal void BeginInvocation()
        {
            Interlocked.Increment(ref this.inflight);
        }

        /// <summary>
        /// Set the digest of the client
        /// </summary>
        /// <param name="digest">Digest of the client</param>
        internal void SetClientDigest(string digest)
        {
            this.authenticationInfo.ClientDigest = digest;
        }

        /// <summary>
        /// Records an invocation has finished.
        /// If we got to zero, runs "runOnFlush" action, if any
        /// </summary>
        internal void EndInvocation()
        {
            if (Interlocked.Decrement(ref this.inflight) == 0)
            {
                Action doRun = Interlocked.Exchange(ref this.runOnFlush, null);

                if (doRun != null)
                {
                    doRun();
                }
            }
        }

        /// <summary>
        /// Updates the timestamp of the last activity of the session
        /// </summary>
        /// <param name="time">Timestamp of the activity</param>
        protected void UpdateLastSessionActivity(DateTime time)
        {
            this.lastSessionActivity = time;
        }

        /// <summary>
        /// Completes the actions for terminating a session.
        /// </summary>
        protected virtual void CompleteTermination()
        {
            Trace.TraceInformation("ClientSession[{0}]: Executing terminate actions", this.SessionId);

            try
            {
                List<KeyValuePair<string, Action<bool>>> torun = null;
                this.actionsOnTerminateLock.EnterWriteLock();

                try
                {
                    if (this.actionsOnTerminate != null)
                    {
                        // we need a copy of the list because the actions can modify the collection itself
                        torun = new List<KeyValuePair<string, Action<bool>>>(this.actionsOnTerminate);

                        this.actionsOnTerminate = null;
                    }

                    this.State = SessionState.Terminating;
                }
                finally
                {
                    this.actionsOnTerminateLock.ExitWriteLock();
                }

                if (torun != null)
                {
                    Trace.TraceInformation("ClientSession[{0}]: Running terminate actions. count={1}", this.SessionId, torun.Count);
                    int watcherTerminateActionsCount = 0;

                    // Run the watcher terminate actions first.
                    foreach (KeyValuePair<string, Action<bool>> item in torun)
                    {
                        if (item.Key.StartsWith("watcher-"))
                        {
                            watcherTerminateActionsCount++;
                            item.Value(true);
                        }
                    }

                    ClientSession.RemoveAllBulkWatchers(this.SessionId);

                    // Then run the non watcher terminate actions.
                    foreach (KeyValuePair<string, Action<bool>> item in torun)
                    {
                        if (!item.Key.StartsWith("watcher-"))
                        {
                            item.Value(true);
                        }
                    }

                    Trace.TraceInformation("ClientSession[{0}]: Completed terminate actions. count={1}, watcherTerminateActionsCount={2}", this.SessionId, torun.Count, watcherTerminateActionsCount);
                }

                this.State = SessionState.Closed;
            }
            finally
            {
                if (this.OnTerminated != null)
                {
                    this.OnTerminated();
                }
            }

            Trace.WriteLine(string.Format("ClientSession[{0}]: Terminated", this.SessionId));
        }

        /// <summary>
        /// Sets the action to run on flush. If we are flushed, run the method now.
        /// Note: this method can only be invoked once in the lifetime of a session
        /// </summary>
        /// <param name="action">The action.</param>
        /// <exception cref="System.InvalidOperationException">if SetRunOnFlush is invoked more than once on this session</exception>
        private void SetRunOnFlush(Action action)
        {
            Action prev = Interlocked.CompareExchange(ref this.runOnFlush, action, null);

            if (prev != null)
            {
                throw new InvalidOperationException("SetRunOnFlush can only be invoked once per session");
            }

            // now, in case we are already flushed, exercise the inflight counter
            this.BeginInvocation();
            this.EndInvocation();
        }

        private void TimeoutIdleSession(object state)
        {
            if (this.lastSessionActivity < DateTime.UtcNow.Subtract(MaxSessionIdleTime))
            {
                Trace.TraceWarning("ClientSession[{0}]: Closing idle session", this.SessionId);

                RingMasterThreadPool.Instance.QueueUserWorkItem(unused =>
                {
                    try
                    {
                        this.Close();
                    }
                    catch (Exception closeEx)
                    {
                        Trace.TraceWarning("ClientSession[{0}]: Caught ignorable exception while closing session: {1}", this.SessionId, closeEx);
                    }
                });
            }
            else
            {
                lock (this.timeoutIdleSessionTimerLockObject)
                {
                    if (this.timeoutIdleSessionTimer != null)
                    {
                        this.timeoutIdleSessionTimer.Change(MaxSessionIdleTime, Timeout.InfiniteTimeSpan);
                    }
                }
            }
        }
    }
}
