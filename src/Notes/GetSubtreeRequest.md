# GetSubtree Request Specification

## Summary
The GetSubtree request type is designed to provide the ability to get the data of all the nodes under a specific path in depth-first order sorted by node name.
Today we support getting a full subtree with a getdata request using $fullsubtree$. However, this does not work for cases where the subtree size is very large as it does not support continuations. Some thought was given to extending the existing $fullsubtree$ getdata request to support continuations, but given extra parameters needed (retrieval conditions) it makes sense to move to a separate request type.

## Input
```
getsubtree <path> <retrieval-condition>
<retrieval-condition> = >:<top>:<continuation> (similar format to getchildren)
<top> = positive integer
<continuation> = path of last node in previous results  
```

## Output
Content = same format as getdata with $fullsubtree$ for compat with existing deserializing tooling

ResponsePath = null if reached end of subtree or the full path of last node in the response if there is more data still to be enumerated

Note that in case of continuations content will need to include already-traversed nodes in the response for building, but we will just include the node name – not the data – if it was previously visited.

Using the ResponsePath field for continuations is nice in that it allows to keep the same data format for content as for $fullsubtree$ getdata. The other option is to provide an explicit continuation in the response, but then the data format would differ.

## Example
```
[localhost:98(127.0.0.1:98)]>>getsubtree /x >:5:
Ok /x (10ms)
/x
x Persistent x
/x/a
x/a Persistent x
/x/a/a
foo
/x/a/b
bar
/x/b
x/b Persistent x
ResponsePath: /x/b (response path is non-null, continue from /x/b)

[localhost:98(127.0.0.1:98)]>>getsubtree /x >:5:/x/b
Ok /x (0ms)
/x  (intermediate nodes already traversed have name included for deserializing, but not the data as it was received previously)
/x/b
/x/c
create /x/c Persistent c
/x/d
d Persistent d
ResponsePath: (response path = null, done enumerating)
```

## Limitations
Initially, no support for adding watchers in getsubtree requests. Supporting watchers in this request type would likely mean adding the watcher to all nodes in the subtree which could easily balloon. 

Bulk watchers are much more suited to this use case and should be used instead.