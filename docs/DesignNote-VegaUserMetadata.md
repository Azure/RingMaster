# Design Note: Vega User Metadata 

## Introduction 

Currently in Vega, each node can store some data in a form of byte array. Now PubSub team (the main consumer team of 
Vega) has a new requirement that besides `Data`, they'll need to maintain additional information of a user, so we need 
to have an additional field in each node, which we will call `UserMetadata`. Similar with `Data`, the new `UserMetadata` 
field will also be a byte array which can be created, updated, read and deleted from the client. It will also be 
persisted to the persistence layer. Similar with how `Data` has a `Version` maintained in the `Stat` of each node, the 
`UserMetadata` will also have a `Uversion` maintained in the `Stat`. 

## API Changes 

We need to add new APIs as well as modifying some existing APIs to support the CRUD operations of `UserMetadata`. When 
modifying existing APIs, we need to make sure it's backward compatible so it will not break our client. However there is 
one exception: we feel that the `GetData` API was not very well designed previously so we modified the interface of it. 
We have communicated this with PubSub team, they're fully aware of this change and will need to make changes on their 
side once they import new Vega libraries. Below I will illustrate each API change in detail. 

* The `Create` API 

The create API creates a new node with given path, data and acl. Now it has a new optional parameter: the user metadata. 
If the create mode has the flag `SuccessEvenIfNodeExists`, the create will essentially become a set operation: the data 
and the user metadata will be updated on this node. Note that in this case if the client doesn't provide the user 
metadata, it will default to null and essentially delete the user metadata. 

* The `SetUserMetadata` API 

This is a new API added to update the user metadata of given node. The workflow is very similar with the `SetData` API: 
first we look for the node based on the path. If we can't find the node or the node's current user metadata version 
doesn't match the version client provided, return error code. Otherwise, acquire a write lock on the node, update the 
user metadata and the Uversion in node's Stat in the in memory tree, and then try to persist the change. If everything 
worked well we commit the change, otherwise roll back to previous state. 

* The `GetData` API 

PubSub team mentioned they will almost always retrieve the data and user metadata of a node at same time, so we decided 
to reuse the `GetData` API and provide an option to return user metadata. `GetData` API is an existing API that is used 
to get data and/or the stat of a node. As previously mentioned, we think the API was not well designed: the most 
frequently used overload of this method requests the data as well as stat to be returned from the server, however the 
method only returns data to the client, so all the cost of serializing and deserializing stat for the node is wasted. 
There was also a method `GetDataWithStat`, which we think is not needed since client can just use the `GetData` API and 
provide a `GetDataOption` that indicates to include stat in the response. For above reasons, we redesigned those APIs. 
Now there's only one `GetData` API, client can pass in different `GetDataOption` which can specify whether to include 
stat and user metadata in the response. The return type is a new class `GetDataResponse` which contains node's data, 
user metadata and stat. This way the API is easier to understand, the code is much more cleaner and extensible. 

* The `GetFullSubtree` API 

This API returns the entire subtree of a given path, and include data and/or stat of each node. Now it can optionally 
return user metadata of each node as well. This API internally calls the `GetData` API but pass in a path with a certain 
postfix, for example the postfix can be "$fullsubtreestat$", which means the operation is get full subtree with stat of 
each node. Same convention is used for user metadata, so the postfix can now be "$fullsubtreeusermetadata$" and 
"$fullsubtreestatandmetadata", depending on the `GetSubtreeOption` client provided. The backend server sees different 
postfix and will return different response accordingly. 

Another change for this API is adding Api version to the request path. In other words, the request path will have a 
postfix like "$apiversion=1$". The reason is this: for other APIs, serializing and deserializing happens in the 
transport layer, where we have good version control so adding/removing fields in request and response can be made 
backward compatible easily. However for this API (and also 'GetSubtree' mentioned below), because the transport layer 
doesn't have reference for the tree node and its structure, the response is serialized into byte array in backend core, 
and deserialized to `TreeNode` at client side. Therefore we can not use the version control in transport layer and will 
have to do version control in the request path. So now when server sees the api version is 1, it knows the request is 
from a new client so it will serialize the Uversion into the response. Otherwise it will ignore this field so the old 
client can deserialize the Stat correctly. 

* The `GetSubtree` API 

This API is similar with `GetFullSubtree` in that it also returns the subtree as a byte array and can also include Stat 
and User metadata. The difference is we can have pagination with this API so we don't have to return the entire subtree 
in a single response which might be too large. The implementation is also different: it doesn't use the `GetData` API 
and has its corresponding method in the backend, so we don't need a postfix to indicate it's a `GetSubtree` operation. 
All other user metadata related change are similar with the `GetFullSubtree` API: new option in `GetSubtreeOptions` is 
added to specify whether to include user metadata in the response. It also uses the api version in the request path to 
do version control. 

* The Multi/Batch API 

Except for `GetFullSubtree`, all above APIs can also be called in a multi/batch request, so the user metadata can also 
be created/read/updated/deleted in multi/batch requests as well. 

## Stat Changes 

Similar with `Version`(version of data), `Aversion`(version of Acl) and `Cversion`(version of children), with user 
metadata we now have `Uversion` in `IStat`. Internally to save memory consumption, different node may have different stat 
implementation. For newly created node, the stat is of type `FirstStat`, which has all versions default to 1 and 
`NumChildren` to 0. The instance of `FirstStat` takes very small memory. When there's update or add child to a node, its 
stat will become `MutableStat`, which has every field except for `Uversion`. When the user metadata field is set for a 
node, its stat will turn into `MutableStatWithMetadataVersion`, which has the `Uversion` field. By doing this if the 
client does not use the user metadata feature, the memory consumption should not increase because of this change. Note 
that those different implementations of Stat are transparent to the client. When requesting the node's stat, the client 
will always get a `Stat` instance. 

## Persistence Layer Change 

The user metadata and the Uversion in Stat need to be serialized and persisted. However again we were lacking version 
control in serialization, so when deserializing there's no easy way to tell if this is new data with user metadata or 
old data that don't have this field. So we had to come up with a hack: the child count of a node should always be 
non-negative, so in the new code, we negate the value of child count if it's positive. If it's 0 we make it 
`int.MinValue`. This way when deserializing by checking the value of child count we know if it's old or new data, and 
then choose if we need to read the user metadata and Uversion. To avoid such hacking in the future, we also added 
version control in this change. A version number will be serialized and persisted in the persistence layer along with 
each node's data. 

## Backward Compatibility 

In this change we ensured backward compatibility: the server should be able to process request and return proper 
response to either old or new clients. However we did not care about forward compatibility, meaning if the server is 
running old bits, it may not handle requests from new clients. So cope with this, the PubSub team will need to import 
new version of server side library, finish rolling out the server side change, and then start roll out client side 
change. This will ensure the client will always talk to new servers. 

## Performance Impact 

Some basic performance tests have been conducted, there's no performance downgrade with this change. Performance of new 
API `SetUserMetadata` is very close to `SetData` API, which makes sense because of their similarity. The performance of 
`GetData` when stat is not required should in theory improved, because we're not wasting time serializing stat now. 

## Testing 

This is a big change so lots of testing have been conducted, including unit tests, service upgrade test, failover test, 
etc. Those test code have been included in the change. However the backward compatibility test can not be easily added 
to the code as unit tests because it involves using old client/server bits. The way I conducted those tests now was to 
run BVT built from old version and target to new version of server. I will also create a script to automate this procedure.

