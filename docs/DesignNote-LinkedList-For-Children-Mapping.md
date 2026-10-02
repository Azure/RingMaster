# Design Note: Use Linked List to Store Children Mapping 

## Overview 

In Vega's in memory tree, a node can have any number of children. Internally, we use 3 different dictionaries to store 
the children mapping for a node, based on its number of children. If the number of children is less than 128 we were 
using a type called SortedArrayList. For children number between 128 to 50,000 a regular dictionary is used. Beyond that 
a SortedNameValueDictionary is implemented to mainly deal with a CloudDNS scenario where one node can have 100 million 
or more children. 

## The Problem 

This design targets to the SortedArrayList specifically. Last year in Vega a change was introduced to [use layered lock 
collection to replace the old logic that acquires the lock while traversing the 
tree](./DesignNote-Locking-in-Backend.md). This has an implication that getting a child node from the children mapping 
is not protected by lock any more, and that leads to a race condition in the SortedArrayList: if one thread is removing 
an element from the array, and another thread tries to get another element which is supposed to be in that same array, 
chances are it may not find that element. This is because for an array the remove operation will shift elements to the 
left. Since this operation cannot be made atomically, the get operation may miss the element during the shift process. 

## The Fix 

There're a few proposals to fix this issue. The simplest fix would be using a lock (monitor or reader writer lock) in 
the remove and get method. Although a rudimentary test did not reveal substantial performance downgrade, it is however 
highly likely to hurt performance for some traffic pattern. So this fix is not ideal. 

Another proposal is to just remove this type, use regular dictionary even though the children count is less than 128. 
Tests showed a regular dictionary does not have the race condition mentioned above, so the correctness can be 
guaranteed. However a major downside is a dictionary uses much more space than a simple array. Imagine there's a large 
number of nodes that have children count less than 128 (which is true for most scenarios), the memory consumption will 
be much higher. In fact I believe this is the reason why SortedArrayList was implemented in the first place. So this 
approach is not ideal either. 

The approach we adopted in the end is to use a linked list to replace the array. In a linked list when removing an 
element only a pointer needs to be changed, which can be made atomically. So the race condition is eliminated. This 
approach is viable also because all existing operations of the array is linear search: there's no random access, so 
using a linked list will not hurt the performance. A linked list implementation does bring extra memory consumption 
because each linked list node need to maintain a pointer to the next node. However this is trivial compared to the 
overall memory. One test shows the memory consumption increased around 5%, which is acceptable. 

