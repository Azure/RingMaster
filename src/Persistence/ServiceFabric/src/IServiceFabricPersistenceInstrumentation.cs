// <copyright file="IServiceFabricPersistenceInstrumentation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.ServiceFabric
{
    using System;

    /// <summary>
    /// Instrumentation for ServiceFabricPersistence.
    /// </summary>
    public interface IServiceFabricPersistenceInstrumentation : IPersistenceInstrumentation
    {
        /// <summary>
        /// Completed processing a rebuild notification for a reliable dictionary
        /// </summary>
        /// <param name="enumeratedCount">Number of <see cref="PersistedData"/> items processed</param>
        /// <param name="elapsed">Elapsed time of the rebuild process</param>
        void ProcessDictionaryRebuildCompleted(long enumeratedCount, TimeSpan elapsed);

        /// <summary>
        /// Failed to process a rebuild notification for a reliable dictionary
        /// </summary>
        /// <param name="elapsed">Elapsed time of the rebuild process</param>
        void ProcessDictionaryRebuildFailed(TimeSpan elapsed);

        /// <summary>
        /// Reports the statistics of the persisted data factory
        /// </summary>
        /// <param name="totalNodes">Number of nodes</param>
        /// <param name="totalDataSize">Data size in byte</param>
        void ReportStatistics(ulong totalNodes, ulong totalDataSize);

        /// <summary>
        /// A service fabric transaction is committed
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <param name="commitSequenceNumber">Commit sequence number</param>
        void TransactionCommitted(long transactionId, long commitSequenceNumber);
    }
}
