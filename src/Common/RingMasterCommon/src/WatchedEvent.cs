// <copyright file="WatchedEvent.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;

    /// <summary>
    /// A watcher notification
    /// </summary>
    [Serializable]
    public class WatchedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchedEvent" /> class.
        /// </summary>
        /// <param name="eventType">Type of the event</param>
        /// <param name="keeperState">State of the keeper</param>
        /// <param name="path">The path of the node associated with this notification</param>
        /// <param name="data">Changed data of the node</param>
        /// <param name="stat">Stat of the node</param>
        /// <param name="childName">Name of the child.</param>
        /// <param name="childData">The child data.</param>
        /// <param name="childStat">The child stat.</param>
        /// <param name="userMetadata">The user metadata.</param>
        /// <param name="childUserMetadata">The child user metadata.</param>
        public WatchedEvent(
            WatchedEventType eventType,
            WatchedEventKeeperState keeperState,
            string path,
            byte[] data = null,
            Data.IStat stat = null,
            string childName = null,
            byte[] childData = null,
            Data.IStat childStat = null,
            byte[] userMetadata = null,
            byte[] childUserMetadata = null)
        {
            this.EventType = eventType;
            this.KeeperState = keeperState;
            this.Path = path;
            this.Data = data;
            this.Stat = stat;
            this.ChildName = childName;
            this.ChildData = childData;
            this.ChildStat = childStat;
            this.UserMetadata = userMetadata;
            this.ChildUserMetadata = childUserMetadata;
        }

        /// <summary>
        /// Type of event.
        /// </summary>
        public enum WatchedEventType
        {
            /// <summary>
            /// No notification.
            /// </summary>
            None = 0,

            /// <summary>
            /// The children of the watched node changed.
            /// </summary>
            NodeChildrenChanged,

            /// <summary>
            /// A new child has been created under the watched node.
            /// </summary>
            NodeCreated,

            /// <summary>
            /// The data of the watched node has changed.
            /// </summary>
            NodeDataChanged,

            /// <summary>
            /// The watched node has been deleted.
            /// </summary>
            NodeDeleted,

            /// <summary>
            /// Watcher set on the node has been removed.
            /// </summary>
            WatcherRemoved,

            /// <summary>
            /// The node data and user metadata changed
            /// </summary>
            NodeDataAndUserMetadataChanged,
        }

        /// <summary>
        /// Keeper state.
        /// </summary>
        public enum WatchedEventKeeperState
        {
            /// <summary>
            /// Keeper state is not known. (Deprecated)
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// Authentication failed.
            /// </summary>
            AuthFailed,

            /// <summary>
            /// The client is in the disconnected state.
            /// </summary>
            Disconnected,

            /// <summary>
            /// The serving cluster has terminated the session because it expired.
            /// </summary>
            Expired,

            /// <summary>
            /// No synchronize connected. (Deprecated)
            /// </summary>
            NoSyncConnected,

            /// <summary>
            /// The client is in the connected state.
            /// </summary>
            SyncConnected,
        }

        /// <summary>
        /// Gets the type of the event.
        /// </summary>
        /// <value>The type of the event.</value>
        public WatchedEventType EventType { get; private set; }

        /// <summary>
        /// Gets the state of the keeper.
        /// </summary>
        public WatchedEventKeeperState KeeperState { get; private set; }

        /// <summary>
        /// Gets the path of the node associated with this notification.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets the data of the node after the change
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Gets the user metadata.
        /// </summary>
        /// <value>
        /// The user metadata.
        /// </value>
        public byte[] UserMetadata { get; private set; }

        /// <summary>
        /// Gets the state after the change
        /// </summary>
        public Data.IStat Stat { get; private set; }

        /// <summary>
        /// Gets the name of the child.
        /// </summary>
        /// <value>
        /// The name of the child.
        /// </value>
        public string ChildName { get; private set; }

        /// <summary>
        /// Gets the child's stat.
        /// </summary>
        public Data.IStat ChildStat { get; private set; }

        /// <summary>
        /// Gets the child data.
        /// </summary>
        public byte[] ChildData { get; private set; }

        /// <summary>
        /// Gets the child user metadata.
        /// </summary>
        /// <value>
        /// The child user metadata.
        /// </value>
        public byte[] ChildUserMetadata { get; private set; }

        /// <summary>
        /// Determines whether the specified <see cref="object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            WatchedEvent other = obj as WatchedEvent;
            if (other == null)
            {
                return false;
            }

            if (other.EventType != this.EventType)
            {
                return false;
            }

            if (other.KeeperState != this.KeeperState)
            {
                return false;
            }

            return string.Equals(this.Path, other.Path);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = this.EventType.GetHashCode();
            hash ^= this.KeeperState.GetHashCode();
            if (this.Path != null)
            {
                hash ^= this.Path.GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// The ToString() implementation for the class
        /// </summary>
        /// <returns>A string representing the class</returns>
        public override string ToString()
        {
            return string.Format(
                "Event:{0} KeeperState:{1} Path:{2} Data:[{3}] Stat:{4} ChildName:{5} ChildData:({6}) ChildStat:{7} UserMetadata:[{8}] ChildUserMetadata:[{9}]",
                this.EventType,
                this.KeeperState,
                this.Path,
                this.Data?.Length,
                this.Stat?.ToString(),
                this.ChildName,
                this.ChildData?.Length,
                this.ChildStat?.ToString(),
                this.UserMetadata?.Length,
                this.ChildUserMetadata?.Length);
        }
    }
}