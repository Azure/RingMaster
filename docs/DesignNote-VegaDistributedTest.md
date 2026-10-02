# Design Note: Vega Distributed Test	

## Overview

In Vega we have various kinds of tests: unit tests, simple end to end test from single client, and Vega distributed test. The distributed test allows tests run from
multiple clients simultaneously, maximizing the load and stress on the backend, so we can get more accurate performance results and more likely to find bugs in the system.
The vega distributed test suite consists three parts: the distributed test client, the distributed test service, and the distributed test contracts. The test contracts
define the protocol between the test client and the test service. The test client sends command and receives response from the test service through Grpc (which we will
talk about later), and the test service will actually runs the tests. 

## The client and service communication

The test contracts defines the protocol between client and the service. We use Grpc with protocol buffer as the communication protocol. The `.proto` file defines the RPC
methods as well as the request and response types. After creating the `.proto` file, we use
`src/Tests/VegaDistributedTest/VegaDistributedTestContracts/GenerateProtos.cmd` to generate the C# code. The distributed test service will host a Grpc service that
listens on client request, and then the test client can send command to the service like start test job, get job states, etc. For more details regarding Grpc or protocol
buffer, see the [official documentation on Grpc](https://grpc.io/docs/)

## The distributed test service

The distributed test service is a stateless service fabric service. Each instance in the service will host a Grpc server. The client can send request to any of the
instance, and then that instance will be responsible to distribute the request to other instances, so all the instances will start running the test. After the test
finished, it will return the results to the client.

## How to add more tests

To add a new test case, add a new class in the distributed test service that implements the `ITestJob` interface. In the test client, add a test method that invokes 
the test in the service. 

## Future work

Currently the test suite only tests and benchmarks the vega application. However there're other key value store systems like Zookeeper and etcd, and we want to compare
their performance with the same test application. So our plan is to make the vega distributed test more generic so that it can also measure the performance for other systems.
