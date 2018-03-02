// <copyright file="RingMasterTool.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterTool
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Tool for interacting with RingMaster.
    /// </summary>
    public static class RingMasterTool
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Status code</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "RingMasterClient is being disposed")]
        public static int Main(string[] args)
        {
            if (args == null || args.Length < 2)
            {
                Console.WriteLine("USAGE: RingMasterTool <targetaddress> <command> [<path>]");
                return 1;
            }

            string targetAddress = args[0];
            string command = args[1].ToLower();

            Console.WriteLine("RingMasterTool targetAddress={0}, command={1}", targetAddress, command);

            Trace.Listeners.Add(new ConsoleTraceListener());

            string[] clientThumbprints = null;
            string[] serviceThumbprints = null;

            try
            {
                string useSslSetting = ConfigurationManager.AppSettings["SSL.UseSSL"];
                if (useSslSetting != null && bool.Parse(useSslSetting))
                {
                    clientThumbprints = ConfigurationManager.AppSettings["SSL.ClientCerts"].Split(new char[] { ';', ',' });
                    serviceThumbprints = ConfigurationManager.AppSettings["SSL.ServerCerts"].Split(new char[] { ';', ',' });
                }
            }
            catch (ConfigurationErrorsException)
            {
                // If there is no configuration, use default values.
            }

            try
            {
                using (var ringMaster = new RingMasterClient(targetAddress, clientThumbprints, serviceThumbprints, 10000, null))
                {
                    if (command == "sendpoisonpill")
                    {
                        if (args.Length < 3)
                        {
                            Console.WriteLine("USAGE: RingMasterTool <targetaddress> sendpoisonpill <path>");
                            return 1;
                        }

                        string path = args[2];
                        Console.WriteLine("SendPoisonPill path={0}", path);

                        SendPoisonPillCommand(ringMaster, path).Wait();
                    }
                    else if (command == "createtree")
                    {
                        if (args.Length < 4)
                        {
                            Console.WriteLine("USAGE: RingMasterTool <targetaddress> createtree <path> <maxnodes>");
                            return 1;
                        }

                        string path = args[2];
                        int maxNodes = int.Parse(args[3]);
                        Console.WriteLine("Create path={0} maxNodes={1}", path, maxNodes);

                        CreateTreeCommand(ringMaster, path, maxNodes).Wait();
                    }
                    else if (command == "waitforstableconnection")
                    {
                        int maxAttempts = 5;

                        if (args.Length > 2)
                        {
                            maxAttempts = int.Parse(args[2]);
                        }

                        if (!WaitForStableConnection(ringMaster, maxAttempts).Result)
                        {
                            Console.WriteLine("Failed to establish connection");
                            return 1;
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.ToString());
                return 1;
            }
        }

        /// <summary>
        /// Send a poison pill to the given ringmaster affecting the given path.
        /// </summary>
        /// <param name="ringMaster">Ring master to which the poison pill must be sent</param>
        /// <param name="path">Path affected by the poison pill</param>
        /// <returns>A Task that tracks execution of this method</returns>
        private static async Task SendPoisonPillCommand(RingMasterClient ringMaster, string path)
        {
            await ringMaster.SetAuth(new Id(AuthSchemes.Digest, "commander"));

            string poisonPillCommand = string.Format("break:{0}", path);
            byte[] data = Encoding.UTF8.GetBytes(poisonPillCommand);
            await ringMaster.Create("$/poisonpill", data, null, CreateMode.Persistent);
        }

        /// <summary>
        /// Create a tree at the given path
        /// </summary>
        /// <param name="ringMaster">Ring master in which the path must be created</param>
        /// <param name="path">Path to be created</param>
        /// <param name="maxNodes">Maximum number of nodes in the tree</param>
        /// <returns>A Task that tracks execution of this method</returns>
        private static async Task CreateTreeCommand(RingMasterClient ringMaster, string path, int maxNodes)
        {
            var paths = new Queue<string>();
            paths.Enqueue(path);

            var random = new Random();

            int numCreated = 0;

            while ((paths.Count > 0) && (numCreated < maxNodes))
            {
                var currentPath = paths.Dequeue();
                await ringMaster.Create(currentPath, null, null, CreateMode.PersistentAllowPathCreation, throwIfNodeExists: false);
                numCreated++;

                for (int i = 0; i < random.Next(1, 10); i++)
                {
                    string childPath = string.Format("{0}/{1}", currentPath, Guid.NewGuid());
                    paths.Enqueue(childPath);
                }
            }
        }

        /// <summary>
        /// Wait until a stable connection can be established with the given ringmaster.
        /// </summary>
        /// <param name="ringMaster">RingMaster client</param>
        /// <param name="maxAttempts">Maximum number of attempts</param>
        /// <returns>A Task that resolves to <c>true</c> if the connection was successful</returns>
        private static async Task<bool> WaitForStableConnection(RingMasterClient ringMaster, int maxAttempts)
        {
            for (int retryCount = 0; retryCount < maxAttempts; retryCount++)
            {
                try
                {
                    await ringMaster.Exists("/", watcher: null);
                    return true;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("WaitForStableConnection. exception={0}, retryCount={1}, maxAttempts={2}", ex.Message, retryCount, maxAttempts);
                }

                await Task.Delay(1000);
            }

            return false;
        }
    }
}