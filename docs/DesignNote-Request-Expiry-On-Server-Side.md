# Design Note: Enable Request Expiry on Server Side

## Background and Motivation

When a Ring Master Request is created from the Ring Master Client, it will first go into a queue on the client side. Ring
Master client will pull requests out from the queue and send to ring master server. The client has a request timeout 
property. If the client doesn't receive a response from the server within the timeout time, it will return a OperationTimeout 
error. When this happens, the request may either still in the client side queue, or already sent to server for processing. In 
the latter case, the server may keep processing the request for a long time, while the client already timed out the request and 
will ignore the response from the server later. This lead to wasted server resources, and potentially degrade server
performance. Therefore we made the change to enable request expiry on server side.

## Design and Implementation

To expire a request on server side, the first thing we need to know is when to expire the request. Since the ring master client
already has a timeout property, we want to honor this timeout. So the server timeout value should be the total timeout set on client
minus the time spent at the client side. When the client is ready to serialize the request and send to server, it calculates the 
server timeout value in milliseconds and send this value to server along with the request.

When the backend receives a request, it deserializes the server timeout and adds it up with the current elapsed time from a global 
timer to generate a request expiry time in TimeSpan format. The reason we're not using Timestamp from DateTime object is timestamp 
from wall clock may be inaccurate, or adjusted unexpectedly when the system clock is resynchronized to time service.

With the request expiry time, when we process a request on the server, at some critical steps we'll check if the request has expired.
If expired, we stop processing the request immediately and return ServerOperationTimeout status code to the client. Currently the 
critical steps where we check request expiry is:

1. When backendcore is ready to process this request. Since we need to order each request coming from the same TCP connection, the 
request may need to wait some time before the backendcore is ready to process it. At this time if the request has already expired,
the backendcore no longer needs to process it.

2. When the request tries to acquire locks. Lock acquisition can also take some time, especially when the server is busy and lock 
contention is high.

If the request is not expired after acquisition of all locks, we will stop checking request timeout and will let it run to complete 
since the rest operations should finish executing very quickly.

One thing to note on the above design is that the request may timeout after the specified server timeout value, but definitely not 
before. We don't want to prematurely stop processing any request.