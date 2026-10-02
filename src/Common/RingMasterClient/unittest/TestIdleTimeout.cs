// <copyright file="TestIdleTimeout.cs" company="Microsoft">
//     Copyright ©  2021
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterClientUnitTest
{
	using System;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Threading;
	using System.Threading.Tasks;

	using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend;
	using Microsoft.Azure.Networking.Infrastructure.RingMaster.Communication;
	using Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol;
	using Microsoft.Azure.Networking.Infrastructure.RingMaster.Persistence.InMemory;
	using Microsoft.Azure.Networking.Infrastructure.RingMaster.Server;
	using Microsoft.Azure.Networking.Infrastructure.RingMaster.Transport;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class TestIdleTimeout
	{
		/// <summary>
		/// Backend core
		/// </summary>
		private static RingMasterBackendCore backendCore;

		/// <summary>
		/// <see cref="RingMasterCommunicationProtocol"/> is used as the communication protocol.
		/// </summary>
		private static ICommunicationProtocol protocol = new RingMasterCommunicationProtocol();

		/// <summary>
		/// Persisted data factory.
		/// </summary>
		private static InMemoryFactory inMemoryFactory = new InMemoryFactory(true, null, CancellationToken.None);

		/// <summary>
		/// Backend server
		/// </summary>
		private static RingMasterServer backendServer;

		/// <summary>
		/// Server transport
		/// </summary>
		private static SecureTransport serverTransport;

		/// <summary>
		/// Endpoint address of the backend server
		/// </summary>
		private static string serverAddress;

		[ClassInitialize]
		public static void Setup(TestContext context)
		{
			backendCore = CreateBackend();
			backendServer = new RingMasterServer(protocol, null, CancellationToken.None);

			var transportConfig = new SecureTransport.Configuration
			{
				MaxConnectionIdleTime = TimeSpan.FromSeconds(10),
				CommunicationProtocolVersion = RingMasterCommunicationProtocol.MaximumSupportedVersion,
			};

			serverTransport = new SecureTransport(transportConfig);

			backendServer.RegisterTransport(serverTransport);
			backendServer.OnInitSession = initRequest =>
			{
				return new CoreRequestHandler(backendCore, initRequest);
			};

			int serverListenPort = GetAvailablePort(9000);
			string connectionString = string.Format("127.0.0.1:{0}", serverListenPort);
			serverTransport.StartServer(serverListenPort);
			serverAddress = string.Format("127.0.0.1:{0}", serverListenPort);
		}

		[TestMethod]
		public async Task TestIdleTimeoutsWithNoHeartBeat()
		{
			// Large heartbeat interval - so no hearbeat be send
			var config = new RingMasterClient.Configuration()
			{
				HeartBeatInterval = TimeSpan.FromSeconds(30000)
			};

			var client = new RingMasterClient(serverAddress, config);
			await client.Create("/ephermal", null, null, CreateMode.Ephemeral);
			await Task.Delay(TimeSpan.FromSeconds(20));


			// After wait for 20 seconds - we expect the client to be kill the existing session and ephermal node to be deleted as Idle wait time is 10 seconds
			Assert.IsNull(await client.Exists("/ephermal", null, true));
		}

		[TestMethod]
		public async Task TestIdleTimeoutsWithSmallHearbeat()
		{
			var config = new RingMasterClient.Configuration()
			{
				HeartBeatInterval = TimeSpan.FromSeconds(5)
			};

			var client = new RingMasterClient(serverAddress, config);
			await client.Create("/ephermal", null, null, CreateMode.Ephemeral);
			await Task.Delay(TimeSpan.FromSeconds(20));

			// After wait for 20 seconds - we still expect the client to be connected and ephermal node *not* to be deleted as Idle wait time is 10 seconds and heartbeat is 5 seconds
			Assert.IsNotNull(await client.Exists("/ephermal", null, true));
		}

		private static RingMasterBackendCore CreateBackend()
		{
			RingMasterBackendCore backend = null;
			try
			{
				var backendStarted = new ManualResetEventSlim();
				backend = new RingMasterBackendCore(inMemoryFactory);

				backend.StartService = (p1, p2) => { backendStarted.Set(); };
				backend.Start(CancellationToken.None);
				backend.OnBecomePrimary();

				Assert.IsTrue(backendStarted.Wait(300000));
				var backendToReturn = backend;
				backend = null;
				return backendToReturn;
			}
			finally
			{
				if (backend != null)
				{
					backend.Dispose();
				}
			}
		}

		internal static int GetAvailablePort(int startPort)
		{
			// Evaluate current system tcp connections. This is the same information provided
			// by the netstat command line application, just in .Net strongly-typed object
			// form.  We will look through the list to find a port that is not used.
			IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
			TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
			int[] usedPorts = tcpConnInfoArray.Select(connectionInformation => connectionInformation.LocalEndPoint.Port).ToArray();

			for (int i = startPort; i < 65536; i++)
			{
				if (!usedPorts.Contains(i))
				{
					return i;
				}
			}

			Assert.Fail("Failed to find an available port");
			return 0;
		}
	}
}
