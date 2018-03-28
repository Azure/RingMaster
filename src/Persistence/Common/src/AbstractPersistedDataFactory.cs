// <copyright file="AbstractPersistedDataFactory.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;

    /// <summary>
    /// Implements functionality that is common to all implementations of <see cref="IPersistedDataFactory{T}"/>.
    /// </summary>
    public abstract class AbstractPersistedDataFactory : IPersistedDataFactory<Node>, IDisposable
    {
        /// <summary>
        /// Number of change list can be queued for replication grouping
        /// </summary>
        private const int DefaultReplicationQueueSize = 16;

        /// <summary>
        /// Total data size of a replication group, beyond this value no further change list will be taken
        /// </summary>
        private const int DefaultReplicationGroupDataSize = 1024 * 1024;

        private const int DataLoadWaitIntervalMs = 10000;

        // The dictionary holding all persisted data resulting from applied transactions (on primary as well as on secondary).
        // This dictionary should be consistent with dataById, but dataById cannot be accessed if the secondary is not active.
        private readonly ConcurrentDictionary<long, PersistedData> dataById = new ConcurrentDictionary<long, PersistedData>();

        // Instrumentation consumer
        private readonly IPersistenceInstrumentation instrumentation;

        // Unique Id provider
        private readonly UIdProvider uidProvider = new UIdProvider();

        // Event that is signalled when data is available.
        private readonly ManualResetEventSlim dataAvailable = new ManualResetEventSlim(false);

        // Token that will be observed for cancellation signal.
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// List of change to be replicated, multiple changes are grouped together to commit.
        /// </summary>
        private readonly ConcurrentQueue<ChangeListReplicationTask> changeListQueue = new ConcurrentQueue<ChangeListReplicationTask>();

        /// <summary>
        /// Measure the replication time in <see cref="DequeueAsync"/>.
        /// </summary>
        private readonly Stopwatch replicationClock = Stopwatch.StartNew();

        /// <summary>
        /// Indicates the amount of free space in the <see cref="changeListQueue"/> for enqueueing
        /// </summary>
        private readonly Semaphore changeListQueueAvailable;

        /// <summary>
        /// Indicates the number of change list in the <see cref="changeListQueue"/> for dequeueing
        /// TODO: replace this with async event
        /// </summary>
        private readonly SemaphoreSlim newChangeListAvailable;

        /// <summary>
        /// async task of replication for a group of change list
        /// </summary>
        private readonly Task replicationTask;

        /// <summary>
        /// Total data size of a replication group, beyond this value no further change list will be taken
        /// </summary>
        private readonly int replicationGroupDataSizeThreshold;

        /// <summary>
        /// Flag to indicate if this object has been disposed or not.
        /// </summary>
        private bool disposed = false;

        // Interface to the client that must receive primary notifications.
        private IPersistedDataFactoryClient client;

        // RingMaster backend.
        private RingMasterBackendCore backend;

        private long lastAssignedChangeListId;

        // The total size of data.
        private long totalData;

        /// <summary>
        /// Total number of items in <see cref="dataById"/>. This is to avoid expensive Count operation.
        /// </summary>
        private long totalDataCount;

        // Maximum Id of PersistedData observed
        private ulong maxId;

        // The highest seen Zxid
        private long lastZxid;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractPersistedDataFactory"/> class.
        /// </summary>
        /// <param name="name">Name of this instance</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="cancellationToken">Token that will be observed for cancellation signal</param>
        /// <param name="changeListQueueSize">Maximum size of the change list queue</param>
        /// <param name="replicationGroupDataSizeThreshold">Maximum size of data replication for grouping</param>
        protected AbstractPersistedDataFactory(
            string name,
            IPersistenceInstrumentation instrumentation,
            CancellationToken cancellationToken,
            int changeListQueueSize,
            int replicationGroupDataSizeThreshold)
        {
            this.Name = name;
            this.instrumentation = instrumentation;
            this.cancellationToken = cancellationToken;
            this.RequiresCallsForEachDelete = true;

            this.changeListQueueAvailable = new Semaphore(changeListQueueSize, changeListQueueSize);
            this.newChangeListAvailable = new SemaphoreSlim(0, changeListQueueSize);
            this.replicationGroupDataSizeThreshold = replicationGroupDataSizeThreshold;

            // This task is saved in the object for informational purpose. Do not wait for it or dispose it during
            // the destruction of the object per .NET guideline.
            this.replicationTask = Task.Run(this.DequeueAsync, this.cancellationToken);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractPersistedDataFactory"/> class.
        /// </summary>
        /// <param name="name">Name of this instance</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="cancellationToken">Token that will be observed for cancellation signal</param>
        protected AbstractPersistedDataFactory(string name, IPersistenceInstrumentation instrumentation, CancellationToken cancellationToken)
            : this(name, instrumentation, cancellationToken, DefaultReplicationQueueSize, DefaultReplicationGroupDataSize)
        {
        }

        /// <summary>
        /// Gets the name of this factory.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the Root node.
        /// </summary>
        public IPersistedData Root { get; set; }

        /// <summary>
        /// Gets the total size of the data hosted by this factory.
        /// </summary>
        public ulong TotalData => (ulong)this.totalData;

        /// <summary>
        /// Gets the total number of nodes hosted by this factory.
        /// </summary>
        public ulong TotalNodes => (ulong)this.totalDataCount;

        /// <summary>
        /// Gets the maximum id seen.
        /// </summary>
        public ulong MaxId => this.maxId;

        /// <summary>
        /// Gets a value indicating whether this factory is active.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is the primary.
        /// </summary>
        public bool IsBackendPrimary => this.backend?.IsPrimary() ?? false;

        /// <summary>
        /// Gets or sets a value indicating whether this instance requires calls for each delete.
        /// </summary>
        public bool RequiresCallsForEachDelete { get; protected set; }

        /// <summary>
        /// Gets or sets the action that must be invoked if a fatal error occurs.
        /// </summary>
        public Action<string, Exception> OnFatalError { get; set; }

        /// <summary>
        /// Retrieves the current latest txId.
        /// </summary>
        /// <returns>The last transaction id</returns>
        public long GetLastXId()
        {
            return this.lastZxid;
        }

        /// <summary>
        /// Sets the backend.
        /// </summary>
        /// <param name="backend">RingMaster backend</param>
        public void SetBackend(RingMasterBackendCore backend)
        {
            this.backend = backend;
        }

        /// <summary>
        /// Activates this instance. This will indicate to the upper layer that we are
        /// becoming the primary replica, and will also initialize the data.
        /// </summary>
        /// <param name="backend">The RingMaster backend</param>
        /// <param name="client">Interface to the client</param>
        public void Activate(RingMasterBackendCore backend, IPersistedDataFactoryClient client)
        {
            this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.IsActive = true;

            PersistenceEventSource.Log.Activate(Process.GetCurrentProcess().Id);
        }

        /// <summary>
        /// Deactivates this instance.
        /// </summary>
        public void Deactivate()
        {
            this.IsActive = false;
            PersistenceEventSource.Log.Deactivate(Process.GetCurrentProcess().Id);
            this.OnDeactivate();
        }

        /// <summary>
        /// Loads the initial tree of nodes.
        /// </summary>
        /// <param name="lastXId">The highest transaction Id loaded.</param>
        /// <returns>Root of the tree.</returns>
        public Node LoadTree(out long lastXId)
        {
            try
            {
                PersistenceEventSource.Log.LoadTreeStarted();
                var timer = Stopwatch.StartNew();

                // Initiate the data load process and wait until
                // root is available.
                Task unused = this.StartLoadingData();

                while (!this.dataAvailable.Wait(DataLoadWaitIntervalMs, this.cancellationToken))
                {
                    PersistenceEventSource.Log.LoadTree_WaitingForData(timer.ElapsedMilliseconds);
                }

                if (this.Root == null)
                {
                    PersistenceEventSource.Log.LoadTree_CreatingNewRoot();

                    var changeList = this.CreateChangeList();
                    var root = new PersistedData(1, this)
                    {
                        Name = "/",
                        Acl = null,
                        Data = null,
                        Stat = new MutableStat(),
                    };

                    this.backend.DoNodeForCreate(root);
                    changeList.RecordAdd(root);

                    using (var rootCreated = new ManualResetEvent(false))
                    {
                        Task unusedTask;
                        changeList.CommitSync(0, rootCreated, out unusedTask);
                        rootCreated.WaitOne();
                    }

                    this.maxId = 1;
                    this.Root = root;
                }

                this.instrumentation?.LoadTreeCompleted(timer.Elapsed);
                PersistenceEventSource.Log.LoadTreeCompleted(timer.ElapsedMilliseconds);
                lastXId = this.GetLastXId();
                this.uidProvider.SetLastId(this.maxId);

                PersistenceEventSource.Log.LoadTree_RootFound(
                    this.Root.Stat.Czxid,
                    this.Root.Stat.Mzxid,
                    this.Root.Stat.Pzxid,
                    this.Root.Stat.Version,
                    this.Root.Stat.Cversion,
                    this.Root.Stat.Aversion,
                    this.Root.Stat.NumChildren);

                return this.Root.Node;
            }
            catch (Exception ex)
            {
                PersistenceEventSource.Log.LoadTree_Failed(ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Creates a new <see cref="IPersistedData"/>.
        /// </summary>
        /// <returns>A newly created object that implements <see cref="IPersistedData"/> interface</returns>
        public IPersistedData CreateNew()
        {
            IPersistedData persistedData = new PersistedData(this.uidProvider.NextUniqueId(), this);
            PersistenceEventSource.Log.CreateNew(persistedData.Id);
            this.RecordStatsDelta(1, 0);
            return persistedData;
        }

        /// <summary>
        /// Deletes the given <see cref="IPersistedData"/>.
        /// </summary>
        /// <param name="persisted">The persisted data that must be deleted</param>
        public void Delete(IPersistedData persisted)
        {
            if (persisted != null)
            {
                persisted.Delete();
                this.RecordStatsDelta(-1, -persisted.Stat.DataLength);
            }
        }

        /// <summary>
        /// Records a change in the total data size.
        /// </summary>
        /// <param name="size">The magnitude of the change</param>
        public void RecordDataDelta(int size)
        {
            this.RecordStatsDelta(0, size);
        }

        /// <summary>
        /// Records a change in the total number of nodes and/or the total data size.
        /// </summary>
        /// <param name="numNodes">Magnitude of the change in total number of nodes</param>
        /// <param name="dataSize">Magnitude of the change in total data size</param>
        public void RecordStatsDelta(int numNodes, int dataSize)
        {
            Interlocked.Add(ref this.totalData, (long)dataSize);
        }

        /// <summary>
        /// Records undeletion of a node.
        /// </summary>
        /// <param name="node">Node that is undeleted.</param>
        public void RecordNodeUndelete(IPersistedData node)
        {
            if (node != null)
            {
                this.RecordStatsDelta(1, node.Stat.DataLength);
            }
        }

        /// <summary>
        /// Dumps all nodes for debugging purposes only.
        /// </summary>
        /// <returns>string with one line per PersistedData object managed by the factory</returns>
        public string DumpAllNodesForDebug()
        {
            return string.Empty;
        }

        /// <summary>
        /// Gets the health definition of this factory.
        /// </summary>
        /// <returns>A dictionary with health definitions</returns>
        public virtual Dictionary<string, HealthDefinition> GetHealth()
        {
            Dictionary<string, HealthDefinition> healthDefinitions = new Dictionary<string, HealthDefinition>();
            bool primary = this.backend.IsPrimary();
            bool loaded = this.Root != null;

            healthDefinitions[this.Name] = new HealthDefinition(primary, loaded ? 1 : 0, "Loaded");
            PersistenceEventSource.Log.HealthDefinition(this.Name, primary, loaded);

            return healthDefinitions;
        }

        /// <summary>
        /// Checks whether it is unsafe for the cluster (i.e. primary doesn't strictly depend on it) to stop an instance.
        /// </summary>
        /// <param name="name">Name of the instance intended to be stopped.</param>
        /// <param name="failoverWillHappen">If true, a failover is predicted.</param>
        /// <returns><c>false</c> as this feature is not supported</returns>
        public bool UnsafeToStopInstance(string name, out bool failoverWillHappen)
        {
            failoverWillHappen = false;

            return false;
        }

        /// <summary>
        /// Request a failover if possible into a different replica.
        /// </summary>
        /// <param name="replicaToFailover">The name of the replica to failover into</param>
        /// <param name="onInitiate">Optional callback to be invoked when the failover is initiated</param>
        public void FailoverInto(string replicaToFailover, Action onInitiate)
        {
            if (onInitiate != null)
            {
                onInitiate();
            }
        }

        /// <summary>
        /// Wipes all data and shutdown.
        /// </summary>
        public void WipeAllDataAndShutdown()
        {
            throw new NotImplementedException("WipeAllDataAndShutdown is not implemented");
        }

        /// <summary>
        /// Imports a checkpoint from the given file location.
        /// </summary>
        /// <param name="location">The file location for the checkpoint file.</param>
        /// <returns><c>true</c> if the import succeeded and the process will die soon, <c>false</c> otherwise.</returns>
        public bool ImportCheckpoint(string location)
        {
            return false;
        }

        /// <summary>
        /// Schedules an immediate checkpoint (memory snapshot).
        /// Note: For the duration of the checkpoint, no writes will be allowed.
        /// </summary>
        /// <returns><c>true</c> if the checkpoint is scheduled, <c>false</c> otherwise.</returns>
        public bool TakeCheckpoint()
        {
            return false;
        }

        /// <inheritdoc />
        public bool TakeCheckpoint(IPersistedData topNode, string filename, int version)
        {
            return false;
        }

        /// <summary>
        /// Returns the list of members as agreed by the cluster itself, or null if no need for tracking this.
        /// </summary>
        /// <returns>An empty list of members</returns>
        public ClusterMember[] GetAgreedMembers()
        {
            return new ClusterMember[0];
        }

        /// <summary>
        /// Changes the agreed memberset based on the given mapping.
        /// </summary>
        /// <param name="changeMapping">The mapping function for the modified members.</param>
        /// <returns><c>false</c> as this is not applicable</returns>
        public bool ChangeAgreedMembers(Dictionary<string, ClusterMember> changeMapping)
        {
            return false;
        }

        /// <summary>
        /// Tries to get value of a persisted data by ID
        /// </summary>
        /// <param name="id">ID as the key</param>
        /// <param name="item">Retrieved data on output</param>
        /// <returns>If the data is retrieved</returns>
        public bool TryGetValue(ulong id, out IPersistedData item)
        {
            PersistedData data;
            if (this.dataById.TryGetValue((long)id, out data))
            {
                PersistenceEventSource.Log.TryGetValue_Found(id, data.Name, data.ParentId, data.GetChildrenCount());
                item = data;
                return true;
            }

            PersistenceEventSource.Log.TryGetValue_NotFound(id);
            item = null;
            return false;
        }

        /// <summary>
        /// Gets all persisted data items
        /// </summary>
        /// <returns>Enumerable of all data items</returns>
        public IEnumerable<IPersistedData> GetAllItems()
        {
            PersistenceEventSource.Log.GetAllItems(this.TotalNodes);
            return this.dataById.Values;
        }

        /// <summary>
        /// Loads the specified collection of persisted data to rebuild the in-memory tree
        /// </summary>
        /// <param name="dataSequence">Persisted data items to reload</param>
        /// <param name="ignoreErrors">If to ignore errors during reload</param>
        public void Load(IEnumerable<PersistedData> dataSequence, bool ignoreErrors = false)
        {
            if (dataSequence == null)
            {
                throw new ArgumentNullException(nameof(dataSequence));
            }

            this.PrepareForRebuild();

            foreach (var data in dataSequence)
            {
                this.ProcessLoad((PersistedData)data);
            }

            this.CompleteRebuild(ignoreErrors);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Create a <see cref="ChangeList"/> object that will hold a set of changes
        /// that must be applied as a unit.
        /// </summary>
        /// <returns>The newly created <see cref="ChangeList"/></returns>
        internal ChangeList CreateChangeList()
        {
            ulong id = (ulong)Interlocked.Increment(ref this.lastAssignedChangeListId);
            return new ChangeList(id, this);
        }

        /// <summary>
        /// Enqueues the specified change list of the queue and optionally starts the replication to keep it going
        /// </summary>
        /// <param name="changeList">Change list to be queued</param>
        /// <returns>async task can be used to track the completion of change list commit</returns>
        internal Task EnqueueAsync(ChangeList changeList)
        {
            // Enqueue the change list and unblock DequeueAsync task
            var it = new ChangeListReplicationTask
            {
                Change = changeList.Clone(),
                TaskCompletion = new TaskCompletionSource<bool>(),
            };

            foreach (var change in changeList.Changes)
            {
                switch (change.ChangeType)
                {
                    case ChangeList.ChangeType.Add:
                        this.CommitAdd(changeList.Id, 0, change.Data);
                        break;
                    case ChangeList.ChangeType.Update:
                        this.CommitUpdate(changeList.Id, 0, change.Data);
                        break;
                    case ChangeList.ChangeType.Remove:
                        this.CommitRemove(changeList.Id, 0, change.Data);
                        break;
                }
            }

            // Regulate the change list queue size
            WaitHandle.WaitAny(new[] { this.changeListQueueAvailable, this.cancellationToken.WaitHandle, });
            this.cancellationToken.ThrowIfCancellationRequested();
            this.changeListQueue.Enqueue(it);

            // Signal the dequeue task that new change is coming if it is waiting
            this.newChangeListAvailable.Release();

            return it.TaskCompletion.Task;
        }

        /// <summary>
        /// Commits all the changes in all available changelists in the queue.
        /// </summary>
        /// <returns>A <see cref="Task"/> that can be used to track the commit</returns>
        internal async Task DequeueAsync()
        {
            while (!this.cancellationToken.IsCancellationRequested)
            {
                await this.newChangeListAvailable.WaitAsync(this.cancellationToken);

                var startTime = this.replicationClock.Elapsed;

                // List of (change list ID, changes) to be replicated
                var changeListSnapshot = new List<Tuple<ulong, ChangeList.Change>>();

                // List of task completion source to signal the ProcessMessage in backend core so the response can be
                // sent back to the client
                var taskCompletionSnapshot = new List<TaskCompletionSource<bool>>();

                // Takes all the change lists in the queue, but don't wait for any additional one
                int dataSize = 0;
                while (dataSize < this.replicationGroupDataSizeThreshold && this.changeListQueue.TryDequeue(out var it))
                {
                    var cid = it.Change.Id;
                    changeListSnapshot.AddRange(it.Change.Changes.Select(
                        c =>
                        {
                            if (c.Data.Data != null)
                            {
                                dataSize += c.Data.Data.Length;
                            }

                            return Tuple.Create(cid, c);
                        }));
                    taskCompletionSnapshot.Add(it.TaskCompletion);
                }

                Debug.Assert(
                    taskCompletionSnapshot.Count > 0,
                    "taskCompletionSnapshot should be greater 0 since new change list is available");

                // Make sure the number of wait matches the number of release in Enqueue method.
                for (int i = 0; i < taskCompletionSnapshot.Count - 1; i++)
                {
                    // Since enqueue of changeListQueue and release of newChangeListAvailable are not atomic, there
                    // may be a small chance when the items in the queue is greater than the semaphore, and the
                    // following wait will return false. In this case we just retry to keep two semaphores in sync
                    // precisely.
                    SpinWait.SpinUntil(() => this.newChangeListAvailable.Wait(0, this.cancellationToken));
                }

                this.changeListQueueAvailable.Release(taskCompletionSnapshot.Count);

                var firstChangeListId = changeListSnapshot[0].Item1;
                var lastChangeListId = changeListSnapshot[changeListSnapshot.Count - 1].Item1;

                PersistenceEventSource.Log.GroupCommit_Started(
                    firstChangeListId,
                    lastChangeListId,
                    changeListSnapshot.Count,
                    dataSize,
                    startTime.ToString());

                try
                {
                    // Start the transaction of replication
                    using (IReplication replication = this.StartReplication(lastChangeListId))
                    {
                        var beforeUpdateDict = this.replicationClock.Elapsed;

                        foreach (var change in changeListSnapshot)
                        {
                            switch (change.Item2.ChangeType)
                            {
                                case ChangeList.ChangeType.Add:
                                    await replication.Add(change.Item2.Data);
                                    break;
                                case ChangeList.ChangeType.Update:
                                    await replication.Update(change.Item2.Data);
                                    break;
                                case ChangeList.ChangeType.Remove:
                                    await replication.Remove(change.Item2.Data);
                                    break;
                            }
                        }

                        var afterUpdateDict = this.replicationClock.Elapsed;

                        // Commit the replication
                        await replication.Commit();

                        var commitDuration = this.replicationClock.Elapsed - afterUpdateDict;
                        var dictUpdateDuration = afterUpdateDict - beforeUpdateDict;

                        // Tell the upper layer that the replication is completed, all responses can be sent back now.
                        foreach (var taskCompletion in taskCompletionSnapshot)
                        {
                            taskCompletion.SetResult(true);
                        }

                        var duration = this.replicationClock.Elapsed - startTime;

                        this.instrumentation?.ChangeListCommitted(duration);
                        PersistenceEventSource.Log.GroupCommit_Succeeded(
                            firstChangeListId,
                            lastChangeListId,
                            replication.Id,
                            dictUpdateDuration.TotalMilliseconds,
                            commitDuration.TotalMilliseconds,
                            duration.TotalMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    // Failed to commit, tell the upper layer that don't send any response to the client
                    foreach (var taskCompletion in taskCompletionSnapshot)
                    {
                        taskCompletion.SetException(ex);
                    }

                    this.instrumentation?.ChangeListCommitFailed();
                    PersistenceEventSource.Log.GroupCommit_Failed(
                        firstChangeListId,
                        lastChangeListId,
                        changeListSnapshot.Count,
                        (this.replicationClock.Elapsed - startTime).TotalMilliseconds,
                        ex.Message);

                    this.ReportFatalError("Commit Failed", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Report a fatal error.
        /// </summary>
        /// <param name="message">Description of the error</param>
        /// <param name="exception">Exception associated with the error</param>
        protected void ReportFatalError(string message, Exception exception)
        {
            if (this.OnFatalError != null)
            {
                this.OnFatalError.Invoke(message, exception);
            }
            else
            {
                Environment.FailFast(message, exception);
            }
        }

        /// <summary>
        /// Initiate the data load process.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected abstract Task StartLoadingData();

        /// <summary>
        /// Start replication of the changes included in the given <see cref="ChangeList"/>.
        /// </summary>
        /// <param name="id">Id of the replication</param>
        /// <returns>An interface that can be used to track the replication</returns>
        protected abstract IReplication StartReplication(ulong id);

        /// <summary>
        /// Deactivate this instance.
        /// </summary>
        protected abstract void OnDeactivate();

        /// <summary>
        /// Disposes this object
        /// </summary>
        /// <param name="isDisposing">If to dispose from managed or native code</param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (!this.disposed)
                {
                    this.disposed = true;

                    this.dataAvailable.Dispose();
                    this.changeListQueueAvailable.Dispose();
                    this.newChangeListAvailable.Dispose();
                }
            }
        }

        /// <summary>
        /// Commit an Add operation.
        /// </summary>
        /// <param name="changeListId">Id of the changelist associated with this commit</param>
        /// <param name="replicationId">Id of the replication associated with this commit</param>
        /// <param name="data">Data to commit</param>
        protected void CommitAdd(ulong changeListId, ulong replicationId, PersistedData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            PersistenceEventSource.Log.CommitAdd(changeListId, replicationId, data.Id, data.Name, data.ParentId);
            if (!this.dataById.TryAdd((long)data.Id, data))
            {
                PersistenceEventSource.Log.CommitAdd_AlreadyExists(changeListId, replicationId, data.Id, data.Name, data.ParentId);
                throw new InvalidOperationException("CommitAddFailed-Data exists already");
            }
            else
            {
                Interlocked.Increment(ref this.totalDataCount);
            }
        }

        /// <summary>
        /// Commit an Update operation.
        /// </summary>
        /// <param name="changeListId">Id of the changelist associated with this commit</param>
        /// <param name="replicationId">Id of the replication associated with this commit</param>
        /// <param name="data">Data to commit</param>
        protected void CommitUpdate(ulong changeListId, ulong replicationId, PersistedData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            PersistenceEventSource.Log.CommitUpdate(changeListId, replicationId, data.Id, data.Name, data.ParentId);
        }

        /// <summary>
        /// Commit a Remove operation.
        /// </summary>
        /// <param name="changeListId">Id of the changelist associated with this commit</param>
        /// <param name="replicationId">Id of the replication associated with this commit</param>
        /// <param name="data">Data to remove</param>
        protected void CommitRemove(ulong changeListId, ulong replicationId, PersistedData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            PersistenceEventSource.Log.CommitRemove(changeListId, replicationId, data.Id, data.Name, data.ParentId);
            PersistedData removedData;
            if (this.dataById.TryRemove((long)data.Id, out removedData))
            {
                Interlocked.Decrement(ref this.totalDataCount);

                if (removedData != data)
                {
                    PersistenceEventSource.Log.CommitRemoveFailed_DataMismatch(changeListId, replicationId, data.Id, data.Name);
                    throw new InvalidOperationException($"CommitRemove DataMismatch. id={data.Id}");
                }
            }
            else
            {
                PersistenceEventSource.Log.CommitRemoveFailed_DataNotFound(changeListId, replicationId, data.Id, data.Name);
                throw new InvalidOperationException($"CommitRemove DataNotFound. id={data.Id}");
            }
        }

        /// <summary>
        /// Process an add notification.
        /// </summary>
        /// <param name="replicationId">Id of the replication that added the data</param>
        /// <param name="data">The data that was added</param>
        protected void ProcessAdd(ulong replicationId, PersistedData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (this.IsActive)
            {
                return;
            }

            PersistenceEventSource.Log.ProcessAdd_Started(replicationId, data.Id);

            Debug.Assert(data.Node == null, "Data being newly added must not have a node");
            Debug.Assert(data.Factory == this, "Data that is being newly added should belong to this factory");

            try
            {
                if (!this.dataById.TryAdd((long)data.Id, data))
                {
                    PersistenceEventSource.Log.ProcessAdd_AlreadyExists(replicationId, data.Id, data.Name, data.ParentId);
                    throw new InvalidOperationException("ProcessAddFailed-NodeExists");
                }

                this.backend.DoNodeForCreate(data);

                Debug.Assert(data.Node != null, "A node must have been created for the data");

                // Updates lastTxId, maxId, totalNode and totalData counters
                this.UpdateLastZxid(data.Stat);
                this.UpdateMaxId(data.Id);
                this.RecordStatsDelta(1, data.Stat.DataLength);
                Interlocked.Increment(ref this.totalDataCount);

                long parentId = (long)data.ParentId;
                if (parentId > 0)
                {
                    PersistedData parent;
                    if (this.dataById.TryGetValue(parentId, out parent))
                    {
                        PersistenceEventSource.Log.ProcessAdd_ConnectWithParent(replicationId, data.Id, data.Name, parent.Id, parent.Name, parent.GetChildrenCount());
                        var childNodeOfParent = parent.Node.TryGetChild(data.Name);

                        Debug.Assert(childNodeOfParent == null, "Parent must not have a child with the same name");

                        if (childNodeOfParent == null)
                        {
                            parent.Node.AddChild(data.Node);
                        }
                        else
                        {
                            PersistenceEventSource.Log.ProcessAdd_ParentHasAChildWithSameName(replicationId, data.Id, data.Name, parent.Id, childNodeOfParent.Persisted?.Id ?? 0);
                            throw new InvalidOperationException("Parent node has different child with same name");
                        }
                    }
                    else
                    {
                        PersistenceEventSource.Log.ProcessAdd_ParentNotFound(replicationId, data.Id, data.Name, data.ParentId);
                        throw new InvalidOperationException("Parent node not found");
                    }
                }
                else if (data.Name == "/")
                {
                    this.Root = data;
                    PersistenceEventSource.Log.ProcessAdd_RootAdded(replicationId, data.Id, data.Name);
                    this.dataAvailable.Set();
                }
                else if (data.Name != "/")
                {
                    PersistenceEventSource.Log.ProcessAdd_NodeHasNoParent(replicationId, data.Id, data.Name);
                    throw new InvalidOperationException("Node has no parent");
                }

                this.instrumentation?.ProcessAddCompleted();
                PersistenceEventSource.Log.ProcessAdd_Completed(replicationId, data.Id, data.Name, data.ParentId, data.Stat.Czxid);
            }
            catch (Exception ex)
            {
                this.instrumentation?.ProcessAddFailed();
                PersistenceEventSource.Log.ProcessAdd_Failed(replicationId, data.Id, ex.ToString());
                Debug.Fail($"ProcessAdd failed with exception {ex.ToString()}");
                throw;
            }
        }

        /// <summary>
        /// Process an update notification.
        /// </summary>
        /// <param name="replicationId">Id of the replication that updated the data</param>
        /// <param name="data">The data that was updated</param>
        protected void ProcessUpdate(ulong replicationId, PersistedData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (this.IsActive)
            {
                return;
            }

            PersistenceEventSource.Log.ProcessUpdate_Started(replicationId, data.Id);
            try
            {
                PersistedData existingData;
                if (this.dataById.TryGetValue((long)data.Id, out existingData))
                {
                    // Updates lastTxId, maxId.
                    this.UpdateLastZxid(data.Stat);
                    this.UpdateMaxId(data.Id);

                    // Update total data size.
                    int dataDelta = data.Stat.DataLength - existingData.Stat.DataLength;
                    this.RecordDataDelta(dataDelta);

                    existingData.Data = data.Data;
                    existingData.Acl = data.Acl;
                    existingData.Stat = data.Stat;

                    if (existingData.ParentId != data.ParentId)
                    {
                        PersistedData previousParent;
                        if (this.dataById.TryGetValue((long)existingData.ParentId, out previousParent))
                        {
                            PersistenceEventSource.Log.ProcessUpdate_RemoveFromExistingParent(replicationId, data.Id, data.Name, previousParent.Id);
                            previousParent.Node.RemoveChild(existingData.Name);
                        }

                        PersistedData parent;
                        if (this.dataById.TryGetValue((long)data.ParentId, out parent))
                        {
                            PersistenceEventSource.Log.ProcessUpdate_AddToNewParent(replicationId, data.Id, data.Name, parent.Id);
                            parent.Node.AddChild(existingData.Node);
                        }
                    }

                    this.instrumentation?.ProcessUpdateCompleted();
                    PersistenceEventSource.Log.ProcessUpdate_Completed(replicationId, data.Id, data.Name, data.ParentId, data.Stat.Mzxid, data.Stat.Pzxid);
                }
                else
                {
                    throw new InvalidOperationException("Could not find PersistedData in nodes dictionary to update with id: " + data.Id);
                }
            }
            catch (Exception ex)
            {
                this.instrumentation?.ProcessUpdateFailed();
                PersistenceEventSource.Log.ProcessUpdate_Failed(replicationId, data.Id, ex.ToString());
                Debug.Fail($"ProcessUpdate failed with exception {ex.ToString()}");
                throw;
            }
        }

        /// <summary>
        /// Process a remove notification.
        /// </summary>
        /// <param name="replicationId">Id of the replication that removed the data</param>
        /// <param name="id">id of the element that was removed</param>
        protected void ProcessRemove(ulong replicationId, ulong id)
        {
            if (this.IsActive)
            {
                return;
            }

            PersistenceEventSource.Log.ProcessRemove_Started(replicationId, id);
            try
            {
                PersistedData child;
                if (this.dataById.TryRemove((long)id, out child))
                {
                    if (child.Node != null && child.Node.ChildrenCount > 0)
                    {
                        PersistenceEventSource.Log.ProcessRemove_NodeHasChildren(replicationId, child.Id, child.Node.ChildrenCount);
                        throw new InvalidOperationException("Remove notification received for non-empty node with id: " + id);
                    }

                    // Updates lastTxId, totalNode and totalData counters
                    this.UpdateLastZxid(child.Stat);
                    this.UpdateMaxId(child.Id);
                    this.RecordStatsDelta(-1, -child.Stat.DataLength);
                    Interlocked.Decrement(ref this.totalDataCount);

                    ulong parentId = child.ParentId;
                    if (parentId != ulong.MaxValue)
                    {
                        PersistedData parent;
                        if (this.dataById.TryGetValue((long)parentId, out parent))
                        {
                            var childNodeOfParent = parent.Node.TryGetChild(child.Name);
                            if (childNodeOfParent == child.Node)
                            {
                                parent.Node.RemoveChild(child.Name);
                                parent.Stat.Pzxid = child.Stat.Mzxid;
                            }
                            else
                            {
                                PersistenceEventSource.Log.ProcessRemove_ParentDoesNotHaveThisNodeAsChild(replicationId, child.Id, child.Name, parentId);
                                throw new InvalidOperationException($"ProcesssRemoveFailed-ParentDoesNotHAveThisNodeAsChild");
                            }
                        }
                        else
                        {
                            PersistenceEventSource.Log.ProcessRemove_ParentNotFound(replicationId, child.Id, child.Name, child.ParentId);
                            throw new InvalidOperationException($"ProcessRemoveFailed-ParentNotFound id={child.Id}, parentId={parentId}");
                        }
                    }

                    this.instrumentation?.ProcessRemoveCompleted();
                    PersistenceEventSource.Log.ProcessRemove_Completed(replicationId, id, child.Name);
                }
                else
                {
                    throw new InvalidOperationException("Remove notification received for non-existing node with id: " + id);
                }
            }
            catch (Exception ex)
            {
                this.instrumentation?.ProcessRemoveFailed();
                PersistenceEventSource.Log.ProcessRemove_Failed(replicationId, id, ex.ToString());
                Debug.Fail($"ProcessRemove failed with exception {ex.ToString()}");
                throw;
            }
        }

        /// <summary>
        /// Clear state in preparation for a complete rebuild.
        /// </summary>
        private void PrepareForRebuild()
        {
            PersistenceEventSource.Log.PrepareForRebuild();
            this.dataById.Clear();
            this.totalData = 0;
            this.totalDataCount = 0;
            this.lastAssignedChangeListId = 0;
            this.maxId = 0;
            this.lastZxid = 0;
            this.uidProvider.SetLastId(0);
            this.Root = null;
            this.dataAvailable.Reset();
        }

        private void CompleteRebuild(bool ignoreErrors)
        {
            try
            {
                var duplicates = new List<PersistedData>();
                var orphans = new List<PersistedData>();
                var timer = Stopwatch.StartNew();
                PersistenceEventSource.Log.CompleteRebuild_Started((int)this.TotalNodes);
                foreach (var pair in this.dataById)
                {
                    PersistedData data = pair.Value;
                    long parentId = (long)pair.Value.ParentId;

                    this.cancellationToken.ThrowIfCancellationRequested();

                    if (parentId > 0)
                    {
                        PersistedData parent;
                        if (this.dataById.TryGetValue(parentId, out parent))
                        {
                            PersistenceEventSource.Log.CompleteRebuild_ConnectWithParent(data.Id, data.Name, parent.Id, parent.Name, parent.GetChildrenCount());
                            var childNodeOfParent = parent.Node.TryGetChild(data.Name);

                            Debug.Assert(childNodeOfParent == null, "Parent must not have a child with the same name");

                            if (childNodeOfParent == null)
                            {
                                parent.Node.AddChild(data.Node);
                            }
                            else
                            {
                                PersistenceEventSource.Log.CompleteRebuild_ParentHasAChildWithSameName(data.Id, data.Name, parent.Id, childNodeOfParent.Persisted?.Id ?? 0);
                                duplicates.Add(data);
                            }
                        }
                        else
                        {
                            PersistenceEventSource.Log.CompleteRebuild_ParentNotFound(data.Id, data.Name, data.ParentId);
                            orphans.Add(data);
                        }
                    }
                    else if (data.Name.Equals("/"))
                    {
                        this.Root = data;
                        PersistenceEventSource.Log.ProcessLoad_RootNodeLoaded(this.Root.Id);
                    }
                    else
                    {
                        PersistenceEventSource.Log.CompleteRebuild_NodeHasNoParent(data.Id, data.Name);
                    }
                }

                this.instrumentation?.DataLoadCompleted((int)this.TotalNodes, duplicates.Count, orphans.Count);

                if (duplicates.Count > 0 || orphans.Count > 0)
                {
                    foreach (var duplicate in duplicates)
                    {
                        PersistenceEventSource.Log.CompleteRebuild_DuplicateFound(
                            duplicate.Id,
                            duplicate.Stat.Czxid,
                            duplicate.Stat.Mzxid,
                            duplicate.Stat.Pzxid,
                            duplicate.Stat.Version,
                            duplicate.Stat.Cversion,
                            duplicate.Stat.Aversion,
                            duplicate.Stat.NumChildren);
                    }

                    foreach (var orphan in orphans)
                    {
                        PersistenceEventSource.Log.CompleteRebuild_OrphanFound(
                            orphan.Id,
                            orphan.Stat.Czxid,
                            orphan.Stat.Mzxid,
                            orphan.Stat.Pzxid,
                            orphan.Stat.Version,
                            orphan.Stat.Cversion,
                            orphan.Stat.Aversion,
                            orphan.Stat.NumChildren);
                    }

                    if (!ignoreErrors)
                    {
                        throw new InvalidOperationException($"LoadFailed duplicatesFound={duplicates.Count}, orphansFound={orphans.Count}");
                    }
                }

                this.dataAvailable.Set();
                PersistenceEventSource.Log.CompleteRebuild_Finished((int)this.TotalNodes, duplicates.Count, orphans.Count, timer.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                PersistenceEventSource.Log.CompleteRebuild_Failed(ex.ToString());
                throw;
            }
        }

        private void BecomePrimary()
        {
            if (this.client.CanBecomePrimary())
            {
                this.client.OnBecomePrimary();
            }
        }

        /// <summary>
        /// Process a node load.
        /// </summary>
        /// <param name="data">The data that was added</param>
        private void ProcessLoad(PersistedData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            PersistenceEventSource.Log.ProcessLoad_Started(data.Id);

            Debug.Assert(data.Node == null, "Data being newly added must not have a node");
            Debug.Assert(data.Factory == this, "Data that is being newly added should belong to this factory");

            try
            {
                if (!this.dataById.TryAdd((long)data.Id, data))
                {
                    PersistenceEventSource.Log.ProcessLoad_AlreadyExists(data.Id, data.Name, data.ParentId);
                    throw new InvalidOperationException("ProcessLoadFailed-NodeExists");
                }

                this.backend.DoNodeForCreate(data);

                Debug.Assert(data.Node != null, "A node must have been created for the data");

                // Updates lastTxId, maxId, totalNode and totalData counters
                this.UpdateLastZxid(data.Stat);
                this.UpdateMaxId(data.Id);
                this.RecordStatsDelta(1, data.Stat.DataLength);
                Interlocked.Increment(ref this.totalDataCount);

                this.instrumentation?.ProcessLoadCompleted();
                PersistenceEventSource.Log.ProcessLoad_Completed(data.Id, data.ParentId, data.Stat.Czxid, data.Stat.Mzxid, data.Stat.Pzxid, data.Stat.Version, data.Stat.Cversion, data.Stat.Aversion, data.Stat.NumChildren);
            }
            catch (Exception ex)
            {
                this.instrumentation?.ProcessLoadFailed();
                PersistenceEventSource.Log.ProcessLoad_Failed(data.Id, ex.ToString());
                Debug.Fail($"ProcessLoad failed with exception {ex.ToString()}");
                throw;
            }
        }

        /// <summary>
        /// Set the maximum Id of PersistedData observed.
        /// </summary>
        /// <param name="id">The id of the new PersistedData</param>
        private void UpdateMaxId(ulong id)
        {
            this.maxId = Math.Max(this.maxId, id);
        }

        /// <summary>
        /// Update the last Zxid based on the given <see cref="IMutableStat"/>.
        /// </summary>
        /// <param name="stat">The stat that must be used to update the zxid</param>
        private void UpdateLastZxid(IMutableStat stat)
        {
            if (stat == null)
            {
                throw new ArgumentNullException(nameof(stat));
            }

            this.lastZxid = Math.Max(this.lastZxid, stat.Czxid);
            this.lastZxid = Math.Max(this.lastZxid, stat.Mzxid);
            this.lastZxid = Math.Max(this.lastZxid, stat.Pzxid);
        }

        /// <summary>
        /// Tuple of change list and task completion source
        /// </summary>
        private struct ChangeListReplicationTask
        {
            /// <summary>
            /// Change list to be replicated
            /// </summary>
            public ChangeList Change;

            /// <summary>
            /// Task completion source to signal the upper layer
            /// </summary>
            public TaskCompletionSource<bool> TaskCompletion;
        }
    }
}
