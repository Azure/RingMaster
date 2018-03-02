// <copyright file="CertificateRulesTests.cs" company="Microsoft">
//     Copyright ©  2016
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.RingMasterCommonUnitTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Backend;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// This class contains tests for SSL wrapping
    /// </summary>
    [TestClass]
    public class CertificateRulesTests
    {
        /// <summary>
        /// This method tests Base Rule
        /// </summary>
        [TestMethod]
        public void TestBasicValidation()
        {
            CertificateRules.CertAccessor.Instance = new TestSslWrapping.TestCertAccessor();
            TestSslWrapping.TestCertificate.SetStore(() =>
            {
                Dictionary<string, TestSslWrapping.TestCertificate> dict = new Dictionary<string, TestSslWrapping.TestCertificate>();

                dict.Add("thumb1", new TestSslWrapping.TestCertificate("thumb1", "serial1", "subject1", "issuer1", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb2", new TestSslWrapping.TestCertificate("thumb2", "serial2", "subject2", "issuer2", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb3", new TestSslWrapping.TestCertificate("thumb3", "serial3", "subject3", "issuer3", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb4", new TestSslWrapping.TestCertificate("thumb4", "serial4", "subject4", "issuer4", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));

                // thumb1 was signed by thumb3, signed by thumb4
                dict["thumb1"].SignatureChainAsString = "thumb1,thumb3,thumb4";
                dict["thumb1"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };

                // thumb2 was signed by thumb3, signed by thumb4
                dict["thumb2"].SignatureChainAsString = "thumb1,thumb3,thumb4";
                dict["thumb2"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };

                return dict;
            });

            TestSslWrapping wr = new TestSslWrapping(new string[] { "thumb1" }, new string[] { "thumb2" }, CertificateValidator.DefaultFlags);
            wr.SetServerCertificate(TestSslWrapping.TestCertificate.Find("thumb2"));
            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb1"));

            wr.SslPolicyErrors = SslPolicyErrors.None;
            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);
                wr.GetValidatedStreamOnServer(client);
            }

            wr.SslPolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch;
            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);
                wr.GetValidatedStreamOnServer(client);
            }
        }

        /// <summary>
        /// This method tests Black list
        /// </summary>
        [TestMethod]
        public void TestBlackList()
        {
            CertificateRules.CertAccessor.Instance = new TestSslWrapping.TestCertAccessor();
            Dictionary<string, TestSslWrapping.TestCertificate> dict = new Dictionary<string, TestSslWrapping.TestCertificate>();
            TestSslWrapping.TestCertificate.SetStore(() =>
            {
                dict.Add("thumb0", new TestSslWrapping.TestCertificate("thumb0", "serial0", "subject0", "issuer0", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb1", new TestSslWrapping.TestCertificate("thumb1", "serial1", "subject1", "issuer1", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb2", new TestSslWrapping.TestCertificate("thumb2", "serial2", "subject2", "issuer2", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb3", new TestSslWrapping.TestCertificate("thumb3", "serial3", "subject3", "issuer3", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb4", new TestSslWrapping.TestCertificate("thumb4", "serial4", "subject4", "issuer4", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));

                // thumb0 was signed by thumb3, signed by thumb4
                dict["thumb0"].SignatureChainAsString = "thumb0,thumb3,thumb4";
                dict["thumb0"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };

                // thumb1 was signed by thumb3, signed by thumb4
                dict["thumb1"].SignatureChainAsString = "thumb1,thumb3,thumb4";
                dict["thumb1"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };

                // thumb2 was signed by thumb3, signed by thumb4
                dict["thumb2"].SignatureChainAsString = "thumb1,thumb3,thumb4";
                dict["thumb2"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };

                return dict;
            });

            CertificateRules.AbstractCertificateRule clientRule = new CertificateRules.AllowIfAllCoherentRule(
                new CertificateRules.AzureValidationRule("*", new string[] { "thumb3" }),
                new CertificateRules.BlackListThumbprintRule("thumb0")).SetAppliesTo(CertificateRules.AbstractCertificateRule.RoleToApply.ClientCert);

            CertificateRules.AbstractCertificateRule serverRule = new CertificateRules.AzureValidationRule(HelperDictionary.ContainedIn(dict["thumb2"])).SetAppliesTo(CertificateRules.AbstractCertificateRule.RoleToApply.ServerCert);

            TestSslWrapping wr = new TestSslWrapping(new string[] { "thumb1" }, new string[] { "thumb2" }, new CertificateRules.AbstractCertificateRule[] { clientRule, serverRule }, CertificateValidator.DefaultFlags);
            wr.SetServerCertificate(TestSslWrapping.TestCertificate.Find("thumb2"));
            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb1"));

            wr.SslPolicyErrors = SslPolicyErrors.None;
            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);
                wr.GetValidatedStreamOnServer(client);
            }

            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb0"));

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);

                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }

            // now, blacklist works even if there is a break-glass rule:
            clientRule = new CertificateRules.AllowIfAllCoherentRule(
                            new CertificateRules.AzureValidationRule("*", new string[] { "thumb3" }),
                            new CertificateRules.BreakGlassThumbprintRule("thumb0"),
                            new CertificateRules.BlackListThumbprintRule("thumb0")).SetAppliesTo(CertificateRules.AbstractCertificateRule.RoleToApply.ClientCert);

            serverRule = new CertificateRules.AzureValidationRule(HelperDictionary.ContainedIn(dict["thumb2"])).SetAppliesTo(CertificateRules.AbstractCertificateRule.RoleToApply.ServerCert);

            wr = new TestSslWrapping(new string[] { "thumb1", "thumb0" }, new string[] { "thumb2" }, new CertificateRules.AbstractCertificateRule[] { clientRule, serverRule }, CertificateValidator.DefaultFlags);
            wr.SetServerCertificate(TestSslWrapping.TestCertificate.Find("thumb2"));

            wr.SslPolicyErrors = SslPolicyErrors.None;
            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb0"));

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);

                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }
        }

        /// <summary>
        /// This method tests the standard rules read from config.
        /// </summary>
        [TestMethod]
        public void TestStandardRulesFromConfig()
        {
            CertificateRules.CertAccessor.Instance = new TestSslWrapping.TestCertAccessor();
            Dictionary<string, TestSslWrapping.TestCertificate> dict = new Dictionary<string, TestSslWrapping.TestCertificate>();

            TestSslWrapping.TestCertificate.SetStore(() =>
            {
                dict.Add("thumb1", new TestSslWrapping.TestCertificate("thumb1", "serial1", "subject1", "issuer1", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb2", new TestSslWrapping.TestCertificate("thumb2", "serial2", "subject2", "issuer2", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb3", new TestSslWrapping.TestCertificate("thumb3", "serial3", "subject3", "issuer3", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb4", new TestSslWrapping.TestCertificate("thumb4", "serial4", "subject4", "issuer4", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb5", new TestSslWrapping.TestCertificate("thumb5", "serial5", "subject5", "issuer5", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb6", new TestSslWrapping.TestCertificate("thumb6", "serial6", "subject6", "issuer6", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));

                // thumb1 was signed by thumb3, signed by thumb4
                dict["thumb1"].SignatureChainAsString = "thumb1,thumb3,thumb4";
                dict["thumb1"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };

                // thumb2 was signed by thumb3, signed by thumb4
                dict["thumb2"].SignatureChainAsString = "thumb2,thumb3,thumb4";
                dict["thumb2"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };

                return dict;
            });

            CertificateRules.CertificateRulesFlags flags;
            Dictionary<string, string> settings = new Dictionary<string, string>();
            settings["SSL.BlacklistedThumbprints"] = "thumb3";
            settings["SSL.BreakGlassThumbprints"] = "thumb4";
            settings["SSL.AllowedSubjectNames"] = "subject1,subject2,subject3";
            settings["SSL.AllowedSigningThumbprints"] = "thumb3,*,*";
            settings["SSL.MaxValidityCertsInDays"] = "99";
            settings["SSL.RelaxValidationForTestCertificates"] = "false";

            var rules = CertificateValidator.GetCertRulesFromConfig(out flags, (s) => { return settings[s]; });

            TestSslWrapping wr = new TestSslWrapping(new string[] { "thumb1" }, new string[] { "thumb2" }, rules, flags);

            wr.SslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;
            wr.SetServerCertificate(TestSslWrapping.TestCertificate.Find("thumb3"));
            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb4"));

            // break-glass and blacklist verification
            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnServer(client);
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnClient("server1", client);
                    });
            }

            // allowed
            wr.SetServerCertificate(TestSslWrapping.TestCertificate.Find("thumb2"));
            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb1"));

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);
                wr.GetValidatedStreamOnServer(client);
            }

            // invalid expiration time
            dict["thumb1"].TestNotAfter = dict["thumb1"].TestNotBefore + TimeSpan.FromDays(1000);

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }
        }

        /// <summary>
        /// This method tests the standard rules.
        /// </summary>
        [TestMethod]
        public void TestStandardRules()
        {
            CertificateRules.CertAccessor.Instance = new TestSslWrapping.TestCertAccessor();
            Dictionary<string, TestSslWrapping.TestCertificate> dict = new Dictionary<string, TestSslWrapping.TestCertificate>();

            TestSslWrapping.TestCertificate.SetStore(() =>
            {
                dict.Add("thumb1", new TestSslWrapping.TestCertificate("thumb1", "serial1", "subject1", "issuer1", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb2", new TestSslWrapping.TestCertificate("thumb2", "serial2", "subject2", "issuer2", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb3", new TestSslWrapping.TestCertificate("thumb3", "serial3", "subject3", "issuer3", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb4", new TestSslWrapping.TestCertificate("thumb4", "serial4", "subject4", "issuer4", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));

                // thumb1 was signed by thumb3, signed by thumb4
                dict["thumb1"].SignatureChainAsString = "thumb1,thumb3,thumb4";
                dict["thumb1"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };

                // thumb2 was signed by thumb3, signed by thumb4
                dict["thumb2"].SignatureChainAsString = "thumb2,thumb3,thumb4";
                dict["thumb2"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };

                return dict;
            });

            CertificateRules.StandardValidationRuleSet sr = new CertificateRules.StandardValidationRuleSet();

            TestSslWrapping wr = new TestSslWrapping(new string[] { "thumb1" }, new string[] { "thumb2" }, sr.Rules, CertificateValidator.DefaultFlags);

            wr.SslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;
            wr.SetServerCertificate(TestSslWrapping.TestCertificate.Find("thumb2"));
            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb1"));

            // nothing allowed.
            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnClient("server1", client);
                    });
            }

            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }

            // allow wrong thumbprint
            sr.AddAcceptedCertificateThumbprint(CertificateRules.AbstractCertificateRule.RoleToApply.ClientCert, "thumb4");
            sr.AddAcceptedCertificateThumbprint(CertificateRules.AbstractCertificateRule.RoleToApply.ServerCert, "thumb3");

            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnClient("server1", client);
                    });
            }

            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }

            // allow right client thumbprint
            sr.AddAcceptedCertificateThumbprint(CertificateRules.AbstractCertificateRule.RoleToApply.ClientCert, "thumb1");

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnServer(client);

                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnClient("server1", client);
                    });
            }

            // allow right server thumbprint
            sr.AddAcceptedCertificateThumbprint(CertificateRules.AbstractCertificateRule.RoleToApply.ServerCert, "thumb2");

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnServer(client);

                wr.GetValidatedStreamOnClient("server1", client);
            }

            // clear all
            sr.Clear(CertificateRules.AbstractCertificateRule.RoleToApply.AllCerts);

            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnClient("server1", client);
                    });
            }

            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }

            // allow right client thumbprints again, and make one cert "bad"
            sr.AddAcceptedCertificateThumbprint(CertificateRules.AbstractCertificateRule.RoleToApply.ClientCert, "thumb1");
            sr.AddAcceptedCertificateThumbprint(CertificateRules.AbstractCertificateRule.RoleToApply.ServerCert, "thumb2");

            dict["thumb1"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.UntrustedRoot) };

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);

                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }

            // break-glass that bad cert
            sr.AddBreakGlassCertificateThumbprint(CertificateRules.AbstractCertificateRule.RoleToApply.ClientCert, "thumb1");

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);
                wr.GetValidatedStreamOnServer(client);
            }

            // blacklist that break-glass cert
            sr.AddBlacklistedCertificateThumbprint(CertificateRules.AbstractCertificateRule.RoleToApply.ClientCert, "thumb1");

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);

                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }

            // clear all
            sr.Clear(CertificateRules.AbstractCertificateRule.RoleToApply.AllCerts);
            dict["thumb1"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };

            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnClient("server1", client);
                    });
            }

            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }

            // allow right client subjects
            sr.AddAcceptedCertificateSubject(CertificateRules.AbstractCertificateRule.RoleToApply.ClientCert, "subject1", new string[] { "thumb3" });
            sr.AddAcceptedCertificateSubject(CertificateRules.AbstractCertificateRule.RoleToApply.ServerCert, "subject2", new string[] { "thumb3" });

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);
                wr.GetValidatedStreamOnServer(client);
            }

            // make sure that subjects are not allowing all
            wr.SetServerCertificate(TestSslWrapping.TestCertificate.Find("thumb4"));
            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb4"));

            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnClient("server1", client);
                    });
            }

            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }

            // restart and validate the expiration limit
            sr.Clear(CertificateRules.AbstractCertificateRule.RoleToApply.AllCerts);

            wr.SetServerCertificate(TestSslWrapping.TestCertificate.Find("thumb2"));
            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb1"));

            sr.AddAcceptedCertificateSubject(CertificateRules.AbstractCertificateRule.RoleToApply.ClientCert, "subject1", new string[] { "thumb3" });
            sr.AddAcceptedCertificateSubject(CertificateRules.AbstractCertificateRule.RoleToApply.ServerCert, "subject2", new string[] { "thumb3" });
            dict["thumb1"].TestNotAfter = dict["thumb1"].TestNotBefore + TimeSpan.FromDays(100);

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);
                wr.GetValidatedStreamOnServer(client);
            }

            sr.SetMaxExpirationTime(CertificateRules.AbstractCertificateRule.RoleToApply.AllCerts, 101);

            dict["thumb1"].TestNotAfter = dict["thumb1"].TestNotBefore + TimeSpan.FromDays(100);

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);
                wr.GetValidatedStreamOnServer(client);
            }

            sr.SetMaxExpirationTime(CertificateRules.AbstractCertificateRule.RoleToApply.AllCerts, 99);

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }
        }

        /// <summary>
        /// This method tests untrusted chain
        /// </summary>
        [TestMethod]
        public void TestUntrustedChain()
        {
            CertificateRules.CertAccessor.Instance = new TestSslWrapping.TestCertAccessor();
            TestSslWrapping.TestCertificate.SetStore(() =>
            {
                Dictionary<string, TestSslWrapping.TestCertificate> dict = new Dictionary<string, TestSslWrapping.TestCertificate>();

                dict.Add("thumb1", new TestSslWrapping.TestCertificate("thumb1", "serial1", "subject1", "issuer1", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb2", new TestSslWrapping.TestCertificate("thumb2", "serial2", "subject2", "issuer2", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb3", new TestSslWrapping.TestCertificate("thumb3", "serial3", "subject3", "issuer3", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb4", new TestSslWrapping.TestCertificate("thumb4", "serial4", "subject4", "issuer4", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));

                // thumb1 was signed by thumb3, signed by thumb4
                dict["thumb1"].SignatureChainAsString = "thumb1,thumb3,thumb4";
                dict["thumb1"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.UntrustedRoot) };

                // thumb2 was signed by thumb3, signed by thumb4
                dict["thumb2"].SignatureChainAsString = "thumb2,thumb3,thumb4";
                dict["thumb2"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.UntrustedRoot) };

                return dict;
            });

            TestSslWrapping wr = new TestSslWrapping(new string[] { "thumb1" }, new string[] { "thumb2" }, CertificateValidator.DefaultFlags);
            wr.SslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;
            wr.SetServerCertificate(TestSslWrapping.TestCertificate.Find("thumb2"));
            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb1"));

            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnClient("server1", client);
                    });
            }

            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }
        }

        /// <summary>
        /// This method tests white lists
        /// </summary>
        [TestMethod]
        public void TastBreakGlass()
        {
            CertificateRules.CertAccessor.Instance = new TestSslWrapping.TestCertAccessor();
            Dictionary<string, TestSslWrapping.TestCertificate> dict = new Dictionary<string, TestSslWrapping.TestCertificate>();

            TestSslWrapping.TestCertificate.SetStore(() =>
            {
                dict.Add("thumb1", new TestSslWrapping.TestCertificate("thumb1", "serial1", "subject1", "issuer1", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb2", new TestSslWrapping.TestCertificate("thumb2", "serial2", "subject2", "issuer2", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb3", new TestSslWrapping.TestCertificate("thumb3", "serial3", "subject3", "issuer3", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb4", new TestSslWrapping.TestCertificate("thumb4", "serial4", "subject4", "issuer4", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));

                // thumb1 was signed by thumb3, signed by thumb4
                dict["thumb1"].SignatureChainAsString = "thumb1,thumb3,thumb4";
                dict["thumb1"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.UntrustedRoot) };

                // thumb2 was signed by thumb3, signed by thumb4
                dict["thumb2"].SignatureChainAsString = "thumb2,thumb3,thumb4";
                dict["thumb2"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.UntrustedRoot) };

                return dict;
            });

            // on client, allow only subject2, but also break-glass thumb4
            CertificateRules.AbstractCertificateRule clientRule = new CertificateRules.AllowIfAllCoherentRule(
                new CertificateRules.AzureValidationRule("subject2", new string[] { "*" }),
                new CertificateRules.BreakGlassThumbprintRule("thumb4")).SetAppliesTo(CertificateRules.AbstractCertificateRule.RoleToApply.ClientCert);

            // on server, allow only thumb2, but break-glass thumb3
            CertificateRules.AbstractCertificateRule serverRule = new CertificateRules.AllowIfAllCoherentRule(
                new CertificateRules.AzureValidationRule(HelperDictionary.ContainedIn(dict["thumb2"])),
                new CertificateRules.BreakGlassThumbprintRule("thumb3")).SetAppliesTo(CertificateRules.AbstractCertificateRule.RoleToApply.ServerCert);

            TestSslWrapping wr = new TestSslWrapping(new string[] { "thumb1" }, new string[] { "thumb2" }, new CertificateRules.AbstractCertificateRule[] { clientRule, serverRule }, CertificateValidator.DefaultFlags);
            wr.SetServerCertificate(dict["thumb2"]);

            wr.SetClientCertificate(dict["thumb1"]);
            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }

            wr.SetClientCertificate(dict["thumb4"]);
            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnServer(client);
            }

            wr.SetServerCertificate(dict["thumb4"]);
            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnClient("server1", client);
                    });
            }

            wr.SetServerCertificate(dict["thumb3"]);
            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);
            }
        }

        /// <summary>
        /// This method tests no CRL available
        /// </summary>
        [TestMethod]
        public void TestNoCRL()
        {
            CertificateRules.CertAccessor.Instance = new TestSslWrapping.TestCertAccessor();
            TestSslWrapping.TestCertificate.SetStore(() =>
            {
                Dictionary<string, TestSslWrapping.TestCertificate> dict = new Dictionary<string, TestSslWrapping.TestCertificate>();

                dict.Add("thumb1", new TestSslWrapping.TestCertificate("thumb1", "serial1", "subject1", "issuer1", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb2", new TestSslWrapping.TestCertificate("thumb2", "serial2", "subject2", "issuer2", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb3", new TestSslWrapping.TestCertificate("thumb3", "serial3", "subject3", "issuer3", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb4", new TestSslWrapping.TestCertificate("thumb4", "serial4", "subject4", "issuer4", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));

                // thumb1 was signed by thumb3, signed by thumb4
                // dict["thumb1"].SignatureChainAsString = TestSslWrapping.TestCertificate.NoSignatureChain;
                dict["thumb1"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.OfflineRevocation) };

                // thumb2 was signed by thumb3, signed by thumb4
                // dict["thumb2"].SignatureChainAsString = TestSslWrapping.TestCertificate.NoSignatureChain;
                dict["thumb2"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.OfflineRevocation) };

                return dict;
            });

            TestSslWrapping wr = new TestSslWrapping(new string[] { "thumb1" }, new string[] { "thumb2" }, CertificateValidator.DefaultFlags);
            wr.SslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;
            wr.SetServerCertificate(TestSslWrapping.TestCertificate.Find("thumb2"));
            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb1"));

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);
                wr.GetValidatedStreamOnServer(client);
            }
        }

        /// <summary>
        /// This method tests no cert available
        /// </summary>
        [TestMethod]
        public void TestNoCert()
        {
            CertificateRules.CertAccessor.Instance = new TestSslWrapping.TestCertAccessor();
            TestSslWrapping.TestCertificate.SetStore(() =>
            {
                Dictionary<string, TestSslWrapping.TestCertificate> dict = new Dictionary<string, TestSslWrapping.TestCertificate>();

                dict.Add("thumb1", new TestSslWrapping.TestCertificate("thumb1", "serial1", "subject1", "issuer1", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb2", new TestSslWrapping.TestCertificate("thumb2", "serial2", "subject2", "issuer2", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb3", new TestSslWrapping.TestCertificate("thumb3", "serial3", "subject3", "issuer3", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb4", new TestSslWrapping.TestCertificate("thumb4", "serial4", "subject4", "issuer4", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));

                // thumb1 was signed by thumb3, signed by thumb4
                // dict["thumb1"].SignatureChainAsString = "thumb1,thumb3,thumb4";
                dict["thumb1"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };

                // thumb2 was signed by thumb3, signed by thumb4
                // dict["thumb2"].SignatureChainAsString = "thumb1,thumb3,thumb4";
                dict["thumb2"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };

                return dict;
            });

            TestSslWrapping wr = new TestSslWrapping(new string[] { "thumb1" }, new string[] { "thumb2" }, CertificateValidator.DefaultFlags);
            wr.SslPolicyErrors = SslPolicyErrors.RemoteCertificateNotAvailable;
            wr.SetServerCertificate(TestSslWrapping.TestCertificate.Find("thumb2"));
            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb1"));

            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnClient("server1", client);
                    });
            }

            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }
        }

        /// <summary>
        /// This method tests bad chains
        /// </summary>
        [TestMethod]
        public void TestBadChain()
        {
            CertificateRules.CertAccessor.Instance = new TestSslWrapping.TestCertAccessor();
            Dictionary<string, TestSslWrapping.TestCertificate> dict = new Dictionary<string, TestSslWrapping.TestCertificate>();

            TestSslWrapping.TestCertificate.SetStore(() =>
            {
                dict.Add("thumb1", new TestSslWrapping.TestCertificate("thumb1", "serial1", "subject1", "issuer1", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb2", new TestSslWrapping.TestCertificate("thumb2", "serial2", "subject2", "issuer2", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb3", new TestSslWrapping.TestCertificate("thumb3", "serial3", "subject3", "issuer3", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb4", new TestSslWrapping.TestCertificate("thumb4", "serial4", "subject4", "issuer4", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));

                // thumb1 was signed by thumb3, signed by thumb4
                // dict["thumb1"].SignatureChainAsString = "thumb1,thumb3,thumb4";
                dict["thumb1"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };
                dict["thumb1"].SignatureChainAsString = "thumb1,thumb3,thumb4";
                
                // thumb2 was signed by thumb3, signed by thumb4
                // dict["thumb2"].SignatureChainAsString = "thumb1,thumb3,thumb4";
                dict["thumb2"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };
                dict["thumb2"].SignatureChainAsString = "thumb2,thumb3,thumb4";
                return dict;
            });

            TestSslWrapping wr = new TestSslWrapping(new string[] { "thumb1" }, new string[] { "thumb2" }, CertificateValidator.DefaultFlags);
            wr.SslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;
            wr.SetServerCertificate(TestSslWrapping.TestCertificate.Find("thumb2"));
            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb1"));

            foreach (X509ChainStatusFlags val in Enum.GetValues(typeof(X509ChainStatusFlags)))
            {
                dict["thumb1"].SignatureStatus[0] = dict["thumb2"].SignatureStatus[0] = TestSslWrapping.TestCertificate.CreateStatus(val);

                if (val == X509ChainStatusFlags.NoError || val == X509ChainStatusFlags.RevocationStatusUnknown || val == X509ChainStatusFlags.OfflineRevocation)
                {
                    using (TestTcpClient client = new TestTcpClient())
                    {
                        wr.GetValidatedStreamOnClient("server1", client);
                        wr.GetValidatedStreamOnServer(client);
                    }
                }
                else
                {
                    using (TestTcpClient client = new TestTcpClient())
                    {
                        MustThrow<TestSslWrapping.TestValidationException>(
                            () =>
                            {
                                wr.GetValidatedStreamOnClient("server1", client);
                            }, 
                            "status=" + val);
                    }

                    using (TestTcpClient client = new TestTcpClient())
                    {
                        MustThrow<TestSslWrapping.TestValidationException>(
                            () =>
                            {
                                wr.GetValidatedStreamOnServer(client);
                            }, 
                            "status=" + val);
                    }
                }
            }
        }

        /// <summary>
        /// This method tests allow subject rule
        /// </summary>
        [TestMethod]
        public void TestAllowSubject()
        {
            CertificateRules.CertAccessor.Instance = new TestSslWrapping.TestCertAccessor();
            Dictionary<string, TestSslWrapping.TestCertificate> dict = new Dictionary<string, TestSslWrapping.TestCertificate>();

            TestSslWrapping.TestCertificate.SetStore(() =>
            {
                dict.Add("thumb1", new TestSslWrapping.TestCertificate("thumb1", "serial1", "subject1", "issuer1", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb2", new TestSslWrapping.TestCertificate("thumb2", "serial2", "subject2", "issuer2", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb3", new TestSslWrapping.TestCertificate("thumb3", "serial3", "subject3", "issuer3", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb4", new TestSslWrapping.TestCertificate("thumb4", "serial4", "subject4", "issuer4", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));

                // thumb1 was signed by thumb4
                dict["thumb1"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };
                dict["thumb1"].SignatureChainAsString = "thumb1,thumb4";

                // thumb2 was signed by thumb4
                dict["thumb2"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };
                dict["thumb2"].SignatureChainAsString = "thumb2,thumb4";

                // thumb3 was signed by thumb4
                dict["thumb3"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };
                dict["thumb3"].SignatureChainAsString = "thumb3,thumb4";
                return dict;
            });

            TestSslWrapping wr = new TestSslWrapping(new string[] { "thumb1" }, new string[] { "thumb2" }, CertificateValidator.DefaultFlags);

            wr.SslPolicyErrors = SslPolicyErrors.None;
            wr.SetServerCertificate(TestSslWrapping.TestCertificate.Find("thumb2"));
            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb3"));

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);

                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }

            CertificateRules.AbstractCertificateRule[] rules = new CertificateRules.AbstractCertificateRule[] 
            {
                new CertificateRules.AllowCertSubjectRule(new string[] { "subject3" }).SetAppliesTo(CertificateRules.AbstractCertificateRule.RoleToApply.ClientCert),
                new CertificateRules.AllowCertificatesRule(HelperDictionary.ContainedIn(dict["thumb2"])).SetAppliesTo(CertificateRules.AbstractCertificateRule.RoleToApply.ServerCert),
            };

            wr = new TestSslWrapping(new string[] { "thumb1" }, new string[] { "thumb2" }, rules, CertificateValidator.DefaultFlags);
            wr.SslPolicyErrors = SslPolicyErrors.None;
            wr.SetServerCertificate(TestSslWrapping.TestCertificate.Find("thumb2"));
            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb3"));

            using (TestTcpClient client = new TestTcpClient())
            {
                wr.GetValidatedStreamOnClient("server1", client);
                wr.GetValidatedStreamOnServer(client);
            }
        }

        /// <summary>
        /// This method tests certificate expired
        /// </summary>
        [TestMethod]
        public void TestExpiredCert()
        {
            CertificateRules.CertAccessor.Instance = new TestSslWrapping.TestCertAccessor();
            Dictionary<string, TestSslWrapping.TestCertificate> dict = new Dictionary<string, TestSslWrapping.TestCertificate>();

            TestSslWrapping.TestCertificate.SetStore(() =>
            {
                dict.Add("thumb1", new TestSslWrapping.TestCertificate("thumb1", "serial1", "subject1", "issuer1", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow - TimeSpan.FromDays(1)));
                dict.Add("thumb2", new TestSslWrapping.TestCertificate("thumb2", "serial2", "subject2", "issuer2", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb3", new TestSslWrapping.TestCertificate("thumb3", "serial3", "subject3", "issuer3", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));
                dict.Add("thumb4", new TestSslWrapping.TestCertificate("thumb4", "serial4", "subject4", "issuer4", DateTime.UtcNow - TimeSpan.FromDays(2), DateTime.UtcNow + TimeSpan.FromDays(2)));

                // thumb1 was signed by thumb4
                dict["thumb1"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };
                dict["thumb1"].SignatureChainAsString = "thumb1,thumb4";

                // thumb2 was signed by thumb4
                dict["thumb2"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };
                dict["thumb2"].SignatureChainAsString = "thumb2,thumb4";

                // thumb3 was signed by thumb4
                dict["thumb3"].SignatureStatus = new X509ChainStatus[] { TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError), TestSslWrapping.TestCertificate.CreateStatus(X509ChainStatusFlags.NoError) };
                dict["thumb3"].SignatureChainAsString = "thumb3,thumb4";
                return dict;
            });

            TestSslWrapping wr;
            MustThrow<ArgumentException>(
                () =>
                {
                    wr = new TestSslWrapping(new string[] { "thumb1" }, new string[] { "thumb2" }, CertificateValidator.DefaultFlags);
                });

            CertificateRules.AbstractCertificateRule[] rules = new CertificateRules.AbstractCertificateRule[]
            {
                new CertificateRules.AzureValidationRule("subject1", new string[] { "*" }),
                new CertificateRules.AzureValidationRule(HelperDictionary.ContainedIn(dict["thumb3"])).SetAppliesTo(CertificateRules.AbstractCertificateRule.RoleToApply.ClientCert),
                new CertificateRules.AzureValidationRule(HelperDictionary.ContainedIn(dict["thumb2"])).SetAppliesTo(CertificateRules.AbstractCertificateRule.RoleToApply.ServerCert)
            };

            wr = new TestSslWrapping(new string[] { "thumb3" }, new string[] { "thumb2" }, rules, CertificateValidator.DefaultFlags);

            wr.SslPolicyErrors = SslPolicyErrors.None;
            wr.SetServerCertificate(TestSslWrapping.TestCertificate.Find("thumb2"));
            wr.SetClientCertificate(TestSslWrapping.TestCertificate.Find("thumb1"));

            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnClient("server1", client);
                    });
            }

            using (TestTcpClient client = new TestTcpClient())
            {
                MustThrow<TestSslWrapping.TestValidationException>(
                    () =>
                    {
                        wr.GetValidatedStreamOnServer(client);
                    });
            }
        }

        /// <summary>
        /// Fails if an exception of the given type is not thrown by the action
        /// </summary>
        /// <typeparam name="T">the expected exception</typeparam>
        /// <param name="action">the action to run</param>
        /// <param name="message">the message for the error</param>
        private static void MustThrow<T>(Action action, string message = null) where T : Exception
        {
            if (message == null)
            {
                message = string.Empty;
            }

            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            catch (Exception e)
            {
                Assert.Fail("Exception seen, but wrong type (Expected: " + typeof(T).Name + ")" + message + ". Seen exception was " + e);
            }

            Assert.Fail("No exception seen, (Expected: " + typeof(T).Name + ") " + message);
        }

        /// <summary>
        /// Helper to simplify access to ad-hoc collections' contain method
        /// </summary>
        private class HelperDictionary
        {
            /// <summary>
            /// returns the Contain function for an array passed as implicit array parameter
            /// </summary>
            /// <param name="list">the list of items contained in the response</param>
            /// <returns>the function to evaluate if an item is in the list</returns>
            public static Func<X509Certificate, bool> ContainedIn(params X509Certificate[] list)
            {
                return c =>
                {
                    foreach (X509Certificate inList in list)
                    {
                        if (CertificateRules.CertAccessor.Instance.Equals(inList, c))
                        {
                            return true;
                        }
                    }

                    return false;
                };
            }
        }

        /// <summary>
        /// test dummy TCP client
        /// </summary>
        private class TestTcpClient : TcpClient
        {
            /// <summary>
            /// fake listener
            /// </summary>
            private TcpListener listener;
            
            /// <summary>
            /// fake server socket
            /// </summary>
            private Socket serverSocket;

            /// <summary>
            /// Initializes a new instance of the <see cref="TestTcpClient"/> class.
            /// </summary>
            /// <param name="basePort">the base port to start looking for an available port</param>
            public TestTcpClient(int basePort = 10240)
            {
                int port = basePort;
                while (true)
                {
                    try
                    {
                        this.listener = new TcpListener(IPAddress.Any, port);
                        this.listener.Start();
                        Task<Socket> acceptTask = this.listener.AcceptSocketAsync();
                        this.Connect(IPAddress.Loopback, port);
                        this.serverSocket = acceptTask.GetAwaiter().GetResult();
                        return;
                    }
                    catch (Exception)
                    {
                        this.Cleanup();
                        port++;
                    }
                }
            }

            /// <summary>
            /// disposes the instance
            /// </summary>
            /// <param name="disposing">if true this is a disposal</param>
            protected override void Dispose(bool disposing)
            {
                if (!disposing)
                {
                    return;
                }

                this.Cleanup();
            }

            /// <summary>
            /// cleans the state up 
            /// </summary>
            private void Cleanup()
            {
                if (this.listener != null)
                {
                    this.listener.Stop();
                    this.listener = null;
                }

                if (this.serverSocket != null)
                {
                    this.serverSocket.Close();
                    this.serverSocket = null;
                }
            }
        }

        /// <summary>
        /// Test dummy SSL wrapping object
        /// </summary>
        private class TestSslWrapping : SslWrapping
        {
            /// <summary>
            /// certificate sent by server
            /// </summary>
            private X509Certificate serverCert;

            /// <summary>
            /// certificate sent by client
            /// </summary>
            private X509Certificate clientCert;

            /// <summary>
            /// Initializes a new instance of the <see cref="TestSslWrapping"/> class.
            /// </summary>
            /// <param name="clientThumbprints">the client thumbprints to use</param>
            /// <param name="serverThumbprints">the server thumbprints to use</param>
            /// <param name="extraRules">any additional rule</param>
            /// <param name="flags">flags of the wrapping</param>
            public TestSslWrapping(string[] clientThumbprints, string[] serverThumbprints, IEnumerable<CertificateRules.AbstractCertificateRule> extraRules, CertificateRules.CertificateRulesFlags flags) 
                : base(clientThumbprints, serverThumbprints, extraRules, flags)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="TestSslWrapping"/> class.
            /// </summary>
            /// <param name="clientThumbprints">the client thumbprints to use</param>
            /// <param name="serverThumbprints">the server thumbprints to use</param>
            /// <param name="flags">flags of the wrapping</param>
            public TestSslWrapping(string[] clientThumbprints, string[] serverThumbprints, CertificateRules.CertificateRulesFlags flags)
                : base(clientThumbprints, serverThumbprints, flags)
            {
            }

            /// <summary>
            /// Gets or sets a value indicating the policy errors to be presented
            /// </summary>
            public SslPolicyErrors SslPolicyErrors { get; set; }

            /// <summary>
            /// sets the server cert to be seen at the client.
            /// </summary>
            /// <param name="serverCert">certificate sent by server</param>
            internal void SetServerCertificate(X509Certificate serverCert)
            {
                this.serverCert = serverCert;
            }

            /// <summary>
            /// sets the client cert to be seen at the server.
            /// </summary>
            /// <param name="clientCert">certificate sent by client</param>
            internal void SetClientCertificate(X509Certificate clientCert)
            {
                this.clientCert = clientCert;
            }

            /// <summary>
            /// creates an SSL stream for the inner stream
            /// </summary>
            /// <param name="innerStream">the non SSL stream</param>
            /// <param name="leaveInnerStreamOpen">if true it will not close the inner stream</param>
            /// <param name="remoteCertValidationCallback">validation callback</param>
            /// <param name="localCertValidationCallback">selection callback</param>
            /// <returns>the validated SSL stream</returns>
            protected override SslStream CreateSslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback remoteCertValidationCallback, LocalCertificateSelectionCallback localCertValidationCallback)
            {
                return new TestSslStream(this, innerStream, leaveInnerStreamOpen, remoteCertValidationCallback, localCertValidationCallback);
            }

            /// <summary>
            /// Test certificate accessor
            /// </summary>
            public class TestCertAccessor : CertificateRules.CertAccessor
            {
                /// <summary>
                /// returns the certificate for the given thumbprints
                /// </summary>
                /// <param name="thumbprint">thumbprints to query</param>
                /// <returns>certs found for each thumbprint</returns>
                public override X509Certificate[] GetCertsFromThumbPrintOrFileName(string[] thumbprint)
                {
                    if (thumbprint == null)
                    {
                        return null;
                    }

                    X509Certificate[] certs = new X509Certificate[thumbprint.Length];

                    for (int i = 0; i < certs.Length; i++)
                    {
                        certs[i] = TestCertificate.Find(thumbprint[i]);
                    }

                    return certs;
                }

                /// <summary>
                /// returns the expiration time
                /// </summary>
                /// <param name="cert">queried certificate</param>
                /// <returns>expiration date</returns>
                public override DateTime NotAfter(X509Certificate cert)
                {
                    return this.AsTC(cert).TestNotAfter;
                }

                /// <summary>
                /// returns the activation date for the certificate
                /// </summary>
                /// <param name="cert">queried certificate</param>
                /// <returns>activation date</returns>
                public override DateTime NotBefore(X509Certificate cert)
                {
                    return this.AsTC(cert).TestNotBefore;
                }

                /// <summary>
                /// returns the certificate at the given position in the chain
                /// </summary>
                /// <param name="chain">the chain</param>
                /// <param name="i">the position</param>
                /// <returns>the certificate</returns>
                public override X509Certificate ChainCertificateAtPosition(X509Chain chain, int i)
                {
                    return this.AsTC(chain).ChainCertificateAtPosition(i);
                }

                /// <summary>
                /// returns the count of elements in the chain
                /// </summary>
                /// <param name="chain">the chain</param>
                /// <returns>the count</returns>
                public override int ChainElementsCount(X509Chain chain)
                {
                    return this.AsTC(chain).ChainElementsCount();
                }

                /// <summary>
                /// returns the status of the chain
                /// </summary>
                /// <param name="chain">the chain</param>
                /// <returns>the status</returns>
                public override X509ChainStatus[] ChainStatus(X509Chain chain)
                {
                    return this.AsTC(chain).TestChainStatus();
                }

                /// <summary>
                /// returns the serial number of the certificate
                /// </summary>
                /// <param name="certificate">certificate to explore</param>
                /// <returns>the serial number of the certificate</returns>
                public override string GetSerialNumberString(X509Certificate certificate)
                {
                    return this.AsTC(certificate).GetSerialNumberString();
                }

                /// <summary>
                /// returns the issuer of the certificate
                /// </summary>
                /// <param name="certificate">certificate to explore</param>
                /// <returns>the issuer of the certificate</returns>
                public override string GetIssuer(X509Certificate certificate)
                {
                    return this.AsTC(certificate).TestIssuer;
                }

                /// <summary>
                /// returns the subject of the certificate
                /// </summary>
                /// <param name="certificate">certificate to explore</param>
                /// <returns>the subject of the certificate</returns>
                public override string GetSubject(X509Certificate certificate)
                {
                    return this.AsTC(certificate).TestSubject;
                }

                /// <summary>
                /// returns the thumbprint of the certificate
                /// </summary>
                /// <param name="certificate">certificate to explore</param>
                /// <returns>the thumbprint of the certificate</returns>
                public override string GetThumbprint(X509Certificate certificate)
                {
                    return this.AsTC(certificate).TestThumbprint;
                }

                /// <summary>
                /// Computes if the two certificates are equal
                /// </summary>
                /// <param name="x">first certificate</param>
                /// <param name="y">second certificate</param>
                /// <returns>true if both certs are equal</returns>
                public override bool Equals(X509Certificate x, X509Certificate y)
                {
                    if (x == y)
                    {
                        return true;
                    }

                    if (x == null || y == null)
                    {
                        return false;
                    }

                    return
                        string.Equals(this.GetSerialNumberString(x), this.GetSerialNumberString(y)) &&
                        string.Equals(this.GetThumbprint(x), this.GetThumbprint(y)) &&
                        string.Equals(this.GetSubject(x), this.GetSubject(y));
                }

                /// <summary>
                /// Computes the hash code for the given object
                /// </summary>
                /// <param name="obj">the object to compute hash for</param>
                /// <returns>the hash code</returns>
                public override int GetHashCode(X509Certificate obj)
                {
                    if (obj == null)
                    {
                        return 0;
                    }

                    return obj.GetHashCode();
                }

                /// <summary>
                /// returns the test certificate for the given certificate
                /// </summary>
                /// <param name="cert">the certificate</param>
                /// <returns>the test certificate</returns>
                private TestCertificate AsTC(X509Certificate cert)
                {
                    TestCertificate tc = cert as TestCertificate;

                    if (tc == null)
                    {
                        throw new ArgumentException("cert is not a test certificate");
                    }

                    return tc;
                }

                /// <summary>
                /// returns the test chain certificate for the given chain certificate
                /// </summary>
                /// <param name="chain">the certificate chain</param>
                /// <returns>the test chain certificate</returns>
                private TestX509Chain AsTC(X509Chain chain)
                {
                    TestX509Chain tc = chain as TestX509Chain;

                    if (tc == null)
                    {
                        throw new ArgumentException("chain is not a test chain");
                    }

                    return tc;
                }
            }

            /// <summary>
            /// test certificate
            /// </summary>
            public class TestCertificate : X509Certificate2
            {
                /// <summary>
                /// the string indicating no signature chain was given
                /// </summary>
                public const string NoSignatureChain = "<nochain>";

                /// <summary>
                /// store of certificates by thumbprint
                /// </summary>
                private static Dictionary<string, TestCertificate> certsByThumbprint = new Dictionary<string, TestCertificate>();

                /// <summary>
                /// Initializes a new instance of the <see cref="TestCertificate"/> class.
                /// </summary>
                /// <param name="thumbprint">the thumbprint</param>
                /// <param name="serial">the serial number</param>
                /// <param name="subject">the subject</param>
                /// <param name="issuer">the issuer</param>
                /// <param name="notbefore">activation time</param>
                /// <param name="notafter">expiration time</param>
                public TestCertificate(string thumbprint, string serial, string subject, string issuer, DateTime notbefore, DateTime notafter)
                {
                    this.TestThumbprint = thumbprint;
                    this.TestSerial = serial;
                    this.TestSubject = subject;
                    this.TestIssuer = issuer;
                    this.TestNotBefore = notbefore;
                    this.TestNotAfter = notafter;
                    this.SignatureChainAsString = TestSslWrapping.TestCertificate.NoSignatureChain;
                }

                /// <summary>
                /// Gets or sets a value indicating whether the expiration time
                /// </summary>
                public DateTime TestNotAfter { get; set; }

                /// <summary>
                /// Gets or sets a value indicating whether the activation time
                /// </summary>
                public DateTime TestNotBefore { get; set; }

                /// <summary>
                /// Gets or sets a value indicating the issuer
                /// </summary>
                public string TestIssuer { get; set; }

                /// <summary>
                /// Gets or sets a value indicating the thumbprint
                /// </summary>
                public string TestThumbprint { get; set; }

                /// <summary>
                /// Gets or sets a value indicating the certificate serial number
                /// </summary>
                public string TestSerial { get; set; }

                /// <summary>
                /// Gets or sets a value indicating the certificate subject
                /// </summary>
                public string TestSubject { get; set; }

                /// <summary>
                /// Gets or sets a value indicating the signature chain, a comma-separated string of thumbprints. 
                /// </summary>
                public string SignatureChainAsString { get; set; }

                /// <summary>
                /// Gets the signature chain for this certificate
                /// </summary>
                public TestX509Chain SignatureChain
                {
                    get
                    {
                        if (this.SignatureChainAsString == null)
                        {
                            return null;
                        }

                        if (this.SignatureChainAsString == NoSignatureChain)
                        {
                            return new TestX509Chain(null, this.SignatureStatus);
                        }

                        return new TestX509Chain(this.SignatureChainAsString.Split(','), this.SignatureStatus);
                    }
                }

                /// <summary>
                /// Gets or sets the signature chain status
                /// </summary>
                public X509ChainStatus[] SignatureStatus { get; set; }

                /// <summary>
                /// returns the thumbprint
                /// </summary>
                /// <returns>thumbprint of the certificate</returns>
                public override string GetCertHashString()
                {
                    return this.TestThumbprint;
                }

                /// <summary>
                /// returns the issuer name
                /// </summary>
                /// <returns>issuer name of the certificate</returns>
                [Obsolete]
                public override string GetIssuerName()
                {
                    return this.TestIssuer;
                }

                /// <summary>
                /// returns the serial number
                /// </summary>
                /// <returns>serial number of the certificate</returns>
                public override string GetSerialNumberString()
                {
                    return this.TestSerial;
                }

                /// <summary>
                /// locates a test certificate using the given thumbprint
                /// </summary>
                /// <param name="thumbprint">the thumbprint to look for</param>
                /// <returns>the found certificate</returns>
                internal static X509Certificate Find(string thumbprint)
                {
                    return certsByThumbprint[thumbprint];
                }

                /// <summary>
                /// Gets the chain for the certificate
                /// </summary>
                /// <param name="certificate">the certificate to explore</param>
                /// <returns>the certificate chain</returns>
                internal static X509Chain GetChain(X509Certificate certificate)
                {
                    TestCertificate c = (TestCertificate)certificate;
                    return c.SignatureChain;
                }

                /// <summary>
                /// Sets the certificate store
                /// </summary>
                /// <param name="dict">the new store</param>
                internal static void SetStore(Func<Dictionary<string, TestCertificate>> dict)
                {
                    certsByThumbprint = dict();
                }

                /// <summary>
                /// Helper method to create a status instance
                /// </summary>
                /// <param name="status">the status flags</param>
                /// <returns>the status instance</returns>
                internal static X509ChainStatus CreateStatus(X509ChainStatusFlags status)
                {
                    return new X509ChainStatus() { Status = status, StatusInformation = status.ToString() };
                }
            }

            /// <summary>
            /// The certificate chain
            /// </summary>
            public class TestX509Chain : X509Chain
            {
                /// <summary>
                /// thumbprints of the chain
                /// </summary>
                private string[] thumbs;

                /// <summary>
                /// status of the chain
                /// </summary>
                private X509ChainStatus[] status;

                /// <summary>
                /// Initializes a new instance of the <see cref="TestX509Chain"/> class.
                /// </summary>
                /// <param name="thumbs">the thumbprints of the chain</param>
                /// <param name="status">the array with the different status of the chain validation</param>
                public TestX509Chain(string[] thumbs, X509ChainStatus[] status)
                {
                    this.thumbs = thumbs;
                    this.status = status;
                }

                /// <summary>
                /// returns the certificate in the given position of the chain
                /// </summary>
                /// <param name="i">the position in the chain required to retrieve</param>
                /// <returns>the certificate found</returns>
                internal X509Certificate ChainCertificateAtPosition(int i)
                {
                    return TestCertificate.Find(this.thumbs[i]);
                }

                /// <summary>
                /// indicates the length of the chain
                /// </summary>
                /// <returns>the length of the chain</returns>
                internal int ChainElementsCount()
                {
                    if (this.thumbs == null)
                    {
                        return 0;
                    }

                    return this.thumbs.Length;
                }

                /// <summary>
                /// returns the array of statuses of the chain
                /// </summary>
                /// <returns>the status array</returns>
                internal X509ChainStatus[] TestChainStatus()
                {
                    return this.status;
                }
            }

            /// <summary>
            /// Exception for this test
            /// </summary>
            public class TestValidationException : Exception
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="TestValidationException"/> class
                /// </summary>
                /// <param name="message">exception description</param>
                public TestValidationException(string message)
                    : base(message)
                {
                }
            }

            /// <summary>
            /// test SSL stream class
            /// </summary>
            public class TestSslStream : SslStream
            {
                /// <summary>
                /// remote cert validation callback
                /// </summary>
                private RemoteCertificateValidationCallback remoteCertValidationCallback;

                /// <summary>
                /// local certificate selection callback
                /// </summary>
                private LocalCertificateSelectionCallback localCertSelectionCallback;

                /// <summary>
                /// the SSL wrapping object
                /// </summary>
                private TestSslWrapping testSslWrapping;

                /// <summary>
                /// Initializes a new instance of the <see cref="TestSslStream"/> class
                /// </summary>
                /// <param name="testSslWrapping">the SSL wrapping object</param>
                /// <param name="innerStream">inner stream</param>
                /// <param name="leaveInnerStreamOpen">leave inner stream open</param>
                /// <param name="remoteCertValidationCallback">remote validation callback</param>
                /// <param name="localCertValidationCallback">local validation callback</param>
                public TestSslStream(TestSslWrapping testSslWrapping, Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback remoteCertValidationCallback, LocalCertificateSelectionCallback localCertValidationCallback)
                    : base(innerStream, leaveInnerStreamOpen, remoteCertValidationCallback, localCertValidationCallback)
                {
                    this.remoteCertValidationCallback = remoteCertValidationCallback;
                    this.localCertSelectionCallback = localCertValidationCallback;
                    this.testSslWrapping = testSslWrapping;
                }

                /// <summary>
                /// authenticates the certificate as client
                /// </summary>
                /// <param name="targetHost">name of the server</param>
                /// <param name="clientCertificates">client certs allowed</param>
                /// <param name="enabledSslProtocols">SSL protocols allowed</param>
                /// <param name="checkCertificateRevocation">revocation check needed</param>
                public override void AuthenticateAsClient(string targetHost, X509CertificateCollection clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
                {
                    if (this.localCertSelectionCallback != null)
                    {
                        this.localCertSelectionCallback(this, targetHost, clientCertificates, this.testSslWrapping.serverCert, new string[] { "*" });
                    }

                    bool ok = this.remoteCertValidationCallback(this, this.testSslWrapping.serverCert, TestCertificate.GetChain(this.testSslWrapping.serverCert), this.testSslWrapping.SslPolicyErrors);

                    if (!ok)
                    {
                        throw new TestValidationException("validation failed");
                    }
                }

                /// <summary>
                /// authenticates the certificate as server
                /// </summary>
                /// <param name="serverCertificate">the server cert</param>
                /// <param name="clientCertificateRequired">client cert is required</param>
                /// <param name="enabledSslProtocols">SSL protocols</param>
                /// <param name="checkCertificateRevocation">revocation check needed</param>
                public override void AuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
                {
                    if (this.localCertSelectionCallback != null)
                    {
                        X509CertificateCollection coll = new X509CertificateCollection();
                        coll.Add(serverCertificate);
                        this.localCertSelectionCallback(this, "localhost", coll, this.testSslWrapping.serverCert, new string[] { "*" });
                    }

                    bool ok = this.remoteCertValidationCallback(this, this.testSslWrapping.clientCert, TestCertificate.GetChain(this.testSslWrapping.clientCert), this.testSslWrapping.SslPolicyErrors);

                    if (!ok)
                    {
                        throw new TestValidationException("validation failed");
                    }
                }
            }
        }
    }
}
