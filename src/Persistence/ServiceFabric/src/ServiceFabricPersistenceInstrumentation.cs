// <copyright file="ServiceFabricPersistenceInstrumentation.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.ServiceFabric
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation;

    /// <summary>
    /// Implementation of the <see cref="IServiceFabricPersistenceInstrumentation"/> interface
    /// that reports metrics using metrics factory.
    /// </summary>
    public sealed class ServiceFabricPersistenceInstrumentation : IServiceFabricPersistenceInstrumentation
    {
        private readonly IMetric0D addRequested;
        private readonly IMetric0D updateRequested;
        private readonly IMetric0D removeRequested;

        private readonly IMetric0D loadTreeCompleted;
        private readonly IMetric0D loadTreeTime;

        private readonly IMetric0D processLoadCompleted;
        private readonly IMetric0D processAddCompleted;
        private readonly IMetric0D processUpdateCompleted;
        private readonly IMetric0D processRemoveCompleted;

        private readonly IMetric0D processLoadFailed;
        private readonly IMetric0D processAddFailed;
        private readonly IMetric0D processUpdateFailed;
        private readonly IMetric0D processRemoveFailed;

        private readonly IMetric0D dictionaryEntriesEnumerated;
        private readonly IMetric0D processDictionaryRebuildCompleted;
        private readonly IMetric0D processDictionaryRebuildTime;
        private readonly IMetric0D processDictionaryRebuildFailed;

        private readonly IMetric0D totalNodes;
        private readonly IMetric0D totalDataSize;

        private readonly IMetric0D transactionCommitted;
        private readonly IMetric0D lastCommittedTransaction;
        private readonly IMetric0D lastCommitSequenceNumber;

        private readonly IMetric0D changeListCommitted;
        private readonly IMetric0D changeListCommitTime;
        private readonly IMetric0D changeListCommitFailed;
        private readonly IMetric0D changeListAborted;

        private readonly IMetric0D dataLoaded;
        private readonly IMetric0D duplicatesFound;
        private readonly IMetric0D orphansFound;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceFabricPersistenceInstrumentation"/> class.
        /// </summary>
        /// <param name="metricsFactory">Metrics factory</param>
        public ServiceFabricPersistenceInstrumentation(IMetricsFactory metricsFactory)
        {
            if (metricsFactory == null)
            {
                throw new ArgumentNullException(nameof(metricsFactory));
            }

            this.addRequested = metricsFactory.Create0D(nameof(this.addRequested));
            this.updateRequested = metricsFactory.Create0D(nameof(this.updateRequested));
            this.removeRequested = metricsFactory.Create0D(nameof(this.removeRequested));

            this.loadTreeCompleted = metricsFactory.Create0D(nameof(this.loadTreeCompleted));
            this.loadTreeTime = metricsFactory.Create0D(nameof(this.loadTreeTime));

            this.processLoadCompleted = metricsFactory.Create0D(nameof(this.processLoadCompleted));
            this.processAddCompleted = metricsFactory.Create0D(nameof(this.processAddCompleted));
            this.processUpdateCompleted = metricsFactory.Create0D(nameof(this.processUpdateCompleted));
            this.processRemoveCompleted = metricsFactory.Create0D(nameof(this.processRemoveCompleted));
            this.processLoadFailed = metricsFactory.Create0D(nameof(this.processLoadFailed));
            this.processAddFailed = metricsFactory.Create0D(nameof(this.processAddFailed));
            this.processUpdateFailed = metricsFactory.Create0D(nameof(this.processUpdateFailed));
            this.processRemoveFailed = metricsFactory.Create0D(nameof(this.processRemoveFailed));

            this.dictionaryEntriesEnumerated = metricsFactory.Create0D(nameof(this.dictionaryEntriesEnumerated));
            this.processDictionaryRebuildCompleted = metricsFactory.Create0D(nameof(this.processDictionaryRebuildCompleted));
            this.processDictionaryRebuildTime = metricsFactory.Create0D(nameof(this.processDictionaryRebuildTime));
            this.processDictionaryRebuildFailed = metricsFactory.Create0D(nameof(this.processDictionaryRebuildFailed));

            this.totalNodes = metricsFactory.Create0D(nameof(this.totalNodes));
            this.totalDataSize = metricsFactory.Create0D(nameof(this.totalDataSize));

            this.transactionCommitted = metricsFactory.Create0D(nameof(this.transactionCommitted));
            this.lastCommittedTransaction = metricsFactory.Create0D(nameof(this.lastCommittedTransaction));
            this.lastCommitSequenceNumber = metricsFactory.Create0D(nameof(this.lastCommitSequenceNumber));

            this.changeListCommitted = metricsFactory.Create0D(nameof(this.changeListCommitted));
            this.changeListCommitFailed = metricsFactory.Create0D(nameof(this.changeListCommitFailed));
            this.changeListAborted = metricsFactory.Create0D(nameof(this.changeListAborted));
            this.changeListCommitTime = metricsFactory.Create0D(nameof(this.changeListCommitTime));

            this.dataLoaded = metricsFactory.Create0D(nameof(this.dataLoaded));
            this.duplicatesFound = metricsFactory.Create0D(nameof(this.duplicatesFound));
            this.orphansFound = metricsFactory.Create0D(nameof(this.orphansFound));
        }

        /// <inheritdoc />
        public void AddRequested(TimeSpan elapsed)
        {
            this.addRequested.LogValue(1);
        }

        /// <inheritdoc />
        public void RemoveRequested(TimeSpan elapsed)
        {
            this.removeRequested.LogValue(1);
        }

        /// <inheritdoc />
        public void UpdateRequested(TimeSpan elapsed)
        {
            this.updateRequested.LogValue(1);
        }

        /// <inheritdoc />
        public void LoadTreeCompleted(TimeSpan elapsed)
        {
            this.loadTreeCompleted.LogValue(1);
            this.loadTreeTime.LogValue((long)elapsed.TotalMilliseconds);
        }

        /// <inheritdoc />
        public void ProcessLoadCompleted()
        {
            this.processLoadCompleted.LogValue(1);
        }

        /// <inheritdoc />
        public void ProcessLoadFailed()
        {
            this.processLoadFailed.LogValue(1);
        }

        /// <inheritdoc />
        public void ProcessAddCompleted()
        {
            this.processAddCompleted.LogValue(1);
        }

        /// <inheritdoc />
        public void ProcessAddFailed()
        {
            this.processAddFailed.LogValue(1);
        }

        /// <inheritdoc />
        public void ProcessDictionaryRebuildCompleted(long enumeratedCount, TimeSpan elapsed)
        {
            this.dictionaryEntriesEnumerated.LogValue(enumeratedCount);
            this.processDictionaryRebuildCompleted.LogValue(1);
            this.processDictionaryRebuildTime.LogValue((long)elapsed.TotalMilliseconds);
        }

        /// <inheritdoc />
        public void ProcessDictionaryRebuildFailed(TimeSpan elapsed)
        {
            this.processDictionaryRebuildFailed.LogValue(1);
        }

        /// <inheritdoc />
        public void ProcessRemoveCompleted()
        {
            this.processRemoveCompleted.LogValue(1);
        }

        /// <inheritdoc />
        public void ProcessRemoveFailed()
        {
            this.processRemoveFailed.LogValue(1);
        }

        /// <inheritdoc />
        public void ProcessUpdateCompleted()
        {
            this.processUpdateCompleted.LogValue(1);
        }

        /// <inheritdoc />
        public void ProcessUpdateFailed()
        {
            this.processUpdateFailed.LogValue(1);
        }

        /// <inheritdoc />
        public void ReportStatistics(ulong totalNodes, ulong totalDataSize)
        {
            this.totalNodes.LogValue((long)totalNodes);
            this.totalDataSize.LogValue((long)totalDataSize);
        }

        /// <inheritdoc />
        public void TransactionCommitted(long transactionId, long commitSequenceNumber)
        {
            this.transactionCommitted.LogValue(1);
            this.lastCommittedTransaction.LogValue(transactionId);
            this.lastCommitSequenceNumber.LogValue(commitSequenceNumber);
        }

        /// <inheritdoc />
        public void ChangeListCommitted(TimeSpan elapsed)
        {
            this.changeListCommitted.LogValue(1);
            this.changeListCommitTime.LogValue((long)elapsed.TotalMilliseconds);
        }

        /// <inheritdoc />
        public void ChangeListCommitFailed()
        {
            this.changeListCommitFailed.LogValue(1);
        }

        /// <inheritdoc />
        public void ChangeListAborted(TimeSpan elapsed)
        {
            this.changeListAborted.LogValue(1);
        }

        /// <inheritdoc />
        public void DataLoadCompleted(long dataCount, long duplicatesCount, long orphansCount)
        {
            this.dataLoaded.LogValue(dataCount);
            this.duplicatesFound.LogValue(duplicatesCount);
            this.orphansFound.LogValue(orphansCount);
        }
    }
}
