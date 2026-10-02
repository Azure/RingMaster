# Design Note: New Watcher Kind: IncludeDataAndChildChange 

## Overview 

This note introduces the design and expected behavior of a new watcher kind: IncludeDataAndChildChange. For a general 
introduction of watcher in Vega, see the DesignNote-Change-Delivery-With-Watcher. 

SDN Pub-Sub has certain scenarios that requires a watcher to deliver not only the changed event type, but also the 
data, stat of the changed node, and even the changed child (added or removed) all in one event, to eliminate possible 
race conditions. This requires a new watcher kind: IncludeDataAndChildChange. 

## Implementation 

`IncludeDataAndChildChange` will be added to the WatcherKind enum. This flag is orthogonal to the other flag: OneUse, so 
we'll not cover that in this design. 

`WatchedEvent` is the type that represents the change event that will be serialized and sent to the client. To enable 
change delivery with child's information, new properties are added to this type, namely: `ChildName` `ChildStat` 
`ChildData`. Those names should be self explanatory. 

WatchedEvent is sent from each node. When a child is added or removed from parent, and if the parent has any bulk 
watchers, the previous behavior is the child will send a NodeDeleted/NodeCreated event, followed by a 
NodeChildrenChanged event from the parent node. Now when the IncludeDataAndChildChange flag is set, the child will not 
send any event, and the parent is responsible to send all changes in a NodeChildrenChanged event, including the child 
name, child data, and child stat. Child name will always be present to identify which child is deleted/added. However 
note that if this is a create event, child stat will not be null, and if this is a delete event, child stat will be 
null. Client need to use this to determine if it's create or delete child event. 

## Expected behavior 

Since a watcher can be a bulk watcher or regular watcher, it can install on parent, on child, or both, the watcher kind 
can be IncludeDataAndChildChange or not, the combination of those settings are complicated and can be confusing. Thus we 
need summarize it here. Below is the expected behavior with different settings: 

1. In case of bulk watcher. 
	1.1. Watcher is on any parent node 
		1.1.1. No IncludeDataAndChildChange flag 
			1.1.1.1. Add/remove child: We will send two events, first is NodeCreated/NodeDeleted from the child, second is 
			NodeChildrenChanged from parent. The events will include the path, but will not include any data, stat, or child's 
			information. This is existing behavior and will remain unchanged. 
			1.1.1.2. Other Changes on child(set data/acl...): One NodeDataChanged event with the changed node path, data and 
			stat. This is also existing behavior. 
		1.1.2. With the IncludeDataAndChildChange flag 
			1.1.2.1. Add/remove child: Only one event will be sent: NodeChildrenChanged with parent path and stat, child name, 
			child data and stat. Client need to tell whether it's add or remove by checking if the child stat is null. 
			1.1.2.2. Other changes: same with 1.1.1.2 
	1.2. Watcher is on child node 
		1.2.1: No IncludeDataAndChildChange flag 
			1.2.1.1: Remove child (can't be add because node already exist): send one event: NodeDeleted, without any data or stat 
			1.2.1.1: Other changes: same with 1.1.1.2 
		1.2.2: IncludeDataAndChildChange: same with 1.2.1 

2. For non-bulk watcher. 
	2.1 Watcher on immediate parent node 
		2.1.1 No IncludeDataAndChildChange flag 
			2.1.1.1: Add/remove child: Send one event: NodeChildrenChanged with parent path, stat, no child info. 
			2.1.1.2: Other changes on child: no event sent. 
		2.1.2 IncludeDataAndChildChange 
			2.1.2.1: Add/remove child: Send one event: NodeChildrenChanged with parent path, stat, child name, data, stat(null 
			for delete) 
			2.1.2.2: other changes on child: no event.
	2.2 Watcher on child node 
		2.2.1 No IncludeDataAndChildChange flag: 
			2.2.1.1: remove child: send one event: NodeDeleted, without any data or stat. 
			2.2.1.2: Other changes: NodeDataChanged event with changed node's path, data, stat 
		2.2.2 IncludeDataAndChildChange: same with 2.2.1 

If there're multiple watchers the events should simply be the union of each watcher's events. For any unmentioned case 
the behavior should remain unchanged. 

