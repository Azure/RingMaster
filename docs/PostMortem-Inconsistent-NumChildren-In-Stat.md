# Post Mortem: NumChildren in Stat is Inconsistent with Actual Number of Children

This note analyzes the bug that causes the `NumChildren` property in `Stat` is inconsistent with the actual number of
children for a given node. This caused a few Sev-2 incidents in multiple regions because NSM (Network Service Manager)
considered the subtree as empty based on returned Stat object from `Exists` request, and subsequently removed the entire
subtree (which made VNET non-operatable).

## Incidents

The following incidents are associated with this post-mortem:

* [IcM:70016265 VNet [Connectivity/Configuration]: Customer is unable to connect VMs RDBMS1WDVPR003 (10.248.98.6) to
  10.248.0.6, 10.248.0.8 . But can connect 10.248.0.4 and 10.248.0.21 through Global VNet
  Peering.](https://icm.ad.msft.net/imp/v3/incidents/details/70016265/home)

In *CA-PA mapping* scenario, multiple NSM in a region publish to the following sub-tree:

    /MadariUserData-{hash}/vnets-{GUID}     --> context selector in PubSub
        |
        +-- /mappings                       --> relative path in PubSub
                |
                +-- lnms                    --> NumChildren inconsistency happens here!
                |      |
                |      +-- cluster_A
                |      +-- cluster_B
                |      +-- cluster_...
                |
                +-- v4ca
                       |
                       +-- CA-PA mapping 1
                       +-- CA-PA mapping 2
                       +-- CA-PA mapping ...

"/mappings/v4ca" subtree is published by NSM running in multiple clusters. To track the ownership of mappings, each NSM
publisher will write its identity in "/mappings/lnms" when it owns some CA-PA mappings in the VNET, and delete its
identity when it no longer has any mappings.  To clean up the subtree after use, the last NSM that removes the identity
under "lnms" will remove the entire VNET.

The logic in NSM is that it sends `Exists` request to PubSub, gets `Stat` object and checks the `NumChildren` property.
If it is zero, NSM assumes no one owns any mappings, and it will send `Delete` request to *recursively* delete the
entire tree.

With respect to the incident, customer reported some anomalies in VNET operation, and NSM on-call found that the VNET
was removed per above logic, PubSub found that recursive delete operation actually had children nodes being deleted,
which means NumChildren property in Stat was zero and inconsistent with the actual children count. Further investigation
confirmed this situation, and more cases of inconsistency were seen, including NumChildren being zero or even negative.

## Stat of a Node

*Stat* is the metadata of a node and it provides statistics for the node. It contains the following properties:

* Czxid: ZXID (transaction ID, will explain later) when the node is created.
* Ctime: Win32 file system time (UTC in 64-bit integer) when the node is created.
* Mzxid: ZXID when the node *data* is most recently modified.
* Mtime: Win32 file system time (UTC in 64-bit integer) when the node data is most recently modified.
* Pzxid: ZXID when the node's children is most recently modified.
* Version: Version of the node data, 32-bit auto-incrementing integer.
* Cversion: Version of the node children list, 32-bit auto-incrementing integer.
* Aversion: Version of the node ACL (access control list), 32-bit auto-incrementing integer.
* DataLength: Length of data as a byte sequence.
* NumChildren: Number of children of this node, including both persisted node and ephemeral node.

ZXID stands for *ZooKeeper transaction ID*. It is a monotocally incrementing 64-bit integer maintained by the backend
and uniquely identify a transaction in the backend.  Note that changes to all nodes in a *Multi* request are in a single
transaction and thus have one ZXID.

In the backend, a `FirstStat` object is associated to a node when it is created. This stat is a simplified version to
optimize the storage space. The assumption is that many nodes may be created once and never get modified, in other words
no change to data or ACL, no children either. As soon as the assumption is violated, `FirstStat` is replaced with
`MutableStat` which is a complete version of stat object.

## Storage of Node and Stat

In the backend, the *root* of the in-memory tree of nodes is stored in `RingMasterBackendCore` class. From this single
root, one can traverse the entire tree to reach all nodes.  To optimize the storage space, two kinds of `Node` are
defined:

* Node: a.k.a. plain node object. It has a direct link to `PersistedData` object, but no children or watchers.
* CompleteNode: it inherits Node and has a list of watchers and mapping of Children (Name to PersistedData).

When a node is freshly created, it is a plain node. As soon as any watcher or child is added, it is promoted to complete
node. Similarly, once all watchers or children are removed, a complete node will be demoted to a plain node.

*PersistedData* object is the actual node with data and it is what is persisted into storage. It contains:

* Flag to indicate whether it is ephemeral (will go away once client session is terminated) or persisted (will not go
  away even the primary fails over).
* ID: 64-bit integer to uniquely identify the node.
* Pointer to the parent node (will not be persisted) and ID of the parent node (will be persisted).
* Pointer to the node object in the in-memory tree (will not be persisted).
* Name.
* Data as a byte sequence.
* Stat object.
* List of ACL.

So *Stat* is metadata and stored along with the data of nodes.

In Service Fabric persistence, PersistedData is stored in `WinFabPersistence.PersistedData` object in a reliable
dictionary named as `dataById` with the ID as the key.  In `AbstractPersistedDataFactory` class, the mapping from node
ID to node PersistedData is stored in a concurrent dictionary named as `dataById`.

## Maintenance of Children Count

The bookkeeping of the children count of a persisted node is performed in three places:

* NumChildren property in Stat: the value also contains the ephemeral nodes.
* ChildrenMapping of PersistedData: as the count property of dictionary object.
* ChildrenCount of PersistedData.

During request processing in the backend core, NumChildren in Stat is incremented or decremented if a child is added or
removed from the node (see `Updatestat` method).  The child addition/removal may cause persisted data addition/removal,
then ChildrenCount property is incremented / decremented accordingly (see `AddChild` and `RemoveChild` methods).

When persisted data object is serialized to byte sequence, NumChildren in Stat is replaced by ChildrenCount because only
the latter contains true number of *persisted* children (i.e. not ephemeral nodes).  When byte sequence is deserialized
to persisted data object, previously saved ChildrenCount is used as the initial value of NumChildren in Stat, but
ChildrenCount is initialized to 0. The reason of zero initialization value is described as follows.

When active secondary feature is disabled, active secondaries may have replicated data deserialized to persisted data
object that are stored in reliable dictionary, but the data is unused. When an active secondary is promoted to primary,
all persisted data object in reliable dictionary are read and added to concurrent dictionary object in
AbstractPersistedDataFactory. Subsequently all nodes are *loaded* meaning they are connected to their parents based on
the ParentId property.  Then the in-memory tree is ready to use. When a node is connected to its parent, it is added to
the children mapping of parent node, and ChildrenCount is incremented.  Once all nodes are fully loaded, ChildrenCount
property should represent the actual number of children.

When active secondary feature is enabled, active secondaries have up-to-date in-memory tree which is kept in sync with
primary by using the reliable collection notification mechanism.  When a reliable dictionary is rebuilt, read and reload
of all nodes are carried out similarly as above. Later when there is any change in persisted data, secondaries will be
notified and children nodes will be added or removed to their parents, and ChildrenCount is maintained accordingly.

Thus we can see the children count is mostly accurate, but there are times it does not reflect the true number of
children, for instance before the nodes are connected to their parents.

It is important to note that serialization of persisted data not only happens when a primary starts to replicate the
changes.  It also happens when a secondary performs the checkpoint so the replication log does not grow indefinitely.
The latter is where the aforementioned bookkeeping fails silently.

## Bugs Identified and Fix

In order to reproduce the incident to facilitate the bug investigation, an in-memory stress program and a local Service
Fabric stress program are developed.  During the stress exercise, three bugs have been identified:

* When the backend receives a Multi request that removes all children and accesses a non-existing node (which fails the
  operation on purpose), demotion of CompleteNode to plain node has a race condition to cause children count being
  incorrect.

* When the backend processes child node create request, it correctly replicates both child node create and parent node
  update to secondaries. But for node delete request, only child node removal is replicated. This means when the most
  recent operation is delete child, then the primary fails over, and deletion has not been checkpointed yet, the new
  primary will see the right number of children count, but the Stat object will be the one before delete operation after
  the transaction log replay.

* When the checkpoint is started on a secondary and for the nodes that are not connected to parents (this is the case
  for active secondary disabled deployment), the children count will be zero and written to Stat.NumChildren during
  serialization. Later when the secondary is promoted to primary, it looks like the NumChildren is reset to 0.

The fix of the first problem is to eliminate the complete node demotion.

The fix of the second problem is to replicate the parent node update when a node is removed.

The fix of the third problem is as follows:

* ChildrenCount in PersistedData is removed. It is no longer maintained.

* NumEphemeralChildren is added to MutableStat object to keep track of ephemeral children count. Backend will increment
  or decrement it in UpdateStat in addition to NumChildren update.

* When persisted data is persisted, difference between NumChildren and NumEphemeralChildren is serialized. In other
  words, only persisted children count matters. This number is accurate even before nodes are connected to their
  parents.

With these code changes, correctness check has no failure in the 10-hour stress with 30-second internal of primary
failover.

## Data Collection

To get all succeeded requests on a node, one can search *RingMasterEvents* table in *SdnPubSub* namespace by TaskName of
"ProcessMessageSucceeded".

To get the change history of a node, one can search *RingMasterPersistence* table using the node ID and Mzxid.

