// <copyright file="ScheduledCommand.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using HelperTypes;
    using Infrastructure.RingMaster.Data;
    using Requests;
    using Code = Infrastructure.RingMaster.Data.RingMasterException.Code;

    /// <summary>This class takes care of monitoring and executing scheduler commands.</summary>
    /// <remarks>
    /// <![CDATA[
    /// Here is the basic algorithm:
    /// - We will serialize request into the data for tree nodes, which are stored in a specific portion of the tree
    /// - This daemon reads all commands, and register to changes in that tree. And on a single thread executes the command one by one (in lexicographical order).
    /// - If the command succeeds, the "command node" is deleted atomically.
    /// - If the command fails, the "command node" is deleted and a corresponding one (with the same name) is created in a different subtree containing a serialization of the failure that the command encountered.
    /// - While the command is being executed, the node is in a third "inflight" subtree.
    /// - If the process restarts midway (i.e. while the command is in the inflight subtree) the next time the daemon starts will see it, and it will be moved back into the commands tree, and the daemon will restart.
    /// - NOTE THAT IT IS THE APPLICATION RESPONSIBILITY TO CLEANUP NODES FROM THE "FAILURES" TREE.
    ///
    /// All transitions are carefully designed to reduce the locking, and ensure exactly once execution. Here are the details:
    ///
    /// - There are three relevant paths in the tree:
    ///       /$metadata/scheduler/commands  --> where all the pending scheduled commands are stored
    ///       /$metadata/scheduler/inflight  --> where the (single) inflight command is stored. When there is an inflight command, the tree also contains a "$<inflighttoken>" node. When there is no inflight command, the token is not there.
    ///       /$metadata/scheduler/failures  --> where all the failed commands are stored.
    ///  - The daemon starts by reading all children of "commands" and registering for changes there
    ///  - There is a single thread that will take the scheduled commands in lexicographic order, and will execute them one by one.
    ///  - Executing a command is a small state machine:
    ///
    ///       [Begin] ----> State_Deserialize_Command ---(onSuccess)---> State_Setup_Command ---(onSuccess)---> State_Execute_Command --(onSuccess)---> [Done]
    ///                                         |                             |                                            |
    ///                                         |                             |                                            +-----(onFailure)------------>----+
    ///                                         v                             |                                                                              |
    ///                                         |                             +--(onFailure)---> State_MoveBackInflightIntoCommands --> [RESTART DAEMON]     v
    ///                                         |                                                                                                            |
    ///                                         +--(onFailure)--> State_MoveToFailure  (---------------------------------------------------------------(-----+
    ///
    ///       State_Deserialize_Command:          reads the node "x" from the commands tree and deserializes the command.
    ///                                               if this fails, it is because the deserialization failed, or the node node disapeared.
    ///       State_Setup_Command:                in a single multi, creates the inflight "token" node and moves the node "x" into the inflight tree.
    ///                                               if there was already a token node, this fails.
    ///       State_Execute_Command:              in a single multi, executes the command deserialized from "x", and deletes the inflight node "x" and the inflight token.
    ///                                               if this fails, it is because the command "x" failed.
    ///       State_MoveBackInflightIntoCommands: in a single multi, move the nodes under "inflight" tree into the commands tree, and deletes the inflight token node.
    ///                                               after this (success or failure) the daemon will be restarted.
    ///       State_MoveTofailure:                in a single multi, delete the command node "x" and create a node "x" under "failures tree" containing the serialization of faulted results from running the command x.
    ///                                               if this fails, the daemon will be restarted
    /// ]]>
    /// </remarks>
    internal class ScheduledCommand : IWatcher
    {
        private const string PathCommands = "/$metadata/scheduler/commands";
        private const string PathFailures = "/$metadata/scheduler/failures";
        private const string PathInflight = "/$metadata/scheduler/inflight";
        private const string PathInflightToken = "/$metadata/scheduler/inflight/$<inflighttoken>";

        private Func<bool> isPrimary;
        private IByteArrayMarshaller marshaller;
        private AbstractRingMaster self;
        private HelperTypes.ExecutionQueue execution = new HelperTypes.ExecutionQueue(1);
        private HelperTypes.ExecutionQueue callbacks = new HelperTypes.ExecutionQueue(1);
        private object childrenProcessorLock = new object();
        private SortedDictionary<string, bool> nodesToProcess = new SortedDictionary<string, bool>();
        private bool registered = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledCommand"/> class.
        /// </summary>
        /// <param name="isPrimary">the function indicating if the backend object is primary</param>
        /// <param name="rm">Ring master object</param>
        /// <param name="marshaller">the marshaller to use when serializing/deserializing commands</param>
        internal ScheduledCommand(Func<bool> isPrimary, AbstractRingMaster rm, IByteArrayMarshaller marshaller)
        {
            if (isPrimary == null)
            {
                throw new ArgumentNullException("isPrimary");
            }

            if (marshaller == null)
            {
                throw new ArgumentNullException("marshaller");
            }

            this.marshaller = marshaller;
            this.isPrimary = isPrimary;
            this.self = rm;
        }

        /// <summary>
        /// Gets a value indicating whether the watcher is for a single use only.
        /// </summary>
        public bool OneUse => this.Kind.HasFlag(WatcherKind.OneUse);

        /// <summary>
        /// Gets the kind of the watcher, if it is for single use and if the data is included on notification
        /// </summary>
        public WatcherKind Kind
        {
            get
            {
                return WatcherKind.OneUse;
            }
        }

        /// <summary>
        /// Gets the value indicating as a watcher, this object's ID
        /// </summary>
        public ulong Id
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets or sets action to perform on abanton
        /// </summary>
        internal Action<Exception> InternalOnAbandon
        {
            get; set;
        }

        /// <summary>
        /// return the paths the scheduler needs in the tree
        /// </summary>
        /// <returns>List of paths</returns>
        public static IEnumerable<string> GetPaths()
        {
            return new string[] { PathCommands, PathFailures, PathInflight };
        }

        /// <summary>
        /// return the commandpath for a scheduled name
        /// </summary>
        /// <param name="scheduledName">the scheduled command name</param>
        /// <returns>the command path</returns>
        public static string GetCommandPath(string scheduledName)
        {
            if (scheduledName == null)
            {
                throw new ArgumentNullException("scheduledName");
            }

            return PathCommands + "/" + scheduledName;
        }

        /// <summary>
        /// returns the failure path for the given command name or command path
        /// </summary>
        /// <param name="commandPath">the command name or command path</param>
        /// <returns>the failure path</returns>
        public static string GetFailurePath(string commandPath)
        {
            if (commandPath == null)
            {
                throw new ArgumentNullException("commandPath");
            }

            string scheduledName = commandPath.Substring(commandPath.LastIndexOf('/') + 1);
            return PathFailures + "/" + scheduledName;
        }

        /// <summary>
        /// returns the bytes corresponding to the request given
        /// </summary>
        /// <param name="req">request to serialize</param>
        /// <param name="marshaller">marshaller to use</param>
        /// <returns>serialization bytes</returns>
        public static byte[] GetBytes(IRingMasterBackendRequest req, IByteArrayMarshaller marshaller)
        {
            if (req == null)
            {
                throw new ArgumentNullException("req");
            }

            if (marshaller == null)
            {
                throw new ArgumentNullException("marshaller");
            }

            RequestCall call = new RequestCall()
            {
                CallId = 0,
                Request = req,
            };

            return marshaller.SerializeRequestAsBytes(call);
        }

        /// <summary>
        /// Returns the bytes corresponding to the given exception
        /// </summary>
        /// <param name="ex">Exception to set in the response</param>
        /// <returns>Serialized byte array</returns>
        public byte[] GetBytes(Exception ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException("ex");
            }

            RequestResponse resp = new RequestResponse()
            {
                ResultCode = (int)Code.Badarguments,
                Content = ex.ToString(),
            };

            return this.marshaller.SerializeResponseAsBytes(resp);
        }

        /// <summary>
        /// returns the bytes corresponding to the request given
        /// </summary>
        /// <param name="req">request to serialize</param>
        /// <returns>serialization bytes</returns>
        public byte[] GetBytes(IRingMasterBackendRequest req)
        {
            return GetBytes(req, this.marshaller);
        }

        /// <summary>
        /// returns the bytes corresponding to the results given
        /// </summary>
        /// <param name="resultCode">result code in the response</param>
        /// <param name="results">Content in the response</param>
        /// <returns>serialization bytes</returns>
        public byte[] GetBytes(Code resultCode, IReadOnlyList<OpResult> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException("results");
            }

            RequestResponse resp = new RequestResponse()
            {
                ResultCode = (int)resultCode,
                Content = results,
            };

            return this.marshaller.SerializeResponseAsBytes(resp);
        }

        /// <summary>
        /// returns the request corresponding to deserializing the bytes given
        /// </summary>
        /// <param name="data">bytes to deserialize</param>
        /// <returns>request deserialized</returns>
        public IRingMasterBackendRequest GetRequest(byte[] data)
        {
            if (data == null)
            {
                return null;
            }

            RequestCall call = this.marshaller.DeserializeRequestFromBytes(data);

            return call.Request;
        }

        /// <summary>
        /// Enqueues the processing of the event asynchronously
        /// </summary>
        /// <param name="evt">Watched event</param>
        public void Process(WatchedEvent evt)
        {
            this.callbacks.Enqueue(this.ProcessSync, evt);
        }

        /// <summary>
        /// Something changed in the command tree. Processes the watcher event now.
        /// </summary>
        /// <param name="evt">the watcher event to process.</param>
        public void ProcessSync(WatchedEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (this.Kind.HasFlag(WatcherKind.OneUse))
            {
                this.registered = false;
            }

            if (evt.EventType == WatchedEvent.WatchedEventType.WatcherRemoved)
            {
                // since we are using a loopback client, the only ways this can happen are:
                // a) the node where this was registered was deleted
                // b) we are no longer the primary
                // in both cases, we don't want to re-register the watcher. Just log the fact.
                this.registered = false;

                return;
            }

            this.Start();
        }

        /// <summary>
        /// Closes this scheduled command daemon
        /// </summary>
        internal void Close()
        {
            this.execution.Drain(HelperTypes.ExecutionQueue.DrainMode.DisallowAllFurtherEnqueues);
            lock (this.childrenProcessorLock)
            {
                this.nodesToProcess.Clear();
                RingMasterServerInstrumentation.Instance.OnScheduledCommandQueueChange(this.nodesToProcess.Count);
            }

            this.self.Close();
            this.self = null;
        }

        /// <summary>
        /// Starts this scheduled command daemon
        /// </summary>
        internal void Start()
        {
            if (this.registered)
            {
                return;
            }

            if (this.self == null)
            {
                throw new ObjectDisposedException("this objects has been closed already");
            }

            this.GetChildrenAndProcess();
        }

        /// <summary>
        /// Gets all children under command paths, and process them
        /// </summary>
        private void GetChildrenAndProcess()
        {
            IReadOnlyList<string> children;

            lock (this.childrenProcessorLock)
            {
                try
                {
                    this.registered = true;

                    children = this.self.GetChildren(PathCommands, this);
                }
                catch (Exception e)
                {
                    this.registered = false;
                    this.OnAbandon(e);
                    return;
                }

                this.AddChildrenAsPending(children);
            }
        }

        /// <summary>
        /// Add the given children as pending
        /// </summary>
        /// <param name="children">children to add</param>
        private void AddChildrenAsPending(IEnumerable<string> children)
        {
            lock (this.childrenProcessorLock)
            {
                int count = this.nodesToProcess.Count;

                if (children != null)
                {
                    foreach (string commandName in children)
                    {
                        this.nodesToProcess[commandName] = true;
                    }

                    if (count == this.nodesToProcess.Count)
                    {
                        return;
                    }

                    RingMasterServerInstrumentation.Instance.OnScheduledCommandQueueChange(this.nodesToProcess.Count);

                    if (this.execution.InFlightCount == 0)
                    {
                        this.execution.Enqueue(this.ExecuteAllCommandsFromDictionary);
                    }
                }
            }
        }

        /// <summary>
        /// executes all commands from the dictionary
        /// </summary>
        private void ExecuteAllCommandsFromDictionary()
        {
            while (true)
            {
                string oneKey = null;

                lock (this.childrenProcessorLock)
                {
                    if (this.nodesToProcess.Count == 0)
                    {
                        break;
                    }

                    // get the first one
                    foreach (string k in this.nodesToProcess.Keys)
                    {
                        oneKey = k;
                        break;
                    }
                }

                try
                {
                    this.ExecuteCommand(oneKey);

                    lock (this.childrenProcessorLock)
                    {
                        this.nodesToProcess.Remove(oneKey);

                        RingMasterServerInstrumentation.Instance.OnScheduledCommandQueueChange(this.nodesToProcess.Count);
                    }
                }
                catch (Exception ex)
                {
                    // if we are not primary, it is expected this will fail, so there is no need to report this particular failure
                    // as a bad thing. Instead we will just log it
                    if (this.isPrimary())
                    {
                        RingMasterServerInstrumentation.Instance.OnUnexpectedException("ScheduledCommand", ex);
                    }
                    else
                    {
                        Trace.TraceWarning("ScheduledCOmmand threw an exception while losing Primariness: {0}", ex);
                    }

                    // if an exception happens, we need to reschedule the completion, so we will start over next time
                    // we are started by the pseudonodes runner.
                    lock (this.childrenProcessorLock)
                    {
                        this.nodesToProcess.Clear();
                        this.registered = false;
                        this.OnAbandon(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Invoked when we have to abandon due to an exception
        /// </summary>
        /// <param name="ex">exception</param>
        private void OnAbandon(Exception ex)
        {
            this.InternalOnAbandon?.Invoke(ex);
        }

        /// <summary>
        /// Executes a single command, with the given name. If there is an uncollected "inflight" item, this method will move it back to commands tree, and throw an exception,
        /// hence making the whole scheduler to restart but this time including those commands that were in the inflight bucket.
        /// </summary>
        /// <param name="scheduledName">the name of the command to execute</param>
        private void ExecuteCommand(string scheduledName)
        {
            byte[] resultbytes = null;

            string scheduledCommandPath = GetCommandPath(scheduledName);
            string faultPath = GetFailurePath(scheduledName);

            RingMasterEventSource.Log.ExecuteScheduledCommandStarted(scheduledName, scheduledCommandPath, faultPath);
            Stopwatch sw = Stopwatch.StartNew();

            bool rethrow = false;

            try
            {
                IStat stat = null;

                IRingMasterBackendRequest req = this.GetRequestOnCommandNode(scheduledCommandPath, ref stat);

                try
                {
                    resultbytes = this.ExecuteCommandsAndGetResultsOnError(scheduledCommandPath, stat, req);
                }
                catch (InflightExistException inflight)
                {
                    Trace.TraceInformation("Inflight command found: " + inflight);
                    string[] inflightNames = this.MoveInflightPathIntoCommandPath();

                    rethrow = true;

                    if (inflightNames != null)
                    {
                        throw new InvalidOperationException($"There were objects in the inflight bucket: [{string.Join(",", inflightNames)}]. We moved them back to the commands tree and we will restart processing");
                    }

                    throw new InvalidOperationException($"There were objects in the inflight bucket but no useful command. We will restart processing");
                }
            }
            catch (Exception ex)
            {
                RingMasterEventSource.Log.ExecuteScheduledCommandException(scheduledName, ex.ToString());
                if (rethrow)
                {
                    throw;
                }

                resultbytes = this.GetBytes(ex);
            }

            if (resultbytes == null)
            {
                // resultbytes is null, means success, and we are done.
                RingMasterServerInstrumentation.Instance.OnScheduledCommandFinished(true, sw.ElapsedMilliseconds);
                RingMasterEventSource.Log.ExecuteScheduledCommandCompleted(scheduledName, sw.ElapsedMilliseconds);
                return;
            }

            // delete the command and report the result.
            // an exception here will mean we suspend the scheduler.
            this.DeleteScheduledCommandAndWriteFailureNode(scheduledCommandPath, faultPath, resultbytes);

            RingMasterServerInstrumentation.Instance.OnScheduledCommandFinished(false, sw.ElapsedMilliseconds);

            RingMasterEventSource.Log.ExecuteScheduledCommandFailed(scheduledName, sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// moves commands from the inflight path into the command path
        /// </summary>
        /// <param name="expectedName">if present, this is the expected name for the single command in the inflight tree. if not present, this method moves all items (not the inflight token) into the commands tree</param>
        /// <returns>the list of names added to the command tree</returns>
        private string[] MoveInflightPathIntoCommandPath(string expectedName = null)
        {
            string[] children;
            if (expectedName != null)
            {
                children = new string[] { expectedName };
            }
            else
            {
                children = this.self.GetChildren(PathInflight, false).ToArray();
            }

            List<string> movedChildren = new List<string>();

            if (children.Length == 0)
            {
                return null;
            }

            List<IRingMasterBackendRequest> ops = new List<IRingMasterBackendRequest>();
            foreach (string child in children)
            {
                string childPath = PathInflight + "/" + child;
                if (childPath == PathInflightToken)
                {
                    ops.Add(new RequestDelete(childPath, null, -1, null, DeleteMode.SuccessEvenIfNodeDoesntExist));
                }
                else
                {
                    movedChildren.Add(child);
                    ops.Add(new RequestMove(childPath, null, -1, PathCommands, null));
                }
            }

            IReadOnlyList<OpResult> results = null;
            Code resultCode = Code.Unknown;

            ManualResetEvent e = ManualResetEventPool.InstancePool.GetOne();

            try
            {
                this.self.Multi(
                    ops.ToArray(),
                    (rc, p, c) =>
                    {
                        resultCode = (Code)rc;

                        if (rc == (int)Code.Ok)
                        {
                            results = p;
                        }

                        e.Set();
                    },
                    null,
                    true);
            }
            finally
            {
                ManualResetEventPool.InstancePool.WaitOneAndReturn(ref e);
            }

            if (!this.AllResultsOk(resultCode, results))
            {
                string details = string.Empty;

                if (results != null)
                {
                    details = string.Join(",", results.Select(c => c.ErrCode));
                }

                throw new InflightExistException($"Could not move command back from inflight: {resultCode}, [{details}]");
            }

            return movedChildren.ToArray();
        }

        /// <summary>
        /// Deletes a command from the commands tree and writes a corresponding node in the failed commands tree.
        /// </summary>
        /// <param name="scheduledCommandPath">the path to the scheduled command</param>
        /// <param name="faultPath">the path to the failure node</param>
        /// <param name="resultbytes">the result bytes (describing the failure) to write in the failure node</param>
        private void DeleteScheduledCommandAndWriteFailureNode(string scheduledCommandPath, string faultPath, byte[] resultbytes)
        {
            IRingMasterBackendRequest[] ops = new IRingMasterBackendRequest[]
            {
                new RequestCreate(faultPath, null, null, null, CreateMode.Persistent, null),
                new RequestMove(scheduledCommandPath, null, -1, faultPath, null),
                new RequestCreate(faultPath + "/ResultData", null, resultbytes, null, CreateMode.PersistentAllowPathCreation | CreateMode.SuccessEvenIfNodeExistsFlag, null),
            };

            ManualResetEvent e = ManualResetEventPool.InstancePool.GetOne();

            IReadOnlyList<OpResult> results = null;
            int resultCode = -1;

            try
            {
                this.self.Multi(
                    ops,
                    (rc, p, c) =>
                    {
                        resultCode = rc;

                        if (rc == (int)Code.Ok)
                        {
                            results = p;
                        }

                        e.Set();
                    },
                    null,
                    true);
            }
            finally
            {
                ManualResetEventPool.InstancePool.WaitOneAndReturn(ref e);
            }

            if (resultCode != (int)Code.Ok)
            {
                throw new AggregateException("error code was not success: " + resultCode);
            }

            if (results == null)
            {
                throw new AggregateException("result list came null");
            }

            if (results.Count == 0)
            {
                throw new AggregateException("result list came empty");
            }

            if (results[0].ErrCode != Code.Ok)
            {
                throw new AggregateException("scheduleCommand failure node could not be created: " + results[0].ErrCode);
            }

            if (results[1].ErrCode != Code.Ok)
            {
                throw new AggregateException("scheduleCommand node could not be moved into scheduledFailure tree: " + results[1].ErrCode);
            }

            if (results[2].ErrCode != Code.Ok)
            {
                throw new AggregateException("scheduleFailure result node could not be created: " + results[2].ErrCode);
            }
        }

        /// <summary>
        /// Executes the given command and obtains the result bytes on error.
        /// </summary>
        /// <param name="scheduledCommandPath">the path to the command</param>
        /// <param name="stat">the expected stat of the command node</param>
        /// <param name="req">the request to execute</param>
        /// <returns>null on success, or the bytes corresponsing to the execution error</returns>
        private byte[] ExecuteCommandsAndGetResultsOnError(string scheduledCommandPath, IStat stat, IRingMasterBackendRequest req)
        {
            if (req == null)
            {
                return this.GetBytes(Code.Badarguments, null);
            }

            IStat readStat = this.self.Exists(scheduledCommandPath, false, false);

            if (readStat == null)
            {
                throw new InvalidOperationException($"Scheduled command stat of [{scheduledCommandPath}] is null");
            }

            if (readStat.Version != stat.Version || readStat.Cversion != stat.Cversion)
            {
                throw new InvalidOperationException(
                    $"Scheduled command stat of [{scheduledCommandPath}] is different from expected command stat; " +
                    $"Expected [{stat.Version} {stat.Cversion}], " +
                    $"Actual [{readStat.Version} {readStat.Cversion}]");
            }

            ManualResetEvent e;

            Code resultCode = Code.Unknown;
            IReadOnlyList<OpResult> results = null;

            // setup the command into the inflight tree
            string inflightPath = this.MoveCommandToInflight(scheduledCommandPath, stat);

            // run the command, and atomically remove the inflight marks on success
            IRingMasterBackendRequest[] ops =
            {
                req,
                new RequestDelete(inflightPath, null, stat.Version, null, DeleteMode.SuccessEvenIfNodeDoesntExist),
                new RequestDelete(PathInflightToken, null, -1, null, DeleteMode.SuccessEvenIfNodeDoesntExist),
            };

            e = ManualResetEventPool.InstancePool.GetOne();

            resultCode = Code.Unknown;
            results = null;

            try
            {
                this.self.Multi(
                    ops,
                    (rc, p, c) =>
                    {
                        resultCode = (Code)rc;

                        if (resultCode == Code.Ok)
                        {
                            results = p;
                        }

                        e.Set();
                    },
                    null,
                    true);
            }
            finally
            {
                ManualResetEventPool.InstancePool.WaitOneAndReturn(ref e);
            }

            bool allok = this.AllResultsOk(resultCode, results);

            if (allok)
            {
                // if all went well, we are done since the command was deleted from the inflight tree atomically with the operation.
                return null;
            }

            // otherwise, we need to undo the setup now
            this.MoveInflightPathIntoCommandPath();

            return this.GetBytes(resultCode, results);
        }

        /// <summary>
        /// returns the path for the inflight tree corresponding to the given scheduled command path
        /// </summary>
        /// <param name="scheduledCommandPath">the scheduled command path to be processed</param>
        /// <returns>the path under the inflight tree</returns>
        private string GetInflightPath(string scheduledCommandPath)
        {
            if (scheduledCommandPath.StartsWith(PathCommands))
            {
                return PathInflight + "/" + scheduledCommandPath.Substring(PathCommands.Length + 1);
            }

            throw new ArgumentException($"scheduledCommandPath {scheduledCommandPath} is not a path under the schedule command tree {PathCommands}");
        }

        /// <summary>
        /// Moves a command into the inflight tree
        /// </summary>
        /// <param name="scheduledCommandPath">the path to move</param>
        /// <param name="stat">the stat of the node to move</param>
        /// <returns>the inflight path where the command was moved</returns>
        private string MoveCommandToInflight(string scheduledCommandPath, IStat stat)
        {
            IRingMasterBackendRequest[] setupOps =
            {
                new RequestCreate(PathInflightToken, null, new byte[0], new Acl[0], CreateMode.Persistent, null),
                new RequestMove(scheduledCommandPath, null, stat.Version, PathInflight, null),
            };

            Code resultCode = Code.Unknown;
            IReadOnlyList<OpResult> results = null;

            ManualResetEvent e = ManualResetEventPool.InstancePool.GetOne();

            try
            {
                this.self.Multi(
                    setupOps,
                    (rc, p, c) =>
                    {
                        resultCode = (Code)rc;

                        if (resultCode == Code.Ok)
                        {
                            results = p;
                        }

                        e.Set();
                    },
                    null,
                    true);
            }
            finally
            {
                ManualResetEventPool.InstancePool.WaitOneAndReturn(ref e);
            }

            if (!this.AllResultsOk(resultCode, results))
            {
                string details = string.Empty;

                if (results != null)
                {
                    details = string.Join(",", results.Select(c => c.ErrCode));
                }

                throw new InflightExistException($"Could not move command to inflight: {resultCode}, [{details}]");
            }

            return this.GetInflightPath(scheduledCommandPath);
        }

        /// <summary>
        /// Indicates if the resultCode and all internal results are successful.
        /// </summary>
        /// <param name="resultCode">the command result code</param>
        /// <param name="results">the individual results</param>
        /// <returns>true if all is successful</returns>
        private bool AllResultsOk(Code resultCode, IEnumerable<OpResult> results)
        {
            if (resultCode != Code.Ok)
            {
                return false;
            }

            foreach (OpResult res in results)
            {
                if (res.ErrCode != Code.Ok)
                {
                    return false;
                }

                if (res.ResultType == OpCode.Multi)
                {
                    OpResult.RunResult asRunResult = (OpResult.RunResult)res;

                    if (!this.AllResultsOk(asRunResult.ErrCode, asRunResult.Results))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Obtains the request form the command tree, given a command path
        /// </summary>
        /// <param name="scheduledCommandPath">the path to the command</param>
        /// <param name="retStat">the retrieved stat</param>
        /// <returns>the deserialized request contained in the command path</returns>
        private IRingMasterBackendRequest GetRequestOnCommandNode(string scheduledCommandPath, ref IStat retStat)
        {
            byte[] data = null;
            IStat stat = null;

            ManualResetEvent e = ManualResetEventPool.InstancePool.GetOne();
            try
            {
                this.self.GetData(
                    scheduledCommandPath,
                    false,
                    (rc, p, ctx, d, st) =>
                    {
                        if (rc == (int)Code.Ok)
                        {
                            data = d;
                            stat = st;
                        }

                        e.Set();
                    },
                    null);
            }
            finally
            {
                ManualResetEventPool.InstancePool.WaitOneAndReturn(ref e);
            }

            retStat = stat;

            if (data == null)
            {
                throw new InvalidOperationException("data found is null");
            }

            return this.GetRequest(data);
        }

        /// <summary>
        /// This exception is thrown if there are unexpected inflight objects in the inflight tree
        /// </summary>
        [Serializable]
        private class InflightExistException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="InflightExistException"/> class.
            /// </summary>
            /// <param name="msg">Exception message</param>
            public InflightExistException(string msg)
                : base(msg)
            {
            }
        }
    }
}
