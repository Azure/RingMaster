// <copyright file="RingMasterTimeStreamRequestHandler.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;

    /// <summary>
    /// An implementation of <see cref="IRingMasterRequestHandler"/> that stamps
    /// each request sent through it with a time stream id.
    /// </summary>
    internal sealed class RingMasterTimeStreamRequestHandler : IRingMasterRequestHandler
    {
        /// <summary>
        /// Id of the time stream.
        /// </summary>
        private readonly ulong timeStreamId;

        /// <summary>
        /// Request handler that will handle requests.
        /// </summary>
        private readonly IRingMasterRequestHandler handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="RingMasterTimeStreamRequestHandler"/> class.
        /// </summary>
        /// <param name="timeStreamId">Id of the time stream to associate with requests</param>
        /// <param name="handler">Underlying handler</param>
        public RingMasterTimeStreamRequestHandler(ulong timeStreamId, IRingMasterRequestHandler handler)
        {
            this.timeStreamId = timeStreamId;
            this.handler = handler;
        }

        /// <inheritdoc/>
        public int Timeout
        {
            get
            {
                return this.handler.Timeout;
            }

            set
            {
                this.handler.Timeout = value;
            }
        }

        /// <inheritdoc/>
        public Task<RequestResponse> Request(IRingMasterRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            request.TimeStreamId = this.timeStreamId;
            return this.handler.Request(request);
        }

        /// <inheritdoc/>
        public void Close()
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}