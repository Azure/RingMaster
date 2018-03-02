// <copyright file="SetDataOperation.cs" company="Microsoft">
//     Copyright ©  2017
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    public sealed class SetDataOperation : ISetDataOperation
    {
        private byte[] bytes;

        public SetDataOperation(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public byte[] RawData
        {
            get
            {
                return this.bytes;
            }
        }
    }
}