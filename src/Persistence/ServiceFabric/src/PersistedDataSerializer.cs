// <copyright file="PersistedDataSerializer.cs" company="Microsoft">
//   Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.ServiceFabric
{
    using System;
    using System.IO;
    using Microsoft.ServiceFabric.Data;

    using PersistedData = Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.PersistedData;
    using WinFabPersistedData = Microsoft.Azure.Networking.Infrastructure.RingMaster.WinFabPersistence.PersistedData;

    /// <summary>
    /// Custom serializer for <see cref="PersistedData"/>.
    /// </summary>
    internal sealed class PersistedDataSerializer : IStateSerializer<WinFabPersistedData>
    {
        internal PersistedDataFactory Factory { get; set; }

        /// <summary>
        /// Deserializes a <see cref="PersistedData"/> from the given <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="binaryReader">The <see cref="BinaryReader"/> to deserialize from</param>
        /// <returns>The deserialized <see cref="PersistedData"/></returns>
        public WinFabPersistedData Read(BinaryReader binaryReader)
        {
            if (binaryReader == null)
            {
                throw new ArgumentNullException(nameof(binaryReader));
            }

            PersistedData pd = new PersistedData(0, this.Factory);
            pd.ReadFrom(binaryReader);
            ServiceFabricPersistenceEventSource.Log.PersistedDataSerializer_Read(pd.Id, pd.Name);

            return new WinFabPersistedData(pd);
        }

        /// <summary>
        /// Serializes a <see cref="PersistedData"/> and writes it to the given <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        /// <param name="binaryWriter">The <see cref="BinaryWriter"/> to serialize to</param>
        public void Write(WinFabPersistedData value, BinaryWriter binaryWriter)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (binaryWriter == null)
            {
                throw new ArgumentNullException(nameof(binaryWriter));
            }

            value.Data.WriteTo(binaryWriter);

            ServiceFabricPersistenceEventSource.Log.PersistedDataSerializer_Write(value.Data.Id, value.Data.Name);
        }

        /// <summary>
        /// Deserializes <see cref="PersistedData"/> from the given <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="baseValue">The base value for the deserialization</param>
        /// <param name="binaryReader">The <see cref="BinaryReader"/> to deserialize from</param>
        /// <returns>The deserialized <see cref="PersistedData"/></returns>
        public WinFabPersistedData Read(WinFabPersistedData baseValue, BinaryReader binaryReader)
        {
            WinFabPersistedData pd = this.Read(binaryReader);
            if (baseValue != null)
            {
                ServiceFabricPersistenceEventSource.Log.PersistedDataSerializer_ReadDifferential(baseValue.Data.Id, baseValue.Data.Name, pd.Data.Id, pd.Data.Name);
            }

            return pd;
        }

        /// <summary>
        /// Serializes the given <see cref="PersistedData"/> and writes it to the given <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="baseValue">The base value for the serialization</param>
        /// <param name="targetValue">The value to serialize</param>
        /// <param name="binaryWriter">The <see cref="BinaryWriter"/> to serialize to</param>
        public void Write(WinFabPersistedData baseValue, WinFabPersistedData targetValue, BinaryWriter binaryWriter)
        {
            if (baseValue != null && targetValue != null)
            {
                ServiceFabricPersistenceEventSource.Log.PersistedDataSerializer_WriteDifferential(baseValue.Data.Id, baseValue.Data.Name, targetValue.Data.Id, targetValue.Data.Name);
            }

            this.Write(targetValue, binaryWriter);
        }
    }
}
