// <copyright file="PersistedDataFactory.cs" company="Microsoft">
//   Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.ServiceFabric
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Fabric;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Data.Notifications;

    using HealthDefinition = Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence.HealthDefinition;
    using PersistedData = Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.PersistedData;
    using WinFabPersistedData = Microsoft.Azure.Networking.Infrastructure.RingMaster.WinFabPersistence.PersistedData;

    /// <summary>
    /// An implementation of the <see cref="IPersistedDataFactory{T}"/> interface that uses
    /// Service Fabric reliable collections to reliably persist data.
    /// </summary>
    public sealed class PersistedDataFactory : AbstractPersistedDataFactory
    {
        /// <summary>
        /// Number of change list can be queued for replication grouping
        /// </summary>
        private const int DefaultReplicationQueueSize = 8 * 1024;

        /// <summary>
        /// Total data size of a replication group, beyond this value no further change list will be taken
        /// </summary>
        private const int DefaultReplicationGroupDataSize = 1024 * 1024;

        private const string DataByIdDictionaryName = "dataById";

        private static readonly PersistedDataSerializer SerializerInstance = new PersistedDataSerializer();

        // Interface to the state manager that must be used to perform operations on ReliableCollections.
        private readonly IReliableStateManager stateManager;

        // Configuration settings
        private readonly Configuration configuration;

        // Interface to instrumentation consumer
        private readonly IServiceFabricPersistenceInstrumentation instrumentation;

        // Token that will be observed for cancellation signal
        private readonly CancellationToken cancellationToken;

        // Current replica role.
        private ReplicaRole replicaRole = ReplicaRole.Unknown;

        // Set to true once rebuild is completed.
        private volatile bool rebuildCompleted = false;

        public PersistedDataFactory(
            IReliableStateManager stateManager,
            string name,
            Configuration configuration,
            IServiceFabricPersistenceInstrumentation instrumentation,
            CancellationToken cancellationToken)
            : this(stateManager, name, configuration, DefaultReplicationQueueSize, DefaultReplicationGroupDataSize, instrumentation, cancellationToken)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistedDataFactory"/> class.
        /// </summary>
        /// <param name="stateManager">The <see cref="IReliableStateManager"/> associated with this instance</param>
        /// <param name="name">Name of this instance</param>
        /// <param name="configuration">Configuration settings</param>
        /// <param name="replicationQueueSize">Size of replication queue in the base class</param>
        /// <param name="maxReplicationDataSize">Size of replication data group</param>
        /// <param name="instrumentation">Instrumentation consumer</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public PersistedDataFactory(
            IReliableStateManager stateManager,
            string name,
            Configuration configuration,
            int replicationQueueSize,
            int maxReplicationDataSize,
            IServiceFabricPersistenceInstrumentation instrumentation,
            CancellationToken cancellationToken)
            : base(name, instrumentation, cancellationToken, replicationQueueSize, maxReplicationDataSize)
        {
            if (stateManager == null)
            {
                throw new ArgumentNullException(nameof(stateManager));
            }

            PersistedDataFactory.SerializerInstance.Factory = this;

            this.stateManager = stateManager;
            this.configuration = configuration ?? new Configuration();
            this.instrumentation = instrumentation;
            this.cancellationToken = cancellationToken;

            if (this.configuration.EnableActiveSecondary)
            {
                this.stateManager.TransactionChanged += this.OnStateManagerTransactionChangedHandler;
                this.stateManager.StateManagerChanged += this.OnStateManagerChangedHandler;
            }

            ServiceFabricPersistenceEventSource.Log.PersistedDataFactory(name, this.configuration.EnableActiveSecondary);
        }

        /// <summary>
        /// Gets the serializer that must be used to serialize an instance of <see cref="IPersistedData"/>.
        /// </summary>
        public static IStateSerializer<WinFabPersistedData> Serializer
        {
            get
            {
                return PersistedDataFactory.SerializerInstance;
            }
        }

        /// <summary>
        /// Create an object that implements <see cref="IReliableStateManagerReplica"/>
        /// </summary>
        /// <param name="context">Context of the stateful service</param>
        /// <returns>An object that implements <see cref="IReliableStateManagerReplica"/></returns>
        public static IReliableStateManagerReplica CreateStateManager(StatefulServiceContext context)
        {
            var serializerInstaller = new SerializerInstaller();
            var stateManager = new ReliableStateManager(
                context,
                new ReliableStateManagerConfiguration(onInitializeStateSerializersEvent: serializerInstaller.OnSerializerSetupAsync));
            serializerInstaller.StateManager = stateManager;
            return stateManager;
        }

        public void ReportStatus()
        {
            ServiceFabricPersistenceEventSource.Log.Status(this.TotalNodes, this.TotalData);
            this.instrumentation?.ReportStatistics(this.TotalNodes, this.TotalData);
        }

        /// <summary>
        /// Sets the replica role.
        /// </summary>
        public void SetRole(ReplicaRole newRole)
        {
            this.replicaRole = newRole;
        }

        /// <summary>
        /// Gets the health definition of this factory.
        /// </summary>
        /// <returns>Dictionary&lt;System.String, System.String&gt;.</returns>
        public override Dictionary<string, HealthDefinition> GetHealth()
        {
            Dictionary<string, HealthDefinition> d = new Dictionary<string, HealthDefinition>();

            if (this.replicaRole.Equals(ReplicaRole.Unknown))
            {
                // Treated as unhealthy secondary
                d[this.Name] = new HealthDefinition(false, 0, "Unknown replica role");
            }
            else
            {
                bool primary = this.replicaRole.Equals(ReplicaRole.Primary);
                bool loaded = false;
                if (primary)
                {
                    loaded = this.IsBackendPrimary;
                }
                else if (this.configuration.EnableActiveSecondary)
                {
                    loaded = this.rebuildCompleted && this.replicaRole.Equals(ReplicaRole.ActiveSecondary);
                }
                else
                {
                    loaded = this.replicaRole.Equals(ReplicaRole.ActiveSecondary);
                }

                d[this.Name] = new HealthDefinition(primary, loaded ? 1 : 0, "Loaded");
            }

            return d;
        }

        /// <summary>
        /// Start the data load process.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        protected override async Task StartLoadingData()
        {
            ServiceFabricPersistenceEventSource.Log.StartLoadingData();

            ConditionalValue<IReliableDictionary<long, WinFabPersistedData>> result = await this.stateManager.TryGetAsync<IReliableDictionary<long, WinFabPersistedData>>(DataByIdDictionaryName);
            if (result.HasValue)
            {
                var dataById = result.Value;
                using (var transaction = this.stateManager.CreateTransaction())
                {
                    long count = await dataById.GetCountAsync(transaction);
                    ServiceFabricPersistenceEventSource.Log.DataByIdDictionaryAlreadyExists(count);

                    // If active secondary feature is not enabled, then the
                    // dictionary must be explicitly loaded. Otherwise, the
                    // dictionary will be loaded when a rebuild notification
                    // is received.
                    if (!this.configuration.EnableActiveSecondary)
                    {
                        await this.LoadDictionary(dataById, transaction);
                    }

                    await transaction.CommitAsync();
                }
            }
            else
            {
                ServiceFabricPersistenceEventSource.Log.CreatingDataByIdDictionary();
                await this.stateManager.GetOrAddAsync<IReliableDictionary<long, WinFabPersistedData>>(DataByIdDictionaryName);
                this.Load(new PersistedData[0]);
            }
        }

        /// <summary>
        /// Replicate the changes included in the given <see cref="ChangeList"/>.
        /// </summary>
        /// <param name="id">Id of the replication</param>
        /// <returns>An object that implements the <see cref="IReplication"/> interface</returns>
        protected override IReplication StartReplication(ulong id)
        {
            ITransaction transaction = null;
            try
            {
                transaction = this.stateManager.CreateTransaction();
                ITransaction tempTransaction = transaction;
                var replication = new Replication(this, tempTransaction);
                transaction = null;
                return replication;
            }
            catch (Exception ex)
            {
                ServiceFabricPersistenceEventSource.Log.StartReplication_Failed(id, ex.ToString());
                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    transaction.Dispose();
                }
            }
        }

        protected override void OnDeactivate()
        {
            ServiceFabricPersistenceEventSource.Log.OnDeactivate();
        }

        /// <summary>
        /// Synchronously add the specified key/value pair to the reliable dictionary.
        /// </summary>
        /// <param name="transaction">Transaction to associate this operation with</param>
        /// <param name="key">The key to be added</param>
        /// <param name="value">PersistedData value</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        private async Task Add(ITransaction transaction, long key, PersistedData value)
        {
            ServiceFabricPersistenceEventSource.Log.Add(transaction.TransactionId, key, value?.Name);
            var timer = Stopwatch.StartNew();
            var dataById = await this.GetDataById(transaction);
            await dataById.AddAsync(transaction, key, new WinFabPersistedData(value));
            this.instrumentation?.AddRequested(timer.Elapsed);
        }

        /// <summary>
        /// Synchronously update the specified key/value pair in the reliable dictionary.
        /// </summary>
        /// <param name="transaction">Transaction to associate this operation with</param>
        /// <param name="key">The key whose value should be updated</param>
        /// <param name="value">The value that replaces the value of the element that has the specified key</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        private async Task Update(ITransaction transaction, long key, PersistedData value)
        {
            ServiceFabricPersistenceEventSource.Log.Update(transaction.TransactionId, key, value?.Name);
            var timer = Stopwatch.StartNew();
            var dataById = await this.GetDataById(transaction);
            await dataById.SetAsync(transaction, key, new WinFabPersistedData(value));
            this.instrumentation?.UpdateRequested(timer.Elapsed);
        }

        /// <summary>
        /// Synchronously attempts to remove the value with the specified key from the reliable dictionary.
        /// This operation won't persist until transaction committed.
        /// </summary>
        /// <param name="transaction">Transaction to associate this operation with</param>
        /// <param name="key">The key to be removed</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        private async Task Remove(ITransaction transaction, long key)
        {
            ServiceFabricPersistenceEventSource.Log.Remove(transaction.TransactionId, key);
            var timer = Stopwatch.StartNew();
            var dataById = await this.GetDataById(transaction);
            await dataById.TryRemoveAsync(transaction, key);
            this.instrumentation?.RemoveRequested(timer.Elapsed);
        }

        private Task<IReliableDictionary<long, WinFabPersistedData>> GetDataById(ITransaction transaction)
        {
            return this.stateManager.GetOrAddAsync<IReliableDictionary<long, WinFabPersistedData>>(transaction, DataByIdDictionaryName);
        }

        private void OnStateManagerTransactionChangedHandler(object sender, NotifyTransactionChangedEventArgs e)
        {
            if (e.Action == NotifyTransactionChangedAction.Commit)
            {
                long transactionId = e.Transaction.TransactionId;
                long commitSequenceNumber = e.Transaction.CommitSequenceNumber;
                ServiceFabricPersistenceEventSource.Log.TransactionCommitted(transactionId, commitSequenceNumber);
                this.instrumentation?.TransactionCommitted(transactionId, commitSequenceNumber);
                this.instrumentation?.ReportStatistics(this.TotalNodes, this.TotalData);
            }
        }

        private void OnStateManagerChangedHandler(object sender, NotifyStateManagerChangedEventArgs e)
        {
            ServiceFabricPersistenceEventSource.Log.OnStateManagerChanged(e.Action.ToString());
            if (e.Action == NotifyStateManagerChangedAction.Rebuild)
            {
                this.ProcessStateManagerRebuildNotification(e).GetAwaiter().GetResult();
                return;
            }

            this.ProcessStateManagerSingleEntityNotification(e);
        }

        private async Task ProcessStateManagerRebuildNotification(NotifyStateManagerChangedEventArgs e)
        {
            int enumerated = 0;
            bool dataByIdFound = false;
            var timer = Stopwatch.StartNew();
            ServiceFabricPersistenceEventSource.Log.ProcessStateManagerRebuildNotification();
            var operation = e as NotifyStateManagerRebuildEventArgs;

            using (var enumerator = operation.ReliableStates.GetAsyncEnumerator())
            {
                while (await enumerator.MoveNextAsync(this.cancellationToken))
                {
                    enumerated++;
                    if (enumerator.Current is IReliableDictionary<long, WinFabPersistedData>)
                    {
                        var dictionary = (IReliableDictionary<long, WinFabPersistedData>)enumerator.Current;
                        this.SetFlagsForReliableDictionary(dictionary);

                        dictionary.RebuildNotificationAsyncCallback = this.OnDictionaryRebuildNotificationHandlerAsync;

                        // Ensure that the handler is registered only once by doing -= followed by +=
                        // It is safe to do -= even if no handler has been added.
                        dictionary.DictionaryChanged -= this.OnDictionaryChangedHandler;
                        dictionary.DictionaryChanged += this.OnDictionaryChangedHandler;

                        dataByIdFound = true;
                        ServiceFabricPersistenceEventSource.Log.ProcessStateManagerRebuildNotification_ReliableDictionaryFound(dictionary.Name.ToString());
                    }
                }
            }

            ServiceFabricPersistenceEventSource.Log.ProcessStateManagerRebuildNotificationCompleted(enumerated, dataByIdFound, timer.ElapsedMilliseconds);
        }

        private void ProcessStateManagerSingleEntityNotification(NotifyStateManagerChangedEventArgs e)
        {
            var operation = e as NotifyStateManagerSingleEntityChangedEventArgs;

            // Register OnDictionaryChangedHandler when dictionary is added so that no notification is missed.
            if (operation.Action == NotifyStateManagerChangedAction.Add &&
                operation.ReliableState is IReliableDictionary<long, WinFabPersistedData>)
            {
                var dictionary = (IReliableDictionary<long, WinFabPersistedData>)operation.ReliableState;
                this.SetFlagsForReliableDictionary(dictionary);

                // Ensure that the handler is registered only once by doing -= followed by +=
                // It is safe to do -= even if no handler has been added.
                dictionary.DictionaryChanged -= this.OnDictionaryChangedHandler;
                dictionary.DictionaryChanged += this.OnDictionaryChangedHandler;

                dictionary.RebuildNotificationAsyncCallback = this.OnDictionaryRebuildNotificationHandlerAsync;
                ServiceFabricPersistenceEventSource.Log.ProcessStateManagerSingleEntityNotification_ReliableDictionaryAdded(dictionary.Name.ToString());
            }
        }

        private async Task OnDictionaryRebuildNotificationHandlerAsync(
            object sender,
            NotifyDictionaryRebuildEventArgs<long, WinFabPersistedData> rebuildNotification)
        {
            var enumerator = rebuildNotification.State.GetAsyncEnumerator();
            await this.ProcessDictionaryRebuild(enumerator);
        }

        private void OnDictionaryChangedHandler(object sender, NotifyDictionaryChangedEventArgs<long, WinFabPersistedData> e)
        {
            lock (this)
            {
                switch (e.Action)
                {
                    // Clear notification should never (?) be receved.
                    case NotifyDictionaryChangedAction.Clear:
                        throw new InvalidOperationException("Unexpected notification: " + e.ToString());

                    case NotifyDictionaryChangedAction.Add:
                        var addEvent = e as NotifyDictionaryItemAddedEventArgs<long, WinFabPersistedData>;
                        this.ProcessAdd((ulong)addEvent.Transaction.TransactionId, addEvent.Value.Data);
                        return;

                    case NotifyDictionaryChangedAction.Update:
                        var updateEvent = e as NotifyDictionaryItemUpdatedEventArgs<long, WinFabPersistedData>;
                        this.ProcessUpdate((ulong)updateEvent.Transaction.TransactionId, updateEvent.Value.Data);
                        return;

                    case NotifyDictionaryChangedAction.Remove:
                        var deleteEvent = e as NotifyDictionaryItemRemovedEventArgs<long, WinFabPersistedData>;
                        this.ProcessRemove((ulong)deleteEvent.Transaction.TransactionId, (ulong)deleteEvent.Key);
                        return;

                    default:
                        break;
                }
            }
        }

        private void SetFlagsForReliableDictionary(IReliableDictionary<long, WinFabPersistedData> dictionary)
        {
            Type distributedDicType = dictionary.GetType();

            FieldInfo storeField = distributedDicType.GetField("dataStore", BindingFlags.Instance | BindingFlags.NonPublic);
            object storeInstance = storeField.GetValue(dictionary);
            Type storeType = storeField.FieldType;

            FieldInfo shouldLoadValues = storeType.GetField("shouldLoadValuesInRecovery", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo enableStrict2PL = storeType.GetField("enableStrict2PL", BindingFlags.Instance | BindingFlags.NonPublic);

            shouldLoadValues.SetValue(storeInstance, true);
            enableStrict2PL.SetValue(storeInstance, true);

            ServiceFabricPersistenceEventSource.Log.SetFlagsForReliableDictionary();
        }

        /// <summary>
        /// Process a rebuild notification sent by the reliable dictionary.
        /// </summary>
        /// <param name="enumerator">Enumerator for the list of nodes</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        private async Task ProcessDictionaryRebuild(IAsyncEnumerator<KeyValuePair<long, WinFabPersistedData>> enumerator)
        {
            Stopwatch timer = Stopwatch.StartNew();
            var dataList = new List<PersistedData>();
            ServiceFabricPersistenceEventSource.Log.ProcessDictionaryRebuildStarted();
            try
            {
                using (enumerator)
                {
                    while (await enumerator.MoveNextAsync(this.cancellationToken))
                    {
                        dataList.Add(enumerator.Current.Value.Data);
                    }
                }

                this.Load(dataList, this.configuration.IgnoreErrorsDuringLoad);

                timer.Stop();

                this.rebuildCompleted = true;
            }
            catch (Exception ex)
            {
                this.instrumentation?.ProcessDictionaryRebuildFailed(timer.Elapsed);
                ServiceFabricPersistenceEventSource.Log.ProcessDictionaryRebuildFailed(dataList.Count, timer.ElapsedMilliseconds, ex.ToString());
                throw;
            }
            finally
            {
                this.instrumentation?.ProcessDictionaryRebuildCompleted(dataList.Count, timer.Elapsed);
                ServiceFabricPersistenceEventSource.Log.ProcessDictionaryRebuildCompleted(dataList.Count, timer.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Explicitly load the data by id dictionary.
        /// </summary>
        /// <param name="dataById">Dictionary to load</param>
        /// <param name="transaction">Transaction to use</param>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        private async Task LoadDictionary(IReliableDictionary<long, WinFabPersistedData> dataById, ITransaction transaction)
        {
            ServiceFabricPersistenceEventSource.Log.LoadDictionaryStarted();
            Stopwatch timer = Stopwatch.StartNew();
            var dataList = new List<PersistedData>();
            try
            {
                ServiceFabricPersistenceEventSource.Log.LoadDictionaryStartEnumeration(transaction.TransactionId);
                IAsyncEnumerable<KeyValuePair<long, WinFabPersistedData>> enumerable = await dataById.CreateEnumerableAsync(transaction);
                using (var enumerator = enumerable.GetAsyncEnumerator())
                {
                    while (await enumerator.MoveNextAsync(CancellationToken.None))
                    {
                        dataList.Add(enumerator.Current.Value.Data);
                    }
                }

                this.Load(dataList, this.configuration.IgnoreErrorsDuringLoad);

                ServiceFabricPersistenceEventSource.Log.LoadDictionaryCompleted(dataList.Count, timer.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                ServiceFabricPersistenceEventSource.Log.LoadDictionaryFailed(dataList.Count, timer.ElapsedMilliseconds, ex.ToString());
                this.ReportFatalError("Failed to load dictionary", ex);
                throw;
            }
        }

        /// <summary>
        /// Configuration settings
        /// </summary>
        public class Configuration
        {
            /// <summary>
            /// Gets or sets a value indicating whether the secondary will actively
            /// maintain its data structures to ensure quick failover.
            /// </summary>
            public bool EnableActiveSecondary { get; set; } = true;

            /// <summary>
            /// Gets or sets a value indicating whether errors during load must be ignored.
            /// </summary>
            public bool IgnoreErrorsDuringLoad { get; set; } = false;
        }

        private sealed class Replication : IReplication
        {
            private readonly PersistedDataFactory factory;
            private readonly ITransaction transaction;

            public Replication(PersistedDataFactory factory, ITransaction transaction)
            {
                this.factory = factory;
                this.transaction = transaction;
            }

            public ulong Id => (ulong)this.transaction.TransactionId;

            public async Task Add(PersistedData value)
            {
                await this.factory.Add(this.transaction, (long)value.Id, value);
            }

            public async Task Update(PersistedData value)
            {
                await this.factory.Update(this.transaction, (long)value.Id, value);
            }

            public async Task Remove(PersistedData value)
            {
                await this.factory.Remove(this.transaction, (long)value.Id);
            }

            public async Task Commit()
            {
                await this.transaction.CommitAsync();
            }

            public void Dispose()
            {
                this.transaction.Dispose();
            }
        }

        private class SerializerInstaller
        {
            public IReliableStateManager StateManager { get; set; }

            public async Task OnSerializerSetupAsync()
            {
#pragma warning disable 612, 618
                this.StateManager.TryAddStateSerializer<WinFabPersistedData>(PersistedDataFactory.Serializer);
#pragma warning restore 612, 618

                await Task.FromResult(true);
            }
        }
    }
}
