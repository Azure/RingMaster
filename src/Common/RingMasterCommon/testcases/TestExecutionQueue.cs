// <copyright file="TestExecutionQueue.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.TestCases
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests that verify that the RingMaster execution queue functionality.
    /// </summary>
    [TestClass]
    public class TestExecutionQueue : RingMasterInterfaceTest
    {
        /// <summary>
        /// Path to the node in the ringmaster where the last applied transaction id is recorded.
        /// </summary>
        public const string LastAppliedTransactionIdPath = "/$tm/lastAppliedTransactionId";

        /// <summary>
        /// Path to the node in the ringmaster where the last applied batch id is recorded.
        /// </summary>
        public const string LastAppliedBatchIdPath = "/$tm/lastAppliedBatchId";

        /// <summary>
        /// Path to the node in the ringmaster where the transaction CRC is recorded
        /// </summary>
        public const string TransactionCrcPath = "/$tm/transactionCrc";

        /// <summary>
        /// Transaction Manager execution queue identifier.
        /// </summary>
        private static readonly Guid TransactionManagerExecutionQueueId = new Guid(0xa49152f7, 0x8f9c, 0x4606, 0x97, 0x6d, 0x74, 0xd7, 0xe9, 0xa6, 0xd8, 0x3c);

        /// <summary>
        /// Initializes the test.
        /// </summary>
        public void Initialize()
        {
            Task.Run(async () =>
            {
                using (var ringMaster = this.ConnectToRingMaster())
                {
                    await ringMaster.Create(TestExecutionQueue.LastAppliedBatchIdPath, BitConverter.GetBytes((ulong)0), null, CreateMode.PersistentAllowPathCreation, throwIfNodeExists: false);
                    await ringMaster.Create(TestExecutionQueue.TransactionCrcPath, BitConverter.GetBytes((ulong)0), null, CreateMode.PersistentAllowPathCreation, throwIfNodeExists: false);
                }
            }).Wait();
        }

        /// <summary>
        /// Verify that ringmaster correctly handles a batch sent to the Transaction Manager execution queue with just book keeping requests.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks completion of this test</returns>
        /// <remarks>
        /// RDBug 6351033: When a batch with just book keeping requests was sent to RingMaster,
        /// it did not update an internal variable that tracked last applied batch id and refused
        /// the subsequent batches.  This verifies the fix for that issue.
        /// </remarks>
        public async Task TestTransactionManagerExecutionQueue_EmptyBatch()
        {
            using (var ringMaster = this.ConnectToRingMaster())
            {
                IRingMasterRequestHandler ringMasterRequestHandler = (IRingMasterRequestHandler)ringMaster;
                ulong lastAppliedBatchId = await QueryRingMasterLastAppliedBatchId(ringMaster);
                ulong batchId = lastAppliedBatchId + 1;

                // First send a regular batch to initialize the lastAppliedBatchId tracker in RM
                RequestBatch initializationBatch = CreateBatch(batchId, CreateTransaction(1));
                RequestResponse batchResponse = await ringMasterRequestHandler.Request(initializationBatch);
                Assert.AreEqual((int)RingMasterException.Code.Ok, batchResponse.ResultCode);

                lastAppliedBatchId = await QueryRingMasterLastAppliedBatchId(ringMaster);
                batchId = lastAppliedBatchId + 1;

                // Create a batch with only book keeping requests.
                RequestBatch firstBatch = CreateBatch(batchId);

                Trace.TraceInformation("Sending first batch. batchId={0}, executionQueueId={1}", batchId, firstBatch.ExecutionQueueId);
                batchResponse = await ringMasterRequestHandler.Request(firstBatch);
                Assert.AreEqual((int)RingMasterException.Code.Ok, batchResponse.ResultCode);

                // The last applied batch id value must have been updated.
                lastAppliedBatchId = await QueryRingMasterLastAppliedBatchId(ringMaster);
                Trace.TraceInformation("After first batch. lastAppliedBatchId={0}", lastAppliedBatchId);
                Assert.AreEqual(batchId, lastAppliedBatchId);

                // Verify that the internal last applied batch id variable has been updated and subsequent
                // batches will be accepted.
                batchId = lastAppliedBatchId + 1;
                RequestBatch secondBatch = CreateBatch(batchId);
                Trace.TraceInformation("Sending second batch. batchId={0} executionQueueId={1}", batchId, secondBatch.ExecutionQueueId);
                batchResponse = await ringMasterRequestHandler.Request(secondBatch);
                Assert.AreEqual((int)RingMasterException.Code.Ok, batchResponse.ResultCode);

                lastAppliedBatchId = await QueryRingMasterLastAppliedBatchId(ringMaster);
                Trace.TraceInformation("After second batch. lastAppliedBatchId={0}", lastAppliedBatchId);
                Assert.AreEqual(batchId, lastAppliedBatchId);
            }
        }

        /// <summary>
        /// Queries the last applied batch id in the given ringmaster.
        /// </summary>
        /// <param name="ringMaster">An object that can handle ringmaster requests</param>
        /// <returns>The last applied transaction id in the given ringmaster or null if the value cannot be retrieved</returns>
        private static async Task<ulong> QueryRingMasterLastAppliedBatchId(IRingMasterRequestHandler ringMaster)
        {
            byte[] result = await ringMaster.GetData(TestExecutionQueue.LastAppliedBatchIdPath, watcher: null);
            return BitConverter.ToUInt64(result, 0);
        }

        /// <summary>
        /// Create a batch request which contains the given requests and book keeping requests.
        /// </summary>
        /// <param name="batchId">Id of the batch</param>
        /// <param name="requests">Requests to include in the batch</param>
        /// <returns>A <see cref="RequestBatch"/> that contains the given requests and book keeping requests</returns>
        private static RequestBatch CreateBatch(ulong batchId, params IRingMasterRequest[] requests)
        {
            var requestArray = new IRingMasterRequest[2 + requests.Length];
            requestArray[0] = new RequestSetData(TestExecutionQueue.LastAppliedBatchIdPath, data: BitConverter.GetBytes(batchId), version: -1);
            requests.CopyTo(requestArray, 1);
            requestArray[requestArray.Length - 1] = new RequestGetData(TestExecutionQueue.TransactionCrcPath, options: RequestGetData.GetDataOptions.None, watcher: null);

            var batchRequest = new RequestBatch(requestArray, completeSynchronously: true);
            batchRequest.Uid = batchId;
            batchRequest.ExecutionQueueId = TransactionManagerExecutionQueueId;
            batchRequest.ExecutionQueueTimeoutMillis = 10000;

            return batchRequest;
        }

        /// <summary>
        /// Creates a transaction request which contains only one book keeping request.
        /// </summary>
        /// <param name="transactionId">Id of the transaction</param>
        /// <returns>A <see cref="RequestMulti"/> that contains just one book keeping request</returns>
        private static RequestMulti CreateTransaction(ulong transactionId)
        {
            var recordTransactionIdRequest = new RequestSetData(TestExecutionQueue.LastAppliedTransactionIdPath, data: BitConverter.GetBytes(transactionId), version: -1);
            var multiRequest = new RequestMulti(new IRingMasterRequest[] { recordTransactionIdRequest }, completeSynchronously: true);

            ulong transactionTime = (ulong)DateTime.UtcNow.ToFileTimeUtc();
            multiRequest.Overrides = new OperationOverrides() { TxTime = transactionTime, TxId = transactionId };
            return multiRequest;
        }
    }
}