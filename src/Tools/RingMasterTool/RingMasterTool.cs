// <copyright file="RingMasterTool.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterTool
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Tool for interacting with RingMaster.
    /// </summary>
    public static class RingMasterTool
    {
        /// <summary>
        /// Logging delegate
        /// </summary>
        private static Action<string> log;

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Status code</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "RingMasterClient is being disposed")]
        public static int Main(string[] args)
        {
            log = s => Console.WriteLine("{0} {1}", DateTime.UtcNow, s);

            if (args == null || args.Length < 2)
            {
                Console.WriteLine("USAGE: RingMasterTool <targetAddress> <command> [<path>]");
                return 1;
            }

            string targetAddress = args[0];
            string command = args[1].ToLower();

            Console.WriteLine("RingMasterTool targetAddress={0}, command={1}", targetAddress, command);

            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            string[] clientThumbprints = null;
            string[] serviceThumbprints = null;

            try
            {
                var path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var builder = new ConfigurationBuilder().SetBasePath(Path.GetDirectoryName(path)).AddJsonFile("appSettings.json");

                var appSettings = builder.Build();
                string useSslSetting = appSettings["SSL.UseSSL"];
                if (useSslSetting != null && bool.Parse(useSslSetting))
                {
                    clientThumbprints = appSettings["SSL.ClientCerts"].Split(new char[] { ';', ',' });
                    serviceThumbprints = appSettings["SSL.ServerCerts"].Split(new char[] { ';', ',' });
                }
            }
            catch (Exception ex) when (ex is FileNotFoundException)
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
                            Console.WriteLine("USAGE: RingMasterTool <targetAddress> sendpoisonpill <path>");
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
                            Console.WriteLine("USAGE: RingMasterTool <targetAddress> createtree <path> <maxnodes> [flat]");
                            return 1;
                        }

                        string path = args[2];
                        int maxNodes = int.Parse(args[3]);
                        bool isFlat = false;
                        if (args.Length > 4)
                        {
                            isFlat = args[4].Equals("flat", StringComparison.OrdinalIgnoreCase);
                        }

                        Console.WriteLine("Create path={0} maxNodes={1} isFlat={2}", path, maxNodes, isFlat);

                        CreateTreeCommand(ringMaster, path, maxNodes, isFlat).Wait();
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
                    else if (command == "getchildcount")
                    {
                        if (args.Length < 3)
                        {
                            Console.WriteLine("USAGE: RingMasterTool <targetAddress> getchildcount <path> [showpath <maxtoshow>]");
                            return 1;
                        }

                        bool showPath = false;
                        int maxToShow = 100;
                        if (args.Length > 3)
                        {
                            showPath = args[3].Equals("showpath", StringComparison.InvariantCultureIgnoreCase);
                            maxToShow = int.Parse(args[4]);
                        }

                        string path = args[2];
                        Console.WriteLine("getchildcount path={0}, showPath={1}, maxChildToShow={2}", path, showPath, maxToShow);

                        GetChildCount(ringMaster, path, showPath, maxToShow).Wait();
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
        /// <param name="isFlat">indicate whether this tree is flat</param>
        /// <returns>A Task that tracks execution of this method</returns>
        private static async Task CreateTreeCommand(RingMasterClient ringMaster, string path, int maxNodes, bool isFlat)
        {
            int[] numCreated = new int[1];
            Task createTree = isFlat ?
                  Task.Run(async () => { await CreateFlatTree(ringMaster, path, maxNodes, numCreated); })
                : Task.Run(async () => await CreateRandomStructuredTree(ringMaster, path, maxNodes, numCreated));

            var cancel = new CancellationTokenSource();

            Task showProgess = Task.Run(async () =>
            {
                while (!cancel.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                    log($"Number of nodes created: {numCreated[0]}");
                }
            });

            await createTree;
            cancel.Cancel();
        }

        private static async Task GetChildCount(RingMasterClient ringMaster, string path, bool showPath, int maxToShow = 100)
        {
            var subtree = await ringMaster.GetFullSubtree(path);
            log($"Total number of children: {GetTotalNumberofChildren(subtree)}, direct children: {(subtree.Children == null ? 0 : subtree.Children.Count)}");

            if (showPath && subtree.Children != null)
            {
                if (subtree.Children.Count > maxToShow)
                {
                    log($"showing the first {maxToShow} direct children:");
                }

                for (var i = 0; i < subtree.Children.Count; i++)
                {
                    if (i == maxToShow)
                    {
                        break;
                    }

                    log($"{path}/{subtree.Children[i].Name}");
                }
            }
        }

        private static int GetTotalNumberofChildren(TreeNode root)
        {
            if (root == null || root.Children == null)
            {
                return 0;
            }

            int num = root.Children.Count;
            foreach (var child in root.Children)
            {
                num += GetTotalNumberofChildren(child);
            }

            return num;
        }

        private static async Task CreateFlatTree(RingMasterClient ringMaster, string path, int maxNodes, int[] numCreated)
        {
            await ringMaster.Create(path, null, null, CreateMode.PersistentAllowPathCreation, throwIfNodeExists: false);

            while (numCreated[0] < maxNodes)
            {
                await ringMaster.Create($"{path}/{Guid.NewGuid()}", null, null, CreateMode.PersistentAllowPathCreation, throwIfNodeExists: false);
                numCreated[0]++;
            }
        }

        private static async Task CreateRandomStructuredTree(RingMasterClient ringMaster, string path, int maxNodes, int[] numCreated)
        {
            var random = new Random();
            var paths = new Queue<string>();
            paths.Enqueue(path);

            while ((paths.Count > 0) && (numCreated[0] < maxNodes))
            {
                var currentPath = paths.Dequeue();
                await ringMaster.Create(currentPath, null, null, CreateMode.PersistentAllowPathCreation, throwIfNodeExists: false);
                numCreated[0]++;

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
