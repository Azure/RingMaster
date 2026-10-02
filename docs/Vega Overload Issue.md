# Vega Server Overload Problem

## Problem Statement

For the typical PubSub scenario, in the in-memory tree in vega there're multiple vnets. In one vnets 
there might be many ca-pa mappings (over 10k), which are the leaf nodes in the tree. Recently we 
observed an issue that one client tries to update all the leaf nodes under one vnet in one big multi
request continuously. In another words, this multi request contains over 10k subrequests, which are
all SetData/SetAcl to the nodes in the same vnet. This caused higher failure rate for other clients
that tried to read from the same vnet.

## Current Observations

### Multi Request Performance

Multi Request Performance is affected mostly by three factors: the number of sub-requests in multi, 
number of bulk watchers registered on vnets, and other clients' traffic. In our benchmark test 
there's no traffic from other clients because it's hard to quantify them. 

If the number of watchers is fixed, the multi request response time is proportional to the number of 
sub-requests in this multi(we assume all sub-requests are write requests). Following table shows the
response time with respect to the number of sub-requests and bulk watchers:

| Number of Sub-Requests | Response Time (No Watcher) | Response Time (45 watchers on each vnet) |
| -----------------------| -------------------------- | ---------------------------------------- |
| 8k                     | 4s                         | 7s                                       |
| 16k                    | 8s                         | 15s                                      |
| 32k                    | 15s                        | 30s                                      |
| 64k                    | 30s                        | 59s                                      |


### Failure Rate

Failure rates are measured by sending GetData/GetAcl requests while there're ongoing multi requests.
Note that more watchers will cause even higher failure rates, for example when there're 135 watchers
on each vnet, the failure rate will more than 1%. However this should not happen in PubSub, at least
for now.

| Number of Sub-Requests | Failure Rate (No Watcher)  | Failure Rate (45 watchers on each vnet)  |
| -----------------------| -------------------------- | ---------------------------------------- |
| 8k                     | 0                          | 0.28%                                    |
| 16k                    | 0                          | 0.43%                                    |
| 32k                    | 0                          | 0.69%                                    |
| 64k                    | 0.038%                     | 0.69%                                    |

All tests mentioned above are done in Vega's test cluster.

## Possible Solutions

### Workaround/Short-Term Solutions

The direct cause of the failures is that those get requests were not able to acquire locks on the
nodes, because write locks were taken by multi request. Therefore to quickly fix/workaround this
issue is to increase the timeout for acquiring locks. The PubSub team has changed this timeout from
2 seconds to 15 seconds. Correspondingly the timeout for RingMasterClient has to be increased as well
to be at least large than the lock acquisition time. The PubSub team has changed client timeout to 
30 seconds. Tests also show with these setting changes the failures are almost gone. 

Another workaround for this issue is simply retry. When we retry the get request the multi request 
may have already finished and then our request will succeed. Retry can be done at client side or 
vega backend. There's a setting `RingMasterBackendCore.MaxRetryOperationMillis` sets the maximum 
process time of a request. If a request failed to acquire lock within this time, it will retry. The
default time currently is 2500 milliseconds.

The downside of these short-term solution is the response time could be much higher when the system 
is overloaded. 

### Long Term Solutions

1. See if it's possible to optimize bulk watcher

2. Adaptive Overload Control:

When the server is not overloaded, don't throttle any client.

When there're signals indicate the server is overloaded (for example higher failure rate), based on
how strong the signal is, limit the RPS of the client that sends expensive request, until the signal
is under certain threshold.




