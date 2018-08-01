// <copyright file="RetriableRingMasterClientTest.cs" company="Microsoft Corporation">
//    Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.MiscellaneousTests
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Vega.Test.Helpers;
    using NSubstitute;
    using VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The retiable ring master client test
    /// </summary>
    [TestClass]
    public sealed class RetriableRingMasterClientTest
    {
        /// <summary>
        /// Logging delegate
        /// </summary>
        private static Action<string> log;

        /// <summary>
        /// Tests the class initialize.
        /// </summary>
        /// <param name="context">The context.</param>
        [ClassInitialize]
        public static void TestClassInitialize(TestContext context)
        {
            if (context.GetType().Name.StartsWith("Dummy"))
            {
                log = s => context.WriteLine(s);
            }
            else
            {
                log = s => Trace.TraceInformation(s);
            }
        }

        /// <summary>
        /// Retriables the ring master client stress.
        /// </summary>
        [TestMethod]
        public void RetriableRingMasterClientStress()
        {
            int maxRequestCount = 1000;
            int initiallyWorkingCount = 500;
            var mockRequestHandler = Substitute.For<IRingMasterRequestHandler>();

            var rnd = new Random();
            int requestCount = 0;
            mockRequestHandler.Request(Arg.Any<IRingMasterRequest>()).Returns<Task<RequestResponse>>((callInfo2) =>
            {
                Interlocked.Increment(ref requestCount);
                if (requestCount <= initiallyWorkingCount)
                {
                    if (rnd.NextDouble() < 0.1)
                    {
                        return Task.FromResult(new RequestResponse() { ResultCode = (int)RingMasterException.Code.Operationtimeout });
                    }
                    else
                    {
                        return Task.FromResult(new RequestResponse() { ResultCode = (int)RingMasterException.Code.Ok });
                    }
                }
                else if (rnd.NextDouble() < 0.5)
                {
                    return Task.FromResult(new RequestResponse() { ResultCode = (int)RingMasterException.Code.Operationtimeout });
                }
                else
                {
                    throw RingMasterException.GetException(new RequestResponse() { ResultCode = (int)RingMasterException.Code.OperationCancelled });
                }
            });

            var createClientFunc = Substitute.For<Func<string, IRingMasterRequestHandler>>();
            createClientFunc(Arg.Any<string>()).Returns((callInfo) =>
            {
                requestCount = 0;
                return mockRequestHandler;
            });

            var vegaServiceInfoReader = Substitute.For<IVegaServiceInfoReader>();
            vegaServiceInfoReader.GetVegaServiceInfo().Returns((callInfo) =>
            {
                Thread.Sleep(rnd.Next(1, 5) * 1000);
                return Tuple.Create(Arg.Any<string>(), Arg.Any<string>());
            });

            var requestFunc = Substitute.For<Func<IRingMasterRequestHandler, Task<RequestResponse>>>();

            requestFunc(Arg.Any<IRingMasterRequestHandler>()).Returns((callInfo) => mockRequestHandler.Request(Arg.Any<IRingMasterRequest>()));

            RetriableRingMasterClient theClient = new RetriableRingMasterClient(createClientFunc, Arg.Any<string>(), vegaServiceInfoReader, log);

            int taskCount = 0;
            int exceptionCount = 0;
            for (int i = 0; i < maxRequestCount; i++)
            {
                taskCount++;
                var unused = theClient.Request(requestFunc)
                    .ContinueWith(t =>
                    {
                        if (t.Exception != null)
                        {
                            Interlocked.Increment(ref exceptionCount);
                        }

                        Interlocked.Decrement(ref taskCount);
                    });
            }

            SpinWait.SpinUntil(() => taskCount == 0);

            Assert.IsTrue(exceptionCount < maxRequestCount - initiallyWorkingCount);
            log($"{exceptionCount}");
        }
    }
}
