// <copyright file="SystemInfo.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// The system info
    /// </summary>
    public class SystemInfo
    {
        /// <summary>
        /// The system information path
        /// </summary>
        public const string SystemInfoPath = "$systeminfo";

        /// <summary>
        /// Gets or sets the responded request count.
        /// </summary>
        private long respondedRequestCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemInfo"/> class.
        /// </summary>
        public SystemInfo()
        {
            this.InitErrorCodeDistribution();
            this.InitRequestTypeDistribution();
        }

        /// <summary>
        /// Gets or sets the responded request count.
        /// </summary>
        /// <value>
        /// The responded request count.
        /// </value>
        public long RespondedRequestCount
        {
            get
            {
                return this.respondedRequestCount;
            }

            set
            {
                this.respondedRequestCount = value;
            }
        }

        /// <summary>
        /// Gets the error code distribution.
        /// </summary>
        /// <value>
        /// The error code distribution.
        /// </value>
        public ConcurrentDictionary<int, long> ErrorCodeDistribution { get; private set; } = new ConcurrentDictionary<int, long>();

        /// <summary>
        /// Gets the received request distribution.
        /// </summary>
        /// <value>
        /// The received request distribution.
        /// </value>
        public ConcurrentDictionary<ushort, long> ReceivedRequestDistribution { get; private set; } = new ConcurrentDictionary<ushort, long>();

        /// <summary>
        /// Determines whether [is system information node] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        ///   <c>true</c> if [is system information node] [the specified path]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsSystemInfoRequest(string path)
        {
            return path == SystemInfoPath;
        }

        /// <summary>
        /// Deserializes the specified bytes.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns>system info object</returns>
        public static SystemInfo Deserialize(byte[] bytes)
        {
            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream(bytes);
                using (var binaryReader = new BinaryReader(ms))
                {
                    ms = null;
                    var systemInfo = new SystemInfo();

                    int dictionarySize = binaryReader.ReadInt32();
                    var requestDictionary = new ConcurrentDictionary<ushort, long>();
                    for (int i = 0; i < dictionarySize; i++)
                    {
                        ushort key = binaryReader.ReadUInt16();
                        requestDictionary[key] = binaryReader.ReadInt64();
                    }

                    systemInfo.ReceivedRequestDistribution = requestDictionary;
                    systemInfo.RespondedRequestCount = binaryReader.ReadInt64();

                    dictionarySize = binaryReader.ReadInt32();
                    var dictionary = new ConcurrentDictionary<int, long>();
                    for (int i = 0; i < dictionarySize; i++)
                    {
                        int key = binaryReader.ReadInt32();
                        dictionary[key] = binaryReader.ReadInt64();
                    }

                    systemInfo.ErrorCodeDistribution = dictionary;

                    return systemInfo;
                }
            }
            finally
            {
                ms?.Dispose();
            }
        }

        /// <summary>
        /// Gets all request count.
        /// </summary>
        /// <returns>get total number of received requests</returns>
        public long GetAllRequestCount()
        {
            var requestDistribution = this.ReceivedRequestDistribution.ToArray();

            return requestDistribution.Sum(p => p.Value);
        }

        /// <summary>
        /// Adds to received request distribution.
        /// </summary>
        /// <param name="request">The request.</param>
        public void AddToReceivedRequestDistribution(IRingMasterRequest request)
        {
            if (request == null)
            {
                return;
            }

            this.ReceivedRequestDistribution.AddOrUpdate((ushort)request.RequestType, 1, (key, oldValue) => oldValue + 1);
        }

        /// <summary>
        /// Increments the responded request count.
        /// </summary>
        public void IncrementRespondedRequestCount()
        {
            Interlocked.Increment(ref this.respondedRequestCount);
        }

        /// <summary>
        /// Adds to error distribution.
        /// </summary>
        /// <param name="response">The response.</param>
        public void AddToErrorDistribution(RequestResponse response)
        {
            if (response == null)
            {
                return;
            }

            this.ErrorCodeDistribution.AddOrUpdate(response.ResultCode, 1, (key, oldValue) => oldValue + 1);
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref this.respondedRequestCount, 0);

            this.InitRequestTypeDistribution();
            this.InitErrorCodeDistribution();
        }

        /// <summary>
        /// Serializes this instance.
        /// </summary>
        /// <returns>serialized system info</returns>
        public byte[] Serialize()
        {
            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream();
                BinaryWriter binaryWriter = new BinaryWriter(ms);
                try
                {
                    var requestDistribution = this.ReceivedRequestDistribution.ToArray();
                    binaryWriter.Write(requestDistribution.Length);
                    foreach (var err in requestDistribution)
                    {
                        binaryWriter.Write(err.Key);
                        binaryWriter.Write(err.Value);
                    }

                    binaryWriter.Write(Interlocked.Read(ref this.respondedRequestCount));

                    var errorDistribution = this.ErrorCodeDistribution.ToArray();
                    binaryWriter.Write(errorDistribution.Length);
                    foreach (var err in errorDistribution)
                    {
                        binaryWriter.Write(err.Key);
                        binaryWriter.Write(err.Value);
                    }

                    return ms.ToArray();
                }
                finally
                {
                    binaryWriter?.Dispose();
                    ms = null;
                }
            }
            finally
            {
                ms?.Dispose();
            }
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Request Type Distribution:");
            foreach (var item in this.ReceivedRequestDistribution)
            {
                if (item.Value != 0)
                {
                    sb.AppendLine($"{(RingMasterRequestType)item.Key}: {item.Value}");
                }
            }

            sb.AppendLine($"Responded: {this.respondedRequestCount}");
            sb.AppendLine("Error Distribution:");

            foreach (var item in this.ErrorCodeDistribution)
            {
                if (item.Value != 0)
                {
                    sb.AppendLine($"{(RingMasterException.Code)item.Key}: {item.Value}");
                }
            }

            return sb.ToString();
        }

        private void InitErrorCodeDistribution()
        {
            var resultCodes = (int[])Enum.GetValues(typeof(RingMasterException.Code));
            foreach (var code in resultCodes)
            {
                if (code != (int)RingMasterException.Code.Ok)
                {
                    this.ErrorCodeDistribution[code] = 0;
                }
            }
        }

        private void InitRequestTypeDistribution()
        {
            var requestTypes = (ushort[])Enum.GetValues(typeof(RingMasterRequestType));
            foreach (var code in requestTypes)
            {
                this.ReceivedRequestDistribution[code] = 0;
            }
        }
    }
}