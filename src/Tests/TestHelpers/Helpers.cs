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
        /// <summary>
        /// The ring master service name
        /// </summary>
        private const string RingMasterServiceName = "/RINGMASTERSERVICE";

        /// <summary>
        /// Gets the backend Service endpoint
        /// </summary>
        /// <returns>The vega service endpoint and primary node name.</returns>
        public static async Task<Tuple<string, string>> GetVegaServiceInfo()
        {
            var fabricClient = new FabricClient();
            var appList = await fabricClient.QueryManager.GetApplicationListAsync();
            foreach (var app in appList)
            {
                foreach (var svc in await fabricClient.QueryManager.GetServiceListAsync(app.ApplicationName))
                {
                    if (!svc.ServiceName.AbsoluteUri.ToUpperInvariant().Contains(RingMasterServiceName))
                    {
                        continue;
                    }

                    var resolvedPartition = await fabricClient.ServiceManager.ResolveServicePartitionAsync(svc.ServiceName);
                    var endpoint = resolvedPartition.Endpoints
                        .Where(ep => ep.Role == ServiceEndpointRole.StatefulPrimary)
                        .Select(ep => ep.Address)
                        .FirstOrDefault();

                    // The format of endpoint is: "name":"uri". For instance:
                    // {"Endpoints":{"ServiceEndpoint":"Tcp:\/\/10.30.78.31:99\/","ZkprServiceEndpoint":"Tcp:\/\/10.30.78.31:100\/"}}
                    var match = Regex.Match(endpoint, $"\"ServiceEndpoint\":\"([^\"]+)\"");
                    var serviceEndpoint = match.Success ? match.Groups[1].Value.Replace(@"\", string.Empty) : null;

                    var replicas = await fabricClient.QueryManager.GetReplicaListAsync(resolvedPartition.Info.Id);
                    var primaryNodeName = replicas.FirstOrDefault(r => ((StatefulServiceReplica)r).ReplicaRole == ReplicaRole.Primary).NodeName;
                    return new Tuple<string, string>(new Uri(serviceEndpoint).Authority, primaryNodeName);
                }
            }

            return new Tuple<string, string>(string.Empty, string.Empty);
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
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
    }
}