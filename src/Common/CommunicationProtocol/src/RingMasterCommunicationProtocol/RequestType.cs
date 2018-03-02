// <copyright file="RequestType.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    /// <summary>
    /// Class RequestType.
    /// </summary>
    public class RequestType
    {
        /// <summary>
        /// The request call
        /// </summary>
        public const uint RequestCall = 0xbeef0101;

        /// <summary>
        /// The request response
        /// </summary>
        public const uint RequestResponse = 0xbeef0102;
    }
}