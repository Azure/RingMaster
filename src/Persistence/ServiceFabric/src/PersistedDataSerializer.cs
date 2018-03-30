// <copyright file="PersistedDataSerializer.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.ServiceFabric
{
    using System;
    using System.IO;
    using Microsoft.ServiceFabric.Data;

    /// <summary>
    /// Custom serializer for <see cref="WinFabPersistence.PersistedData"/>.
    /// </summary>
    internal sealed class PersistedDataSerializer : IStateSerializer<WinFabPersistence.PersistedData>
    {
        /// <summary>
        /// Gets or sets the persisted data factory instance
        /// </summary>
        internal PersistedDataFactory Factory { get; set; }

        /// <summary>
        /// Deserializes a <see cref="WinFabPersistence.PersistedData"/> from the given <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="binaryReader">The <see cref="BinaryReader"/> to deserialize from</param>
        /// <returns>The deserialized <see cref="WinFabPersistence.PersistedData"/></returns>
        public WinFabPersistence.PersistedData Read(BinaryReader binaryReader)
        {
            if (binaryReader == null)
            {
                throw new ArgumentNullException(nameof(binaryReader));
            }

            var pd = new PersistedData(0, this.Factory);
            pd.ReadFrom(binaryReader);
            ServiceFabricPersistenceEventSource.Log.PersistedDataSerializer_Read(pd.Id, pd.Name);

            return new WinFabPersistence.PersistedData(pd);
        }

        /// <summary>
        /// Serializes a <see cref="Persistence.PersistedData"/> and writes it to the given <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="value">The value to serialize</param>
        /// <param name="binaryWriter">The <see cref="BinaryWriter"/> to serialize to</param>
        public void Write(WinFabPersistence.PersistedData value, BinaryWriter binaryWriter)
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
        /// Deserializes <see cref="WinFabPersistence.PersistedData"/> from the given <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="baseValue">The base value for the deserialization</param>
        /// <param name="binaryReader">The <see cref="BinaryReader"/> to deserialize from</param>
        /// <returns>The deserialized <see cref="WinFabPersistence.PersistedData"/></returns>
        public WinFabPersistence.PersistedData Read(WinFabPersistence.PersistedData baseValue, BinaryReader binaryReader)
        {
            var pd = this.Read(binaryReader);
            if (baseValue != null)
            {
                ServiceFabricPersistenceEventSource.Log.PersistedDataSerializer_ReadDifferential(baseValue.Data.Id, baseValue.Data.Name, pd.Data.Id, pd.Data.Name);
            }

            return pd;
        }

        /// <summary>
        /// Serializes the given <see cref="WinFabPersistence.PersistedData"/> and writes it to the given <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="baseValue">The base value for the serialization</param>
        /// <param name="targetValue">The value to serialize</param>
        /// <param name="binaryWriter">The <see cref="BinaryWriter"/> to serialize to</param>
        public void Write(WinFabPersistence.PersistedData baseValue, WinFabPersistence.PersistedData targetValue, BinaryWriter binaryWriter)
        {
            if (baseValue != null && targetValue != null)
            {
                ServiceFabricPersistenceEventSource.Log.PersistedDataSerializer_WriteDifferential(baseValue.Data.Id, baseValue.Data.Name, targetValue.Data.Id, targetValue.Data.Name);
            }

            this.Write(targetValue, binaryWriter);
        }
    }
}
