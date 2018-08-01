// <copyright file="TestRSL.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RSLValidation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using ManagedRSLib;
    using VisualStudio.TestTools.UnitTesting;
    using static BasicStateMachine;

    [TestClass]
    public class TestRSL
    {
        private static int basePort = 10000;

        static TestRSL()
        {
            if (Environment.CommandLine.Split(' ').Contains("attach"))
            {
                Debugger.Launch();
            }

            SetupCertificates(false);
            AppDomain.CurrentDomain.ProcessExit += (_, _2) =>
            {
                SetupCertificates(true);
            };
        }

        [Flags]
        private enum TestActions
        {
            FailoverPrimary = 1,
            RecoverFirstInstance = 2,
            ReturnToFirstPrimary = 4,
            ThumbprintRollover = 8,
        }

        public static void Main(params string[] args)
        {
            if (args == null)
            {
                return;
            }

            TestRSL t = new TestRSL();

            t.TestAll();
        }

        public void TestAll()
        {
            ConsoleColor color = Console.ForegroundColor;
            List<string> failed = new List<string>();
            int succeeded = 0;

            foreach (MethodInfo minf in this.GetType().GetMethods())
            {
                if (minf.GetCustomAttributes<TestMethodAttribute>().Any())
                {
                    string name = minf.DeclaringType.FullName + "." + minf.Name;

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine();
                    Console.WriteLine("=========== Starting test {0}", name);
                    Console.ForegroundColor = color;
                    try
                    {
                        minf.Invoke(this, new object[0]);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("=========== PASSED test {0}", name);
                        succeeded++;
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("=========== FAILED test {0}", name);
                        Console.WriteLine(e);
                        failed.Add(name);
                    }
                    finally
                    {
                        Console.ForegroundColor = color;
                    }
                }
            }

            if (failed.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine();
            string verdict = string.Format("Final summary: Passed: {0} Failed:{1}", succeeded, failed.Count);

            Console.WriteLine(verdict);

            foreach (string fail in failed)
            {
                Console.WriteLine("FAILED TEST: {0}", fail);
            }

            Console.ForegroundColor = color;

            if (failed.Count > 0)
            {
                Environment.Exit(1);
            }
        }

        [TestMethod]
        public void BaseTest()
        {
            TestConfig cfg = new TestConfig();

            this.DoTest(cfg, TestActions.FailoverPrimary | TestActions.RecoverFirstInstance | TestActions.ReturnToFirstPrimary, 15000);
        }

        [TestMethod]
        public void CertTest()
        {
            X509Certificate[] certs = GetLocalCertificates(3);

            if (certs == null)
            {
                throw new InvalidOperationException("certs");
            }

            TestConfig cfg = new TestConfig();
            cfg.Th1 = ((X509Certificate2)certs[0]).Thumbprint; // "1a05316b5190cdef09ef5bf1c3c4fe9dd310d9a6";
            cfg.Th2 = null;

            X509Certificate2 parent = GetSigningCert((X509Certificate2)certs[0]);

            // this subject validates Th1
            cfg.Subject1 = ((X509Certificate2)certs[0]).Subject; // "Pluma2W10.redmond.corp.microsoft.com";
            cfg.ThumbprintsParent1 = parent.Thumbprint; // "3776672132b08abbae9c6f209e96ffa01ba646cd";

            this.DoTest(cfg, TestActions.FailoverPrimary | TestActions.RecoverFirstInstance | TestActions.ReturnToFirstPrimary, 60000);
        }

        [TestMethod]
        public void CertRolloverTest()
        {
            X509Certificate[] certs = GetLocalCertificates(3);

            if (certs == null)
            {
                throw new InvalidOperationException("certs");
            }

            TestConfig cfg = new TestConfig();
            cfg.Th1 = ((X509Certificate2)certs[0]).Thumbprint; // "1a05316b5190cdef09ef5bf1c3c4fe9dd310d9a6";
            cfg.Th2 = null;

            X509Certificate2 parent = GetSigningCert((X509Certificate2)certs[0]);

            // this subject validates Th1
            cfg.Subject1 = ((X509Certificate2)certs[0]).Subject; // "Pluma2W10.redmond.corp.microsoft.com";
            cfg.ThumbprintsParent1 = parent.Thumbprint; // "3776672132b08abbae9c6f209e96ffa01ba646cd";

            // this subject validates Th3
            cfg.Subject2 = ((X509Certificate2)certs[1]).Subject; // "40000e86-6d7f-4629-91d3-d0d0fbe5bbe2";

            parent = GetSigningCert((X509Certificate2)certs[1]);
            cfg.Subjectparent2 = parent.Thumbprint; // "b56ad7cfd74c7ce466f11ed80f88926139ea8fed";

            cfg.Th3 = ((X509Certificate2)certs[1]).Thumbprint; // "2351a54165eeb4b431e37113aa1439c81d8b5bff";
            cfg.Th4 = cfg.Th1;

            this.DoTest(cfg, TestActions.FailoverPrimary | TestActions.RecoverFirstInstance | TestActions.ReturnToFirstPrimary | TestActions.ThumbprintRollover, 60000);
        }

        [TestMethod]
        public void CertBadRolloverTest()
        {
            X509Certificate[] certs = GetLocalCertificates(3);

            if (certs == null)
            {
                throw new InvalidOperationException("certs");
            }

            TestConfig cfg = new TestConfig();
            cfg.Th1 = ((X509Certificate2)certs[0]).Thumbprint; // "1a05316b5190cdef09ef5bf1c3c4fe9dd310d9a6";
            cfg.Th2 = null;

            X509Certificate2 parent = GetSigningCert((X509Certificate2)certs[0]);

            // this subject validates Th1
            cfg.Subject1 = ((X509Certificate2)certs[0]).Subject; // "Pluma2W10.redmond.corp.microsoft.com";
            cfg.ThumbprintsParent1 = parent.Thumbprint; // "3776672132b08abbae9c6f209e96ffa01ba646cd";

            // No subject will validate this thumbprint
            cfg.Th3 = ((X509Certificate2)certs[2]).Thumbprint; // "2351a54165eeb4b431e37113aa1439c81d8b5bff";
            // cfg.Th3 = "9d3d3ec58caf470235a3908e87521a7e340edcd1";
            cfg.Th4 = null;

            try
            {
                this.DoTest(cfg, TestActions.FailoverPrimary | TestActions.RecoverFirstInstance | TestActions.ReturnToFirstPrimary | TestActions.ThumbprintRollover, 60000);
            }
            catch (TimeoutException)
            {
                return;
            }

            Assert.Fail("didn't timeout!");
        }

        /// <summary>
        /// returns the certificate signing the given one
        /// </summary>
        /// <param name="cert">the given cert</param>
        /// <returns>the signing cert</returns>
        private static X509Certificate2 GetSigningCert(X509Certificate2 cert)
        {
            using (X509Chain ch = new X509Chain(true))
            {
                ch.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority | X509VerificationFlags.IgnoreCertificateAuthorityRevocationUnknown | X509VerificationFlags.IgnoreCtlSignerRevocationUnknown;

                ch.Build(cert);

                if (ch.ChainElements.Count < 1)
                {
                    Assert.Fail("test failed because chain couldn't be built");
                }

                if (ch.ChainElements.Count < 2)
                {
                    return null;
                }

                return ch.ChainElements[1].Certificate;
            }
        }

        private static void SetupCertificates(bool onlycleanup)
        {
            ConsoleColor color = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(onlycleanup ? "Cleaning up certificates" : "creating certificates");
            Console.ForegroundColor = color;

            string[] items = Assembly.GetEntryAssembly().CodeBase.Replace("file:///", string.Empty).Split('/');

            string pathOfThisExe = string.Join("\\", items);
            string workingDirectory = pathOfThisExe.Substring(0, pathOfThisExe.LastIndexOf('\\'));
            string filename = "CMD.exe";
            string arguments = "/C RunRSLValidationUnitTest.cmd" + (onlycleanup ? " cleanup" : string.Empty);

            int exitCode = RunWithRedirect(filename, arguments, workingDirectory, (line, _isOutput) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine(line);
                Console.ForegroundColor = color;
            });

            if (exitCode != 0)
            {
                Assert.Fail("couldn't set up certificates");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(onlycleanup ? "Cleaning up certificates: done" : "creating certificates: done");
                Console.ForegroundColor = color;
            }
        }

        private static int RunWithRedirect(string filename, string arguments, string workingDirectory, Action<string /*line*/, bool /*isOutput*/> onOutputOrError)
        {
            if (onOutputOrError == null)
            {
                throw new ArgumentNullException(nameof(onOutputOrError));
            }

            using (Process p = new Process())
            {
                p.StartInfo.FileName = filename;
                p.StartInfo.Arguments = arguments;
                p.StartInfo.WorkingDirectory = workingDirectory;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.UseShellExecute = false;

                p.OutputDataReceived += (_, data) =>
                {
                    onOutputOrError(data.Data, true);
                };

                p.ErrorDataReceived += (_, data) =>
                {
                    onOutputOrError(data.Data, false);
                };

                Console.ForegroundColor = ConsoleColor.DarkCyan;

                Console.WriteLine("  Executing: " + p.StartInfo.FileName + " " + p.StartInfo.Arguments);

                p.Start();
                p.BeginOutputReadLine();

                p.WaitForExit();

                return p.ExitCode;
            }
        }

        /// <summary>
        /// Get the specified number of X509Certificates from the personal certificate store in the local machine.
        /// </summary>
        /// <param name="count">Number of certificates</param>
        /// <returns>An array with the specified number of certificates</returns>
        private static X509Certificate[] GetLocalCertificates(int count)
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                var certificates = new List<X509Certificate>();
                store.Open(OpenFlags.ReadOnly);
                DateTime now = DateTime.Now;

                // Select certificates from the personal store in the local machine that were specifically
                // created for use by this unit test.
                foreach (var certificate in store.Certificates)
                {
                    if (certificate.Subject.Contains("RSLValidationUnitTest.Certificate"))
                    {
                        certificates.Add(certificate);
                    }

                    if (certificates.Count >= count)
                    {
                        break;
                    }
                }

                Assert.AreEqual(certificates.Count, count);

                return certificates.ToArray();
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "we want SM to survive")]
        private void DoTest(TestConfig cfg, TestActions actions, int timeoutMillis)
        {
            ManualResetEvent ev = new ManualResetEvent(false);

            cfg.Ports = new ushort[6];

            for (int i = 0; i < 6; i++)
            {
                cfg.Ports[i] = StateMachineManager.GetFreePort();
            }

            cfg.RslData = ".\\rsldata";
            cfg.LogPath = ".\\logpath";

            if (Directory.Exists(cfg.RslData))
            {
                Directory.Delete(cfg.RslData, true);
            }

            if (Directory.Exists(cfg.LogPath))
            {
                Directory.Delete(cfg.LogPath, true);
            }

            TestTracer tr = new TestTracer();

            AppDomainStarter ad0 = null;
            AppDomainStarter ad1 = null;
            AppDomainStarter ad2 = null;
            Exception resultException = null;

            if (cfg.Subject1 != null)
            {
                cfg.Subject1 = cfg.Subject1.Split('=').Last();
            }

            if (cfg.Subject2 != null)
            {
                cfg.Subject2 = cfg.Subject2.Split('=').Last();
            }

            ManagedRSLStateMachine.Init(cfg.LogPath, cfg.Th1, cfg.Th2, cfg.Subject1, cfg.ThumbprintsParent1, cfg.Subject2, cfg.Subjectparent2, false, true, false, false);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (!ev.WaitOne(timeoutMillis))
                {
                    resultException = new TimeoutException("not finished on time");
                    ad0.Stop();
                    ad1.Stop();
                    ad2.Stop();
                }
            });

            try
            {
                ad0 = new AppDomainStarter("sm0", 0, cfg);
                ad0.Tracer = tr;
                ad0.Start(true);

                ad1 = new AppDomainStarter("sm1", 1, cfg);
                ad1.Tracer = tr;
                ad1.Start(false);

                ad2 = new AppDomainStarter("sm2", 2, cfg);
                ad2.Tracer = tr;
                ad2.Start(false);

                ad0.SM.WaitForPrimary(30000);

                Console.WriteLine("we got the primary!!");
                ad0.SM.ReplicateCommand(new CommandA(11));
                ad0.SM.ReplicateCommand(new CommandB("eleven"), true);

                if (actions.HasFlag(TestActions.FailoverPrimary))
                {
                    Console.WriteLine("stopping first instance...");
                    ad1.SM.CanBP = true;
                    ad0.Stop();
                    ad1.SM.WaitForPrimary(30000);
                }

                Thread.Sleep(5000);
                if (actions.HasFlag(TestActions.ThumbprintRollover))
                {
                    Console.WriteLine("rolling over thumbprints");
                    ManagedRSLStateMachine.ReplaceThumbprints(cfg.Th3, cfg.Th4, false, false);
                }

                if (actions.HasFlag(TestActions.RecoverFirstInstance) || actions.HasFlag(TestActions.ReturnToFirstPrimary))
                {
                    Console.WriteLine("restarting first instance");
                    ad0.Start(false);
                }

                if (actions.HasFlag(TestActions.ReturnToFirstPrimary))
                {
                    Console.WriteLine("failing second instance into first instance");
                    ad0.SM.CanBP = true;
                    ad1.Stop();
                    ad0.SM.WaitForPrimary(30000);
                }

                ev.Set();
            }
            catch (Exception e)
            {
                if (resultException == null)
                {
                    resultException = e;
                }
            }
            finally
            {
                Console.WriteLine("Unloading all");

                if (ad0 != null)
                {
                    ad0.Stop();
                }

                if (ad1 != null)
                {
                    ad1.Stop();
                }

                if (ad2 != null)
                {
                    ad2.Stop();
                }

                ManagedRSLStateMachine.Unload();
            }

            if (resultException != null)
            {
                if (resultException.InnerException != null)
                {
                    resultException = resultException.InnerException;
                }

                throw resultException;
            }
        }

        private class StateMachineManager
        {
            [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "we want SM to survive")]
            public static BasicStateMachine.Reference CreateSMSec(int instance, TestConfig cfg, TestTracer tracer)
            {
                BasicStateMachine sm = CreateSM(instance, false, cfg, tracer);
                return sm.SMReference;
            }

            public static BasicStateMachine CreateSM(int instance, bool designatedPrimary, TestConfig cfg, TestTracer tracer)
            {
                ManagedRSLNode[] nodes = new ManagedRSLNode[cfg.Ports.Length / 2];
                for (int i = 0; i < nodes.Length; i++)
                {
                    nodes[i] = BasicStateMachine.CreateNode(cfg.Ports[2 * i], cfg.Ports[(2 * i) + 1]);
                }

                BasicStateMachine sm = new BasicStateMachine(tracer, nodes[instance].RslPort, nodes[instance].RslLearnPort);

                try
                {
                    sm.CanBP = designatedPrimary;

                    using (ManagedRSLConfigParam rslCfg = SetupConfiguration(cfg.RslData))
                    {
                        Stopwatch w = Stopwatch.StartNew();

                        while (true)
                        {
                            if (sm.Initialize(rslCfg, sm.SelfNode, ManagedRSLProtocolVersion.ManagedRSLProtocolVersion_5, false))
                            {
                                break;
                            }

                            if (w.ElapsedMilliseconds > 10000)
                            {
                                throw new InvalidOperationException("couldn't initialize the SM");
                            }

                            Thread.Sleep(100);
                        }

                        sm.InitiateBootstrap(nodes, 10);

                        return sm;
                    }
                }
                catch (Exception e)
                {
                    try
                    {
                        sm.Log("Exception during InitializeStateMachine {0}", e);
                    }
                    catch (Exception)
                    {
                        // ignore
                    }

                    sm.Dispose();
                    throw;
                }
            }

            public static ushort GetFreePort()
            {
                // Evaluate current system tcp connections. This is the same information provided
                // by the netstat command line application, just in .Net strongly-typed object
                // form.  We will look through the list to find a port that is not used.
                IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
                int[] usedPorts = tcpConnInfoArray.Select(connectionInformation => connectionInformation.LocalEndPoint.Port).ToArray();

                int i;

                while ((i = Interlocked.Increment(ref basePort)) < 65536)
                {
                    if (!usedPorts.Contains(i))
                    {
                        return (ushort)i;
                    }
                }

                throw new KeyNotFoundException("cannot find a free port");
            }

            /// <summary>
            /// Setups the configuration.
            /// </summary>
            /// <param name="rslPath">the path to rsl</param>
            /// <returns>ManagedRSLConfigParam.</returns>
            private static ManagedRSLConfigParam SetupConfiguration(string rslPath)
            {
                ManagedRSLConfigParam cfg = new ManagedRSLConfigParam();

                try
                {
                    // the following settings affect only how the local replica will handle its own logs.
                    // bear in mind that codexes are sent to the primary and other replicas at times, so
                    // - more frequent codexes (shorter logfiles) will cause longer times to catch up since replicas
                    //   will need to use codexes instead of the log entries they missed.
                    // - larger logfiles enable better recoverability (a node can be down for longer and catch up
                    //   without requiring a codex) but it delays the restart time of replicas, since the logfile
                    //   must be replayed.
                    // - Keeping a lot of logfiles or codex files will enable a more precise "time travel", but will
                    //   consume more disk space on the replica
                    cfg.WorkingDir = rslPath;
                    cfg.MaxLogLenMB = 200;
                    cfg.MaxVotesInLog = 100000;
                    cfg.MaxCheckpoints = 4;
                    cfg.MaxCheckpointIntervalSec = (int)TimeSpan.FromMinutes(10).TotalSeconds;
                    cfg.MaxLogs = 10;
                    cfg.LogLenRandomize = 15;

                    // When a node is primary, it sends heartbeats to the rest.
                    // - The frequency of those (HeartBeatIntervalSec) determines the fault detection time.
                    // - The grace period is the time a replica needs to start suspecting from the primary if no HN is seen.
                    // - After a replica suspects from a primary, it will wait for
                    //   "ElectionDelaySec" seconds before initiating another primary election. The longer this value,
                    //   the fewer unnecessary reconfigurations due to partial failures, but the longer fault recovery
                    //   on real failures.
                    // - The randomization times reduces the chances for two replicas to try at the same time to become
                    //   primary when they suspect from the primary (often they will suspect nearly at the same time).
                    cfg.HeartBeatIntervalSec = 1;
                    cfg.NewLeaderGracePeriodSec = 5;
                    cfg.ElectionDelaySec = 2;
                    cfg.ElectionRandomize = 2;
                    cfg.MaxElectionRandomizeSec = 1;
                    cfg.AllowPrimaryPromotionWhileCatchingUp = false;

                    // Retry limits and intervals are intended to cover for partial, transient, short term failures.
                    // the larger these values, the more chances a transient failure will be cover will exist. However,
                    // larger values will geopardize the fault detection time when a real failure happened.
                    cfg.InitializeRetryIntervalSec = 1;
                    cfg.PrepareRetryIntervalSec = 1;
                    cfg.VoteRetryIntervalSec = 3;
                    cfg.VoteMaxOutstandingIntervalSec = 480;
                    cfg.CPQueryRetryIntervalSec = 5;

                    // Longer timeouts and larger message size limit allow for bigger transactions, but they also
                    // geopardize fault detection time, and unresponsiveness of the primary during replication
                    cfg.SendTimeoutSec = 5;
                    cfg.ReceiveTimeoutSec = 5;
                    cfg.MaxMessageSizeMB = 400;

                    cfg.MaxOutstandingPerReplica = 10;
                    cfg.MaxCacheLengthMB = 50;
                    cfg.FastReadSeqThreshold = 0;

                    return cfg;
                }
                catch (Exception)
                {
                    cfg.Dispose();
                    throw;
                }
            }
        }

        [Serializable]
        private class TestConfig
        {
            public string Th1 { get; set; } = null;

            public string Th2 { get; set; } = null;

            public string Subject1 { get; set; } = null;

            public string ThumbprintsParent1 { get; set; } = null;

            public string Subject2 { get; set; } = null;

            public string Subjectparent2 { get; set; } = null;

            public string Th3 { get; set; } = null;

            public string Th4 { get; set; } = null;

            public ushort[] Ports { get; set; }

            public string RslData { get; set; }

            public string LogPath { get; set; }
        }

        [Serializable]
        private class AppDomainStarter
        {
            [NonSerialized]
            private AppDomain ad;

            private TestConfig testConfig;

            private bool asPrimary;

            [NonSerialized]
            private Remote remote;

            public AppDomainStarter(string name, int instance, TestConfig testConfig)
            {
                this.Name = name;
                this.Instance = instance;
                this.testConfig = testConfig;
                this.remote = new Remote(this);
            }

            public BasicStateMachine.Reference SM
            {
                get
                {
                    return this.remote.SM;
                }

                set
                {
                    this.remote.SM = value;
                }
            }

            public TestTracer Tracer
            {
                get
                {
                    return this.remote.Tracer;
                }

                set
                {
                    this.remote.Tracer = value;
                }
            }

            public string Name { get; private set; }

            public int Instance { get; private set; }

            public void Start(bool asPrimary = true)
            {
                this.asPrimary = asPrimary;

                this.ad = AppDomain.CreateDomain(this.Name);
                this.ad.SetData("remote", this.remote);

                this.ad.DoCallBack(this.Create);
                this.ad.DomainUnload += this.Ad_DomainUnloadFromOutside;
                if (asPrimary)
                {
                    this.SM.CanBP = true;
                }
            }

            public void StartInThisAppDomain(bool asPrimary = true)
            {
                this.asPrimary = asPrimary;

                this.ad = AppDomain.CurrentDomain;
                this.ad.SetData("remote", this.remote);

                this.Create();

                this.ad.DomainUnload += this.Ad_DomainUnloadFromOutside;

                if (asPrimary)
                {
                    this.SM.CanBP = true;
                }
            }

            public bool Stop()
            {
                try
                {
                    this.StopAndUnload(currentDomain: false);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }

                return true;
            }

            private void StopAndUnload(bool currentDomain)
            {
                if (currentDomain)
                {
                    if (this.Name == AppDomain.CurrentDomain.FriendlyName)
                    {
                        AppDomain.Unload(AppDomain.CurrentDomain);
                    }
                    else
                    {
                        this.Ad_DomainUnload(null, null);
                    }
                }
                else
                {
                    if (this.ad != null)
                    {
                        AppDomain.Unload(this.ad);

                        this.ad = null;
                    }
                }
            }

            private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
            {
                TestTracer tr = this.Tracer;

                string msg = string.Format("Unhandled Exception seen: sender {0} exception {1}", sender, e.ExceptionObject);

                if (tr == null)
                {
                    Console.WriteLine("replica {0} {1} {2}", this.SM.MemberId, TestTracer.EventType.Generic, msg);
                }
                else
                {
                    tr.OnEvent("replica " + this.SM.MemberId, TestTracer.EventType.Generic, msg);
                }

                this.StopAndUnload(currentDomain: true);
            }

            private void Ad_DomainUnloadFromOutside(object sender, EventArgs e)
            {
                if (this.ad != null && this.ad.NeedsRestart())
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        Thread.Sleep(2000);
                        this.Tracer.OnEvent("AppDomain", TestTracer.EventType.Generic, "Restarting instance " + this.Name);
                        this.Stop();
                        this.Start(this.asPrimary);
                    });
                }
            }

            private void Create()
            {
                this.remote = (Remote)AppDomain.CurrentDomain.GetData("remote");
                this.SM = StateMachineManager.CreateSMSec(this.Instance, this.testConfig, this.Tracer);
                AppDomain.CurrentDomain.UnhandledException += this.CurrentDomain_UnhandledException;
                AppDomain.CurrentDomain.DomainUnload += this.Ad_DomainUnload;
            }

            private void Ad_DomainUnload(object sender, EventArgs e)
            {
                if (this.SM != null)
                {
                    this.SM.DisposeThisStateMachine();
                    this.SM = null;
                }
            }

            private class Remote : MarshalByRefObject
            {
                private AppDomainStarter st;

                public Remote(AppDomainStarter st)
                {
                    this.st = st;
                }

                public BasicStateMachine.Reference SM
                {
                    get;
                    set;
                }

                public TestTracer Tracer
                {
                    get;
                    set;
                }
            }
        }
    }
}