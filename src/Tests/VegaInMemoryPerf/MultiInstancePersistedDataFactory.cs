// <copyright file="MultiInstancePersistedDataFactory.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence;

    /// <summary>
    /// Persisted data factory for multi-replica unit test
    /// </summary>
    internal sealed class MultiInstancePersistedDataFactory : AbstractPersistedDataFactory
    {
        /// <summary>
        /// Mapping from stateful service partition name to list of changes.
        /// </summary>
        private static readonly ConcurrentDictionary<string, List<Tuple<ChangeList.ChangeType, byte[]>>> MockPersistedStore =
            new ConcurrentDictionary<string, List<Tuple<ChangeList.ChangeType, byte[]>>>();

        private readonly string partitionName;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiInstancePersistedDataFactory"/> class.
        /// </summary>
        /// <param name="partitionName">Stateful partition name.</param>
        /// <param name="name">Name of the persisted data factory.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public MultiInstancePersistedDataFactory(
            string partitionName,
            string name,
            CancellationToken cancellationToken)
            : base(name, null, cancellationToken, true)
        {
            this.partitionName = partitionName;
            MockPersistedStore.TryAdd(partitionName, new List<Tuple<ChangeList.ChangeType, byte[]>>());
        }

        /// <inheritdoc/>
        protected override bool IsRetriable(Exception ex)
        {
            return false;
        }

        /// <inheritdoc />
        protected override void OnDeactivate()
        {
            // No op.
        }

        /// <inheritdoc />
        protected override async Task StartLoadingData(CancellationToken cancellation)
        {
            await Task.Delay(16).ConfigureAwait(false);

            var dataList = new Dictionary<ulong, PersistedData>();
            foreach (var d in MockPersistedStore[this.partitionName])
            {
                // Deserialize the data.
                using (var ms = new MemoryStream(d.Item2))
                {
                    using (var reader = new BinaryReader(ms))
                    {
                        var pd = new PersistedData(0);
                        pd.ReadFrom(reader);

                        switch (d.Item1)
                        {
                            case ChangeList.ChangeType.Add:
                                dataList.Add(pd.Id, pd);
                                break;

                            case ChangeList.ChangeType.Remove:
                                dataList.Remove(pd.Id);
                                break;

                            case ChangeList.ChangeType.Update:
                                dataList[pd.Id] = pd;
                                break;
                        }
                    }
                }
            }

            Trace.TraceInformation($"Loading data on partition {this.partitionName} - {this.Name}, {dataList.Count} data.");
            this.Load(dataList.Values, false);
        }

        /// <inheritdoc />
        protected override IReplication StartReplication(ulong id)
        {
            return new MultiInstanceSharedReplication(MockPersistedStore[this.partitionName], id);
        }

        private sealed class MultiInstanceSharedReplication : IReplication
        {
            private readonly List<Tuple<ChangeList.ChangeType, byte[]>> dataStore;
            private readonly List<Tuple<ChangeList.ChangeType, byte[]>> changes;

            /// <summary>
            /// Initializes a new instance of the <see cref="MultiInstanceSharedReplication"/> class.
            /// </summary>
            /// <param name="dataStore">Data store.</param>
            /// <param name="id">Replication ID.</param>
            public MultiInstanceSharedReplication(List<Tuple<ChangeList.ChangeType, byte[]>> dataStore, ulong id)
            {
                this.dataStore = dataStore;
                this.Id = id;
                this.changes = new List<Tuple<ChangeList.ChangeType, byte[]>>();
            }

            public ulong Id { get; private set; }

            public Task Add(PersistedData data)
            {
                this.changes.Add(Tuple.Create(ChangeList.ChangeType.Add, this.Serialize(data)));
                return Task.CompletedTask;
            }

            public Task Commit()
            {
                this.dataStore.AddRange(this.changes);
                ////Trace.TraceInformation($"Committed {this.changes.Count} changes.");
                return Task.CompletedTask;
            }

            public void Dispose()
            {
                // No op.
            }

            public Task Remove(PersistedData data)
            {
                this.changes.Add(Tuple.Create(ChangeList.ChangeType.Remove, this.Serialize(data)));
                return Task.CompletedTask;
            }

            public Task Update(PersistedData data)
            {
                this.changes.Add(Tuple.Create(ChangeList.ChangeType.Update, this.Serialize(data)));
                return Task.CompletedTask;
            }

            private byte[] Serialize(PersistedData pd)
            {
                using (var ms = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(ms))
                    {
                        pd.WriteTo(writer);

                        writer.Flush();
                        return ms.ToArray();
                    }
                }
            }
        }
    }
}
