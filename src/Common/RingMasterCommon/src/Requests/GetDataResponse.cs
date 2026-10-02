// <copyright file="GetDataResponse.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests
{
    using System;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Response for a GetData request.
    /// </summary>
    public class GetDataResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetDataResponse"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="userMetadata">The user metadata.</param>
        /// <param name="stat">The stat.</param>
        public GetDataResponse(byte[] data, byte[] userMetadata, IStat stat)
        {
            this.Data = data;
            this.UserMetadata = userMetadata;
            this.Stat = stat;
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Gets the user metadata.
        /// </summary>
        /// <value>
        /// The user metadata.
        /// </value>
        public byte[] UserMetadata { get; private set; }

        /// <summary>
        /// Gets the stat.
        /// </summary>
        /// <value>
        /// The stat.
        /// </value>
        public IStat Stat { get; private set; }

        /// <summary>
        /// To the get data response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>a GetDataResponse instance</returns>
        public static GetDataResponse ToGetDataResponse(RequestResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            var getDataResponse = response.Content as GetDataResponse;
            if (getDataResponse == null)
            {
                // This means the response is from an old server that not returning GetDataResponse.
                // The response.Content should instead be a byte array.
                getDataResponse = new GetDataResponse((byte[])response.Content, null, null);
            }

            getDataResponse.Stat = response.Stat;
            return getDataResponse;
        }
    }
}
