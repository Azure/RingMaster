# System Info Pseudo Node Design Note

## Overview

When debugging issues or livesites in Vega, it is very useful to know the current 
information of the system, for instance, number of inflight requests, errors and 
their distribution based on error code, etc. Some of those information can be quired
from the log, but it would take some time to query and parse the log. It would be 
useful to be able to query this from a simple API call, so we can automate this in
production environments easily. For this reason we added a pseudo node $systeminfo,
calling GetData on this path will return current system information.

## Implementation

1. The SystemInfo Class

We added a new class `SystemInfo` which contains all fields we needed: a concurrent
dictionary contains number of errors grouped by error code, another concurrent
dictionary which stores number of received requests grouped by request type, and 
number of responded requests. From the last two fields we can easily get the inflight
request count. In future we may want to return more information from this type, then 
we can just add new fields in this class and change the Serialize and Deserialize 
methods accordingly. Note that there could be multiple clients sending requests 
simultaneously, so updating/reading the fields in this class should be thread safe.

2. The API
  
The API to request system info is `GetData`. In GetData method we first check if the 
request path is "$systeminfo". If it is then we serialize the SystemInfo instance
maintained in RingMasterBackendCore and return the byte array. The client can then
call the static Deserialize method in SystemInfo to deserialize the byte array. 

Whenever there're requests and errors in the system, the SystemInfo instance will be
updated. However we don't persist this data, and it will get reset when a new primary
starts. We also don't lock this node when accessing it because this is for internal
use only, we will not have multiple threads accessing it. And even there're some data
inconsistency in the returned byte array it is not the end of the world.

## Usage

Currently we can use Madari's Cmdlet to call this API against Prod PubSub system. For
more details on Madari Cmdlet see [this doc in Madari repo](https://msazure.visualstudio.com/One/_git/Networking-Madari?path=%2Fdocs%2FGetting-Started-Guide.md&version=GBdevelop).
