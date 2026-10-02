# Design Node: Change Delivery With Watcher

## Introduction

Besides the key-value store with hierachical path, *Change Delivery* is an important feature provided by Ring Master,
and this differentiates itself from other products such as ZooKeeper. At the time of writing this node, SDN Pub-Sub is
the primary user of this feature and it leverages this in the Notification Service. Read requests, including *Exists*,
*GetData*, and *GetChildren* (as well as such requests inside compound requests), may specify a *watcher* to receive
notifications for changes on the given path. A client may choose to use the watcher once (same behavior as ZooKeeper),
or use it during the lifetime of the client session. When the node under watcher is changed, the following information
is notified back to the client:

* Watched event type: new node created, data changed in existing node, children changed, node deleted, or the watcher
  removed.
* State of the event keeper: authentication failed, client disconnected, client session expired, or the client in
  connected state.
* Path of the node associated with the notification.

Besides above information, a client may also choose to receive the change itself, such as data after the change for node
creation and update, or list of children for node children update. At this point, for every watcher trigger,
Notificationi Service will issue *GetData* to get back the change. This feature eliminates an uneccessary network
round-trip and potentially improves the performance of Pub-Sub service.

## Contracts and Data Structures

Interface `IWatcher` is the contract to specify the watcher on a particular node. It has the following members:

* Id: a 64-bit integer to uniquely represent the watcher object.
* Type: whether the watcher is single use, and whether it requires changed data back in the notification.
* Process: callback method to process the specified event. The argument is `WatchedEvent` detailed below.

The watcher type is a flag enum. Bit 0 indicates single use, bit 1 indicates the data back in the notification. Previous
version of the client (which use a bool of `OneUse` instead of enum) is compatible with the new format.

Class `WatchedEvent` is used to notify the change from the backend to the client. It has the following data members:

* EventType
* KeeperState
* Path
* Data (newly added)
* Stat

The object `Data` is a byte array for node data change, and `Stat` is the stat after the change. `Children` is omitted
because the child can only be added or removed, and the name of child being changed is already reflected in the `Path`.
If corresponding element has no update, the data member will be null and omitted in the serialization. Both members are
placed at the end of stream during serialization, so that the new data format from the backend is compatible with the
previous version of the client.

Class `WatcherCall` is a combination of both for the notification purpose.

## Implementation

### Serialization

Serialization of `IWatcher` and `WatcherCall` are performed in `Serializer` class in `RingMasterCommunicationProtocol`.

### Processing in Backend

When a packet is received by the TCP listener in the backend, it will be deserialized to `RequestCall` object with
`IRingMasterRequest` in it. Then the request object is wrapped to `IRingMasterBackendRequest` object and passed to
`ProcessMessage` method. If no write request is allowed per policy, nothing will happen, otherwise
`RegisterWatcherOnNode` will register the watcher on the node during commit of transaction. Depending on if the watcher
is bulk watcher or not, it will be added to either `WatcherCollection` in the client session or to the watcher list of
the node itself. Later when a node is being changed (create, set, children change), `Node.ScheduleTriggerWatchers` will
invoke `Process` to serialize the `WatcherCall`, where the to-be-delivered event data is stored, on all watchers
associated with the node being changed or all bulk watchers in the client session that are associated with the given
path.

### Processing in Client

When the notification is delivered back to the client, the data packet as a byte array is deserialized to
`RequestResponse` object by the communication protocol. If the call ID in the response indicates it is a watcher
notification, the content of the response is casted to `WatcherCall` object, and it will find the watcher object in the
mapping from watcher ID to watcher object in the request handler, then the `Process` callback of the said watcher object
is invoked with the `WatchedEvent` object of the `WatcherCall`.

Once the user of the RingMasterClient, for instance Notification Service in SDN Pub-Sub, receives the `WatchedEvent`, it
may further check if the data and stat list are available and use it accordingly.

## Backward Compatibility

## New Backend and Old Client

The old version of the client will send 0b01 as `OneUse` in `IWatcher` (ZooKeeper semantics) or 0b00 for reusable
watcher (most of time). At the backend, this is interpreted as the same meaning of the client, and the `WatchedEvent`
sent back will not contain data/stat after the change when the watcher is triggered. This is exactly the desired
behavior.

## Old Backend and New Client

For the watcher without change delivery, the new version of the client will send 0b01 as `OneUse` or 0b00 if reusable
watcher is used. Same behavior applies here.

If change delivery is required when the watcher is triggered, the client will send 0b11 as `OneUse` or 0b10 as reusable
watcher. Old version of the backend will interpret both as `OneUse` per the implementation of
`BinaryReader.ReadBoolean`. If later the watcher is triggered, the backend will send back the `WatchedEvent` once and
remove the watcher, and the removal is notified to the client. In the `WatchedEvent` data, the backend will send back
0b01 and the client will not attempt to read data / stat list fields in the received packet. In this case, the new
client will not exercise unexpected code path and will be able to detect that the feature is supported.

## Implementation Notes

Type of watcher is named *WatcherKind* to avoid the name conflict with .NET type.
