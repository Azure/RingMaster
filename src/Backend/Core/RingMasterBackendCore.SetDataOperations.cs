// <copyright file="RingMasterBackendCore.SetDataOperations.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;

    /// <summary>
    /// Backend core - SetData operations
    /// </summary>
    public partial class RingMasterBackendCore
    {
        /// <summary>
        /// SetData operations
        /// </summary>
        internal class SetDataOperations
        {
            private readonly RequestSetData req;
            private readonly IPersistedData prevData;
            private readonly SetDataOperationCode operation;
            private readonly long number;

            private SetDataOperations(RequestSetData req, IPersistedData prevData, SetDataOperationCode operation, long number)
            {
                this.req = req;
                this.prevData = prevData;
                this.operation = operation;
                this.number = number;
            }

            /// <summary>
            /// Creates an instance of <see cref="SetDataOperations"/> class
            /// </summary>
            /// <param name="prevData">Persisted data object</param>
            /// <param name="req">SetData request</param>
            /// <returns>instance of <see cref="SetDataOperations"/> class</returns>
            public static SetDataOperations TryCreate(IPersistedData prevData, RequestSetData req)
            {
                if (req == null)
                {
                    throw new ArgumentNullException(nameof(req));
                }

                SetDataOperationCode operation;
                long number;

                if (!SetDataOperationHelper.Instance.TryRead(req.Data, out operation, out number))
                {
                    return null;
                }

                return new SetDataOperations(req, prevData, operation, number);
            }

            /// <summary>
            /// Gets the request data as the byte array
            /// </summary>
            /// <returns>Request data</returns>
            public byte[] GetRequestData()
            {
                switch (this.operation)
                {
                    case SetDataOperationCode.InterlockedAddIfVersion:
                        {
                            byte[] data = this.prevData.Data;
                            if (data.Length != sizeof(long))
                            {
                                throw new InvalidOperationException("data in the node should be able to hold a long");
                            }

                            IoSession ios = new IoSession() { Buffer = data, MaxBytes = data.Length };
                            IoSession res_ios = new IoSession() { Buffer = new byte[data.Length], MaxBytes = data.Length };

                            long prevlong;
                            DataEncodingHelper.Read(ios, out prevlong);
                            prevlong += this.number;
                            ios.Pos = 0;
                            DataEncodingHelper.Write(prevlong, res_ios);

                            return res_ios.Buffer;
                        }

                    case SetDataOperationCode.InterlockedXORIfVersion:
                        {
                            byte[] data = this.prevData.Data;
                            if (data.Length != sizeof(long))
                            {
                                throw new InvalidOperationException("data in the node should be able to hold a long");
                            }

                            IoSession ios = new IoSession() { Buffer = data, MaxBytes = data.Length };
                            IoSession res_ios = new IoSession() { Buffer = new byte[data.Length], MaxBytes = data.Length };

                            long prevlong;
                            DataEncodingHelper.Read(ios, out prevlong);
                            prevlong ^= this.number;
                            ios.Pos = 0;
                            DataEncodingHelper.Write(prevlong, res_ios);

                            return res_ios.Buffer;
                        }

                    default:
                        throw new NotImplementedException("I don't understand operation " + this.operation);
                }
            }
        }
    }
}
