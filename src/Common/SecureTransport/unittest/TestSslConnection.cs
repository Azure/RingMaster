// <copyright file="TestSslConnection.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.SecureTransportUnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Unit tests for the SslConnection class.
    /// </summary>
    /// <remarks>
    /// FluentAssertions are documented here: https://github.com/dennisdoomen/fluentassertions/wiki
    /// </remarks>
    [TestClass]
    public class TestSslConnection
    {
        private const string TestCertPrefix = "SecureTransportTestCert";

        [AssemblyInitialize]
        public static void InstallCerts(TestContext context)
        {
            Action<string> log = null;
            if (context == null)
            {
                log = Console.WriteLine;
            }
            else
            {
                log = s => context.WriteLine(s);
            }

            try
            {
                Process.Start("makecert.exe", $"-n \"CN={TestCertPrefix}1\" -r -pe -ss My -sr CurrentUser -a sha1")
                    .WaitForExit();
                Process.Start("makecert.exe", $"-n \"CN={TestCertPrefix}2\" -r -pe -ss My -sr CurrentUser -a sha1")
                    .WaitForExit();

                log($"Created {TestCertPrefix}1 and {TestCertPrefix}2 in CurrentUser\\My");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to run makecert.exe: {ex}");
                throw;
            }
        }

        [AssemblyCleanup]
        public static void RemoveCerts()
        {
            // Use other store locations if your certificate is not in the current user store.
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite | OpenFlags.IncludeArchived);

            foreach (var cert in store.Certificates)
            {
                if (cert.SubjectName.Name.Contains(TestCertPrefix))
                {
                    Console.Out.WriteLine("Removing cert: {0}", cert.SubjectName.Name);

                    // Remove the certificate
                    store.Remove(cert);
                }
            }

            store.Close();
        }

        /// <summary>
        /// Verify that <see cref="ArgumentNullException"/> is thrown if null is passed to the serverCertificates parameter.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport.SslConnection", Justification = "SSLConnection connection constructor is expected to throw")]
        public void TestEnsureServerCertificatesAreProvided()
        {
            var configuration = new SslConnection.Configuration()
            {
                ClientCertificates = null,
                ServerCertificates = null
            };

            Action createSslConnection = () => new SslConnection(0, configuration);
            createSslConnection.ShouldThrow<ArgumentException>();
        }

        /// <summary>
        /// Verify that <see cref="ArgumentException"/> is thrown if an empty array is passed to the serverCertificates parameter..
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport.SslConnection", Justification = "SSLConnection connection constructor is expected to throw")]
        public void TestEnsureServerCertificatesListIsNotEmpty()
        {
            var configuration = new SslConnection.Configuration()
            {
                ClientCertificates = null,
                ServerCertificates = new X509Certificate[0]
            };

            Action createSslConnection = () => new SslConnection(0, configuration);
            createSslConnection.ShouldThrow<ArgumentException>();
        }

        /// <summary>
        /// Verify that <see cref="ArgumentException"/> is thrown if an array with null is passed to the serverCertificates parameter..
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport.SslConnection", Justification = "SSLConnection connection constructor is expected to throw")]
        public void TestEnsureServerCertificatesListIsValid()
        {
            var configuration = new SslConnection.Configuration()
            {
                ClientCertificates = null,
                ServerCertificates = new X509Certificate[1] { null }
            };

            Action createSslConnection = () => new SslConnection(0, configuration);
            createSslConnection.ShouldThrow<ArgumentException>();
        }

        /// <summary>
        /// Verify that <see cref="ArgumentException"/> is thrown if an array with null is passed to the clientCertificates parameter..
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport.SslConnection", Justification = "SSLConnection connection constructor is expected to throw")]
        public void TestEnsureClientCertificatesListIsValid()
        {
            var certificates = GetLocalCertificates(1);
            var configuration = new SslConnection.Configuration()
            {
                ClientCertificates = new X509Certificate[1] { null },
                ServerCertificates = new X509Certificate[1] { certificates[0] }
            };

            Action createSslConnection = () => new SslConnection(0, configuration);
            createSslConnection.ShouldThrow<ArgumentException>();
        }

        /// <summary>
        /// Verify that authentication succeeds if the server and client certificates to expect
        /// are configured correctly at both ends.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestSuccessfulAuthentication()
        {
            var certificates = GetLocalCertificates(2);

            X509Certificate[] clientCertificates = new X509Certificate[] { certificates[0] };
            X509Certificate[] serverCertificates = new X509Certificate[] { certificates[1] };

            int port = TestSecureTransport.GetAvailablePort(10000);
            var serverAcceptedClient = new ManualResetEventSlim();
            var clientConnected = new ManualResetEventSlim();

            using (var server = CreateTransport(clientCertificates, serverCertificates))
            using (var client = CreateTransport(clientCertificates, serverCertificates))
            {
                server.OnNewConnection = _ => serverAcceptedClient.Set();
                client.OnNewConnection = _ => clientConnected.Set();
                server.StartServer(port);
                client.StartClient(new IPEndPoint(IPAddress.Loopback, port));

                serverAcceptedClient.Wait(30000).Should().BeTrue();
                clientConnected.Wait(30000).Should().BeTrue();
            }
        }

        /// <summary>
        /// Verify that authentication can succeed if the server does not require any client certificates and
        /// the client provides no certificates.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestNoClientCertificate()
        {
            var certificates = GetLocalCertificates(1);

            X509Certificate[] serverCertificates = new X509Certificate[] { certificates[0] };

            int port = TestSecureTransport.GetAvailablePort(10000);
            Task<bool> serverTask = this.AcceptClient(null, serverCertificates, port);
            Task<bool> clientTask = this.ConnectToServer(null, serverCertificates, port);

            // Client task will not succeed because the test server certificate is self-signed
            // and trust chain validation fails
            clientTask.Result.Should().BeFalse();

            serverTask.Result.Should().BeTrue();
        }

        /// <summary>
        /// Verify that authentication can succeed if the server does not require any client certificates.
        /// Even if the client provides some certificates, they will be ignored.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestIgnoreClientCertificate()
        {
            var certificates = GetLocalCertificates(2);

            X509Certificate[] clientCertificates = new X509Certificate[] { certificates[0] };
            X509Certificate[] serverCertificates = new X509Certificate[] { certificates[1] };

            int port = TestSecureTransport.GetAvailablePort(10000);
            Task<bool> serverTask = this.AcceptClient(null, serverCertificates, port);
            Task<bool> clientTask = this.ConnectToServer(clientCertificates, serverCertificates, port);

            // Client task will not succeed because the test server certificate is self-signed
            // and trust chain validation fails
            clientTask.Result.Should().BeFalse();

            serverTask.Result.Should().BeTrue();
        }

        /// <summary>
        /// Verify that certificates that do not pass trust chain validation are not accepted
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestBadTrustChainCertificates()
        {
            var certificates = GetLocalCertificates(2);

            X509Certificate[] clientCertificates = new X509Certificate[] { certificates[0] };
            X509Certificate[] serverCertificates = new X509Certificate[] { certificates[1] };

            int port = TestSecureTransport.GetAvailablePort(10000);
            Task<bool> serverTask = this.AcceptClient(clientCertificates, serverCertificates, port);
            Task<bool> clientTask = this.ConnectToServer(clientCertificates, serverCertificates, port);

            // Server should not accept the connection because the client certificate is self-signed and trust chain validation fails
            serverTask.Result.Should().BeFalse();

            // Client should not accept the connection because server certificate is self-signed and trust chain validation fails
            clientTask.Result.Should().BeFalse();
        }

        /// <summary>
        /// Verify that AuthenticateAsServer observes the cancellation token.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCancelAuthenticateAsServer()
        {
            var certificates = GetLocalCertificates(2);

            X509Certificate[] clientCertificates = new X509Certificate[] { certificates[0] };
            X509Certificate[] serverCertificates = new X509Certificate[] { certificates[1] };

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();

                int port = TestSecureTransport.GetAvailablePort(10000);
                Task<bool> serverTask = this.AcceptClient(clientCertificates, serverCertificates, port, Timeout.InfiniteTimeSpan, cancellationTokenSource.Token);
                Task<bool> clientTask = this.ConnectToServer(clientCertificates, serverCertificates, port);

                Action waitForBoth = () => Task.WaitAll(serverTask, clientTask);

                waitForBoth.ShouldThrow<AggregateException>();

                // Server should throw a SecureTransportException because the cancellation token has been cancelled.
                serverTask.IsFaulted.Should().BeTrue();
                serverTask.Exception.InnerException.Should().BeOfType<SecureTransportException>();
            }
        }

        /// <summary>
        /// Verify that AuthenticateAsServer honors timeout.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestTimeoutAuthenticateAsServer()
        {
            var certificates = GetLocalCertificates(2);

            X509Certificate[] clientCertificates = new X509Certificate[] { certificates[0] };
            X509Certificate[] serverCertificates = new X509Certificate[] { certificates[1] };

            int port = TestSecureTransport.GetAvailablePort(10000);
            Task<bool> serverTask = this.AcceptClient(clientCertificates, serverCertificates, port, TimeSpan.Zero, CancellationToken.None);
            Task<bool> clientTask = this.ConnectToServer(clientCertificates, serverCertificates, port);

            Action waitForBoth = () => Task.WaitAll(serverTask, clientTask);

            waitForBoth.ShouldThrow<AggregateException>();

            // Server should throw a SecureTransportException because the timeout expired.
            serverTask.IsFaulted.Should().BeTrue();
            serverTask.Exception.InnerException.Should().BeOfType<SecureTransportException>();
        }

        /// <summary>
        /// Verify that AuthenticateAsClient observes the cancellation token.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestCancelAuthenticateAsClient()
        {
            var certificates = GetLocalCertificates(2);

            X509Certificate[] clientCertificates = new X509Certificate[] { certificates[0] };
            X509Certificate[] serverCertificates = new X509Certificate[] { certificates[1] };

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();

                int port = TestSecureTransport.GetAvailablePort(10000);
                Task<bool> serverTask = this.AcceptClient(clientCertificates, serverCertificates, port);
                Task<bool> clientTask = this.ConnectToServer(clientCertificates, serverCertificates, port, Timeout.InfiniteTimeSpan, cancellationTokenSource.Token);

                Action waitForBoth = () => Task.WaitAll(serverTask, clientTask);

                waitForBoth.ShouldThrow<AggregateException>();

                // Client should throw a SecureTransportException because the cancellation token has been cancelled.
                clientTask.IsFaulted.Should().BeTrue();
                clientTask.Exception.InnerException.Should().BeOfType<SecureTransportException>();
            }
        }

        /// <summary>
        /// Verify that AuthenticateAsClient honors timeout.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestTimeoutAuthenticateAsClient()
        {
            var certificates = GetLocalCertificates(2);

            X509Certificate[] clientCertificates = new X509Certificate[] { certificates[0] };
            X509Certificate[] serverCertificates = new X509Certificate[] { certificates[1] };

            int port = TestSecureTransport.GetAvailablePort(10000);
            Task<bool> serverTask = this.AcceptClient(clientCertificates, serverCertificates, port);
            Task<bool> clientTask = this.ConnectToServer(clientCertificates, serverCertificates, port, TimeSpan.Zero, CancellationToken.None);

            Action waitForBoth = () => Task.WaitAll(serverTask, clientTask);

            waitForBoth.ShouldThrow<AggregateException>();

            // Client should throw a SecureTransportException because the timeout expired.
            clientTask.IsFaulted.Should().BeTrue();
            clientTask.Exception.InnerException.Should().BeOfType<SecureTransportException>();
        }

        /// <summary>
        /// Verify that if a certificate validation callback is provided to validate the client certificate,
        /// it is invoked before the server accepts the connection.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestClientCertificateValidationCallback()
        {
            var certificates = GetLocalCertificates(2);

            X509Certificate[] clientCertificates = new X509Certificate[] { certificates[0] };
            X509Certificate[] serverCertificates = new X509Certificate[] { certificates[1] };

            int port = TestSecureTransport.GetAvailablePort(10000);
            var serverAcceptedClient = new ManualResetEventSlim();
            var clientConnected = new ManualResetEventSlim();
            var clientCertificateValidationCallbackCalled = new ManualResetEventSlim();

            var configuration = new SecureTransport.Configuration()
            {
                UseSecureConnection = true,
                ClientCertificates = clientCertificates,
                ServerCertificates = serverCertificates,
                CommunicationProtocolVersion = 1,
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    clientCertificateValidationCallbackCalled.Set();
                    return true;
                }
            };

            using (var server = new SecureTransport(configuration, null, CancellationToken.None))
            using (var client = CreateTransport(clientCertificates, serverCertificates))
            {
                server.OnNewConnection = _ => serverAcceptedClient.Set();
                client.OnNewConnection = _ => clientConnected.Set();
                server.StartServer(port);
                client.StartClient(new IPEndPoint(IPAddress.Loopback, port));

                // Client certificate validation callback must be called before
                // the server accepts the connection.
                clientCertificateValidationCallbackCalled.Wait(3000).Should().BeTrue();
                serverAcceptedClient.Wait(30000).Should().BeTrue();
                clientConnected.Wait(30000).Should().BeTrue();
            }
        }

        /// <summary>
        /// Verify that if a certificate validation callback is provided to validate the server certificate,
        /// it is invoked before the client accepts the connection.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestServerCertificateValidationCallback()
        {
            var certificates = GetLocalCertificates(2);

            X509Certificate[] clientCertificates = new X509Certificate[] { certificates[0] };
            X509Certificate[] serverCertificates = new X509Certificate[] { certificates[1] };

            int port = TestSecureTransport.GetAvailablePort(10000);
            var serverAcceptedClient = new ManualResetEventSlim();
            var clientConnected = new ManualResetEventSlim();
            var serverCertificateValidationCallbackCalled = new ManualResetEventSlim();

            var configuration = new SecureTransport.Configuration()
            {
                UseSecureConnection = true,
                ClientCertificates = clientCertificates,
                ServerCertificates = serverCertificates,
                CommunicationProtocolVersion = 1,
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    serverCertificateValidationCallbackCalled.Set();
                    return true;
                }
            };

            using (var server = CreateTransport(clientCertificates, serverCertificates))
            using (var client = new SecureTransport(configuration, null, CancellationToken.None))
            {
                server.OnNewConnection = _ => serverAcceptedClient.Set();
                client.OnNewConnection = _ => clientConnected.Set();
                server.StartServer(port);
                client.StartClient(new IPEndPoint(IPAddress.Loopback, port));

                serverAcceptedClient.Wait(30000).Should().BeTrue();

                // Client certificate validation callback must be called before
                // the server accepts the connection.
                serverCertificateValidationCallbackCalled.Wait(3000).Should().BeTrue();
                clientConnected.Wait(30000).Should().BeTrue();
            }
        }

        /// <summary>
        /// Verify that if a certificate selection callback is provided to select the client certificate,
        /// it is invoked before the client accepts the connection.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestClientCertificateSelectionCallback()
        {
            var certificates = GetLocalCertificates(2);

            X509Certificate[] clientCertificates = new X509Certificate[] { certificates[0] };
            X509Certificate[] serverCertificates = new X509Certificate[] { certificates[1] };

            int port = TestSecureTransport.GetAvailablePort(10000);
            var serverAcceptedClient = new ManualResetEventSlim();
            var clientConnected = new ManualResetEventSlim();
            var clientCertificateSelectionCallbackCalled = new ManualResetEventSlim();

            var configuration = new SecureTransport.Configuration()
            {
                UseSecureConnection = true,
                ClientCertificates = certificates,
                ServerCertificates = serverCertificates,
                CommunicationProtocolVersion = 1,
                MustCheckCertificateTrustChain = false,
                LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) =>
                {
                    clientCertificateSelectionCallbackCalled.Set();
                    return certificates[0];
                }
            };

            using (var server = CreateTransport(clientCertificates, serverCertificates))
            using (var client = new SecureTransport(configuration, null, CancellationToken.None))
            {
                server.OnNewConnection = _ => serverAcceptedClient.Set();
                client.OnNewConnection = _ => clientConnected.Set();
                server.StartServer(port);
                client.StartClient(new IPEndPoint(IPAddress.Loopback, port));

                serverAcceptedClient.Wait(30000).Should().BeTrue();

                // Client certificate selection callback must be called before
                // the client accepts the connection.
                clientCertificateSelectionCallbackCalled.Wait(3000).Should().BeTrue();
                clientConnected.Wait(30000).Should().BeTrue();
            }
        }

        /// <summary>
        /// Verify that if a certificate selection callback is provided to select the server certificate,
        /// it is invoked before the server accepts the connection.
        /// </summary>
        [TestMethod]
        [Timeout(30000)]
        public void TestServerCertificateSelectionCallback()
        {
            var certificates = GetLocalCertificates(2);

            X509Certificate[] clientCertificates = new X509Certificate[] { certificates[0] };
            X509Certificate[] serverCertificates = new X509Certificate[] { certificates[1] };

            int port = TestSecureTransport.GetAvailablePort(10000);
            var serverAcceptedClient = new ManualResetEventSlim();
            var clientConnected = new ManualResetEventSlim();
            var serverCertificateSelectionCallbackCalled = new ManualResetEventSlim();

            var configuration = new SecureTransport.Configuration()
            {
                UseSecureConnection = true,
                ClientCertificates = certificates,
                ServerCertificates = serverCertificates,
                CommunicationProtocolVersion = 1,
                MustCheckCertificateTrustChain = false,
                LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) =>
                {
                    serverCertificateSelectionCallbackCalled.Set();
                    return certificates[1];
                }
            };

            using (var server = new SecureTransport(configuration, null, CancellationToken.None))
            using (var client = CreateTransport(clientCertificates, serverCertificates))
            {
                server.OnNewConnection = _ => serverAcceptedClient.Set();
                client.OnNewConnection = _ => clientConnected.Set();
                server.StartServer(port);
                client.StartClient(new IPEndPoint(IPAddress.Loopback, port));

                // Server certificate selection callback must be called before
                // the server accepts the connection.
                serverCertificateSelectionCallbackCalled.Wait(3000).Should().BeTrue();
                serverAcceptedClient.Wait(30000).Should().BeTrue();
                clientConnected.Wait(30000).Should().BeTrue();
            }
        }

        /// <summary>
        /// Get the specified number of X509Certificates from the personal certificate store in the local machine.
        /// </summary>
        /// <param name="count">Number of certificates</param>
        /// <returns>An array with the specified number of certificates</returns>
        private static X509Certificate[] GetLocalCertificates(int count)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            var certificates = new List<X509Certificate>();
            try
            {
                store.Open(OpenFlags.ReadOnly);
                DateTime now = DateTime.Now;

                // Select certificates from the personal store in the local machine that were specifically
                // created for use by this unit test.
                foreach (var certificate in store.Certificates)
                {
                    if (certificate.Subject.Contains(TestCertPrefix))
                    {
                        certificates.Add(certificate);
                    }

                    if (certificates.Count >= count)
                    {
                        break;
                    }
                }

                certificates.Count.Should().Be(count);

                return certificates.ToArray();
            }
            finally
            {
                store.Close();
            }
        }

        private static SecureTransport CreateTransport(X509Certificate[] clientCertificates, X509Certificate[] serverCertificates)
        {
            var configuration = new SecureTransport.Configuration()
            {
                UseSecureConnection = true,
                ClientCertificates = clientCertificates,
                ServerCertificates = serverCertificates,
                CommunicationProtocolVersion = 1,
                MustCheckCertificateTrustChain = false,
            };

            return new SecureTransport(configuration, null, CancellationToken.None);
        }

        private async Task<bool> ConnectToServer(X509Certificate[] clientCertificates, X509Certificate[] serverCertificates, int port, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(IPAddress.Loopback, port);
                var configuration = new SslConnection.Configuration()
                {
                    ClientCertificates = clientCertificates,
                    ServerCertificates = serverCertificates,
                };

                var clientPolicy = new SslConnection(0, configuration);
                try
                {
                    using (Stream clientStream = await clientPolicy.AuthenticateAsClient("localhost", client, timeout, cancellationToken))
                    {
                        Trace.TraceInformation("AuthenticateAsClient succeeded");
                        var message = new byte[1];
                        await clientStream.WriteAsync(message, 0, 1);
                        return true;
                    }
                }
                catch (AuthenticationException ex)
                {
                    Trace.TraceError($"AuthenticateAsClient failed. exception={ex}");
                    return false;
                }
            }
        }

        private Task<bool> ConnectToServer(X509Certificate[] clientCertificates, X509Certificate[] serverCertificates, int port)
        {
            return this.ConnectToServer(clientCertificates, serverCertificates, port, Timeout.InfiniteTimeSpan, CancellationToken.None);
        }

        private async Task<bool> AcceptClient(X509Certificate[] clientCertificates, X509Certificate[] serverCertificates, int port, TimeSpan timeout, CancellationToken cancellationToken)
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, port);

            listener.Start();

            var configuration = new SslConnection.Configuration()
            {
                ClientCertificates = clientCertificates,
                ServerCertificates = serverCertificates,
            };

            var serverPolicy = new SslConnection(0, configuration);

            try
            {
                using (TcpClient connection = await listener.AcceptTcpClientAsync())
                using (Stream stream = await serverPolicy.AuthenticateAsServer(connection, timeout, cancellationToken))
                {
                    var message = new byte[1];
                    await stream.ReadAsync(message, 0, message.Length);
                    Trace.TraceInformation("AuthenticateAsServer succeeded");
                    return true;
                }
            }
            catch (AuthenticationException ex)
            {
                Trace.TraceError($"AuthenticateAsServer failed. exception={ex}");
                return false;
            }
            finally
            {
                listener.Stop();
            }
        }

        private Task<bool> AcceptClient(X509Certificate[] clientCertificates, X509Certificate[] serverCertificates, int port)
        {
            return this.AcceptClient(clientCertificates, serverCertificates, port, Timeout.InfiniteTimeSpan, CancellationToken.None);
        }
    }
}
