// <copyright file="ISecureConnectionPolicy.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface to a secure connection policy.
    /// </summary>
    public interface ISecureConnectionPolicy
    {
        Task<Stream> AuthenticateAsServer(TcpClient client, TimeSpan timeout, CancellationToken cancellationToken);

        Task<Stream> AuthenticateAsClient(string serverName, TcpClient client, TimeSpan timeout, CancellationToken cancellationToken);
    }
}
