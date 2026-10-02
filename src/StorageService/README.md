# Storage Service

A stateful service that exposes a small set of storage primitives over HTTP/2 + gRPC. The service is domain-agnostic — every component is generic and reusable. NRP is the first consumer, but no API in this service is NRP-specific.

The service is hosted on Service Fabric. Every RPC is served by the current primary replica; persisted state lives in Service Fabric Reliable Collections (IReliableDictionary3) and is replicated to secondaries before the handler returns.

## APIs

| API | Purpose |
|---|---|
| **Transacted Key-Value Storage** | Sorted-string keys, `byte[]` values, with transactional batch reads and writes. |
| **Lock Management** | Acquire / release locks scoped by an opaque owner ID. Used for cooperative mutual exclusion between callers. |
| **Persisted Counters** | Atomic increment / decrement of named counters. Designed for quota management. |
| **Persisted Bit-Array** | Sparse bit-allocation across a $2^256$ address space. Designed for inventory allocation — IP addresses, MAC addresses, GRE keys, VLAN ID, and similar ID pools. |
| **Persisted Queue** | At-least-once enqueue / dequeue with visibility timeouts. Designed for offline jobs and front-end-to-front-end synchronization. |
| **Ephemeral Cache** | Non-transactional temporary data and front-end-to-front-end synchronization. Backed by Service Fabric Reliable Collections (IReliableDictionary3). |

## Design tenets

- **Generic primitives, no domain coupling.** No component encodes NRP (or any other consumer's) schema. Callers map their domain onto the generic APIs.
- **OK + body, not status codes.** Application-level results (not found, lock denied, pool exhausted, stale receipt) are returned via response body fields. gRPC status codes are reserved for transport- and contract-level faults.
- **Additive proto only.** Field numbers are never reused or renumbered. New fields append at the next available number; the wire format carries no version prefix.
- **Failover-safe durability.** Committed writes to the persisted APIs survive primary failover. In-memory state (the lock table) is rebuilt or reset on promotion as appropriate.

## Layout

```
src/StorageService/
├── Protos/        # storage.proto — single source of truth for the wire contract
├── Service/       # Service Fabric stateful service host + gRPC handlers
└── Tests/         # unit and integration tests
```
