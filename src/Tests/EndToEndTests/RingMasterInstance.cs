// <copyright file="RingMasterInstance.cs" company="Microsoft">
//     Copyright ©  2017
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.EndToEndTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.InMemory;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Represents an instance of RingMaster
    /// </summary>
    [TestClass]
    public sealed class RingMasterInstance : IDisposable
    {
        private readonly InMemoryFactory persistedDataFactory;
        private readonly RingMasterBackendCore backend;
        private readonly TaskCompletionSource<object> hasBackendStarted = new TaskCompletionSource<object>();

        static RingMasterInstance()
        {
            RingMasterBackendCore.GetSettingFunction = GetSetting;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterInstance"/> class.
        /// </summary>
        /// <param name="id">Unique Id of the instance</param>
        /// <param name="isPrimary">If <c>true</c> this instance is the primary</param>
        public RingMasterInstance(int id, bool isPrimary)
        {
            this.Id = id;
            Trace.TraceInformation($"RingMasterInstance id={this.Id}");

            this.persistedDataFactory = new InMemoryFactory(isPrimary, null, CancellationToken.None);
            this.backend = new RingMasterBackendCore(this.persistedDataFactory);
            this.persistedDataFactory.SetBackend(this.backend);

            this.backend.StartService = this.OnStartService;
            this.persistedDataFactory.OnFatalError = (message, exception) =>
            {
                Assert.Fail($"RingMasterInstance.FatalError: instanceId={id} message={message}, exception={exception}");
            };

            this.persistedDataFactory.OnChangeListCommitted = this.OnChangeListCommitted;
        }

        /// <summary>
        /// Gets or sets the maximum allowed node name length.
        /// </summary>
        public static int MaxNodeNameLength { get; set; } = 0;

        /// <summary>
        /// Gets or sets the maximum allowed node path length.
        /// </summary>
        public static int MaxNodePathLength { get; set; } = 0;

        /// <summary>
        /// Gets or sets the maximum allowed node data size.
        /// </summary>
        public static int MaxNodeDataSize { get; set; } = 0;

        /// <summary>
        /// Gets or sets the maximum number of results per get children request.
        /// </summary>
        public static int MaxGetChildrenEnumerationCount { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the minimum threshold for the number of nodes for which sorted dictionary will be used.
        /// </summary>
        public static int MinSortedDictionaryThreshold { get; set; } = 2000;

        /// <summary>
        /// Gets or sets the maximum threshold for the number of nodes after which sorted dictionary will be used.
        /// </summary>
        public static int MaxSortedDictionaryThreshold { get; set; } = 2500;

        /// <summary>
        /// Gets the unique id of this instance.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Gets the total number of nodes.
        /// </summary>
        public ulong TotalNodes => this.persistedDataFactory.TotalNodes;

        /// <summary>
        /// Gets a list of <see cref="ChangeLists"/> that have been committed in this instance.
        /// </summary>
        public List<InMemoryFactory.CommittedChangeList> CommittedChangeLists { get; } = new List<InMemoryFactory.CommittedChangeList>();

        /// <summary>
        /// Register the given <see cref="RingMasterInstance"/> as a secondary.
        /// </summary>
        /// <param name="secondary">The instance to register as secondary</param>
        public void RegisterSecondary(RingMasterInstance secondary)
        {
            if (secondary == null)
            {
                throw new ArgumentNullException(nameof(secondary));
            }

            this.persistedDataFactory.RegisterSecondary(secondary.persistedDataFactory);
        }

        /// <summary>
        /// Gets the descendants of the given node from the in-memory tree.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <returns>A <see cref="Task"/> that resolves to the list of descendant node paths</returns>
        public async Task<IList<string>> GetDescendantsFromMemory(string path)
        {
            var nodePaths = new List<string>();
            using (var ringMaster = this.Connect())
            {
                await ringMaster.ForEachDescendant(
                    path,
                    RingMasterInstance.MaxGetChildrenEnumerationCount,
                    childPath => nodePaths.Add(childPath));
            }

            Trace.TraceInformation($"RingMasterInstance.GetDescendantsFromMemory instanceId={this.Id}, path={path}, descendantCount={nodePaths.Count}");
            return nodePaths;
        }

        public IEnumerable<PersistedData> EnumerateFromSnapshot()
        {
            byte[] snapshot = this.GetSnapshot();

            using (var memoryStream = new MemoryStream(snapshot))
            {
                foreach (var data in this.persistedDataFactory.EnumerateFrom(memoryStream))
                {
                    yield return data;
                }
            }
        }

        /// <summary>
        /// Gets the descendants of the given node from a snapshot of the current state of this instance.
        /// </summary>
        /// <param name="path">Path to the node</param>
        /// <returns>A <see cref="Task"/> that resolves to the list of descendant node paths</returns>
        public async Task<IList<string>> GetDescendantsFromSnapshot(string path)
        {
            Trace.TraceInformation($"RingMasterInstance.GetDescendantsFromSnapshot instanceId={this.Id}, path={path}");

            byte[] snapshot = this.GetSnapshot();
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var nodes = new List<string>();
            using (var factory = new InMemoryFactory())
            {
                factory.LoadState = () =>
                {
                    using (var stream = new MemoryStream(snapshot))
                    {
                        factory.LoadFrom(stream);
                    }
                };

                factory.OnFatalError = (message, exception) =>
                {
                    Assert.Fail($"FatalError: message={message}, exception={exception}");
                };

                using (var backend = CreateBackend(factory))
                {
                    using (var ringMaster = new CoreRequestHandler(backend))
                    {
                        await ringMaster.ForEachDescendant(
                            path,
                            RingMasterInstance.MaxGetChildrenEnumerationCount,
                            descendantPath => nodes.Add(descendantPath));
                    }
                }
            }

            Trace.TraceInformation($"RingMasterInstance.GetDescendantsFromSnapshot-Completed instanceId={this.Id}, path={path}, descendantsCount={nodes.Count}");
            return nodes;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        /// <returns>A <see cref="Task"/> that tracks execution of this method</returns>
        public Task Start()
        {
            Trace.TraceInformation($"RingMasterInstance.Start id={this.Id}");
            this.backend.Start(CancellationToken.None);
            this.backend.OnBecomePrimary();
            return this.hasBackendStarted.Task;
        }

        /// <summary>
        /// Connects to this instance.
        /// </summary>
        /// <returns>A <see cref="IRingMasterRequestHandler"/> that can be used to interact with this instance</returns>
        public IRingMasterRequestHandler Connect()
        {
            return new CoreRequestHandler(this.backend);
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            this.backend.Dispose();
            this.persistedDataFactory.Dispose();
        }

        private static string GetSetting(string settingName)
        {
            switch (settingName)
            {
                case "RingMaster.DontStartPseudoNodes":
                    return "false";
                case "RslCommandReplication.ParanoiaChildrenValidation":
                    return "true";
                case "RslCommandReplication.AllowAutoFixing":
                    return "false";
                case "RingMasterLimits.MaxGetChildrenEnumerationCount":
                    return MaxGetChildrenEnumerationCount.ToString();
                case "RingMasterLimits.MaxNodeNameLength":
                    return MaxNodeNameLength.ToString();
                case "RingMasterLimits.MaxNodePathLength":
                    return MaxNodePathLength.ToString();
                case "RingMasterLimits.MaxNodeDataSize":
                    return MaxNodeDataSize.ToString();
                case "RingMasterLimits.MaxSortedDictionaryThreshold":
                    return MaxSortedDictionaryThreshold.ToString();
                case "RingMasterLimits.MinSortedDictionaryThreshold":
                    return MinSortedDictionaryThreshold.ToString();
            }

            return null;
        }

        private static RingMasterBackendCore CreateBackend(InMemoryFactory persistedDataFactory)
        {
            var backendStarted = new ManualResetEventSlim();

            RingMasterBackendCore.GetSettingFunction = GetSetting;
            RingMasterBackendCore backend = null;

            try
            {
                backend = new RingMasterBackendCore(persistedDataFactory);

                backend.StartService = (p1, p2) => { backendStarted.Set(); };
                backend.Start(CancellationToken.None);
                backend.OnBecomePrimary();

                backendStarted.Wait();
                RingMasterBackendCore backendReturn = backend;
                backend = null;
                return backendReturn;
            }
            finally
            {
                if (backend != null)
                {
                    backend.Dispose();
                }
            }
        }

        private void OnStartService(bool mustStartMainEndpointOnSecondary, bool mustStartExtraEndpointOnSecondary)
        {
            Trace.TraceInformation($"RingMasterInstance.OnStartService id={this.Id}, mustStartMainEndpointOnSecondary={mustStartMainEndpointOnSecondary}, mustStartExtraEndpointOnSecondary={mustStartExtraEndpointOnSecondary}");
            this.hasBackendStarted.SetResult(null);
        }

        private void OnChangeListCommitted(InMemoryFactory.CommittedChangeList changeList)
        {
            Trace.TraceInformation($"ChangeList Committed changeListId={changeList.Id}, changeCount={changeList.Changes.Count}");
            this.CommittedChangeLists.Add(changeList);
            foreach (var change in changeList.Changes)
            {
                Trace.TraceInformation($"Change type={change.ChangeType} id={change.Data.Id}, name={change.Data.Name}, parentId={change.Data.ParentId}");
            }
        }

        private byte[] GetSnapshot()
        {
            using (var stream = new MemoryStream())
            {
                this.persistedDataFactory.SaveTo(stream);
                return stream.GetBuffer();
            }
        }
    }
}
