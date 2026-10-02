// <copyright file="Helpers.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test.Helpers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Fabric;
    using System.Fabric.Query;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;

    /// <summary>
    /// The helpers class.
    /// </summary>
    public static class Helpers
    {
        private const string FabricClientPathRelativeToProgramFiles = @"Microsoft Service Fabric\bin\Fabric\Fabric.Code\FabricClient.dll";

        /// <summary>
        /// The ring master application type name
        /// </summary>
        private const string RingMasterAppTypeName = "RingMasterApplication";

        /// <summary>
        /// Creates a Service Fabric client with a clearer error when the native runtime is missing.
        /// </summary>
        /// <param name="serviceHostEndpoint">Optional endpoint for connecting to a remote gateway.</param>
        /// <param name="serverName">The expected remote server name.</param>
        /// <param name="thumbprint">The expected remote certificate thumbprint.</param>
        /// <returns>A configured <see cref="FabricClient"/> instance.</returns>
        public static FabricClient CreateFabricClient(string serviceHostEndpoint = "", string serverName = "", string thumbprint = "")
        {
            try
            {
                return string.IsNullOrEmpty(serviceHostEndpoint)
                    ? new FabricClient()
                    : new FabricClient(GetCredential(serverName, thumbprint), serviceHostEndpoint);
            }
            catch (Exception ex) when (IsFabricClientLoadFailure(ex))
            {
                throw new InvalidOperationException(BuildFabricClientLoadFailureMessage(serviceHostEndpoint), ex);
            }
        }

        /// <summary>
        /// Gets the backend Service endpoint
        /// </summary>
        /// <param name="targetServiceIndex">Index of the target service.</param>
        /// <param name="serviceHostEndpoint">The service host endpoint.</param>
        /// <param name="serverName">Name of the server.</param>
        /// <param name="thumbprint">The thumbprint.</param>
        /// <param name="serviceBaseUri">The service base URI.</param>
        /// <returns>
        /// The vega service endpoint and primary node name.
        /// </returns>
        public static async Task<Tuple<string, string>> GetVegaServiceInfo(int targetServiceIndex = 0, string serviceHostEndpoint = "", string serverName = "", string thumbprint = "", string serviceBaseUri = "fabric:/RingMaster/RingMasterService")
        {
            var targetServiceUri = GetTargetServiceUri();
            var rnd = new Random();
            using FabricClient fabricClient = CreateFabricClient(serviceHostEndpoint, serverName, thumbprint);

            var ringMasterApp = (await fabricClient.QueryManager.GetApplicationListAsync()).Where(app => app.ApplicationTypeName == RingMasterAppTypeName).FirstOrDefault();
            var svc = (await fabricClient.QueryManager.GetServiceListAsync(ringMasterApp.ApplicationName)).Where(s => s.ServiceName.AbsoluteUri == targetServiceUri).FirstOrDefault();
            if (svc == null)
            {
                return new Tuple<string, string>(string.Empty, string.Empty);
            }

            var resolvedPartition = await fabricClient.ServiceManager.ResolveServicePartitionAsync(svc.ServiceName);
            var endpoint = resolvedPartition.Endpoints
                .Where(ep => ep.Role == ServiceEndpointRole.StatefulPrimary)
                .Select(ep => ep.Address)
                .FirstOrDefault();

            var match = Regex.Match(endpoint, $"\"ServiceEndpoint\":\"([^\"]+)\"");
            var serviceEndpoint = match.Success ? match.Groups[1].Value.Replace(@"\", string.Empty) : null;

            var replicas = await fabricClient.QueryManager.GetReplicaListAsync(resolvedPartition.Info.Id);
            var primaryNodeName = replicas.FirstOrDefault(r => ((StatefulServiceReplica)r).ReplicaRole == ReplicaRole.Primary).NodeName;
            return new Tuple<string, string>(new Uri(serviceEndpoint).Authority, primaryNodeName);

            string GetTargetServiceUri()
            {
                if (targetServiceIndex == 0)
                {
                    return serviceBaseUri;
                }
                else
                {
                    return $"{serviceBaseUri}{targetServiceIndex}";
                }
            }
        }

        /// <summary>
        /// Gets the server address if not provided.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>server address</returns>
        public static string GetServerAddressIfNotProvided(string server)
        {
            if (!string.IsNullOrWhiteSpace(server))
            {
                return server;
            }
            else
            {
                var serviceInfo = GetVegaServiceInfo().Result;
                return serviceInfo.Item1;
            }
        }

        /// <summary>
        /// Make a random data payload
        /// </summary>
        /// <param name="rnd">Random object</param>
        /// <param name="dataLength">Length of payload</param>
        /// <returns>Byte array</returns>
        public static byte[] MakeRandomData(Random rnd, int dataLength)
        {
            if (rnd == null)
            {
                return null;
            }

            var data = new byte[dataLength];
            rnd.NextBytes(data);
            return data;
        }

        /// <summary>
        /// Make a sequential data payload
        /// </summary>
        /// <param name="dataLength">Length of payload</param>
        /// <returns>Byte array</returns>
        public static byte[] MakeSequentialData(int dataLength)
        {
            return Enumerable.Range(0, dataLength).Select(n => (byte)n).ToArray();
        }

        /// <summary>
        /// Starts multiple threads for the given action
        /// </summary>
        /// <param name="threadCount">Number of threads to be started</param>
        /// <param name="action">Thread body</param>
        /// <returns>List of threads</returns>
        public static Thread[] StartMultipleThreads(int threadCount, ParameterizedThreadStart action)
        {
            var threads = new List<Thread>();
            for (int i = 0; i < threadCount; i++)
            {
                threads.Add(new Thread(action));
            }

            for (int i = 0; i < threadCount; i++)
            {
                threads[i].Start(i);
            }

            return threads.ToArray();
        }

        /// <summary>
        /// Run a list of async tasks in parallel (and not to schedule too many async tasks at once)
        /// </summary>
        /// <typeparam name="T">Type name of elements in the enumerable</typeparam>
        /// <param name="source">Source enumerator</param>
        /// <param name="body">async method body for each element</param>
        /// <param name="partitionCount">Number of partition to parallelize the source</param>
        /// <returns>Async task</returns>
        public static Task ForEachAsync<T>(IEnumerable<T> source, Func<T, Task> body, int partitionCount = -1)
        {
            if (partitionCount <= 0)
            {
                partitionCount = Environment.ProcessorCount;
            }

            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(partitionCount)
                select Task.Run(async () =>
                {
                    using (partition)
                    {
                        while (partition.MoveNext())
                        {
                            await body(partition.Current);
                        }
                    }
                }));
        }

        /// <summary>
        /// Setup trace log.
        /// </summary>
        /// <param name="logFileDirectory">the log file directory name.</param>
        public static void SetupTraceLog(string logFileDirectory)
        {
            if (string.IsNullOrWhiteSpace(logFileDirectory))
            {
                throw new ArgumentNullException(nameof(logFileDirectory));
            }

            LogFileEventTracing.Start(logFileDirectory);

            int index = 0;
            while (index < Trace.Listeners.Count)
            {
                var listener = Trace.Listeners[index] as LogFileTraceListener;
                if (listener != null)
                {
                    return;
                }

                index++;
            }

            Trace.Listeners.Add(new LogFileTraceListener());

            AppDomain.CurrentDomain.ProcessExit +=
                (sender, eventArgs) =>
                {
                    LogFileEventTracing.Stop();
                };
        }

        /// <summary>
        /// Create and install Certificate.
        /// </summary>
        /// <param name="subject">the certificate subject.</param>
        /// <param name="storeName">the name of the store.</param>
        /// <param name="storeLocation">the location of the store.</param>
        public static void InstallCert(string subject, StoreName storeName = StoreName.My, StoreLocation storeLocation = StoreLocation.CurrentUser)
        {
            bool isOSWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            try
            {
                if (isOSWindows)
                {
                    var powershell = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\WindowsPowerShell\v1.0\PowerShell.exe");
                    Process.Start(
                        powershell,
                        $"-ExecutionPolicy bypass -command New-SelfSignedCertificate -DnsName {subject} -CertStoreLocation Cert:\\{storeLocation}\\{storeName}")
                        .WaitForExit();
                }

                // 1) Create certificate and private key -> convert to pfx file
                // 2) Import to storeLocation\\storeName and then delete created files
                else
                {
                    Process.Start(
                        "openssl",
                        $"req -x509 -new -subj /CN={subject} -nodes -sha1 -newkey rsa:2048 -keyout cert.key -out cert.crt")
                        .WaitForExit();
                    Process.Start(
                        "openssl",
                        $"pkcs12 -export -in cert.crt -inkey cert.key -out cert.pfx -passout pass:")
                        .WaitForExit();

                    using (X509Store store = new X509Store(storeName, storeLocation))
                    {
                        store.Open(OpenFlags.ReadWrite);
                        X509Certificate2 x509 = new X509Certificate2("cert.pfx");
                        store.Add(x509);
                        store.Close();
                    }

                    File.Delete("cert.key");
                    File.Delete("cert.crt");
                    File.Delete("cert.pfx");
                }
            }
            catch (Exception ex)
            {
                if (isOSWindows)
                {
                    Console.WriteLine($"Failed to run New-SelfSignedCertificate: {ex}");
                }
                else
                {
                    Console.WriteLine($"Failed to run openssl: {ex}");
                }

                throw;
            }
        }

        /// <summary>
        /// Creates the ring master connection.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="timeStreamId">The time stream identifier.</param>
        /// <returns>ring master client connection</returns>
        public static IRingMasterRequestHandler CreateRingMasterTimeStreamRequestHandler(string connectionString, CancellationToken cancellationToken, ulong timeStreamId)
        {
            var configuration = new RingMasterClient.Configuration();
            var ringMaster = new RingMasterClient(connectionString, configuration, null, cancellationToken);
            return ringMaster.OpenTimeStream(timeStreamId);
        }

        /// <summary>
        /// Gets the instance field use reflection.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="instance">The instance.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>the field value</returns>
        public static object GetInstanceField(Type type, object instance, string fieldName)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }

        private static X509Credentials GetCredential(string serverName, string thumbprint)
        {
            var xc = new X509Credentials
            {
                FindType = X509FindType.FindByThumbprint,
                FindValue = thumbprint,
                StoreLocation = StoreLocation.LocalMachine,
                StoreName = "My",
            };

            xc.RemoteCommonNames.Add(serverName);
            xc.RemoteCertThumbprints.Add(thumbprint);
            xc.ProtectionLevel = ProtectionLevel.EncryptAndSign;
            return xc;
        }

        private static string BuildFabricClientLoadFailureMessage(string serviceHostEndpoint)
        {
            var defaultInstallPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                FabricClientPathRelativeToProgramFiles);

            var discoveryHint = string.IsNullOrEmpty(serviceHostEndpoint)
                ? "This test is trying to discover the RingMaster endpoint through Service Fabric because no server address was provided."
                : "This test is trying to connect through Service Fabric using the provided host endpoint.";

            return
                $"Failed to load the native Service Fabric client library 'FabricClient.dll'. " +
                $"The 'Microsoft.ServiceFabric' NuGet package only provides the managed 'System.Fabric' assemblies; " +
                $"the native runtime comes from a local Service Fabric installation, typically at '{defaultInstallPath}'. " +
                $"{discoveryHint} Install the Service Fabric runtime first " +
                $"(for CloudTest see 'src\\CloudUnitTests\\CloudTestSetup.cmd' or 'ServiceFabric.XCopyPackage\\InstallFabric.ps1'), " +
                $"or set the test's 'ServerAddress' so it can skip Service Fabric discovery when that is supported.";
        }

        private static bool IsFabricClientLoadFailure(Exception exception)
        {
            while (exception != null)
            {
                switch (exception)
                {
                    case DllNotFoundException:
                        return true;
                    case FileNotFoundException fileNotFound when ContainsFabricClientText(fileNotFound.FileName) || ContainsFabricClientText(fileNotFound.Message):
                        return true;
                    case FileLoadException fileLoad when ContainsFabricClientText(fileLoad.FileName) || ContainsFabricClientText(fileLoad.Message):
                        return true;
                    case BadImageFormatException badImageFormat when ContainsFabricClientText(badImageFormat.FileName) || ContainsFabricClientText(badImageFormat.Message):
                        return true;
                    default:
                        if (ContainsFabricClientText(exception.Message))
                        {
                            return true;
                        }

                        exception = exception.InnerException;
                        break;
                }
            }

            return false;
        }

        private static bool ContainsFabricClientText(string text)
        {
            return !string.IsNullOrEmpty(text)
                && text.IndexOf("FabricClient.dll", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
