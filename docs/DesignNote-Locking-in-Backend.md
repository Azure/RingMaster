Design Note: Locking In Backend Request Processing
==================================================

This document describes the desired behavior of locking while processing the requests in the backend core. It details
the need of locking for concurrent requests processing, lock pool, and how locks are acquired and released during the
life time of the requests.

Single-Root In Memory Tree
--------------------------

Most, if not all, requests are retrieving information or manipulation information stored in a single-root tree of nodes
which are all stored in memory and backed by persistent storage. Each node has a unique ID which is represented by
monotically increasing 64-bit integer, a name in Unicode string (Surrogate supported), unstructured data in byte array,
and other metadata. The root node has an empty name, all other nodes must have non-empty names. The path of a given node
is defined by "/" joined string from root node to the specified node. Other than the root, each node has one and only
one parent node, which is referenced by node ID. Each node may have zero or any number of children nodes (subject to
certain technical limit). Under each parent node, the names of children are unique and lexically sortable.  Under
different nodes, names may have duplication as long as previous rule is not violated.

Requests to the backend may be read-only (RO) or read-write depending on whether they may make any change to the in
memory tree, for instance:

- GetNodeData "/a/b/c" is a RO operation.
- GetNodeChildren "/a/b/c" is a RO operation.
- SetNodeData "/a/b/c" is a RW operation, although the data may stay unchanged, metadata will be updated.
- CreateNode "/a/b/c/d" is a RW operation.

Requests and Concurrency Requirement
------------------------------------

Requests can be classified to two types:

- Simple requests that operate on a single path only. Note some operation may manipulate multiple nodes, for instance
  creation of non-existing path or recursive deletion.

- Complex requests that are composite of other simple or complex requests. For instance, *Batch* request is a group of
  sub-requests which may succeed or fail individually, *Multi* request is a group of sub-requests which must succeed or
  fail as a group (a.k.a. all or none).

In order to maximize the overall throughput, multiple requests are processed in parallel by multiple execution tasks /
threads while the basic ordering and consistency guarantee is not violated. Ordering means the execution of two requests
follows the same order as the order of receiving order in the TCP connection. Consistency means during the request
processing, the nodes along the path being requested are not altered by any other request so the response is obtained
from a snapshot of the in memory tree.

Consistency requirement implies the isolation between processing of RO and RW operations, which is why locking is
applied. Furthermore, single giant lock on the entire tree makes impossible to handle different RW operations even when
no conflict between them, more granular locking is therefore designed to maximize the concurrency.

Multi-level Readers-Writer Lock Pool
------------------------------------

