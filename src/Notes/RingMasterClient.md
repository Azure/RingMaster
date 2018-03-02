# RingMasterClient

## Introduction
*RingMasterClient* is used to connect to and perform operations on the *RingMaster* system.   To use RingMasterClient, take a dependency on the package [Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterClient](https://msazure.visualstudio.com/DefaultCollection/One/_packaging?feed=Official&_a=package&package=Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterClient&version=1.3.4&protocolType=NuGet) which can be found in the [Official](https://msazure.pkgs.visualstudio.com/DefaultCollection/_apis/packaging/Official/nuget/index.json) feed.

## Connecting to RingMaster

*RingMaster* can either be a single server or a cluster of servers out of which one is the primary at any time.  A Connection string is used to specify the ringmaster server endpoints.  The Connection string consistes of one or more "ipaddress:port" pairs separated by ';'

```csharp
var client = new RingMasterClient("127.0.0.1:99", clientCertificates: null, serverCertificates: null, requestTimeout: 10000);

var client = new RingMasterClient("10.0.0.1:99;10.0.0.2:99;10:0.0.3:99", clientCertificates: null, serverCertificates: null, requestTimeout: 10000);
```
The connection to *RingMaster* can be secured by TLS in which case the certificates that must be used for the connection can be specified in the constructor. The *clientCertificates* parameter takes an array of *X509Certificates* out of which the first certificate is used to identify the client to the server.   The *serverCetificates* parameter takes an array of *X509Certificates* that can be used to verify the identity of the server.

```csharp
X509Certificate[] clientCertificates = // Client certificates
X509Certificate[] serverCertificates = // Server certificates

var client = new RingMasterClient("127.0.0.1:99", clientCertificates, serverCertificates, requestTimeout: 10000);
``` 

The *RingMasterClient* will automatically manage the connection to the server.  If the connection is broken, it keep trying to reconnect until it is able to connect to one of the specified servers.  The *requestTimeout* parameter specifies the timeout in milliseconds for requests sent through *RingMasterClient*.   If a response is not received for a sent request for longer than this time, the request is completed with an *OperationTimedout* result code.

## Creating a node

*RingMaster* maintains a tree of nodes.  Each node can have data and an access control list associated with it.  A node can also have 0 or more children.

```csharp
using (var ringMasterClient = ConnectToRingMaster())
{
    byte[] data = // data
    IReadOnlyList<Acl> acl = null;
    string createdNodeName = await ringMasterClient.Create("/node", data, acl, CreateMode.Persistent);
}
```

### Persistent and Ephemeral nodes
Nodes can be **Persistent** or **Ephemeral**.  Persistent nodes are retained until they are explicitly deleted.  Ephemeral nodes are automatically deleted once the session that created those nodes is closed.

```csharp
using (var ringMasterClient = ConnectToRingMaster())
{
    string persistentNodeName = await ringMasterClient.Create("/persistentNode", null, null, CreateMode.Persistent);

    string ephemeralNodeName = await ringMasterClient.Create("/ephemeralNode", null, null, CreateMode.Ephemeral);

    // persistentNode would exist here.
    // ephemeralNode would exist here because the connection that created the node is still alive.
}

// persistentNode would still exist here even after the  connection that created it is closed.

// ephemeralNode would have been deleted at this point because the connection which created the node has been closed.
```

### Sequential creation mode
RingMaster also supports the **PersistentSequential** and **EphemeralSequential** create modes.  When a node is created with one of these modes, a monotonically increasing number is automatically appended to the given name.

```csharp
var createdNodeName = await ringMasterClient.Create("node", null, null, CreateMode.PersistentSequential);

Assert.AreEqual("Node0000000001", createdNodeName);
```

## Verifying the existence of a node

The *Exists* method can be used to verify the existence of a node.
```csharp

var newNode = await ringMasterClient.Create("/new", null, null, CreateMode.Persistent);

IStat stat = await ringMasterClient.Exists("/new", watcher: null);
// stat will contain information about the node

IStat statDoesntExist = await ringMasterClient.Exists("/doesnotexist", watcher: null);

// Exists would have thrown an RingMasterException with ErrorCode set to "Nonode"
```

A **watcher** can be specified as the second parameter to the Exists call.  The watcher is an object that implements the *IWatcher* interface.  It will be notified when the node being watched is modified.

```csharp

var watcher = new WatcherImpl(); // Implements IWatcher
var stat = ringMasterClient.Exists("/node", watcher);
ringMasterClient.Delete("/node", version: -1);
// Watcher will be notified of the delete
```

## Deleting a node

The *Delete* method can be used to delete a node

```csharp
var node = await ringMasterClient.Create("/node", null, null, CreateMode.Persistent);

bool wasDeleted = await ringMasterClient.Delete("/node", version: -1);
```

The **version** paramter specifies the version of the node to be deleted.  If the value is set to -1, the node is deleted regardless of its current version.

The safer thing to do would be to first query the version and then invoke delete with the expected version.  Delete will only succeed if the node has not been modified in the interim.

```csharp
var popularNodeStat = await ringMaster.Exists("/popularNode", watcher: null);

// A modification occurred that changed the Version of the node.

bool wasDeleted = await ringMaster.Delete("/popularNode", popularNodeStat.Version);

// wasDeleted would be false, because the node did not have the expected version at the time of delete.
```

### DeleteModes

- **None** Deletes only the specified node
- **CascadeDelete** Deletes the specified node and all its children
- **SuccessEvenIfNodeDoesntExist** The Delete operation succeeds even if the node does not exist.
- **FastDelete** Accelerates the **CascadeDelete** operation.

## Getting the data associated with a node

The *GetData* method can be used to retrieve the data associated with a node.

```csharp
byte[] originalData = await ringMasterClient.Create("/node", originalData, null, CreateMode.Persistent);

byte[] retrievedData = await ringMasterClient.GetData("/node", watcher: null);

// The contents of retrievedData will be equal to the contents of originalData
```

A **watcher** can be specified to watch for changes to the node.

```csharp
byte[] nodeData = await ringMasterClient.GetData("/node", watcher);
```

## Modifying the data associated with a node

The *SetData* method can be used to modify the data associated with a node.

```csharp
byte[] originalData = // Some data

await ringMasterClient.Create("/node", originalData, null, CreateMode.Persistent);

byte[] modifiedData = // Different data

await ringMasterClient.SetData("/node", modifiedData, version: -1);

byte[] retrievedData = await ringMasterClient.GetData("/node", watcher: null);

// The contents of retrievedData will be the same as the contents of modifiedData *not* originalData
```

Just like *Delete* above, *SetData* can also be made conditional on the version number.  

```csharp
var stat = await ringMasterClient.Exists("/node", watcher: null);

// Node data was modified here

await ringMasterClient.SetData("/node", newData, stat.Version);

// SetData would have thrown a RingMasterException with error code BadVersion because the version of the node has changed.
```

## Retrieving the children of a node

The *GetChildren* method can be used to retrieve the children of a node.

```csharp

await ringMasterClient.Create("/parent", null, null, CreateMode.Persistent);

await ringMasterClient.Create("/parent/child1", null, null, CreateMode.Persistent);

await ringMasterClient.Create("/parent/child2", null, null, CreateMode.Persistent);

IReadOnlyList<string> children = await ringMasterClient.GetChildren("/parent", watcher: null);

// children will contain 2 entries "child1" and "child2"

```

GetChildren also supports installation of a watcher that will be notified when the node changes.

### Retrieval Conditions

When a node has a large number of children, it is better to retrieve only a subset of children at a time.  A **Retrieval Condition** can be specified to select the subset of children to retrieve.

The retrieval condition is of the form ">[Top]:[ChildName] where _Top_ is a number that specifies the maximum number of children to retrieve and _ChildName_ is the name of the starting child name.

- **">:1000:contoso"** means retrieve the first 1000 children whose names are greater than "contoso" in ordinal order.

- **">:1000:" means retrieve the first 1000 children.  The name of the last child in the retrieved list can be used as the starting child name for the next GetChildren call.


## Protecting access to a node

The *SetAuth* method can be used to associate an identity with a session.

```csharp
Id digest1Identity = new Id(AuthSchemes.Digest, "digest1");

await ringMasterClient.SetAuth(digest1Identity);

// The session now has digest1Identity
```

The *GetACL* and *SetACL* methods can be used to retrieve and change the *Access control list* associated with a node.

```csharp

IReadOnlyList<Acl> acls = new List<Acl>();

// Allow all permissions to digest1Identity
acls.Add(new Acl(Acl.Perm.All, digest1Identity));

await ringMaster.SetACL("/node", acls, -1);

var stat = await ringMaster.Exists("/node", watcher: null);
var retrievedAcls = await ringMaster.GetAcl("/node", stat);

// The retrievedAcls will be the same as what was set.
```

## Applying multiple operations together

The *Multi* method can be used to apply a sequence of operations as a transaction.  Either all of them are applied or none of them or applied.

The *Batch* method can be used to apply a sequence of operations in sequence.  The operations are applied in sequence until the first failure.

Both the above operations are invoked by first building up a list of operations.

```csharp
IReadOnlyList<Op> operations = new List<Op>();

operations.Add(Op.Create("/node1", null, null, CreateMode.Persistent);

operations.Add(Op.Create("/node2", null, null, CreateMode.Persistent));

IReadOnlyList<OpResult> multiResults = await ringMaster.Multi(operations);

// If an error occurred when the operation to create node2 was performed, node1 will not exist eventhough the operation to create node1 had no error.

IReadOnlyList<OpResult> batchResults = await ringMaster.Batch(operations);

// If an error occurred when the operation to create node2 was performed, node1 will still exist.
```

### Examples

Examples of various RingMasterClient use cases are available here:

```
src/Common/RingMasterCommon/testcases/TestFunctionality.cs
```