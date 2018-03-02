// <copyright file="ServiceFabricPersistenceEventSource.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.ServiceFabric
{
    using System.Diagnostics;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event Source
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-Persistence-ServiceFabric")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class ServiceFabricPersistenceEventSource : EventSource
    {
        public ServiceFabricPersistenceEventSource()
        {
        }

        public static ServiceFabricPersistenceEventSource Log { get; } = new ServiceFabricPersistenceEventSource();

        [Event(2, Level = EventLevel.LogAlways, Version = 4)]
        public void PersistedDataFactory(string name, bool enableActiveSecondary)
        {
            this.WriteEvent(2, name, enableActiveSecondary);
        }

        [Event(3, Level = EventLevel.LogAlways, Version = 2)]
        public void LoadTreeCompleted(long elapsedMilliseconds)
        {
            this.WriteEvent(3, elapsedMilliseconds);
        }

        [Event(4, Level = EventLevel.Informational, Version = 2)]
        public void LoadTreeStatistics(int totalNodeCount, long dictionaryCount, int addCount, int updateCount, int removeCount)
        {
            this.WriteEvent(4, totalNodeCount, dictionaryCount, addCount, updateCount, removeCount);
        }

        [Event(5, Level = EventLevel.LogAlways, Version = 2)]
        public void ProcessDictionaryRebuildStarted()
        {
            this.WriteEvent(5);
        }

        [Event(6, Level = EventLevel.LogAlways, Version = 3)]
        public void ProcessDictionaryRebuildCompleted(long enumeratedCount, long elapsedMilliseconds)
        {
            this.WriteEvent(6, enumeratedCount, elapsedMilliseconds);
        }

        [Event(7, Level = EventLevel.LogAlways, Version = 2)]
        public void LoadTreeStarted()
        {
            this.WriteEvent(7);
        }

        [Event(8, Level = EventLevel.Error, Version = 2)]
        public void LoadTreeNew_DanglingChildrenFound(int danglingChildrenCount)
        {
            this.WriteEvent(8, danglingChildrenCount);
        }

        [Event(9, Level = EventLevel.Warning, Version = 2)]
        public void LoadTreeNew_IncorrectMemoryState(int nodeCount, long dictionaryCount, int addCount, int updateCount, int removeCount)
        {
            this.WriteEvent(9, nodeCount, dictionaryCount, addCount, updateCount, removeCount);
        }

        [Event(10, Level = EventLevel.Informational, Version = 2)]
        public void LoadTreeNew_Match(int nodeCount, long dictionaryCount, int addCount, int updateCount, int removeCount)
        {
            this.WriteEvent(10, nodeCount, dictionaryCount, addCount, updateCount, removeCount);
        }

        [Event(11, Level = EventLevel.Informational, Version = 2)]
        public void LoadTreeNew_Completed(int nodeCount, long elapsedMilliseconds)
        {
            this.WriteEvent(11, nodeCount, elapsedMilliseconds);
        }

        [Event(12, Level = EventLevel.Warning, Version = 2)]
        public void Commit_InvalidOperation(long transactionId, string exception)
        {
            this.WriteEvent(12, transactionId, exception);
        }

        [Event(14, Level = EventLevel.Warning, Version = 2)]
        public void Commit_TransactionAlreadyDisposed(long transactionId)
        {
            this.WriteEvent(14, transactionId);
        }

        [Event(15, Level = EventLevel.Verbose, Version = 2)]
        public void Commit_Completed(ulong changeListId, long transactionId, long elapsedMilliseconds)
        {
            this.WriteEvent(15, changeListId, transactionId, elapsedMilliseconds);
        }

        [Event(16, Level = EventLevel.Informational, Version = 2)]
        public void Abort_Completed(long transactionId, long elapsedMilliseconds)
        {
            this.WriteEvent(16, transactionId, elapsedMilliseconds);
        }

        [Event(17, Level = EventLevel.Warning, Version = 2)]
        public void Abort_TransactionAlreadyDisposed(long transactionId)
        {
            this.WriteEvent(17, transactionId);
        }

        [Event(18, Level = EventLevel.Informational, Version = 2)]
        public void HealthDefinition(string name, bool isPrimary, bool isLoaded)
        {
            this.WriteEvent(18, name, isPrimary, isLoaded);
        }

        [Event(19, Level = EventLevel.LogAlways, Version = 2)]
        public void Activate()
        {
            this.WriteEvent(19);
        }

        [Event(20, Level = EventLevel.LogAlways, Version = 2)]
        public void Deactivate()
        {
            this.WriteEvent(20);
        }

        [Event(21, Level = EventLevel.Verbose, Version = 5)]
        public void ProcessAdd_Completed(ulong dataId, long elapsedMilliseconds)
        {
            this.WriteEvent(21, dataId, elapsedMilliseconds);
        }

        [Event(22, Level = EventLevel.Error, Version = 2)]
        public void ProcessUpdate_IdNotFound(ulong dataId)
        {
            this.WriteEvent(22, dataId);
        }

        [Event(23, Level = EventLevel.Error, Version = 2)]
        public void ProcessUpdate_UpdateFailed(ulong dataId)
        {
            this.WriteEvent(23, dataId);
        }

        [Event(24, Level = EventLevel.Verbose, Version = 2)]
        public void ProcessUpdate_Completed(ulong dataId, long elapsedMilliseconds)
        {
            this.WriteEvent(24, dataId, elapsedMilliseconds);
        }

        [Event(25, Level = EventLevel.Error, Version = 2)]
        public void ProcessRemove_IdNotFound(long dataId)
        {
            this.WriteEvent(25, dataId);
        }

        [Event(26, Level = EventLevel.Error, Version = 2)]
        public void ProcessRemove_NodeHasChildren(ulong dataId, int childrenCount)
        {
            this.WriteEvent(26, dataId, childrenCount);
        }

        [Event(27, Level = EventLevel.Verbose, Version = 2)]
        public void ProcessRemove_Completed(ulong dataId, long elapsedMilliseconds)
        {
            this.WriteEvent(27, dataId, elapsedMilliseconds);
        }

        [Event(28, Level = EventLevel.Verbose, Version = 2)]
        public void Update(long transactionId, long key, string nodeName)
        {
            this.WriteEvent(28, transactionId, key, nodeName);
        }

        [Event(29, Level = EventLevel.Informational, Version = 2)]
        public void Add(long transactionId, long key, string nodeName)
        {
            this.WriteEvent(29, transactionId, key, nodeName);
        }

        [Event(30, Level = EventLevel.Verbose, Version = 2)]
        public void Remove(long transactionId, long key)
        {
            this.WriteEvent(30, transactionId, key);
        }

        [Event(31, Level = EventLevel.LogAlways, Version = 2)]
        public void ProcessStateManagerRebuildNotification_ReliableDictionaryFound(string name)
        {
            this.WriteEvent(31, name);
        }

        [Event(32, Level = EventLevel.Informational, Version = 2)]
        public void SetFlagsForReliableDictionary()
        {
            this.WriteEvent(32);
        }

        [Event(33, Level = EventLevel.LogAlways, Version = 2)]
        public void CreateRootNode(ulong lastId)
        {
            this.WriteEvent(33, lastId);
        }

        [Event(34, Level = EventLevel.Verbose, Version = 2)]
        public void OnDictionaryChanged(long transactionId, string action)
        {
            this.WriteEvent(34, transactionId, action);
        }

        [Event(35, Level = EventLevel.Verbose, Version = 2)]
        public void PersistedDataSerializer_Read(ulong dataId, string name)
        {
            this.WriteEvent(35, dataId, name);
        }

        [Event(36, Level = EventLevel.Verbose, Version = 2)]
        public void PersistedDataSerializer_ReadDifferential(ulong baseDataId, string baseName, ulong dataId, string name)
        {
            this.WriteEvent(36, baseDataId, baseName, dataId, name);
        }

        [Event(37, Level = EventLevel.Verbose, Version = 2)]
        public void PersistedDataSerializer_Write(ulong dataId, string name)
        {
            this.WriteEvent(37, dataId, name);
        }

        [Event(38, Level = EventLevel.Verbose, Version = 2)]
        public void PersistedDataSerializer_WriteDifferential(ulong baseDataId, string baseName, ulong dataId, string name)
        {
            this.WriteEvent(38, baseDataId, baseName, dataId, name);
        }

        [Event(39, Level = EventLevel.Verbose, Version = 2)]
        public void ProcessAdd_Started(ulong dataId)
        {
            this.WriteEvent(39, dataId);
        }

        [Event(40, Level = EventLevel.LogAlways, Version = 2)]
        public void OnStateManagerChanged(string action)
        {
            this.WriteEvent(40, action);
        }

        [Event(41, Level = EventLevel.LogAlways, Version = 3)]
        public void ProcessStateManagerRebuildNotification()
        {
            this.WriteEvent(41);
        }

        [Event(42, Level = EventLevel.LogAlways, Version = 2)]
        public void ProcessStateManagerRebuildNotificationCompleted(int enumerated, bool dataByIdFound, long elapsedMilliseconds)
        {
            this.WriteEvent(42, enumerated, dataByIdFound, elapsedMilliseconds);
        }

        [Event(43, Level = EventLevel.LogAlways, Version = 3)]
        public void StartLoadingData()
        {
            this.WriteEvent(43);
        }

        [Event(44, Level = EventLevel.LogAlways, Version = 3)]
        public void CreateOrGetRoot_CreatingNewRoot(long transactionId)
        {
            this.WriteEvent(44, transactionId);
        }

        [Event(45, Level = EventLevel.LogAlways, Version = 2)]
        public void CreateOrGetRoot_RootPresent(ulong id)
        {
            this.WriteEvent(45, id);
        }

        [Event(46, Level = EventLevel.LogAlways, Version = 2)]
        public void ProcessStateManagerSingleEntityNotification_ReliableDictionaryAdded(string name)
        {
            this.WriteEvent(46, name);
        }

        [Event(47, Level = EventLevel.LogAlways, Version = 2)]
        public void ProcessAdd_RootNodeCreated(ulong rootId)
        {
            this.WriteEvent(47, rootId);
        }

        [Event(48, Level = EventLevel.LogAlways, Version = 3)]
        public void ProcessDictionaryRebuildFailed(long enumeratedCount, long elapsedMilliseconds, string exception)
        {
            this.WriteEvent(48, enumeratedCount, elapsedMilliseconds, exception);
        }

        [Event(49, Level = EventLevel.Error, Version = 2)]
        public void ProcessRemove_Failed(ulong dataId, string exception)
        {
            this.WriteEvent(49, dataId, exception);
        }

        [Event(50, Level = EventLevel.Error, Version = 2)]
        public void ProcessUpdate_Failed(ulong dataId, string exception)
        {
            this.WriteEvent(50, dataId, exception);
        }

        [Event(51, Level = EventLevel.Error, Version = 2)]
        public void ProcessAdd_Failed(ulong dataId, string exception)
        {
            this.WriteEvent(51, dataId, exception);
        }

        [Event(52, Level = EventLevel.LogAlways, Version = 2)]
        public void Status(ulong totalNodes, ulong totalDataSize)
        {
            this.WriteEvent(52, totalNodes, totalDataSize);
        }

        [Event(53, Level = EventLevel.Verbose, Version = 2)]
        public void TransactionCommitted(long transactionId, long commitSequenceNumber)
        {
            this.WriteEvent(53, transactionId, commitSequenceNumber);
        }

        [Event(54, Level = EventLevel.Informational, Version = 2)]
        public void ProcessRemove_NoParent(ulong nodeId)
        {
            this.WriteEvent(54, nodeId);
        }

        [Event(55, Level = EventLevel.Informational, Version = 2)]
        public void ProcessRemove_RemoveChildFromParent(ulong nodeId, ulong parentId)
        {
            this.WriteEvent(55, nodeId, parentId);
        }

        [Event(56, Level = EventLevel.Error, Version = 3)]
        public void PersistedData_RemoveChild_NotParent(ulong nodeId, ulong childId, ulong actualParentId)
        {
            this.WriteEvent(56, nodeId, childId, actualParentId);
        }

        [Event(57, Level = EventLevel.Verbose, Version = 1)]
        public void PersistedData_OnCreate(long transactionId, ulong id, string name)
        {
            this.WriteEvent(57, transactionId, id, name);
        }

        [Event(58, Level = EventLevel.Verbose, Version = 1)]
        public void PersistedData_OnUpdate(long transactionId, ulong id, string name)
        {
            this.WriteEvent(58, transactionId, id, name);
        }

        [Event(59, Level = EventLevel.Verbose, Version = 1)]
        public void PersistedData_OnRemove(long transactionId, ulong id, string name)
        {
            this.WriteEvent(59, transactionId, id, name);
        }

        [Event(60, Level = EventLevel.Error, Version = 2)]
        public void Commit_Failed(ulong changeListId, long elapsedMilliseconds, string exception)
        {
            this.WriteEvent(60, changeListId, elapsedMilliseconds, exception);
        }

        [Event(61, Level = EventLevel.Error, Version = 2)]
        public void StartReplication_Failed(ulong changeListId, string exception)
        {
            this.WriteEvent(61, changeListId, exception);
        }

        [Event(62, Level = EventLevel.LogAlways, Version = 2)]
        public void OnDeactivate()
        {
            this.WriteEvent(62);
        }

        [Event(63, Level = EventLevel.LogAlways, Version = 1)]
        public void LoadDictionaryStarted()
        {
            this.WriteEvent(63);
        }

        [Event(64, Level = EventLevel.LogAlways, Version = 1)]
        public void LoadDictionaryCompleted(long enumeratedCount, long elapsedMilliseconds)
        {
            this.WriteEvent(64, enumeratedCount, elapsedMilliseconds);
        }

        [Event(65, Level = EventLevel.Error, Version = 1)]
        public void LoadDictionaryFailed(long enumeratedCount, long elapsedMilliseconds, string exception)
        {
            this.WriteEvent(65, enumeratedCount, elapsedMilliseconds, exception);
        }

        [Event(66, Level = EventLevel.LogAlways, Version = 1)]
        public void LoadDictionaryStartEnumeration(long transactionId)
        {
            this.WriteEvent(66, transactionId);
        }

        [Event(67, Level = EventLevel.LogAlways, Version = 1)]
        public void DataByIdDictionaryAlreadyExists(long count)
        {
            this.WriteEvent(67, count);
        }

        [Event(68, Level = EventLevel.LogAlways, Version = 1)]
        public void CreatingDataByIdDictionary()
        {
            this.WriteEvent(68);
        }
    }
}