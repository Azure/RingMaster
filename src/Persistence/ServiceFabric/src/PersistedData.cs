// <copyright file="PersistedData.cs" company="Microsoft">
//   Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.WinFabPersistence
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence;

    using BasePersistedData = Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.PersistedData;

    /// <summary>
    /// PersistedData class is the type that is serialized to the <see cref="IReliableDictionary"/> managed by
    /// ServiceFabric.
    /// </summary>
    /// <remarks>
    /// This class has to be in the Microsoft.Azure.Networking.Infrastructure.RingMaster.WinFabPersistence namespace
    /// in the Microsoft.RingMaster.WinFabPersistence assembly to maintain backward compatibility.
    /// This typename and assembly name are recorded in the backing files maintained by ServiceFabric for the ReliableDictionary
    /// and when that dictionary is instantiated, it expects to find a type with the above name in an assembly with the above name.
    /// </remarks>
    public sealed class PersistedData
    {
        public PersistedData(BasePersistedData data)
        {
            this.Data = data;
        }

        /// <summary>
        /// Get the persisted data.
        /// </summary>
        public BasePersistedData Data { get; private set; }
    }
}
