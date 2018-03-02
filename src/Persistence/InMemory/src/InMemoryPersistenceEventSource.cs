// <copyright file="InMemoryPersistenceEventSource.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.InMemory
{
    using System.Diagnostics;
    using System.Diagnostics.Tracing;

    [EventSource(Name = "Microsoft-Azure-Networking-Infrastructure-RingMaster-Persistence-InMemory")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "This is an EventSource and methods map to trace messages")]
    internal sealed class InMemoryPersistenceEventSource : EventSource
    {
        public InMemoryPersistenceEventSource()
        {
        }

        public static InMemoryPersistenceEventSource Log { get; } = new InMemoryPersistenceEventSource();

        [Event(2, Level = EventLevel.Verbose, Version = 1)]
        public void PersistedData_OnCreate(ulong transactionId, ulong id, string name)
        {
            this.WriteEvent(2, transactionId, id, name);
        }

        [Event(3, Level = EventLevel.Verbose, Version = 1)]
        public void PersistedData_OnUpdate(ulong transactionId, ulong id, string name)
        {
            this.WriteEvent(3, transactionId, id, name);
        }

        [Event(4, Level = EventLevel.Verbose, Version = 1)]
        public void PersistedData_OnRemove(ulong transactionId, ulong id, string name)
        {
            this.WriteEvent(4, transactionId, id, name);
        }

        [Event(5, Level = EventLevel.Verbose, Version = 1)]
        public void ChangeListCommit(ulong changeListId, long xid, int changeCount)
        {
            this.WriteEvent(5, changeListId, xid, changeCount);
        }

        [Event(6, Level = EventLevel.Verbose, Version = 1)]
        public void ChangeListAbort(ulong changeListId, int changeCount)
        {
            this.WriteEvent(6, changeListId, changeCount);
        }

        [Event(7, Level = EventLevel.LogAlways, Version = 1)]
        public void LoadFromStream()
        {
            this.WriteEvent(7);
        }

        [Event(8, Level = EventLevel.LogAlways, Version = 1)]
        public void SaveToStream()
        {
            this.WriteEvent(8);
        }

        [Event(9, Level = EventLevel.LogAlways, Version = 1)]
        public void CreateOrGetRoot_CreatingNewRoot()
        {
            this.WriteEvent(9);
        }

        [Event(10, Level = EventLevel.LogAlways, Version = 1)]
        public void CreateOrGetRoot_Completed(ulong rootId)
        {
            this.WriteEvent(10, rootId);
        }

        [Event(11, Level = EventLevel.LogAlways, Version = 1)]
        public void ApplyCreateRoot(ulong rootId)
        {
            this.WriteEvent(11, rootId);
        }

        [Event(12, Level = EventLevel.Verbose, Version = 1)]
        public void ApplyCreateAfterRoot(ulong id)
        {
            this.WriteEvent(12, id);
        }

        [Event(13, Level = EventLevel.LogAlways, Version = 1)]
        public void FinishFlatNodesLoading()
        {
            this.WriteEvent(13);
        }

        [Event(14, Level = EventLevel.LogAlways, Version = 1)]
        public void FinishNodesLoading(ulong rootId)
        {
            this.WriteEvent(14, rootId);
        }

        [Event(15, Level = EventLevel.LogAlways, Version = 1)]
        public void GetItemCount(int itemCount)
        {
            this.WriteEvent(15, itemCount);
        }

        [Event(16, Level = EventLevel.Verbose, Version = 1)]
        public void ReadFromStream(ulong id)
        {
            this.WriteEvent(16, id);
        }

        [Event(17, Level = EventLevel.LogAlways, Version = 1)]
        public void Reset()
        {
            this.WriteEvent(17);
        }

        [Event(18, Level = EventLevel.Verbose, Version = 1)]
        public void DeserializedChange(int changeType, ulong id, string name, int childrenCount)
        {
            this.WriteEvent(18, changeType, id, name, childrenCount);
        }

        [Event(19, Level = EventLevel.Verbose, Version = 1)]
        public void SerializedChange(int changeType, ulong id, string name, int childrenCount)
        {
            this.WriteEvent(19, changeType, id, name, childrenCount);
        }

        [Event(20, Level = EventLevel.Verbose, Version = 1)]
        public void ApplyChangeList(ulong changeListId)
        {
            this.WriteEvent(20, changeListId);
        }

        [Event(21, Level = EventLevel.Verbose, Version = 1)]
        public void ChangeListCommitCompletionNotification(ulong changeListId)
        {
            this.WriteEvent(21, changeListId);
        }

        [Event(22, Level = EventLevel.Verbose, Version = 1)]
        public void ProcessPendingChangelistsTaskCompleted()
        {
            this.WriteEvent(22);
        }

        [Event(23, Level = EventLevel.Error, Version = 1)]
        public void ProcessPendingChangelistsTaskFailed(string exception)
        {
            this.WriteEvent(23, exception);
        }

        [Event(24, Level = EventLevel.Verbose, Version = 1)]
        public void ApplyAddChildAfterLoad(ulong parentId, string parentName, ulong childId, string childName, int childrenCount)
        {
            this.WriteEvent(24, parentId, parentName, childId, childName, childrenCount);
        }

        [Event(25, Level = EventLevel.Verbose, Version = 1)]
        public void StorePersistedData(ulong id, string name, ulong parentId, string parentName, int childrenCount)
        {
            this.WriteEvent(25, id, name, parentId, parentName, childrenCount);
        }

        [Event(26, Level = EventLevel.Verbose, Version = 1)]
        public void LoadPersistedData(ulong id, string name, ulong parentId, string parentName, int childrenCount)
        {
            this.WriteEvent(26, id, name, parentId, parentName, childrenCount);
        }

        [Event(27, Level = EventLevel.Verbose, Version = 1)]
        public void PersistedDataStateAfterChangeList(ulong changeListId, ulong id, string name, ulong parentId, string parentName, int childrenCount)
        {
            this.WriteEvent(27, changeListId, id, name, parentId, parentName, childrenCount);
        }

        [Event(28, Level = EventLevel.Verbose, Version = 1)]
        public void StartReplication(ulong changeListId, int changeCount)
        {
            this.WriteEvent(28, changeListId, changeCount);
        }

        [Event(29, Level = EventLevel.LogAlways, Version = 1)]
        public void OnDeactivate_WaitingForProcessPendingChangelistsTask()
        {
            this.WriteEvent(29);
        }

        [Event(30, Level = EventLevel.LogAlways, Version = 1)]
        public void OnDeactivate_Completed(long elapsedMilliseconds)
        {
            this.WriteEvent(30, elapsedMilliseconds);
        }
    }
}