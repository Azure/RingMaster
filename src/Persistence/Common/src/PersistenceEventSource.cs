// <copyright file="PersistenceEventSource.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence
{
    using System.Diagnostics;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event Source
    /// </summary>
    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-Persistence")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class PersistenceEventSource : EventSource
    {
        public PersistenceEventSource()
        {
            this.TraceLevel = TraceLevel.Info;
        }

        public static PersistenceEventSource Log { get; } = new PersistenceEventSource();

        // Note: TraceLevel has EventId=1 as compiler will auto-generate a method for the property so we
        // must start at 2. Pay attention to fix the event ids if more properties are added in future.
        public TraceLevel TraceLevel { get; set; }

        [Event(2, Level = EventLevel.Informational, Version = 1)]
        public void PersistedDataDelete(ulong id, ulong parentId)
        {
            this.WriteEvent(2, id, parentId);
        }

        [Event(3, Level = EventLevel.Informational, Version = 1)]
        public void PersistedDataAddChild(ulong parentId, string parentName, ulong childId, string childName, int childrenCount)
        {
            this.WriteEvent(3, parentId, parentName, childId, childName, childrenCount);
        }

        [Event(4, Level = EventLevel.Informational, Version = 1)]
        public void PersistedDataAddChild_RemovingChildFromExistingParent(ulong parentId, string parentName, ulong childId, string childName)
        {
            this.WriteEvent(4, parentId, parentName, childId, childName);
        }

        [Event(5, Level = EventLevel.Informational, Version = 1)]
        public void PersistedDataRemoveChild(ulong parentId, string parentName, ulong childId, string childName, int childrenCount)
        {
            this.WriteEvent(5, parentId, parentName, childId, childName, childrenCount);
        }

        [Event(6, Level = EventLevel.Informational, Version = 1)]
        public void PersistedDataRemoveChild_ParentIsNull(ulong childId, string childName)
        {
            this.WriteEvent(6, childId, childName);
        }

        [Event(7, Level = EventLevel.Informational, Version = 1)]
        public void PersistedDataRemoveChild_ParentIsNotThis(ulong childId, string childName, ulong thisId, string thisName, ulong actualParentId, string actualParentName)
        {
            this.WriteEvent(7, childId, childName, thisId, thisName, actualParentId, actualParentName);
        }

        [Event(8, Level = EventLevel.Informational, Version = 1)]
        public void PersistedDataAppendCreate(ulong id, string name)
        {
            this.WriteEvent(8, id, name);
        }

        [Event(9, Level = EventLevel.Informational, Version = 1)]
        public void PersistedDataAppendAddChild(ulong id, string name, ulong childId, string childName)
        {
            this.WriteEvent(9, id, name, childId, childName);
        }

        [Event(10, Level = EventLevel.Informational, Version = 1)]
        public void PersistedDataAppendRemoveChild(ulong id, string name, ulong childId, string childName)
        {
            this.WriteEvent(10, id, name, childId, childName);
        }

        [Event(11, Level = EventLevel.Informational, Version = 1)]
        public void PersistedDataAppendRemove(ulong id, string name, bool isRecursive)
        {
            this.WriteEvent(11, id, name, isRecursive);
        }

        [Event(12, Level = EventLevel.Informational, Version = 1)]
        public void PersistedDataAppendSetAcl(ulong id, string name)
        {
            this.WriteEvent(12, id, name);
        }

        [Event(13, Level = EventLevel.Informational, Version = 1)]
        public void PersistedDataAppendSetData(ulong id, string name)
        {
            this.WriteEvent(13, id, name);
        }

        [Event(14, Level = EventLevel.LogAlways, Version = 1)]
        public void PersistedDataAppendPoison(ulong id, string name, string spec)
        {
            this.WriteEvent(14, id, name, spec);
        }

        [Event(15, Level = EventLevel.Verbose, Version = 1)]
        public void PersistedDataAppendSetParent(ulong id, string name)
        {
            this.WriteEvent(15, id, name);
        }

        [Event(16, Level = EventLevel.Verbose, Version = 1)]
        public void PersistedDataAppendRead(ulong id, string name)
        {
            this.WriteEvent(16, id, name);
        }

        [Event(17, Level = EventLevel.LogAlways, Version = 1)]
        public void HealthDefinition(string name, bool isPrimary, bool isLoaded)
        {
            this.WriteEvent(17, name, isPrimary, isLoaded);
        }

        [Event(18, Level = EventLevel.LogAlways, Version = 2)]
        public void Activate(int processId)
        {
            this.WriteEvent(18, processId);
        }

        [Event(19, Level = EventLevel.LogAlways, Version = 2)]
        public void Deactivate(int processId)
        {
            this.WriteEvent(19, processId);
        }

        [Event(20, Level = EventLevel.LogAlways, Version = 1)]
        public void EnsureRoot()
        {
            this.WriteEvent(20);
        }

        [Event(21, Level = EventLevel.LogAlways, Version = 1)]
        public void EnsureRoot_CreatingNewRoot()
        {
            this.WriteEvent(21);
        }

        [Event(22, Level = EventLevel.LogAlways, Version = 1)]
        public void EnsureRoot_CreateRootNode()
        {
            this.WriteEvent(22);
        }

        [Event(23, Level = EventLevel.LogAlways, Version = 1)]
        public void LoadTreeCompleted(long elapsedMilliseconds)
        {
            this.WriteEvent(23, elapsedMilliseconds);
        }

        [Event(24, Level = EventLevel.Verbose, Version = 2)]
        public void ProcessAdd_Started(ulong replicationId, ulong id)
        {
            this.WriteEvent(24, replicationId, id);
        }

        [Event(25, Level = EventLevel.Verbose, Version = 2)]
        public void ProcessAdd_RootNodeCreated(ulong replicationId, ulong id)
        {
            this.WriteEvent(25, replicationId, id);
        }

        [Event(26, Level = EventLevel.Informational, Version = 3)]
        public void ProcessAdd_Completed(ulong replicationId, ulong id, string name, ulong parentId, long czxid)
        {
            this.WriteEvent(26, replicationId, id, name, parentId, czxid);
        }

        [Event(27, Level = EventLevel.Error, Version = 2)]
        public void ProcessAdd_Failed(ulong replicationId, ulong id, string exception)
        {
            this.WriteEvent(27, replicationId, id, exception);
        }

        [Event(28, Level = EventLevel.Verbose, Version = 2)]
        public void ProcessUpdate_Started(ulong replicationId, ulong id)
        {
            this.WriteEvent(28, replicationId, id);
        }

        [Event(29, Level = EventLevel.Informational, Version = 3)]
        public void ProcessUpdate_Completed(ulong replicationId, ulong id, string name, ulong parentId, long mzxid, long pzxid)
        {
            this.WriteEvent(29, replicationId, id, name, parentId, mzxid, pzxid);
        }

        [Event(30, Level = EventLevel.Error, Version = 2)]
        public void ProcessUpdate_Failed(ulong replicationId, ulong id, string exception)
        {
            this.WriteEvent(30, replicationId, id, exception);
        }

        [Event(31, Level = EventLevel.Verbose, Version = 2)]
        public void ProcessRemove_Started(ulong replicationId, ulong id)
        {
            this.WriteEvent(31, replicationId, id);
        }

        [Event(32, Level = EventLevel.Error, Version = 2)]
        public void ProcessRemove_NodeHasChildren(ulong replicationId, ulong id, int childrenCount)
        {
            this.WriteEvent(32, replicationId, id, childrenCount);
        }

        [Event(33, Level = EventLevel.Informational, Version = 2)]
        public void ProcessRemove_Completed(ulong replicationId, ulong id, string name)
        {
            this.WriteEvent(33, replicationId, id, name);
        }

        [Event(34, Level = EventLevel.Error, Version = 2)]
        public void ProcessRemove_Failed(ulong replicationId, ulong id, string exception)
        {
            this.WriteEvent(34, replicationId, id, exception);
        }

        [Event(35, Level = EventLevel.LogAlways, Version = 1)]
        public void LoadTreeStarted()
        {
            this.WriteEvent(35);
        }

        [Event(36, Level = EventLevel.Verbose, Version = 1)]
        public void CreateNew(ulong id)
        {
            this.WriteEvent(36, id);
        }

        [Event(37, Level = EventLevel.LogAlways, Version = 1)]
        public void GetAllItems(ulong count)
        {
            this.WriteEvent(37, count);
        }

        [Event(38, Level = EventLevel.Verbose, Version = 1)]
        public void TryGetValue_Found(ulong id, string name, ulong parentId, int childrenCount)
        {
            this.WriteEvent(38, id, name, parentId, childrenCount);
        }

        [Event(39, Level = EventLevel.LogAlways, Version = 1)]
        public void ProcessNewRoot(ulong id)
        {
            this.WriteEvent(39, id);
        }

        [Event(40, Level = EventLevel.Verbose, Version = 1)]
        public void PersistedDataAppendAddChild_SkippingEphemeralNode(ulong id, string name, ulong childId, string childName, int childrenCount)
        {
            this.WriteEvent(40, id, name, childId, childName, childrenCount);
        }

        [Event(41, Level = EventLevel.Verbose, Version = 2)]
        public void ProcessAdd_ConnectWithParent(ulong replicationId, ulong id, string name, ulong parentId, string parentName, int childrenCount)
        {
            this.WriteEvent(41, replicationId, id, name, parentId, parentName, childrenCount);
        }

        [Event(42, Level = EventLevel.Error, Version = 3)]
        public void ProcessAdd_ParentNotFound(ulong replicationId, ulong id, string name, ulong parentId)
        {
            this.WriteEvent(42, replicationId, id, name, parentId);
        }

        [Event(43, Level = EventLevel.Verbose, Version = 2)]
        public void ProcessAdd_NodeIsParentOfDanglingSiblings(ulong replicationId, ulong id, string name, int siblingCount)
        {
            this.WriteEvent(43, replicationId, id, name, siblingCount);
        }

        [Event(44, Level = EventLevel.Verbose, Version = 1)]
        public void ProcessAdd_InstanceIsPrimary(ulong id, string name)
        {
            this.WriteEvent(44, id, name);
        }

        [Event(45, Level = EventLevel.Verbose, Version = 2)]
        public void ProcessAdd_NodeHasNoParent(ulong replicationId, ulong id, string name)
        {
            this.WriteEvent(45, replicationId, id, name);
        }

        [Event(46, Level = EventLevel.Verbose, Version = 1)]
        public void TryGetValue_NotFound(ulong id)
        {
            this.WriteEvent(46, id);
        }

        [Event(47, Level = EventLevel.Informational, Version = 4)]
        public void CommitAdd(ulong changeListId, ulong replicationId, ulong id, string name, ulong parentId)
        {
            this.WriteEvent(47, changeListId, replicationId, id, name, parentId);
        }

        [Event(48, Level = EventLevel.Informational, Version = 4)]
        public void CommitRemove(ulong changeListId, ulong replicationId, ulong id, string name, ulong parentId)
        {
            this.WriteEvent(48, changeListId, replicationId, id, name, parentId);
        }

        [Event(49, Level = EventLevel.Error, Version = 2)]
        public void CommitRemoveFailed_DataMismatch(ulong changeListId,  ulong replicationId, ulong id, string name)
        {
            this.WriteEvent(49, changeListId, replicationId, id, name);
        }

        [Event(50, Level = EventLevel.Error, Version = 2)]
        public void CommitRemoveFailed_DataNotFound(ulong changeListId, ulong replicationId, ulong id, string name)
        {
            this.WriteEvent(50, changeListId, replicationId, id, name);
        }

        [Event(51, Level = EventLevel.Error, Version = 1)]
        public void ProcessUpdate_ParentChanged(ulong id, string name, ulong existingParentId, ulong newParentId)
        {
            this.WriteEvent(51, id, name, existingParentId, newParentId);
        }

        [Event(52, Level = EventLevel.Informational, Version = 3)]
        public void ProcessUpdate_RemoveFromExistingParent(ulong replicationId, ulong id, string name, ulong parentId)
        {
            this.WriteEvent(52, replicationId, id, name, parentId);
        }

        [Event(53, Level = EventLevel.Informational, Version = 3)]
        public void ProcessUpdate_AddToNewParent(ulong replicationId, ulong id, string name, ulong parentId)
        {
            this.WriteEvent(53, replicationId, id, name, parentId);
        }

        [Event(54, Level = EventLevel.Verbose, Version = 1)]
        public void ProcessUpdate_KeepSameParent(ulong id, string name, ulong parentId)
        {
            this.WriteEvent(54, id, name, parentId);
        }

        [Event(55, Level = EventLevel.LogAlways, Version = 1)]
        public void ChangeListAbort(ulong id)
        {
            this.WriteEvent(55, id);
        }

        [Event(56, Level = EventLevel.Informational, Version = 2)]
        public void RecordAddition(ulong changeListId, ulong id, string name, long czxid)
        {
            this.WriteEvent(56, changeListId, id, name, czxid);
        }

        [Event(57, Level = EventLevel.Informational, Version = 2)]
        public void RecordUpdate(ulong changeListId, ulong id, string name, long mzxid, long pzxid)
        {
            this.WriteEvent(57, changeListId, id, name, mzxid, pzxid);
        }

        [Event(58, Level = EventLevel.Informational, Version = 1)]
        public void RecordRemoval(ulong changeListId, ulong id, string name)
        {
            this.WriteEvent(58, changeListId, id, name);
        }

        [Event(59, Level = EventLevel.Informational, Version = 1)]
        public void CommitChangeList(ulong changeListId, long xid)
        {
            this.WriteEvent(59, changeListId, xid);
        }

        [Event(60, Level = EventLevel.Warning, Version = 2)]
        public void ProcessRemove_ParentNotFound(ulong replicationId, ulong id, string name, ulong parentId)
        {
            this.WriteEvent(60, replicationId, id, name, parentId);
        }

        [Event(61, Level = EventLevel.LogAlways, Version = 1)]
        public void PrepareForRebuild()
        {
            this.WriteEvent(61);
        }

        [Event(62, Level = EventLevel.Warning, Version = 2)]
        public void ProcessRemove_ParentDoesNotHaveThisNodeAsChild(ulong replicationId, ulong id, string name, ulong parentId)
        {
            this.WriteEvent(62, replicationId, id, name, parentId);
        }

        [Event(63, Level = EventLevel.Informational, Version = 2)]
        public void Commit_Succeeded(ulong changeListId, ulong replicationId, int changeCount, long elapsedMilliseconds)
        {
            this.WriteEvent(63, changeListId, replicationId, changeCount, elapsedMilliseconds);
        }

        [Event(64, Level = EventLevel.Error, Version = 1)]
        public void Commit_Failed(ulong changeListId, int changeCount, long elapsedMilliseconds, string exception)
        {
            this.WriteEvent(64, changeListId, changeCount, elapsedMilliseconds, exception);
        }

        [Event(65, Level = EventLevel.Informational, Version = 1)]
        public void Commit_Started(ulong changeListId, int changeCount)
        {
            this.WriteEvent(65, changeListId, changeCount);
        }

        [Event(66, Level = EventLevel.Error, Version = 2)]
        public void ProcessAdd_ParentHasAChildWithSameName(ulong replicationId, ulong id, string name, ulong parentId, ulong existingChildId)
        {
            this.WriteEvent(66, replicationId, id, name, parentId, existingChildId);
        }

        [Event(67, Level = EventLevel.Error, Version = 2)]
        public void ProcessAdd_AlreadyExists(ulong replicationId, ulong id, string name, ulong parentId)
        {
            this.WriteEvent(67, replicationId, id, name, parentId);
        }

        [Event(68, Level = EventLevel.Error, Version = 2)]
        public void CommitAdd_AlreadyExists(ulong changeListId,  ulong replicationId, ulong id, string name, ulong parentId)
        {
            this.WriteEvent(68, changeListId, replicationId, id, name, parentId);
        }

        [Event(69, Level = EventLevel.Informational, Version = 4)]
        public void CommitUpdate(ulong changeListId, ulong replicationId, ulong id, string name, ulong parentId)
        {
            this.WriteEvent(69, changeListId, replicationId, id, name, parentId);
        }

        [Event(70, Level = EventLevel.Informational, Version = 1)]
        public void CommitChangeListSync(ulong changeListId, long xid)
        {
            this.WriteEvent(70, changeListId, xid);
        }

        [Event(71, Level = EventLevel.Informational, Version = 1)]
        public void CommitChangeListSyncCompleted(ulong changeListId, long xid, long elapsedMilliseconds)
        {
            this.WriteEvent(71, changeListId, xid, elapsedMilliseconds);
        }

        [Event(72, Level = EventLevel.Error, Version = 1)]
        public void CommitChangeListSyncFailed(ulong changeListId, long xid, string exception)
        {
            this.WriteEvent(72, changeListId, xid, exception);
        }

        [Event(73, Level = EventLevel.Verbose, Version = 1)]
        public void ProcessLoad_Started(ulong id)
        {
            this.WriteEvent(73, id);
        }

        [Event(74, Level = EventLevel.Error, Version = 1)]
        public void ProcessLoad_AlreadyExists(ulong id, string name, ulong parentId)
        {
            this.WriteEvent(74, id, name, parentId);
        }

        [Event(75, Level = EventLevel.Informational, Version = 2)]
        public void ProcessLoad_Completed(ulong id, ulong parentId, long czxid, long mzxid, long pzxid, int version, int cversion, int aversion, int numChildren)
        {
            this.WriteEvent(75, id, parentId, czxid, mzxid, pzxid, version, cversion, aversion, numChildren);
        }

        [Event(76, Level = EventLevel.Error, Version = 1)]
        public void ProcessLoad_Failed(ulong id, string exception)
        {
            this.WriteEvent(76, id, exception);
        }

        [Event(77, Level = EventLevel.LogAlways, Version = 2)]
        public void ProcessLoad_RootNodeLoaded(ulong id)
        {
            this.WriteEvent(77, id);
        }

        [Event(78, Level = EventLevel.Verbose, Version = 1)]
        public void CompleteRebuild_ConnectWithParent(ulong id, string name, ulong parentId, string parentName, int childrenCount)
        {
            this.WriteEvent(78, id, name, parentId, parentName, childrenCount);
        }

        [Event(79, Level = EventLevel.Error, Version = 1)]
        public void CompleteRebuild_ParentHasAChildWithSameName(ulong id, string name, ulong parentId, ulong existingChildId)
        {
            this.WriteEvent(79, id, name, parentId, existingChildId);
        }

        [Event(80, Level = EventLevel.Error, Version = 1)]
        public void CompleteRebuild_ParentNotFound(ulong id, string name, ulong parentId)
        {
            this.WriteEvent(80, id, name, parentId);
        }

        [Event(81, Level = EventLevel.Error, Version = 1)]
        public void CompleteRebuild_NodeHasNoParent(ulong id, string name)
        {
            this.WriteEvent(81, id, name);
        }

        [Event(82, Level = EventLevel.LogAlways, Version = 1)]
        public void CompleteRebuild_Started(int dataCount)
        {
            this.WriteEvent(82, dataCount);
        }

        [Event(83, Level = EventLevel.LogAlways, Version = 2)]
        public void CompleteRebuild_Finished(int dataCount, int duplicateCount, int orphanCount, long elapsedMilliseconds)
        {
            this.WriteEvent(83, dataCount, duplicateCount, orphanCount, elapsedMilliseconds);
        }

        [Event(84, Level = EventLevel.Error, Version = 1)]
        public void LoadTree_RootIsNull()
        {
            this.WriteEvent(84);
        }

        [Event(85, Level = EventLevel.Error, Version = 1)]
        public void LoadTree_RootNodeNotCreated()
        {
            this.WriteEvent(85);
        }

        [Event(86, Level = EventLevel.LogAlways, Version = 1)]
        public void LoadTree_RootFound(long czxid, long mzxid, long pzxid, int version, int cversion, int aversion, int numChildren)
        {
            this.WriteEvent(86, czxid, mzxid, pzxid, version, cversion, aversion, numChildren);
        }

        [Event(87, Level = EventLevel.Error, Version = 1)]
        public void LoadTree_Failed(string exception)
        {
            this.WriteEvent(87, exception);
        }

        [Event(88, Level = EventLevel.Error, Version = 1)]
        public void CompleteRebuild_Failed(string exception)
        {
            this.WriteEvent(88, exception);
        }

        [Event(89, Level = EventLevel.LogAlways, Version = 1)]
        public void LoadTree_WaitingForData(long elapsedMilliseconds)
        {
            this.WriteEvent(89, elapsedMilliseconds);
        }

        [Event(90, Level = EventLevel.LogAlways, Version = 1)]
        public void LoadTree_CreatingNewRoot()
        {
            this.WriteEvent(90);
        }

        [Event(92, Level = EventLevel.Error, Version = 1)]
        public void CompleteRebuild_DuplicateFound(ulong id, long czxid, long mzxid, long pzxid, int version, int cversion, int aversion, int numChildren)
        {
            this.WriteEvent(92, id, czxid, mzxid, pzxid, version, cversion, aversion, numChildren);
        }

        [Event(93, Level = EventLevel.Error, Version = 1)]
        public void CompleteRebuild_OrphanFound(ulong id, long czxid, long mzxid, long pzxid, int version, int cversion, int aversion, int numChildren)
        {
            this.WriteEvent(93, id, czxid, mzxid, pzxid, version, cversion, aversion, numChildren);
        }

        [Event(94, Level = EventLevel.LogAlways, Version = 1)]
        public void ProcessAdd_RootAdded(ulong replicationId, ulong id, string name)
        {
            this.WriteEvent(94, replicationId, id, name);
        }

        [Event(95, Level = EventLevel.Warning, Version = 1)]
        public void ReplicationWaitForTooLong(long durationInMs)
        {
            this.WriteEvent(95, durationInMs);
        }

        [Event(96, Level = EventLevel.Informational, Version = 1)]
        public void GroupCommit_Started(ulong firstChangeListId, ulong lastChangeListId, int changeCount)
        {
            this.WriteEvent(96, firstChangeListId, lastChangeListId, changeCount);
        }

        [Event(97, Level = EventLevel.Informational, Version = 1)]
        public void GroupCommit_Succeeded(ulong firstChangeListId, ulong lastChangeListId, ulong replicationId, int changeCount, long elapsedMilliseconds)
        {
            this.WriteEvent(97, firstChangeListId, lastChangeListId, replicationId, changeCount, elapsedMilliseconds);
        }

        [Event(98, Level = EventLevel.Error, Version = 1)]
        public void GroupCommit_Failed(ulong firstChangeListId, ulong lastChangeListId, int changeCount, long elapsedMilliseconds, string exception)
        {
            this.WriteEvent(98, firstChangeListId, lastChangeListId, changeCount, elapsedMilliseconds, exception);
        }
    }
}