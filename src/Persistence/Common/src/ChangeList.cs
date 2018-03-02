﻿// <copyright file="ChangeList.cs" company="Microsoft">
//     Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;

    /// <summary>
    /// Records a list of changes that have happened to <see cref="IPersistedData"/>.
    /// </summary>
    public sealed class ChangeList : IChangeList
    {
        /// <summary>
        /// Pool of events for reusing in commiting change list
        /// </summary>
        private static ConcurrentStack<ManualResetEventSlim> eventPool = new ConcurrentStack<ManualResetEventSlim>();

        private readonly AbstractPersistedDataFactory factory;
        private long transactionTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeList"/> class.
        /// </summary>
        /// <param name="id">Unique Id</param>
        /// <param name="factory">The <see cref="AbstractPersistedDataFactory"/> associated with this change list</param>
        public ChangeList(ulong id, AbstractPersistedDataFactory factory)
        {
            this.Id = id;
            this.factory = factory;
        }

        /// <summary>
        /// Types of changes that can be recorded in this change list.
        /// </summary>
        public enum ChangeType
        {
            Add,
            Update,
            Remove
        }

        /// <summary>
        /// Gets the unique id of this change list.
        /// </summary>
        public ulong Id { get; private set; }

        /// <summary>
        /// Gets the list of changes included in this change list.
        /// </summary>
        public List<Change> Changes { get; private set; } = new List<Change>();

        public ChangeList Clone()
        {
            return new ChangeList(this.Id, this.factory)
            {
                Changes = this.Changes.Select(c => c.Clone()).ToList(),
            };
        }

        /// <summary>
        /// Sets the time associated with this changelist.
        /// </summary>
        /// <param name="txTime">Time stamp</param>
        public void SetTime(long txTime)
        {
            this.transactionTime = txTime;
        }

        /// <summary>
        /// Abort this change list.
        /// </summary>
        public void Abort()
        {
            PersistenceEventSource.Log.ChangeListAbort(this.Id);
        }

        /// <summary>
        /// Commit this change list locally, but do not wait for remote commit.
        /// </summary>
        /// <param name="xid">The id that must be assigned to this commit</param>
        /// <param name="task">async task to indicate the completion of change replication</param>
        public void Commit(long xid, out Task task)
        {
            PersistenceEventSource.Log.CommitChangeList(this.Id, xid);
            this.CommitSync(xid, null, out task);
        }

        /// <summary>
        /// Commit this change list and signal the given event when both the local and remote commits are done.
        /// </summary>
        /// <param name="xid">The id that must be assigned to this commit</param>
        /// <param name="completedEvent">Event to be signalled</param>
        /// <param name="commitTask">async task to indicate the completion of change replication</param>
        public void CommitSync(long xid, ManualResetEvent completedEvent, out Task commitTask)
        {
            var timer = Stopwatch.StartNew();
            if (completedEvent != null)
            {
                PersistenceEventSource.Log.CommitChangeListSync(this.Id, xid);
            }

            // Group multiple change list and commit together. Response to the client after the commit is
            // completed, i.e. majority in the replica set commit the change.
            // ManualResetEventSlim committedToPrimaryEvent = GetEvent();
            commitTask = this.factory.EnqueueAsync(this);

            if (completedEvent != null)
            {
                commitTask.ContinueWith(task =>
                {
                    if (!task.IsFaulted && !task.IsCanceled)
                    {
                        PersistenceEventSource.Log.CommitChangeListSyncCompleted(this.Id, xid, timer.ElapsedMilliseconds);
                        completedEvent.Set();
                    }
                    else
                    {
                        PersistenceEventSource.Log.CommitChangeListSyncFailed(this.Id, xid, task.Exception.ToString());
                    }
                });
            }
        }

        internal void RecordAdd(PersistedData data)
        {
            var change = new Change(ChangeType.Add, data);
            this.Changes.Add(change);
        }

        internal void RecordUpdate(PersistedData data)
        {
            var change = new Change(ChangeType.Update, data);
            this.Changes.Add(change);
        }

        internal void RecordRemove(PersistedData data)
        {
            var change = new Change(ChangeType.Remove, data);
            this.Changes.Add(change);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope",
            Justification = "event lifetime managed by the pool")]
        private static ManualResetEventSlim GetEvent()
        {
            ManualResetEventSlim evt;

            if (!eventPool.TryPop(out evt))
            {
                evt = new ManualResetEventSlim(false);
            }

            evt.Reset();

            return evt;
        }

        private static void ReturnEvent(ManualResetEventSlim evt)
        {
            eventPool.Push(evt);
        }

        public sealed class Change
        {
            public Change(ChangeType changeType, PersistedData data)
            {
                this.ChangeType = changeType;
                this.Data = data;
            }

            public ChangeType ChangeType { get; private set; }

            public PersistedData Data { get; private set; }

            public Change Clone()
            {
                return new Change(this.ChangeType, this.Data.Clone());
            }
        }
    }
}
