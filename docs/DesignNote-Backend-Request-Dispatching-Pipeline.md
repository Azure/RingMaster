# Design Note: Backend Request Dispatching Pipeline

## Request Dispatching Overview

The backend is hosted by a stateful service and communicates with client via network interface. It receives requests
from multiple clients, and sends back the responses. The basic principle is *total ordering* of request processing, in
other words for a particular client session, if message A is delivered before message B, then the processing of A at the
backend (including local commit and replication) should be no later than that of B. The communication channel between
backend and clients is typically based on TCP, and the client session is associated with a TCP connection.

Note that messages across different sessions have no ordering guarantee.

On the high level, the processing of a request message has the following steps:

1. Transport layer receives network packet as a form of byte array.
2. Deserialize byte array to strongly typed CLR request object.
3. Request is delivered to the backend core and gone through a sanity check.
4. Compute all reader/writer locks required on which nodes, collect them in a lock list, and acquire all locks.
5. Perform read or write operations on the in-memory tree, accumulate required changes in the lock list.
6. Commit the changes in the lock list locally, enqueue the changes to be replicated in the persisted data factory, and
   release all locks. This step is called *local commit*.
7. A dedicated task in the persisted data factory eventually dequeues the changes to be replicated, sends down to
   persistence layer (such as Service Fabric state manager), wait for completion of replication, and notifies the
   response is completed. This step is called *full replication*.
8. Response is ready to be sent back to client. The CLR object is serialized to byte array.
9. Transport layer sends the network packet back via the TCP connection.

For two requests received by the backend for "concurrent" processing, technically the only step to be synchronized are
step 4 to 6. As long as later request starts to acquire locks *after* lock acquisition of early request, and the
replication is the same order, the total ordering is ensured. For the simplicity and consistency, the maximum level of
concurrency is achieved when:

* Subsequent request starts to process when the local commit of previous request is done. Or the local commit is
  serialized.
* Full replication is overlapped, i.e. there may be many requests with local commit completed and waiting for
  replication so the response can be sent back.

Above are the basic principles of efficient request dispatching detailed in this design note.

Note that in the current design of backend core, the return of `ProcessMessage` indicates the completion of local
commit, the invocation of callback indicates the completion of full replication.

## Current Workflow in RingMasterService

**RingMasterService** is a stateful microservice hosted on Service Fabric. As a part of **RingMasterApplication**, it
provides conventional TCP message exchange based interface and ZooKeeper interface. `RingMasterService` class is the
SF stateful service implementation.

This service contains:

1. Instance of `RingMasterBackendCore` (a.k.a. backend), the primary request processing and business logic component.
2. Instance of `PersistedDataFactory` as the abstraction of the underlying persistence and consensus layer.
3. Instance of `RingMasterRequestExecutor` (a.k.a. executor), which dispatches the requests to the backend core.

In addition, it exposes the request dispatching via `TcpCommunicationListener` and `ZooKeeperTcpListener` interfaces.
When the service is started, it

1. Starts the executor by starting a given number of threads.
2. Starts the backend, which in turn activate the persisted data factory and load the persisted data to the in-memory
   tree.
3. Informs the backend that it becomes the primary.

When the service is stopped, it stops the backend and executor.

The communication listener in the service translates the requests over the network to the strongly typed request
objects.  The working horse under the hood is `SecureTransport` object, the TCP connection management component. When
the communication listener is opened, it creates an instance of `RingMasterServer`, registers the transport object, and
starts to listen on a TCP port.

### Client session on a new connection

When the TCP listener accepts a new connection in transport, it calls `RingMasterServer.OnNewConnection` for the
following:

1. Create a `Session` object in the context of `RingMasterServer`.
2. Register `OnPacketReceived` to process a received packet from the client.
3. Register `OnConnectionLost` to close the session.

After a connection is established, the first request is *Init*. Upon receiving this,
`TcpCommunicationListener.OnInitSession` is triggered to create a `CoreRequestHandler` from the executor, and this is
assigned as the request handler in the session. Backend core `ClientSession` is also created to associate with the
request handler, which is required on every backend request processing.

### Process received requests from network

When a new packet is received, transport callback will queue an async task to run on the ThreadPool, where the task will
call async method `Session.OnPacketReceived`. Now the request context is within a session, or a particular TCP
connection from a client to the backend.  In this context, the packet of byte array is deserialized to strongly typed
`RequestCall` object (call ID plus request object). The request will be processed and the response will be serialized
from `RequestResponse` object to byte array, and sends the response back to the client via transport `Send` method
(current implementation is to start the async send and throw away the Task object).

The request processing in `Session` is delegated to the per-session request handler, which is assigned when the `Init`
request is processed.  In `CoreRequestHandler` the RM request is wrapped into a backend request, a new call ID is
assigned (different from the previous client assigned call ID), the request is processed by the executor, on the
executor callback for `ProcessMessage`, call ID is set in the response, and this async task is marked as completed.

After the task is completed, the call ID is replaced with original client provided call ID, and the response is
serialized and sent back to the client.

### Request executor