The basic idea is to associate each node in the tree with a Readers-Writer lock. Each operation will acquire reader lock
on all nodes it reads, and writer locks on all nodes it may modify (either the data, ACL, or children).  The lock
properties can be referred to [Wikipedia definition](https://en.wikipedia.org/wiki/Readers%E2%80%93writer_lock). It
allows concurrent access for RO operations while write operations require exclusive access. Because writer-starvation is
unacceptable, write-preferring RW locks are used which is also what .NET implementation does.

The number of nodes stored in the backend is over 100 million in production. It is impractical to have a dedicated RW
lock for each node. Therefore, a lock pool is designed to cap the number of locks and also pre-allocate them for
efficiency. To determine which lock to use for a node, a Hash code is calculated from the given node (currently using
the node name in string), then take the modulus against the pool size. Note that once the pool size is fixed and hashing
is used, lock collision may happen where two distinct nodes map to the same lock.

The lock collision issue may have a substantial impact if it happens to nodes at different levels of the tree --
consider a CreateNode operation takes a write lock deep in the tree, but the lock is shared with the root. To prevent
this from happening, each level in the tree has its dedicated pool. Since the level of tree is unpredictable and may be
very large, the number of pools is fixed at 6 with default configuration as follows:

- 1 lock at top level.
- 50 locks at 2nd level.
- 2,500 at 3rd level.
- 10,000 at 4th level.
- 100,000 at 5th level.
- 500,000 for 6th level and beyond.

This configuration can be customized using app setting `RingMaster.LockSizesPerLevel`.

### Unpredictibility of lock collision

Questions may rise such as *why not use Node ID as the Hash?* This can be answered by lock collision prevention
strategy. Once collision happens, involved operations will be slowed down if writer lock is acquired because concurrent
entry is no longer possible. If clients may be able to predict how to generate collision from the input, the backend can
be DOS attacked. As a security measure, the following two approaches are taken into the design:

- String Hashing is salted, and the salt is randomly generated during primary initialization.
- Lock pool size at each level is configured to be a prime number.

Above work is pending implementation at the time of writing this note.

### Detecting and resolving lock collision

In the current multi-level lock pool design, the collision happens because nodes of different nodes at the same level
map to the same lock object. The collision becomes a real problem when the first node acquires a reader lock, and the
subsequent nodes attempt to acquire writer lock, which is considered as *upgrade* and is explicitly prohibited.  This
can be detected and fixed in the following way: after collecting all nodes to be locked, retrieve the list of lock
objects at each level, if there is any duplicate it is a collision and then upgrade the first entry to the highest lock
and discard the rest. An ephemeral Hash table is used to perform this operation.

Lock Upgrade and Deadlock
-------------------------

Processing of any operation starts with lock acquisition. In the current implementation, this is performed by
incremental acquire and upgrade. Specifically for simple requests, reader lock is acquired for the node on the path, and
upgrade to writer lock if needed; for a list of requests in a Multi, all paths are sorted, acquire reader lock, and then
ugprade if needed. For instance, SetNodeData on path "/a/b/c" will acquire reader lock on root, a, b, c, then upgrade
reader lock on c to writer lock.

This approach may lead to deadlock if multiple requests operating on overlapped sub-tree, because they may acquire all
reader locks at the same time, then start to upgrade. Since other threads are holding the reader lock, upgrade to writer
lock must wait, while no new reader may enter the lock.

In the actual implementation, lock acquisition is set to time out after a certain time (500 ms by default). The symptom
of "deadlock" is server operation timeout caused by inability to acquire lock.

In VNET CA-PA mapping scenario, a VNET may span across multiple clusters, then the mappings will be published by
multiple NSM as pub-sub clients simultaneously. NSM publishes mappings in Multi operations, and this causes frequent,
systematic, and long standing server operation timeout issues.

Elimination of Lock Upgrade
---------------------------

The new locking design is proposed as follows:

- RW lock abstraction `ILockObject` no longer supports lock upgrade. Any operation must acquire the right lock (or the
  highest level required) on the first attempt.

- In the low-probability collision event where different nodes in the same level share the same lock object, performance
  will be degraded but no special handling is required. In the extreme case, all nodes at a level may share a single
  reader-writer lock, and consistency can be ensured.

- It is straightforward to acquire the desired lock on the first attempt for simple operations. For Multi, the program
  will build a list of required locks by scanning the sub-operations, sort the list, then acquire the locks once from
  the list.

Examaple, for the following Multi:
- GetNodeData: /a/b/c/d
- GetNodeData: /a/b
- SetNodeData: /a/b/c

The list being built will be:
- (/, R), (a, R), (b, R), (c, R), (d, R)
- (/, R), (a, R), (b, R)
- (/, R), (a, R), (b, R), (c, W)

Sort will place Write before Read, the result  will be:
- (/, R), (a, R), (b, R)
- (/, R), (a, R), (b, R), (c, W)
- (/, R), (a, R), (b, R), (c, R), (d, R)

The lock acquisition order will be:
- Reader on /.
- Reader on a.
- Reader on b.
- Skip, skip, skip, Writer on c.
- Skip, skip, skip, skip, skip (existing writer lock detected while trying to acquire reader lock on the previous node,
  then all subsequent paths are skipped).

For concurrent operations working on overlapped sub-tree, this algorithm ensures that locking on the tree is performed
in order (top-down, left-right), and no upgrade is required.

*No backoff* -- if any lock acquisition times out, retry on that lock until the operation times out.  The alternative
approach is to release all locks that have been acquired, then re-acquire locks from the begining. This has fairness
issue and may lead to starvation of certain RW operations.

Implementation
--------------

The implmentation and testing are planned to complete by the end of February, 2018.
