// <copyright file="PersistedData.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.WinFabPersistence
{
    /// <summary>
    /// PersistedData class is the type that is serialized to the reliable dictionary managed by Service Fabric.
    /// </summary>
    /// <remarks>
    /// This class has to be in the Microsoft.Azure.Networking.Infrastructure.RingMaster.WinFabPersistence namespace
    /// in the Microsoft.RingMaster.WinFabPersistence assembly to maintain backward compatibility.
    /// This typename and assembly name are recorded in the backing files maintained by ServiceFabric for the ReliableDictionary
    /// and when that dictionary is instantiated, it expects to find a type with the above name in an assembly with the above name.
    /// </remarks>
    public sealed class PersistedData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistedData"/> class.
        /// </summary>
        /// <param name="data">persisted data object</param>
        public PersistedData(Persistence.PersistedData data)
        {
            this.Data = data;
        }

        /// <summary>
        /// Gets the persisted data.
        /// </summary>
        public Persistence.PersistedData Data { get; private set; }
    }
}
