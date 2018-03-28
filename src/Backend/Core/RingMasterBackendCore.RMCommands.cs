// <copyright file="RingMasterBackendCore.RMCommands.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.AsyncCallback;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Data;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    using Code = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.RingMasterException.Code;
    using Perm = Microsoft.Azure.Networking.Infrastructure.RingMaster.Data.Acl.Perm;
    using RequestDefinitions = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests;
    using RequestResponse = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RequestResponse;
    using RingMasterRequestType = Microsoft.Azure.Networking.Infrastructure.RingMaster.Requests.RingMasterRequestType;

    /// <summary>
    /// Backend core - Ring Master Commands
    /// </summary>
    public partial class RingMasterBackendCore
    {
        /// <summary>
        /// commands to RM
        /// </summary>
        protected class RMCommands
        {
            /// <summary>
            /// Commander digest
            /// </summary>
            public const string CommanderDigest = "digest:commander";

            /// <summary>
            /// command paths will not start with the ususal '/...' but with '$/...'
            /// </summary>
            private const string PrefixPath = "$/";

            /// <summary>
            /// Backend core
            /// </summary>
            private readonly RingMasterBackendCore backend;

            /// <summary>
            /// Initializes a new instance of the <see cref="RMCommands"/> class.
            /// </summary>
            /// <param name="be">Backend core</param>
            public RMCommands(RingMasterBackendCore be)
            {
                this.backend = be;
            }

            /// <summary>
            /// Gets the object used to synchronize certain actions with the rest of the system
            /// </summary>
            internal object SyncObject => new object();

            /// <summary>
            /// commands are encoded in a create operation for a path.
            /// The path contains the operation and the arguments.
            /// </summary>
            /// <param name="path">the path potentially containing the command</param>
            /// <returns>true if the path is a command</returns>
            public virtual bool IsCommand(string path)
            {
                if (path == null)
                {
                    return false;
                }

                if (path.Length > 2 && path[0] != '/' && path.StartsWith(PrefixPath))
                {
                    return true;
                }

                return false;
            }

            /// <summary>
            /// commands are encoded in a create operation for a path.
            /// This routines tells if the given request is a single command
            /// </summary>
            /// <param name="req">the request potentially containing the command</param>
            /// <returns>true if the request is a command</returns>
            public virtual bool IsCommand(IRingMasterBackendRequest req)
            {
                if (req == null)
                {
                    return false;
                }

                if (req.RequestType != RingMasterRequestType.Create)
                {
                    return false;
                }

                return this.IsCommand(req.Path);
            }

            /// <summary>
            /// Gets the list of commands
            /// </summary>
            /// <returns>Commands supported</returns>
            public virtual string GetCommands()
            {
                return "getcommands getstats dumpnodes getsetting getverbositylevel getlocaltime setverbositylevel measure trace restart die gc takecheckpoint cleanuprsl failoverinto downloadurlintolocation importcheckpoint poisonpill";
            }

            /// <summary>
            /// Determines whether the command is allowed at this time
            /// </summary>
            /// <param name="session">The session invoking the command.</param>
            /// <param name="command">The command.</param>
            /// <returns><c>true</c> if the command for the path is allowed at this time; otherwise, <c>false</c>.</returns>
            public virtual bool IsCommandAllowed(ClientSession session, string command)
            {
                bool writesOk = true;
                if (session != null)
                {
                    writesOk = session.WritesAllowed;
                }

                if (!this.backend.WritesAllowed)
                {
                    writesOk = false;
                }

                if (writesOk || command == null)
                {
                    return true;
                }

                switch (command)
                {
                    case "getstats":
                    case "dumpnodes":
                    case "getcommands":
                    case "getsetting":
                    case "getverbositylevel":
                    case "getlocaltime":
                    case "mount":
                    case "remountroot":
                    case "unmount":
                    case "remount":
                    case "buildtreefile":
                    case "setverbositylevel":
                    case "trace":
                    case "measure":
                    case "restart":
                    case "die":
                    case "gc":
                    case "cleanuprsl":
                    case "downloadurlintolocation":
                    case "importcheckpoint":
                    case "takecheckpoint":
                        return true;
                    case "failoverinto":
                    case "poisonpill":
                        return false;
                    default:
                        return false;
                }
            }

            /// <summary>
            /// runs the command encoded in the given path
            /// </summary>
            /// <param name="path">the command encoded in the path</param>
            /// <param name="data">optionally, the data associated with the request</param>
            /// <param name="session">Client session</param>
            /// <param name="lockList">the lock list of the invocation</param>
            /// <param name="requestedCommand">Requested command on return</param>
            /// <param name="content">More info on the command on return</param>
            /// <returns>code for possible error</returns>
            public Code RunCommandPath(string path, byte[] data, ClientSession session, ILockListTransaction lockList, out string requestedCommand, out object content)
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                Code result = Code.Apierror;

                requestedCommand = "unknown";
                content = "unknown command";

                if (!this.IsCommand(path))
                {
                    return result;
                }

                string[] pieces = path.Split('/');

                if (pieces.Length >= 2)
                {
                    requestedCommand = pieces[1].ToLower();

                    if (!this.IsCommandAllowed(session, requestedCommand))
                    {
                        content = "command not allowed now";
                        return Code.Apierror;
                    }

                    RingMasterEventSource.Log.RunCommand(requestedCommand);
                    this.backend.auditConsumer?.OnRunCommand(requestedCommand);

                    switch (requestedCommand)
                    {
                        case "getsetting":
                            {
                                content = RingMasterBackendCore.GetSetting(pieces[2]);
                                result = Code.Ok;
                                break;
                            }

                        case "gc":
                            {
                                GC.Collect();
                                content = "GC scheduled";
                                result = Code.Ok;
                                break;
                            }

                        case "takecheckpoint":
                            {
                                bool ok = this.backend.Factory.TakeCheckpoint();
                                content = string.Format("Checkpoint {0}scheduled", ok ? string.Empty : "not ");
                                result = Code.Ok;
                                break;
                            }

                        case "buildtreefile":
                            {
                                string commandResult;
                                string[] args = Encoding.UTF8.GetString(data).Split('|');
                                try
                                {
                                    int ver = int.Parse(args[2]);
                                    RequestResponse resp = this.backend.BuildTreeFile(nodepath: args[0], filename: args[1], version: ver);
                                    commandResult = resp.ResultCode == (int)Code.Ok ? "success" : "failed";
                                    result = (Code)resp.ResultCode;
                                }
                                catch (Exception e)
                                {
                                    commandResult = "failed:" + e.Message;
                                    result = Code.Systemerror;
                                }

                                content = string.Format("buildtreefile: nodepath={0} filename={1} result={2}", args[0], args[1], commandResult);

                                break;
                            }

                        case "mount":
                            {
                                string commandResult;
                                string[] args = Encoding.UTF8.GetString(data).Split('|');
                                try
                                {
                                    RequestResponse resp = this.backend.Mount(nodepath: args[0], filename: args[1], allowremount: false);
                                    commandResult = resp.ResultCode == (int)Code.Ok ? "success" : "failed";
                                    result = (Code)resp.ResultCode;
                                }
                                catch (Exception e)
                                {
                                    commandResult = "failed:" + e.Message;
                                    result = Code.Unknown;
                                }

                                content = string.Format("mount: nodepath={0} filename={1} result={2}", args[0], args[1], commandResult);

                                break;
                            }

                        case "remountroot":
                            {
                                string commandResult;
                                string filepath = Encoding.UTF8.GetString(data);
                                try
                                {
                                    RequestResponse resp = this.backend.Mount(nodepath: "/", filename: filepath, allowremount: true, mountRoot: true);
                                    commandResult = resp.ResultCode == (int)Code.Ok ? "success" : "failed";
                                    result = (Code)resp.ResultCode;
                                }
                                catch (Exception e)
                                {
                                    commandResult = "failed:" + e.Message;
                                    result = Code.Unknown;
                                }

                                content = string.Format("mountroot: nodepath=/ filename={0} result={1}", filepath, commandResult);

                                break;
                            }

                        case "unmount":
                            {
                                string commandResult;
                                string[] args = Encoding.UTF8.GetString(data).Split('|');

                                try
                                {
                                    RequestResponse resp = this.backend.Unmount(args[0]);
                                    commandResult = resp.ResultCode == (int)Code.Ok ? "success" : "failed";
                                    result = (Code)resp.ResultCode;
                                }
                                catch (Exception e)
                                {
                                    commandResult = "failed:" + e.Message;
                                    result = Code.Unknown;
                                }

                                content = string.Format("mount: nodepath={0} result={1}", args[0], commandResult);

                                break;
                            }

                        case "remount":
                            {
                                string commandResult;
                                string[] args = Encoding.UTF8.GetString(data).Split('|');
                                try
                                {
                                    RequestResponse resp = this.backend.Mount(nodepath: args[0], filename: args[1], allowremount: true);
                                    commandResult = resp.ResultCode == (int)Code.Ok ? "success" : "failed";
                                    result = (Code)resp.ResultCode;
                                }
                                catch (Exception e)
                                {
                                    commandResult = "failed:" + e.Message;
                                    result = Code.Unknown;
                                }

                                content = string.Format("remount: nodepath={0} result={1}", args[0], commandResult);

                                break;
                            }

                        case "measure":
                            {
                                StringBuilder sb = new StringBuilder();

                                Dictionary<string, object> results;
                                try
                                {
                                    this.backend.CanMeasure = true;
                                    ((IUnsafeTreeAccess)this.backend).LockRootNoSync();

                                    results = this.backend.MeasureFullTree(PlatformHelper.ProcessorCount, maxTimeForMeasurementInMillis);
                                }
                                finally
                                {
                                    this.backend.CanMeasure = false;
                                    ((IUnsafeTreeAccess)this.backend).ReleaseRoot();
                                }

                                if (results != null)
                                {
                                    foreach (KeyValuePair<string, object> line in results)
                                    {
                                        string text;
                                        string child_i = line.Key;

                                        Exception e = line.Value as Exception;

                                        if (e != null)
                                        {
                                            text = string.Format("Metrics for {0}: {1}", child_i, e);
                                        }
                                        else
                                        {
                                            ulong metrics = (ulong)line.Value;
                                            text = string.Format("Metrics for {0} is {1:X}", child_i, metrics);
                                        }

                                        sb.AppendLine(text);
                                    }
                                }

                                content = sb.ToString();
                                result = Code.Ok;
                                break;
                            }

                        case "getlocaltime":
                            {
                                content = string.Format("local time: " + DateTime.UtcNow.ToString("o"));
                                result = Code.Ok;
                                break;
                            }

                        case "getcommands":
                            {
                                content = this.GetCommands();
                                result = Code.Ok;
                                break;
                            }

                        case "trace":
                            {
                                string message = "<null>";
                                if (data != null)
                                {
                                    message = Encoding.UTF8.GetString(data);
                                }

                                content = "traced: " + message;
                                result = Code.Ok;
                                Trace.TraceInformation(message);
                                break;
                            }

                        case "failoverinto":
                            {
                                string replica = "<any>";
                                if (data != null)
                                {
                                    replica = Encoding.UTF8.GetString(data);
                                }

                                content = "Failing over into: " + replica + " in 10 seconds";
                                this.backend.FailoverInto(replica, 10000);
                                result = Code.Ok;
                                break;
                            }

                        case "downloadurlintolocation":
                            {
                                string arguments = null;

                                if (data != null)
                                {
                                    arguments = Encoding.UTF8.GetString(data);
                                }

                                // we run this one command asynchronously with any potential replication
                                bool hasStarted = this.backend.DownloadUrlIntoLocation(arguments);
                                if (hasStarted)
                                {
                                    content = string.Format("DownloadUrlIntoLocation started arguments={0}", arguments);
                                    result = Code.Ok;
                                }
                                else
                                {
                                    content = string.Format("DownloadUrlIntoLocation functionality is not implemented");
                                    result = Code.Unimplemented;
                                }

                                break;
                            }

                        case "poisonpill":
                            {
                                string spec = null;

                                if (data != null)
                                {
                                    spec = Encoding.UTF8.GetString(data);
                                }

                                RequestResponse res = this.GeneratePoisonPill(spec, session, lockList);
                                if (res != null)
                                {
                                    content = string.Format("poison pill executed. Result = {0}", res.Content);
                                    result = (Code)res.ResultCode;
                                }
                                else
                                {
                                    content = "Poison pills not allowed from config";
                                    result = Code.Authfailed;
                                }

                                break;
                            }

                        case "importcheckpoint":
                            {
                                string location = null;

                                if (data != null)
                                {
                                    location = Encoding.UTF8.GetString(data);
                                }

                                RingMasterThreadPool.Instance.QueueUserWorkItem(_ =>
                                {
                                    Thread.Sleep(5000);
                                    bool ok = this.backend.ImportCheckpoint(location);
                                    Trace.TraceWarning("Import checkpoint {0}: {1}", location, ok ? "Succeeded" : "Failed");
                                    if (ok)
                                    {
                                        Trace.TraceWarning("This instance will die now");
                                        Environment.Exit(1);
                                    }
                                });

                                content = "Importing checkpoint " + location + "file and restarting";
                                result = Code.Ok;

                                break;
                            }

                        case "getstats":
                            {
                                lock (this.SyncObject)
                                {
                                    content = string.Format("PersistentData.TotalNodes={0} PersistentData.TotalData={1} EphemeralData.TotalNodes={2} EphemeralData.TotalData={3}", this.backend.Factory.TotalNodes, this.backend.Factory.TotalData, this.backend.EphemeralFactory.TotalNodes, this.backend.EphemeralFactory.TotalData);
                                }

                                result = Code.Ok;

                                break;
                            }

                        case "dumpnodes":
                            {
                                bool scanEphemerals = pieces.Length > 2 && string.Equals(pieces[2], "scanephemerals", StringComparison.InvariantCultureIgnoreCase);
                                content = this.backend.Factory.DumpAllNodesForDebug();

                                if (scanEphemerals)
                                {
                                    lock (this.SyncObject)
                                    {
                                        content = content + Environment.NewLine + "---Eph:" + Environment.NewLine + this.backend.ScanForEphemeral() + Environment.NewLine + "---";
                                    }
                                }

                                result = Code.Ok;

                                break;
                            }

                        case "getverbositylevel":
                            {
                                content = "verbosity level is " + this.backend.GetVerbosityLevel();
                                result = Code.Ok;
                                break;
                            }

                        case "setverbositylevel":
                            {
                                int vlev = int.Parse(pieces[2]);
                                content = string.Format("verbosity level was {0} now is set to {1}", this.backend.GetVerbosityLevel(), vlev);
                                this.backend.SetupVerbosityLevel(vlev);
                                result = Code.Ok;
                                break;
                            }

                        case "cleanuprsl":
                            {
                                RingMasterThreadPool.Instance.QueueUserWorkItem(_ =>
                                {
                                    Thread.Sleep(10000);
                                    Trace.TraceWarning("This instance will wipe its state {0} now", this.backend.GetType().Name);
                                    this.backend.Factory.WipeAllDataAndShutdown();
                                    Trace.TraceWarning("This instance will die now");
                                    Environment.Exit(1);
                                });
                                content = "cleaning up RSL folder and restarting";
                                result = Code.Ok;
                                break;
                            }

                        case "restart":
                            {
                                uint waitMS = 10000;

                                if (pieces.Length > 2)
                                {
                                    if (!uint.TryParse(pieces[2], out waitMS))
                                    {
                                        waitMS = 10000;
                                    }
                                }

                                // 2 minutes max wait
                                waitMS = Math.Min(waitMS, 120000);

                                RingMasterThreadPool.Instance.QueueUserWorkItem(_ =>
                                {
                                    Thread.Sleep((int)waitMS);
                                    Trace.TraceInformation("This instance will stop {0} now", this.backend.GetType().Name);
                                    this.backend.Stop();

                                    Thread.Sleep(2000);
                                    Trace.TraceInformation("This instance will restart {0} now", this.backend.GetType().Name);
                                    this.backend.Start();
                                });

                                content = "restarted in " + waitMS + " miliseconds";
                                result = Code.Ok;
                                break;
                            }

                        case "die":
                            {
                                RingMasterThreadPool.Instance.QueueUserWorkItem(_ =>
                                {
                                    Thread.Sleep(10000);
                                    Trace.TraceWarning("This instance will die now");
                                    Environment.Exit(1);
                                });

                                if (pieces.Length > 2)
                                {
                                    this.backend.FailoverInto(pieces[2]);
                                }

                                content = "die in 10 seconds";
                                result = Code.Ok;
                                break;
                            }
                    }
                }

                return result;
            }

            /// <summary>
            /// runs the command from the path
            /// </summary>
            /// <param name="req">the request containing the command</param>
            /// <param name="session">the session this command was run from</param>
            /// <param name="lockList">the lock list of the invocation</param>
            /// <returns>the repsonse to the command</returns>
            internal virtual RequestResponse RunCommand(RequestCreate req, ClientSession session, ILockListTransaction lockList)
            {
                if (req == null)
                {
                    throw new ArgumentNullException(nameof(req));
                }

                if (session == null)
                {
                    throw new ArgumentNullException(nameof(session));
                }

                MutableStat stat = new MutableStat(new FirstStat(0, MutableStat.ConvertTime(DateTime.UtcNow), 0));

                // we require the digest 'Commander'
                if (!string.Equals(session.Auth.ClientDigest, CommanderDigest, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new RequestResponse()
                    {
                        Content = "not executed",
                        ResponsePath = "command",
                        ResultCode = (int)Code.Authfailed,
                        Stat = stat,
                    };
                }

                string requestedCommand;
                object content;
                Code result = this.RunCommandPath(req.Path, req.Data, session, lockList, out requestedCommand, out content);

                return new RequestResponse()
                {
                    Content = content,
                    ResponsePath = requestedCommand,
                    ResultCode = (int)result,
                    Stat = stat,
                };
            }

            /// <summary>
            /// Generates a poison pill.
            /// </summary>
            /// <param name="spec">the poison pill specification</param>
            /// <param name="session">the session where this poison pill needs to be executed</param>
            /// <param name="lockList">the lock list of the invocation</param>
            /// <returns>the result of applying the generated poison pill, or null if not allowed</returns>
            private RequestResponse GeneratePoisonPill(string spec, ClientSession session, ILockListTransaction lockList)
            {
                if (!ArePoisonPillAllowed)
                {
                    return null;
                }

                try
                {
                    if (string.IsNullOrWhiteSpace(spec))
                    {
                        throw new ArgumentException("spec must have a content.");
                    }

                    if (session == null)
                    {
                        throw new ArgumentNullException(nameof(session));
                    }

                    int idx = spec.IndexOf(':');

                    if (idx == -1)
                    {
                        throw new FormatException("spec must be <action>:<path>");
                    }

                    string action = spec.Substring(0, idx);
                    string path = spec.Substring(idx + 1);

                    this.backend.SetPoisonPillPath(path, action, session, lockList);

                    return new RequestResponse()
                    {
                        CallId = 0,
                        Content = "poisonpill prepared",
                        ResponsePath = path,
                        ResultCode = (int)Code.Ok,
                    };
                }
                catch (FormatException e)
                {
                    return new RequestResponse()
                    {
                        CallId = 0,
                        Content = "Exception while parsing poisonpill spec: " + e.Message,
                        ResponsePath = "/",
                        ResultCode = (int)Code.Badarguments,
                    };
                }
                catch (Exception e)
                {
                    return new RequestResponse()
                    {
                        CallId = 0,
                        Content = "Exception while parsing poisonpill spec: " + e.Message,
                        ResponsePath = "/",
                        ResultCode = (int)Code.Apierror,
                    };
                }
            }
        }
    }
}
