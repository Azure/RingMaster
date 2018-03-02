// <copyright file="ClientSession.cs" company="Microsoft">
//     Copyright ©  2015
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
        /// The friendly name.
        /// </summary>
        private string _friendlyName;

        /// <summary>
        /// The inflightthe number of in flight requests
        /// </summary>
        private int inflight = 0;

        /// <summary>
        /// If not null, the action to run next time there is no request in flight.
        /// </summary>
        private Action runOnFlush = null;

        /// <summary>
        /// Provider for unique session Ids.
        /// </summary>
        private static readonly UIdProvider SsessionIdProvider = new UIdProvider();

        /// <summary>
        /// Authentication info for this session
        /// </summary>
        private readonly SessionAuth _authenticationInfo = new SessionAuth();

        /// <summary>
        /// Collection of active bulk watchers.
        /// </summary>
        private static readonly WatcherCollection SbulkWatchers = new WatcherCollection();

        /// <summary>
        /// Delegate to be invoked with received messages.
        /// </summary>
        protected readonly ProcessMessageDelegate _processMessage;

        /// <summary>
        /// Table of actions to execute when the connection is terminated.
        /// </summary>
        private Dictionary<string, Action<bool>> _actionsOnTerminate = new Dictionary<string, Action<bool>>();

        /// <summary>
        /// Lock that guards the table of terminate actions.
        /// </summary>
        private readonly ReaderWriterLockSlim _actionsOnTerminateLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

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
        /// if true (default) the read only operations will require to acquire a RO lock. If false, they don't.
        /// </summary>
        public bool ROInterfaceRequiresLocks { get { return roInterfaceRequiresLocks; } set { roInterfaceRequiresLocks = value; } }

        /// <summary>
        /// if false (default) the session can run any write operations, otherwise, write operations can only be done on ephemeral nodes
        /// </summary>
        public bool OnlyEphemeralChangesAllowed { get; set; }

        /// <summary>
        /// do we want to use ROlocks?
        /// </summary>
        private static bool useROLocks = true;

        /// <summary>
        /// do we want to drain requests upon session termination
        /// </summary>
        private static bool DrainRequestsOnTermination = false;

        /// <summary>
        /// Maximum amount of time a session can be idle for
        /// </summary>
        private static readonly TimeSpan MaxSessionIdleTime = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Redirection policy for write operations addressing a non-primary or non-master
        /// </summary>
        public RedirectionPolicy Redirection = RedirectionPolicy.ServerDefault;

        /// <summary>
        /// Does this session allow any writes?
        /// </summary>
        public bool writesAllowed = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSession"/> class.
        /// </summary>
        /// <param name="processMessage">Delegate to invoke to process received messages</param>
        public ClientSession(ProcessMessageDelegate processMessage)
        {
            this._processMessage = processMessage;
            this.SessionId = SsessionIdProvider.NextUniqueId();
            this.FriendlyName = "sid-" + this.SessionId;
        }

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
                return this._friendlyName;
            }
            set
            {
                RmAssert.IsTrue(value != null);
                this._friendlyName = value;
            }
        }

        /// <summary>
        /// Gets the authentication info for this session
        /// </summary>
        public ISessionAuth Auth
        {
            get { return this._authenticationInfo; }
        }

        /// <summary>
        /// Action that must be executed when the session is terminated.
        /// </summary>
        internal Action OnTerminated { get; set; }

        /// <summary>
        /// Signature of the delegate invoked to process received messages.
        /// </summary>
        /// <param name="call">The call.</param>
        /// <param name="session">The session.</param>
        /// <returns>RequestResponse.</returns>
        public delegate void ProcessMessageDelegate(RequestCall call, ClientSession session, Action<RequestResponse> onCompleted);

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

            this._actionsOnTerminateLock.EnterWriteLock();
            try
            {
                if (this._actionsOnTerminate == null)
                {
                    runAction = true;
                    return;
                }

                RingMasterEventSource.Log.AddOnTerminateAction(this.SessionId, name);
                this._actionsOnTerminate.Add(name, action);
            }
            finally
            {
                this._actionsOnTerminateLock.ExitWriteLock();

                if (runAction)
                {
                    action(true);
                }
            }
        }

        /// <summary>
        /// indicates if there is a terminate action for this session
        /// </summary>
        public bool HasOnTerminateActions
        {
            get
            {
                if (this._actionsOnTerminate == null)
                {
                    return false;
                }

                this._actionsOnTerminateLock.EnterReadLock();
                try
                {
                    if (this._actionsOnTerminate == null)
                    {
                        return false;
                    }

                    return this._actionsOnTerminate.Count != 0;
                }
                finally
                {
                    this._actionsOnTerminateLock.ExitReadLock();
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
            this._actionsOnTerminateLock.EnterWriteLock();
            try
            {
                if (this._actionsOnTerminate == null)
                {
                    action = null;
                    return false;
                }

                if (this._actionsOnTerminate.TryGetValue(name, out action))
                {
                    RingMasterEventSource.Log.TryRemoveOnTerminateAction(this.SessionId, name, true);
                    this._actionsOnTerminate.Remove(name);
                    return true;
                }

                RingMasterEventSource.Log.TryRemoveOnTerminateAction(this.SessionId, name, false);
                return false;
            }
            finally
            {
                this._actionsOnTerminateLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes the on terminate action.
        /// </summary>
        /// <param name="name">Name of the action to remove</param>
        public void RemoveOnTerminateAction(string name)
        {
            this._actionsOnTerminateLock.EnterWriteLock();
            try
            {
                if (this._actionsOnTerminate != null)
                {
                    RingMasterEventSource.Log.RemoveOnTerminateAction(this.SessionId, name);
                    this._actionsOnTerminate.Remove(name);
                }
            }
            finally
            {
                this._actionsOnTerminateLock.ExitWriteLock();
            }
        }

        public void DrainRequests()
        {
            if (DrainRequestsOnTermination)
            {
                this.SetRunOnFlush(CompleteTermination);
            }
            else
            {
                CompleteTermination();
            }
        }

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
            Closed
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>The state.</value>
        public SessionState State { get; internal set; }

        #region Bulk Watchers
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
                auth = this._authenticationInfo;
            }

            if (this.ROInterfaceRequiresLocks || this.writesAllowed || !useROLocks)
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

        internal static void RemoveAllBulkWatchers(ulong sessionId)
        {
            int count = SbulkWatchers.RemoveAllWatchersForSession(sessionId);
            RingMasterServerInstrumentation.Instance.UpdateBulkWatcherCount(count);
        }

        #endregion

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
                this._actionsOnTerminateLock.EnterWriteLock();

                try
                {
                    if (this._actionsOnTerminate != null)
                    {
                        // we need a copy of the list because the actions can modify the collection itself
                        torun = new List<KeyValuePair<string, Action<bool>>>(this._actionsOnTerminate);

                        this._actionsOnTerminate = null;
                    }

                    this.State = SessionState.Terminating;
                }
                finally
                {
                    this._actionsOnTerminateLock.ExitWriteLock();
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

        public virtual void Close()
        {
            this.CompleteTermination();
        }

        /// <summary>
        /// this will close some sessions that are expired (not used for a long time)
        /// </summary>
        public static void CloseSomeExpiredSessions()
        {
            // for now, it is a no-op
        }

        /// <summary>
        /// Records an invocation has started.
        /// </summary>
        internal void BeginInvocation()
        {
            Interlocked.Increment(ref inflight);
        }

        /// <summary>
        /// Sets the action to run on flush. If we are flushed, run the method now.
        /// Note: this method can only be invoked once in the lifetime of a session
        /// </summary>
        /// <param name="action">The action.</param>
        /// <exception cref="System.InvalidOperationException">if SetRunOnFlush is invoked more than once on this session</exception>
        private void SetRunOnFlush(Action action)
        {
            Action prev = Interlocked.CompareExchange(ref runOnFlush, action, null);

            if (prev != null)
            {
                throw new InvalidOperationException("SetRunOnFlush can only be invoked once per session");
            }

            // now, in case we are already flushed, exercise the inflight counter
            this.BeginInvocation();
            this.EndInvocation();
        }

        public void SetClientInfo(string clientIP, string clientIdentity)
        {
            this._authenticationInfo.ClientIP = clientIP;
            this._authenticationInfo.ClientIdentity = clientIdentity;
        }

        internal void SetClientDigest(string digest)
        {
            this._authenticationInfo.ClientDigest = digest;
        }

        /// <summary>
        /// Records an invocation has finished.
        /// If we got to zero, runs "runOnFlush" action, if any
        /// </summary>
        internal void EndInvocation()
        {
            if (0 == Interlocked.Decrement(ref inflight))
            {
                Action doRun = Interlocked.Exchange(ref runOnFlush, null);

                if (doRun != null)
                {
                    doRun();
                }
            }
        }

        public void StartTimeoutIdleSessionTimer()
        {
            this.timeoutIdleSessionTimer = new Timer(TimeoutIdleSession, null, MaxSessionIdleTime, Timeout.InfiniteTimeSpan);

            this.AddOnTerminateAction("RemoveTimeoutIdleSessionTimer", _ =>
            {
                try
                {
                    lock (timeoutIdleSessionTimerLockObject)
                    {
                        if (this.timeoutIdleSessionTimer != null)
                        {
                            timeoutIdleSessionTimer.Dispose();
                            this.timeoutIdleSessionTimer = null;
                        }
                    }
                }
                catch (Exception)
                {
                }
            });
        }

        private void TimeoutIdleSession(object state)
        {
            if (this.lastSessionActivity < DateTime.UtcNow.Subtract(MaxSessionIdleTime))
            {
                Trace.TraceWarning("ClientSession[{0}]: Closing idle session", this.SessionId);

                RingMasterThreadPool.Instance.QueueUserWorkItem(__ =>
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
                lock (timeoutIdleSessionTimerLockObject)
                {
                    if (this.timeoutIdleSessionTimer != null)
                    {
                        timeoutIdleSessionTimer.Change(MaxSessionIdleTime, Timeout.InfiniteTimeSpan);
                    }
                }
            }
        }
    }
}
