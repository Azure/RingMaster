// <copyright file="PersistedDataFactoryTest.cs" company="Microsoft Corporation">
//    Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.MiscellaneousTests
{
    using System;
    using System.Diagnostics;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.ServiceFabric;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using static Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.ServiceFabric.PersistedDataFactory;

    /// <summary>
    /// Tests for persisted data factory
    /// </summary>
    [TestClass]
    public sealed class PersistedDataFactoryTest
    {
        /// <summary>
        /// Tests if the method retries the commit operation in the presence of retriable exceptions.
        /// </summary>
        [TestMethod]
        public void DequeueRetryTest_ShouldRetryCommit_OnRetriableException()
        {
            // ARRANGE
            var mockStateManager = Substitute.For<IReliableStateManager>();
            var mockTransaction = Substitute.For<ITransaction>();
            mockStateManager.CreateTransaction().Returns(mockTransaction);

            var persistenceInstrumentationMock = Substitute.For<IServiceFabricPersistenceInstrumentation>();
            var callOrder = 0;

            mockTransaction.CommitAsync().ReturnsForAnyArgs(ci =>
            {
                callOrder++;
                if (callOrder == 1)
                {
                    throw new FabricTransientException("Simulated transient exception", new TimeoutException());
                }
                else if (callOrder == 2)
                {
                    throw new InvalidOperationException("Simulated invalid operation exception");
                }

                return Task.CompletedTask;
            });

            var config = new Configuration()
            {
                MaxRetryCount = 3,
            };

            // ACT
            using (var factory = new PersistedDataFactory(mockStateManager, "TestFactory", config, persistenceInstrumentationMock, CancellationToken.None))
            {
                factory.Activate(new RingMasterBackendCore(factory), Substitute.For<IPersistedDataFactoryClient>());

                var cc = new ChangeList(123, factory);
                var persistedData = new PersistedData(123) { Data = this.GenerateRandomData(1000 * 1000) };

                persistedData.AppendCreate(cc);
                cc.Commit(123, out var task);
                task.Wait();
            }

            // ASSERT
            mockStateManager.Received(3).CreateTransaction();
            persistenceInstrumentationMock.Received(1).ChangeListCommitted(Arg.Any<TimeSpan>());
            persistenceInstrumentationMock.Received(0).ChangeListCommitFailed();
        }

        /// <summary>
        /// Tests if the method only peeks the next changelist when its data size exceeds the threshold.
        /// </summary>
        [TestMethod]
        public void DequeueTest_ShouldPeekOnlyIfNextChangelistExceedsDataSizeThreshold()
        {
            // ARRANGE
            var mockStateManager = Substitute.For<IReliableStateManager>();
            var mockTransaction = Substitute.For<ITransaction>();
            mockStateManager.CreateTransaction().Returns(mockTransaction);

            var persistenceInstrumentationMock = Substitute.For<IServiceFabricPersistenceInstrumentation>();

            mockTransaction.CommitAsync().Returns(Task.CompletedTask);

            var config = new Configuration()
            {
                MaxReplicationDataSize = 1000 * 1000,
            };

            // ACT
            using (var factory = new PersistedDataFactory(mockStateManager, "TestFactory", config, persistenceInstrumentationMock, CancellationToken.None))
            {
                factory.Activate(new RingMasterBackendCore(factory), Substitute.For<IPersistedDataFactoryClient>());

                var changeList1 = new ChangeList(12345, factory);
                var changeList2 = new ChangeList(23456, factory);
                var persistedData1 = new PersistedData(12345) { Data = this.GenerateRandomData(1000) };
                var persistedData2 = new PersistedData(23456) { Data = this.GenerateRandomData(2000 * 2000) };

                persistedData1.AppendCreate(changeList1);
                persistedData2.AppendCreate(changeList2);

                changeList1.Commit(12345, out var task1);
                changeList2.Commit(23456, out var task2);

                Task.WhenAll(task1, task2).Wait();
            }

            // ASSERT
            mockStateManager.Received(2).CreateTransaction();
            persistenceInstrumentationMock.Received(2).ChangeListCommitted(Arg.Any<TimeSpan>());
        }

        /// <summary>
        /// Tests if the method only peeks the next changelist when its change count exceeds the threshold.
        /// </summary>
        [TestMethod]
        public void DequeueTest_ShouldPeekOnlyIfNextChangeCountExceedsThreshold()
        {
            // ARRANGE
            var mockStateManager = Substitute.For<IReliableStateManager>();
            var mockTransaction = Substitute.For<ITransaction>();
            mockStateManager.CreateTransaction().Returns(mockTransaction);

            var persistenceInstrumentationMock = Substitute.For<IServiceFabricPersistenceInstrumentation>();

            mockTransaction.CommitAsync().Returns(Task.CompletedTask);

            var config = new Configuration()
            {
                MaxReplicationQueueSize = 1,
                MaxReplicationDataSize = 1000 * 1000,
            };

            // ACT
            using (var factory = new PersistedDataFactory(mockStateManager, "TestFactory", config, persistenceInstrumentationMock, CancellationToken.None))
            {
                factory.Activate(new RingMasterBackendCore(factory), Substitute.For<IPersistedDataFactoryClient>());

                var changeList1 = new ChangeList(12345, factory);
                var changeList2 = new ChangeList(23456, factory);
                var persistedData1 = new PersistedData(12345) { Data = this.GenerateRandomData(1000) };
                var persistedData2 = new PersistedData(23456) { Data = this.GenerateRandomData(1000) };

                persistedData1.AppendCreate(changeList1);
                persistedData2.AppendCreate(changeList2);

                changeList1.Commit(12345, out var task1);
                changeList2.Commit(23456, out var task2);

                Task.WhenAll(task1, task2).Wait();
            }

            // ASSERT
            mockStateManager.Received(2).CreateTransaction();
            persistenceInstrumentationMock.Received(2).ChangeListCommitted(Arg.Any<TimeSpan>());
        }

        /// <summary>
        /// Tests if the method includes the next changelist when both its size and count are within the defined thresholds.
        /// </summary>
        [TestMethod]
        public void DequeueTest_ShouldIncludeNextChangelistIfWithinSizeAndCountThresholds()
        {
            // ARRANGE
            var mockStateManager = Substitute.For<IReliableStateManager>();
            var mockTransaction = Substitute.For<ITransaction>();
            mockStateManager.CreateTransaction().Returns(mockTransaction);

            var persistenceInstrumentationMock = Substitute.For<IServiceFabricPersistenceInstrumentation>();

            mockTransaction.CommitAsync().Returns(Task.CompletedTask);

            var config = new Configuration()
            {
                MaxRetryCount = 3,
                MaxReplicationQueueSize = 1000,
                MaxReplicationDataSize = 10000 * 10000,
            };

            // ACT
            using (var factory = new PersistedDataFactory(mockStateManager, "TestFactory", config, persistenceInstrumentationMock, CancellationToken.None))
            {
                factory.Activate(new RingMasterBackendCore(factory), Substitute.For<IPersistedDataFactoryClient>());

                var changeList1 = new ChangeList(12345, factory);
                var changeList2 = new ChangeList(23456, factory);
                var persistedData1 = new PersistedData(12345) { Data = this.GenerateRandomData(1000) };
                var persistedData2 = new PersistedData(23456) { Data = this.GenerateRandomData(1000 * 1000) };

                persistedData1.AppendCreate(changeList1);
                persistedData2.AppendCreate(changeList2);

                changeList1.Commit(12345, out var task1);
                changeList2.Commit(23456, out var task2);

                Task.WhenAll(task1, task2).Wait();
            }

            // ASSERT
            mockStateManager.Received(1).CreateTransaction();
            persistenceInstrumentationMock.Received(1).ChangeListCommitted(Arg.Any<TimeSpan>());
        }

        private byte[] GenerateRandomData(int length)
        {
            byte[] data = new byte[length];
            Random rnd = new Random();
            rnd.NextBytes(data);
            return data;
        }
    }
}
