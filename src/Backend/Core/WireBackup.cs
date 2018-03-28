// <copyright file="WireBackup.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Wire backup implementation
    /// </summary>
    public class WireBackup
    {
        /// <summary>
        /// File name extension for the ongoing ones
        /// </summary>
        public const string OngoingExtension = ".wbtmp";

        /// <summary>
        /// File name extension for the completed ones
        /// </summary>
        public const string ReadyExtension = ".wb";

        private static readonly Random Rand = new Random((int)(DateTime.UtcNow.Ticks % int.MaxValue));

        private readonly int maxEvents;
        private readonly int maxTimeInMillis;
        private readonly int maxEventsBetweenSnapshots;
        private readonly int maxTimeInMillisBetweenSnapshots;
        private readonly Func<bool> takeSnapshot;

        private readonly string path;
        private readonly int keepBackupForSec;

        private ExecutionQueue toUpload = null;

        private DateTime nextSnapshotTime;
        private long lastTxId;
        private StreamWriter currentFile;
        private string currentFileName;
        private int currentCount = 0;
        private int grandCount = 0;
        private Timer resetTimer;
        private bool takingSnapshot = false;
        private object fileLockObj = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="WireBackup"/> class.
        /// </summary>
        /// <param name="path">the markPath where to drop the files</param>
        /// <param name="takeSnapshot">the function to be used when a snapshot is needed</param>
        /// <param name="maxEvents">the max number of events per file</param>
        /// <param name="maxTimeInMillis">the max time in milliseconds for a single file</param>
        /// <param name="maxEventsBetweenSnapshots">max events between snapshots</param>
        /// <param name="maxTimeInMillisBetweenSnapshots">max time between snapshots</param>
        /// <param name="keepBackupForSec">Number of seconds to keep the wireback before deleting it</param>
        public WireBackup(string path, Func<bool> takeSnapshot, int maxEvents, int maxTimeInMillis, int maxEventsBetweenSnapshots, int maxTimeInMillisBetweenSnapshots, int keepBackupForSec)
        {
            this.takeSnapshot = takeSnapshot;

            this.maxEvents = maxEvents;
            this.maxTimeInMillis = maxTimeInMillis;
            this.keepBackupForSec = keepBackupForSec;

            if (takeSnapshot == null)
            {
                throw new ArgumentNullException("takeSnapshot");
            }

            if (maxEventsBetweenSnapshots < maxEvents)
            {
                throw new ArgumentException("maxEventsBetweenSnapshots must be bigger than maxEvents");
            }

            if (maxTimeInMillisBetweenSnapshots < maxTimeInMillis)
            {
                throw new ArgumentException("maxTimeInMillisBetweenSnapshots must be bigger than maxTimeInMillis");
            }

            // randomize a little the numbers obtained
            maxEventsBetweenSnapshots = Randomize(maxEventsBetweenSnapshots);
            maxTimeInMillisBetweenSnapshots = Randomize(maxTimeInMillisBetweenSnapshots);

            this.maxEventsBetweenSnapshots = maxEventsBetweenSnapshots;
            this.maxTimeInMillisBetweenSnapshots = maxTimeInMillisBetweenSnapshots;

            this.nextSnapshotTime = DateTime.UtcNow + TimeSpan.FromMilliseconds(maxTimeInMillisBetweenSnapshots);

            this.path = path;
            this.lastTxId = -1;

            this.CreateUploaderMark(/*path*/);
        }

        /// <summary>
        /// Gets or sets the pending files is really running behind, 1000 by default
        /// </summary>
        public int MaxPending { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the free space minimum, 10GB by default
        /// </summary>
        public ulong MinAvailableBytes { get; set; } = 10 * (ulong)(1024 * 1024 * 1024);

        /// <summary>
        /// Converts a string to UTF-8 encoded byte array
        /// </summary>
        /// <param name="base64String">String to convert</param>
        /// <param name="output">byte array output</param>
        public static void FromString(string base64String, out byte[] output)
        {
            if (string.Equals(base64String, "<null>"))
            {
                output = null;
                return;
            }

            output = Encoding.UTF8.GetBytes(base64String);
        }

        /// <summary>
        /// Converts byte array to the equivalent string representation that is encoded with base-64 digits
        /// </summary>
        /// <param name="data">byte array to convert</param>
        /// <returns>Converted string</returns>
        public static string ToString(byte[] data)
        {
            if (data == null)
            {
                return "<null>";
            }

            return Convert.ToBase64String(data);
        }

        /// <summary>
        /// converts the specified string to list of ACLs
        /// </summary>
        /// <param name="stringAcls">List of ACLs in string</param>
        /// <param name="acls">Converted ACLs</param>
        public static void FromString(string stringAcls, out IList<Acl> acls)
        {
            if (stringAcls == null)
            {
                throw new ArgumentNullException("stringAcls");
            }

            if (stringAcls.Equals("<null>"))
            {
                acls = null;
                return;
            }

            string[] aclpieces = stringAcls.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            List<Acl> r = new List<Acl>();

            for (int i = 0; i < aclpieces.Length; i++)
            {
                Acl a;
                FromString(aclpieces[i], out a);
                r.Add(a);
            }

            acls = r;
        }

        /// <summary>
        /// Converts a list of ACLs to string
        /// </summary>
        /// <param name="acls">ACLs to convert</param>
        /// <returns>string representation of ACLs</returns>
        public static string ToString(IReadOnlyList<Acl> acls)
        {
            if (acls == null)
            {
                return "<null>";
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < acls.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(";");
                }

                ToString(acls[i], sb);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts the specified string to ACL
        /// </summary>
        /// <param name="aclStr">ACL in string</param>
        /// <param name="acl">Converted ACL</param>
        public static void FromString(string aclStr, out Acl acl)
        {
            if (aclStr == null)
            {
                throw new ArgumentNullException("aclStr");
            }

            string[] aclpieces = aclStr.Split(',');

            if (aclpieces.Length < 3)
            {
                throw new ArgumentException("Acl string is not in expected format", "aclStr");
            }

            acl = new Acl(int.Parse(aclpieces[0]), new Id(aclpieces[1], aclpieces[2]));
        }

        /// <summary>
        /// Starts the wire backup
        /// </summary>
        public void Start()
        {
            this.toUpload = new ExecutionQueue(1);
            this.FindFilesToArchived();
        }

        /// <summary>
        ///  Stops the wire backup
        /// </summary>
        public void Stop()
        {
            this.OnTimer(null);
            this.toUpload.Drain(ExecutionQueue.DrainMode.DisallowAllFurtherEnqueuesAndRemoveAllElements);
            this.toUpload = null;
        }

        /// <summary>
        /// Appends SetAcl to file
        /// </summary>
        /// <param name="id">Node ID</param>
        /// <param name="list">List of ACL</param>
        /// <param name="txtime">Transaction time</param>
        /// <param name="xid">Transaction ID</param>
        public void AppendSetAcl(ulong id, IReadOnlyList<Acl> list, long txtime, long xid)
        {
            if (this.toUpload == null)
            {
                return;
            }

            try
            {
                this.SetTx(xid);
                string line = string.Join("|", "SA", txtime, xid, id, ToString(list));
                this.AppendLine(line);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup AppendSetAcl: " + e.Message);
            }
        }

        /// <summary>
        /// Appends AddChild to file
        /// </summary>
        /// <param name="parentId">Parent node ID</param>
        /// <param name="childId">Child node ID</param>
        /// <param name="txtime">Transaction time</param>
        /// <param name="xid">Transaction ID</param>
        public void AppendAddChild(ulong parentId, ulong childId, long txtime, long xid)
        {
            if (this.toUpload == null)
            {
                return;
            }

            try
            {
                this.SetTx(xid);
                string line = string.Join("|", "AC", txtime, xid, parentId, childId);
                this.AppendLine(line);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup AppendChild: " + e.Message);
            }
        }

        /// <summary>
        /// Appends Delete to file
        /// </summary>
        /// <param name="parentId">Parent node ID</param>
        /// <param name="childId">Child node ID</param>
        /// <param name="txtime">Transaction time</param>
        /// <param name="xid">Transaction ID</param>
        public void AppendDelete(ulong parentId, ulong childId, long txtime, long xid)
        {
            if (this.toUpload == null)
            {
                return;
            }

            try
            {
                this.SetTx(xid);
                string line = string.Join("|", "DN", txtime, xid, parentId, childId);
                this.AppendLine(line);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup AppendRemove: " + e.Message);
            }
        }

        /// <summary>
        /// Appends RemoveChild to file
        /// </summary>
        /// <param name="parentId">Parent node ID</param>
        /// <param name="childId">Child node ID</param>
        /// <param name="txtime">Transaction time</param>
        /// <param name="xid">Transaction ID</param>
        public void AppendRemoveChild(ulong parentId, ulong childId, long txtime, long xid)
        {
            if (this.toUpload == null)
            {
                return;
            }

            try
            {
                this.SetTx(xid);
                string line = string.Join("|", "RC", txtime, xid, parentId, childId);
                this.AppendLine(line);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup AppendRemove: " + e.Message);
            }
        }

        /// <summary>
        /// Appends SetData to file
        /// </summary>
        /// <param name="id">Node ID</param>
        /// <param name="data">Node Data</param>
        /// <param name="txtime">Transaction time</param>
        /// <param name="xid">Transaction ID</param>
        public void AppendSetData(ulong id, byte[] data, long txtime, long xid)
        {
            if (this.toUpload == null)
            {
                return;
            }

            try
            {
                this.SetTx(xid);
                string line = string.Join("|", "SD", txtime, xid, id, ToString(data));
                this.AppendLine(line);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup AppendSetData: " + e.Message);
            }
        }

        /// <summary>
        /// Appends Create to file
        /// </summary>
        /// <param name="node">Node object</param>
        /// <param name="txtime">Transaction time</param>
        /// <param name="xid">Transaction ID</param>
        public void AppendCreate(IPersistedData node, long txtime, long xid)
        {
            if (this.toUpload == null)
            {
                return;
            }

            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            try
            {
                this.SetTx(xid);
                string line = string.Join("|", "CN", txtime, xid, node.Id, node.Name, ToString(node.Acl), ToString(node.Data));
                this.AppendLine(line);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup AppendCreate: " + e.Message);
            }
        }

        /// <summary>
        /// Appends Transaction Manager transaction
        /// </summary>
        /// <param name="xid">Transaction ID</param>
        /// <param name="data">data byte array</param>
        public void AppendTMTransaction(long xid, byte[] data)
        {
            if (this.toUpload == null)
            {
                return;
            }

            try
            {
                this.SetTx(xid);
                string line = string.Format("TM||{0}||{1}", xid, ToString(data));
                this.AppendLine(line);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup AppendSetData: " + e.Message);
            }
        }

        /// <summary>
        /// Achieves the specified file
        /// </summary>
        /// <param name="file">File to achieve</param>
        internal void ArchiveFile(string file)
        {
            if (this.toUpload == null)
            {
                return;
            }

            // else, append the upload task for this file to the queue. BUT only if there is no more than X elements in the queue. otherwise, disable this.
            if (this.toUpload.PendingCount > this.MaxPending)
            {
                Console.WriteLine("WIREBACKUP: had to delete a file: " + file);
                File.Delete(file);
            }
            else
            {
                Console.WriteLine("WIREBACKUP: scheduling file: " + file);
                this.toUpload.Enqueue<string>(this.ArchiveAsync, file);
            }
        }

        /// <summary>
        /// Achieves the specified file in async
        /// </summary>
        /// <param name="file">File to achieve</param>
        internal void ArchiveAsync(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            try
            {
                string filepath = Path.GetDirectoryName(file);
                if (!filepath.EndsWith("\\"))
                {
                    filepath += '\\';
                }

                ulong freeAvail = GetAvailableDiskSpace(filepath);

                if (freeAvail < this.MinAvailableBytes)
                {
                    Console.WriteLine("WIREBACKUP: had to delete file due to space: " + file);
                    File.Delete(file);
                    return;
                }

                Console.WriteLine("WIREBACKUP: archiving file: " + file);

                string archived;
                int n = 1;
                string extension = ReadyExtension;
                while (true)
                {
                    archived = Path.ChangeExtension(file, extension);
                    if (!File.Exists(archived))
                    {
                        break;
                    }

                    extension = string.Format("{0}{1}", n, ReadyExtension);
                    n++;
                }

                File.Move(file, archived);
                Console.WriteLine("WIREBACKUP: archived file: {0} into {1}", file, archived);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup ArchiveAsync: " + e.Message);
            }
        }

        /// <summary>
        /// Gets the disk free space ex.
        /// </summary>
        /// <param name="lpDirectoryName">Name of the lp directory.</param>
        /// <param name="lpFreeBytesAvailable">The lp free bytes available.</param>
        /// <param name="lpTotalNumberOfBytes">The lp total number of bytes.</param>
        /// <param name="lpTotalNumberOfFreeBytes">The lp total number of free bytes.</param>
        /// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetDiskFreeSpaceEx(
            string lpDirectoryName,
            out ulong lpFreeBytesAvailable,
            out ulong lpTotalNumberOfBytes,
            out ulong lpTotalNumberOfFreeBytes);

        /// <summary>
        /// returns the available disk space on the path
        /// </summary>
        /// <param name="path">that in question</param>
        /// <returns>the amount in Bytes available in the disk for that path</returns>
        private static ulong GetAvailableDiskSpace(string path)
        {
            ulong freeAvail;
            ulong totalCapacity;
            ulong totalfreeBytes;

            if (!GetDiskFreeSpaceEx(path, out freeAvail, out totalCapacity, out totalfreeBytes))
            {
                freeAvail = 0;
            }

            return freeAvail;
        }

        private static void ToString(Acl acl, StringBuilder sb)
        {
            sb.AppendFormat("{0},{1},{2}", acl.Perms, acl.Id.Scheme, acl.Id.Identifier);
        }

        /// <summary>
        /// returns a number close to the given one, but multiplied by a random number between 0.8 and 1.2
        /// the above is true as long as the given number is not int.MaxValue, in which case the same int.MaxValue is returned
        /// </summary>
        /// <returns>a randomized value, or int.MaxValue</returns>
        private static int Randomize(int number)
        {
            if (number != int.MaxValue)
            {
                number = (int)(0.1 * Rand.Next(8, 12) * number);
            }

            return number;
        }

        private void CreateUploaderMark(/* string markPath */)
        {
            /*UploaderSourceFolderConfiguration wireBackupFolderConfig = new UploaderSourceFolderConfiguration();
            wireBackupFolderConfig.ContainerName = "WireBackup";
            wireBackupFolderConfig.CompressFiles = true;
            wireBackupFolderConfig.DeleteAfterUpload = true;
            wireBackupFolderConfig.FilePattern = "*" + ReadyExtension;

            //cfg.DeleteFilesOlderThanSeconds = this.keepBackupForSec.ToString();

            /// just create a marker file that will indicate uploader to go fetch all these files and push them somewhere
            string markfilename = Path.Combine(markPath, UploaderSourceFolderConfiguration.ConfigFileName);
            if (!Directory.Exists(markPath))
            {
                Directory.CreateDirectory(markPath);
            }

            wireBackupFolderConfig.Serialize(markfilename, true);*/
        }

        private void FindFilesToArchived()
        {
            string searchpattern = "*" + OngoingExtension;
            foreach (string file in Directory.GetFiles(this.path, searchpattern))
            {
                this.ArchiveFile(file);
            }
        }

        private void SetTx(long xid)
        {
            if (!this.takingSnapshot)
            {
                this.lastTxId = xid;
            }
        }

        private void AppendLine(string line)
        {
            lock (this.fileLockObj)
            {
                if (!this.takingSnapshot)
                {
                    if (this.currentCount > this.maxEvents)
                    {
                        this.OnTimer(null);
                    }

                    if (this.currentFile == null)
                    {
                        this.currentFile = this.CreateFile(out this.currentFileName);
                        if (this.resetTimer == null)
                        {
                            this.resetTimer = new Timer(this.OnTimer, null, this.maxTimeInMillis, Timeout.Infinite);
                        }
                        else
                        {
                            this.resetTimer.Change(this.maxTimeInMillis, Timeout.Infinite);
                        }
                    }

                    this.currentCount++;
                }

                this.currentFile.WriteLine(line);
            }
        }

        private StreamWriter CreateFile(out string currentfilename)
        {
            currentfilename = this.GetFileName();

            FileStream fs = null;
            BufferedStream bs = null;
            StreamWriter sw = null;
            try
            {
                fs = new FileStream(currentfilename, FileMode.CreateNew);
                bs = new BufferedStream(fs, 64 * 1024);
                fs = null;
                sw = new StreamWriter(bs);
                bs = null;
                return sw;
            }
            finally
            {
                if (bs != null)
                {
                    bs.Dispose();
                }

                if (fs != null)
                {
                    fs.Dispose();
                }
            }
        }

        private string GetFileName()
        {
            return Path.Combine(
                this.path,
                string.Format("wirebackup-{0}{1}{2}", this.lastTxId, this.takingSnapshot ? "-snap" : string.Empty, OngoingExtension));
        }

        private void OnTimer(object ign)
        {
            try
            {
                lock (this.fileLockObj)
                {
                    if (this.resetTimer == null || this.currentFile == null)
                    {
                        return;
                    }

                    if (this.currentCount > 0)
                    {
                        this.currentFile.Close();
                        this.ArchiveFile(this.currentFileName);
                        this.currentFile = null;
                        this.currentFileName = null;
                        this.grandCount += this.currentCount;

                        if (this.grandCount > this.maxEventsBetweenSnapshots || DateTime.UtcNow >= this.nextSnapshotTime)
                        {
                            this.takingSnapshot = true;
                            this.currentFile = this.CreateFile(out this.currentFileName);
                            try
                            {
                                bool taken = this.takeSnapshot();
                                this.currentFile.Close();
                                if (taken)
                                {
                                    this.ArchiveFile(this.currentFileName);
                                }
                                else
                                {
                                    File.Delete(this.currentFileName);
                                }
                            }
                            catch (Exception)
                            {
                                if (this.currentFile != null)
                                {
                                    this.currentFile.Close();
                                    File.Delete(this.currentFileName);
                                }

                                throw;
                            }
                            finally
                            {
                                this.takingSnapshot = false;
                                this.currentFile = null;
                                this.currentFileName = null;
                                this.grandCount = 0;
                                this.nextSnapshotTime = DateTime.UtcNow + TimeSpan.FromMilliseconds(this.maxTimeInMillisBetweenSnapshots);
                            }
                        }

                        this.currentCount = 0;
                    }

                    this.resetTimer.Change(this.maxTimeInMillis, Timeout.Infinite);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup timer: " + e);
            }
        }
    }
}
