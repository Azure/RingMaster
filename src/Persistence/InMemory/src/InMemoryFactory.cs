// <copyright file="InMemoryFactory.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.InMemory
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence;
    using static Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.ChangeList;

    /// <summary>
    /// This factory is used for testing
    /// </summary>
    public sealed class InMemoryFactory : AbstractPersistedDataFactory
    {
        private const int MaxPendingChangeListCount = 10;
        private readonly List<InMemoryFactory> secondaries = new List<InMemoryFactory>();
        private readonly BlockingCollection<PendingChangeList> pendingChangeLists = new BlockingCollection<PendingChangeList>(MaxPendingChangeListCount);
        private readonly SemaphoreSlim changeListAvailable = new SemaphoreSlim(0, MaxPendingChangeListCount);
        private readonly CancellationTokenSource cancellationSource;
        private readonly Task processPendingChangeListsTask;
        private readonly bool isPrimary = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryFactory"/> class.
        /// </summary>
        public InMemoryFactory()
            : this(true, null, CancellationToken.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryFactory"/> class.
        /// </summary>
        /// <param name="isPrimary">Whether this replica is primary or not</param>
        /// <param name="instrumentation">Instrumentation object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public InMemoryFactory(bool isPrimary, IPersistenceInstrumentation instrumentation, CancellationToken cancellationToken)
            : base("InMemory", instrumentation, cancellationToken)
        {
            this.RequiresCallsForEachDelete = true;
            this.isPrimary = isPrimary;
            this.cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            this.processPendingChangeListsTask = Task.Run(this.ProcessPendingChangelists);
        }

        /// <summary>
        /// Gets or sets the action that will be invoked to load state.
        /// </summary>
        public Action LoadState { get; set; }

        /// <summary>
        /// Gets or sets the Action that must be invoked after each changelist is committed.
        /// </summary>
        public Action<CommittedChangeList> OnChangeListCommitted { get; set; }

        /// <summary>
        /// Enumerates the stream and returns a collection of persisted data
        /// </summary>
        /// <param name="stream">Stream to read</param>
        /// <returns>collection of persisted data object</returns>
        public IEnumerable<PersistedData> EnumerateFrom(Stream stream)
        {
            InMemoryPersistenceEventSource.Log.LoadFromStream();
            using (var binaryReader = new BinaryReader(stream))
            {
                ulong itemCount = binaryReader.ReadUInt64();

                for (ulong i = 0; i < itemCount; i++)
                {
                    PersistedData data = new PersistedData(0, this);
                    data.ReadFrom(binaryReader);
                    yield return data;
                }
            }
        }

        /// <summary>
        /// Loads the persisted data from the specified stream to rebuild the in-memory tree
        /// </summary>
        /// <param name="stream">Stream to read</param>
        public void LoadFrom(Stream stream)
        {
            InMemoryPersistenceEventSource.Log.LoadFromStream();
            this.Load(this.EnumerateFrom(stream));
        }

        /// <summary>
        /// Saves the entire in-memory tree to the specified stream
        /// </summary>
        /// <param name="stream">Stream to write</param>
        public void SaveTo(Stream stream)
        {
            InMemoryPersistenceEventSource.Log.SaveToStream();
            using (var binaryWriter = new BinaryWriter(stream))
            {
                binaryWriter.Write(this.TotalNodes);
                foreach (var data in this.GetAllItems())
                {
                    ((PersistedData)data).WriteTo(binaryWriter);
                }
            }
        }

        /// <summary>
        /// Registers a secondary replica to this factory
        /// </summary>
        /// <param name="secondary">Factory object of secondary replica</param>
        public void RegisterSecondary(InMemoryFactory secondary)
        {
            if (secondary == null)
            {
                throw new ArgumentNullException(nameof(secondary));
            }

            this.secondaries.Add(secondary);
        }

        /// <inheritdoc />
        protected override async Task StartLoadingData()
        {
            if (this.LoadState != null)
            {
                await Task.Run(() => this.LoadState());
            }
            else
            {
                this.Load(new PersistedData[0]);
            }
        }

        /// <summary>
        /// Disposes this object
        /// </summary>
        /// <param name="isDisposing">whether to dispose from managed code or native code</param>
        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                this.cancellationSource.Cancel();
                this.pendingChangeLists.CompleteAdding();
                this.processPendingChangeListsTask.Wait();
                this.pendingChangeLists.Dispose();
                this.changeListAvailable.Dispose();
                this.cancellationSource.Dispose();
            }

            base.Dispose(isDisposing);
        }

        /// <inheritdoc />
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "IReplication will be disposed by caller")]
        protected sealed override IReplication StartReplication(ulong id)
        {
            return new PendingReplication(id, this, this.secondaries);
        }

        /// <inheritdoc />
        protected sealed override void OnDeactivate()
        {
            this.cancellationSource.Cancel();
            var timer = Stopwatch.StartNew();
            InMemoryPersistenceEventSource.Log.OnDeactivate_WaitingForProcessPendingChangelistsTask();
            this.processPendingChangeListsTask.GetAwaiter().GetResult();
            InMemoryPersistenceEventSource.Log.OnDeactivate_Completed(timer.ElapsedMilliseconds);
        }

        private Task QueueChangeList(ulong id, List<byte[]> serializedChanges)
        {
            var pendingChangeList = new PendingChangeList(id, serializedChanges);
            this.pendingChangeLists.Add(pendingChangeList);
            this.changeListAvailable.Release();
            return pendingChangeList.CompletionSource.Task;
        }

        private async Task ProcessPendingChangelists()
        {
            try
            {
                CancellationToken cancellationToken = this.cancellationSource.Token;
                while (!cancellationToken.IsCancellationRequested)
                {
                    await this.changeListAvailable.WaitAsync(cancellationToken);
                    var pendingChangeList = this.pendingChangeLists.Take();
                    InMemoryPersistenceEventSource.Log.ApplyChangeList(pendingChangeList.Id);
                    this.Apply(pendingChangeList);
                    pendingChangeList.CompletionSource.SetResult(true);
                }

                InMemoryPersistenceEventSource.Log.ProcessPendingChangelistsTaskCompleted();
            }
            catch (OperationCanceledException)
            {
                InMemoryPersistenceEventSource.Log.ProcessPendingChangelistsTaskCompleted();
            }
            catch (Exception ex)
            {
                InMemoryPersistenceEventSource.Log.ProcessPendingChangelistsTaskFailed(ex.ToString());
                Debug.Fail($"ProcessPendingChangelistsTaskFailed exception={ex.ToString()}");

                this.ReportFatalError("ProcessPendingChangelists failed", ex);
                throw;
            }
        }

        private void Apply(PendingChangeList changeList)
        {
            var committedChangeList = new CommittedChangeList(changeList.Id);
            foreach (var change in changeList.EnumerateChanges(this))
            {
                switch (change.ChangeType)
                {
                    case ChangeList.ChangeType.Add:
                        this.ProcessAdd(changeList.Id, change.Data);
                        break;
                    case ChangeList.ChangeType.Update:
                        this.ProcessUpdate(changeList.Id, change.Data);
                        break;
                    case ChangeList.ChangeType.Remove:
                        this.ProcessRemove(changeList.Id, change.Data.Id);
                        break;
                }

                committedChangeList.Changes.Add(change);
            }

            if (this.OnChangeListCommitted != null)
            {
                InMemoryPersistenceEventSource.Log.ChangeListCommitCompletionNotification(changeList.Id);
                this.OnChangeListCommitted.Invoke(committedChangeList);
            }
        }

        /// <summary>
        /// Change list that has been committed
        /// </summary>
        public sealed class CommittedChangeList
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CommittedChangeList"/> class.
            /// </summary>
            /// <param name="id">Change list ID</param>
            internal CommittedChangeList(ulong id)
            {
                this.Id = id;
                this.Changes = new List<ChangeList.Change>();
            }

            /// <summary>
            /// Gets the identifier of the change list
            /// </summary>
            public ulong Id { get; private set; }

            /// <summary>
            /// Gets the list of changes
            /// </summary>
            public List<ChangeList.Change> Changes { get; private set; }
        }

        private sealed class PendingChangeList
        {
            public PendingChangeList(ulong id, List<byte[]> serializedChanges)
            {
                this.Id = id;
                this.CompletionSource = new TaskCompletionSource<bool>();
                this.SerializedChanges = serializedChanges;
            }

            public ulong Id { get; private set; }

            public List<byte[]> SerializedChanges { get; private set; }

            public TaskCompletionSource<bool> CompletionSource { get; private set; }

            public IEnumerable<ChangeList.Change> EnumerateChanges(AbstractPersistedDataFactory factory)
            {
                foreach (var serializedChange in this.SerializedChanges)
                {
                    yield return Deserialize(serializedChange, factory);
                }
            }

            /// <summary>
            /// Deserializes a <see cref="Change"/> from the given byte array.
            /// </summary>
            /// <param name="serializedChange">Byte array containing a serialized change</param>
            /// <param name="factory">The <see cref="AbstractPersistedDataFactory"/> that must be associated with the deserialized change list</param>
            /// <returns>A <see cref="ChangeList"/></returns>
            private static ChangeList.Change Deserialize(byte[] serializedChange, AbstractPersistedDataFactory factory)
            {
                MemoryStream memoryStream = new MemoryStream(serializedChange);
                try
                {
                    using (var binaryReader = new BinaryReader(memoryStream))
                    {
                        memoryStream = null;

                        ChangeList.ChangeType changeType = (ChangeList.ChangeType)binaryReader.ReadInt32();
                        PersistedData data = new PersistedData(0, factory);
                        data.ReadFrom(binaryReader);

                        return new ChangeList.Change(changeType, data);
                    }
                }
                finally
                {
                    if (memoryStream != null)
                    {
                        memoryStream.Dispose();
                    }
                }
            }
        }

        private sealed class PendingReplication : IReplication
        {
            private readonly InMemoryFactory primary;
            private readonly List<InMemoryFactory> secondaries;
            private readonly List<byte[]> serializedChanges = new List<byte[]>();

            public PendingReplication(ulong id, InMemoryFactory primary, List<InMemoryFactory> secondaries)
            {
                this.Id = id;
                this.primary = primary;
                this.secondaries = secondaries;
            }

            public ulong Id { get; private set; }

            public Task Add(PersistedData data)
            {
                var change = new ChangeList.Change(ChangeList.ChangeType.Add, data);
                this.serializedChanges.Add(Serialize(change));
                return Task.FromResult<object>(null);
            }

            public Task Update(PersistedData data)
            {
                var change = new ChangeList.Change(ChangeList.ChangeType.Update, data);
                this.serializedChanges.Add(Serialize(change));
                return Task.FromResult<object>(null);
            }

            public Task Remove(PersistedData data)
            {
                var change = new ChangeList.Change(ChangeList.ChangeType.Remove, data);
                this.serializedChanges.Add(Serialize(change));
                return Task.FromResult<object>(null);
            }

            public async Task Commit()
            {
                InMemoryPersistenceEventSource.Log.StartReplication(this.Id, this.serializedChanges.Count);
                var tasks = new List<Task>();
                tasks.Add(this.primary.QueueChangeList(this.Id, this.serializedChanges));
                foreach (var secondary in this.secondaries)
                {
                    tasks.Add(secondary.QueueChangeList(this.Id, this.serializedChanges));
                }

                await Task.WhenAll(tasks);
            }

            public void Dispose()
            {
            }

            /// <summary>
            /// Serializes the given <see cref="Change"/> to a byte array.
            /// </summary>
            /// <param name="change"><see cref="Change"/> to serialize</param>
            /// <returns>A byte array containing the serialized change list</returns>
            private static byte[] Serialize(ChangeList.Change change)
            {
                if (change == null)
                {
                    throw new ArgumentNullException(nameof(change));
                }

                MemoryStream memoryStream = new MemoryStream();
                try
                {
                    using (var binaryWriter = new BinaryWriter(memoryStream))
                    {
                        memoryStream = null;
                        binaryWriter.Write((int)change.ChangeType);
                        change.Data.WriteTo(binaryWriter);

                        return ((MemoryStream)binaryWriter.BaseStream).ToArray();
                    }
                }
                finally
                {
                    if (memoryStream != null)
                    {
                        memoryStream.Dispose();
                    }
                }
            }
        }
    }
}