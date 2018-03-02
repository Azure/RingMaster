// <copyright file="IServiceFabricPersistenceInstrumentation.cs" company="Microsoft">
//   Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.ServiceFabric
{
    using System;

    /// <summary>
    /// Instrumentation for ServiceFabricPersistence.
    /// </summary>
    public interface IServiceFabricPersistenceInstrumentation : IPersistenceInstrumentation
    {
        void ProcessDictionaryRebuildCompleted(long enumeratedCount, TimeSpan elapsed);

        void ProcessDictionaryRebuildFailed(TimeSpan elapsed);

        void ReportStatistics(ulong totalNodes, ulong totalDataSize);

        void TransactionCommitted(long transactionId, long commitSequenceNumber);
    }
}