`RingMasterRequestExecutor` is a producer-consumer component where many sessions *produce* the requests, which are
*consumed* by pre-defined number of threads for efficient processing. In reality, a single concurrent queue often causes
performance degradation under heavy lock contention. When the executor is started, a fixed number of thread are created
(currently set to twice of processor count available to CLR). When a request message arrives, it is simply enqueued into
the queue of `BlockingCollection` of the executor. On return, there is no indication either local commit or replication
is completed (in fact, neither are started in most cases). When the executor is stopped, the queue is marked as
completed so no further request can be queued, then all threads are cancelled and joined. In each worker thread, the
request is dequeued and passed to backend core if it is not timed out and no cancellation is requested yet.

### Packet receiving in transport

Note that the behavior is specifically for `SecureTransport`, not `SecureTransportSlim`. The latter has different
internal logic.

When a new connection is established, a long-running async task is started. A `BufferedStream` is created out of network
stream (associated with socket object), and packet length (DWORD) and payload are read from the stream to form a packet.
If the packet is null, the connection is lost, otherwise the callback `OnPacketReceived` is invoked. The callback is
expected to not throw, any exception will cause connection lost.

It is important to note that the receive packet task is not running on the I/O completion port thread. Blocking the task
for a short period of time may cause the buffer to be full but it will not directly stall the network packet receiving
from the underlying stock.

### Request dispatching call stack

The following diagram illustrates the async task and calls for dispatching a request.

```
Connection.OnPacketReceived
|
+--> Task.Run
     |
     +--> await Session.OnPacketReceived
          |
          +--> await Session.ProcessRequest
          |    |
          |    +--> await CoreRequestHandler.Request
          |         |
          |         +--> await RingMasterRequestExecutor.ProcessMessage (via TaskCompletionSource)
          |         |    |
          |         |    +--> Add to BlockingCollection
          |         |    |... scheduling ...
          |         |    +--> Dequeue from BlockingCollection
          |         |    +--> RingMasterBackendCore.ProcessMessage (via callback)
          |         |         |
          |         |         |... some processing, lock acquisition, etc. ...
          |         |         |
          |         |         +--> Local commit, release locks
          |         |         +--> Enqueue change list for replication.
          |         |         |
          |         |         |... scheduling ...
          |         |         |
          |         |         +--> Dequeue and replicate in the persistence.
          |         |         +--> Invoke callback in RingMasterBackendCore.ProcessMessage
          |         +--> RingMasterRequestExecutor.ProcessMessage is marked as completed
          +--> Session.ProcessRequest async task is completed
          +--> Connection.Send
```

## Streamlined Request Dispatching

Performance benchmark and profiling data show that major slowdown is caused by the heavy overhead in the queueing and
scheduling in `RingMasterRequestExecutor` as of Vega master 2.1.0.9. In the proposed workflow, the below principles are
followed to increase the overall throughput of the request processing:

* Eliminate redundant computation.
* Only async in two conditions: delay or wait is expected and it is caused by resource or I/O; the task cannot block the
  current thread for too long(e.g. I/O completion port thread).
* Leverage the TCP flow control in the communication channel to regulate the incoming request rate.
* Noisy neighbors should have minimal impact to the dispatching in other client sessions. 

In the entire processing of a network packet, there are following places where the current thread may await:

1. Wait for local commit of the last request.
2. Wait for lock acquisition.
3. Wait for full replication.
4. Wait for response being transmitting to client.

The last wait is out of the processing pipeline. The first wait is localized to a specific client session since it
reflects how quickly a client may send requests, so it can be used as back pressure in the communication channel. The
second wait is affected by processing in other sessions, so that may be scheduled as async task. The third wait (along
with the last) can be a part of backend `ProcessMessage` callback. The number of inflight operations can be factored
into the first wait.

The proposed workflow is:

```
Connection.OnPacketReceived
|
+--> Session.OnPacketReceived
     |
     +--> wait for per-session throttling (via SemaphoreSlim)
     +--> wait for last local commit (via AutoResetEvent)
     +--> Deserialize request to CLR object.
     +--> Session.ProcessRequest with callback (move onto the next packet after this)
          |
          +--> await CoreRequestHandler.Request with callback
               |
               +--> wrap to backend RequestCall
               +--> RingMasterBackendCore.ProcessMessage with callback
               +--> signal local commit completion.

Callback
|
+--> If replication is succeeded
|    |
|    +--> Replace call ID with client provided one
+--> If replication is failed
|    |
|    +--> Create a RequestResponse object with SystemError and client provided call ID
|
+--> Serialize response object to byte array
+--> Connection.Send
+--> signal per-session throttling semaphore
```

With the new workflow, total ordering, correctness, and fairness can be ensured with high throughput.

## Implementation Notes

In the new workflow, the request method in CoreRequestHandler is
`IRingMasterRequestHandlerOverlapped.RequestOverlapped`. Since it is a sync call, and the only point to ensure the
ordering is the async task, the local commit signal is replaced with a semaphore to ensure the requests in the same
client session is started as the same order as they are received.
