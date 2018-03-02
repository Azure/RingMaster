namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using System.Threading;
    using System.Runtime.InteropServices;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.HelperTypes;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend.Persistence;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    public class WireBackup
    {
        private readonly int maxEvents;
        private readonly int maxTimeInMillis;
        private readonly int maxEventsBetweenSnapshots;
        private readonly int maxTimeInMillisBetweenSnapshots;
        private readonly Func<bool> TakeSnapshot;

        private readonly string path;
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

        public int MaxPending = 1000; // 1k pending files is really running behind.
        public ulong MinAvailableBytes = 10 * (ulong)(1024 * 1024 * 1024); // 10GB of free space minimum

        private readonly static Random rand = new Random((int)(DateTime.UtcNow.Ticks % int.MaxValue));
        private readonly int keepBackupForSec;

        /// <summary>
        /// returns a number close to the given one, but multiplied by a random number between 0.8 and 1.2
        /// the above is true as long as the given number is not int.MaxValue, in which case the same int.MaxValue is returned
        /// </summary>
        /// <returns>a randomized value, or int.MaxValue</returns>
        private static int Randomize(int number)
        {
            if (number != int.MaxValue)
            {
                number = (int)(0.1 * rand.Next(8, 12) * number);
            }

            return number;
        }

        /// <summary>
        /// creates a wirebackup object.
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
            this.TakeSnapshot = takeSnapshot;

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

            CreateUploaderMark(path);
        }

        private void CreateUploaderMark(string markPath)
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

        #region archive logics

        public void Start()
        {
            this.toUpload = new ExecutionQueue(1);
            FindFilesToArchived();
        }

        public void Stop()
        {
            OnTimer(null);
            this.toUpload.Drain(ExecutionQueue.DrainMode.DisallowAllFurtherEnqueuesAndRemoveAllElements);
            this.toUpload = null;
        }

        private void FindFilesToArchived()
        {
            string searchpattern = "*" + OngoingExtension;
            foreach (string file in Directory.GetFiles(path, searchpattern))
            {
                ArchiveFile(file);
            }
        }

        internal void ArchiveFile(string file)
        {
            if (this.toUpload == null)
            {
                return;
            }

            // else, append the upload task for this file to the queue. BUT only if there is no more than X elements in the queue. otherwise, disable this.
            if (this.toUpload.PendingCount > MaxPending)
            {
                Console.WriteLine("WIREBACKUP: had to delete a file: " + file);
                File.Delete(file);
            }
            else
            {
                Console.WriteLine("WIREBACKUP: scheduling file: " + file);
                this.toUpload.Enqueue<string>(ArchiveAsync, file);
            }
        }

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

                if (freeAvail < MinAvailableBytes)
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
        #endregion

        #region RM specific business logics
        public void AppendSetAcl(ulong id, IReadOnlyList<Acl> list, long txtime, long xid)
        {
            if (this.toUpload == null)
            {
                return;
            }

            try
            {
                SetTx(xid);
                string line = string.Format("SA|{0}|{1}|{2}|{3}", txtime, xid, id, ToString(list));
                AppendLine(line);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup AppendSetAcl: " + e.Message);
            }
        }

        private void SetTx(long xid)
        {
            if (!takingSnapshot)
            {
                lastTxId = xid;
            }
        }

        public void AppendAddChild(ulong parentId, ulong childId, long txtime, long xid)
        {
            if (this.toUpload == null)
            {
                return;
            }
            try
            {
                SetTx(xid);
                string line = string.Format("AC|{0}|{1}|{2}|{3}", txtime, xid, parentId, childId);
                AppendLine(line);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup AppendChild: " + e.Message);
            }
        }

        public void AppendDelete(ulong parentId, ulong childId, long txtime, long xid)
        {
            if (this.toUpload == null)
            {
                return;
            }
            try
            {
                SetTx(xid);
                string line = string.Format("DN|{0}|{1}|{2}|{3}", txtime, xid, parentId, childId);
                AppendLine(line);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup AppendRemove: " + e.Message);
            }
        }

        public void AppendRemoveChild(ulong parentId, ulong childId, long txtime, long xid)
        {
            if (this.toUpload == null)
            {
                return;
            }
            try
            {
                SetTx(xid);
                string line = string.Format("RC|{0}|{1}|{2}|{3}", txtime, xid, parentId, childId);
                AppendLine(line);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup AppendRemove: " + e.Message);
            }
        }

        public void AppendSetData(ulong id, byte[] data, long txtime, long xid)
        {
            if (this.toUpload == null)
            {
                return;
            }
            try
            {
                SetTx(xid);
                string line = string.Format("SD|{0}|{1}|{2}|{3}", txtime, xid, id, ToString(data));
                AppendLine(line);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup AppendSetData: " + e.Message);
            }
        }

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
                SetTx(xid);
                string line = string.Format("CN|{0}|{1}|{2}|{3}|{4}|{5}", txtime, xid, node.Id, node.Name, ToString(node.Acl), ToString(node.Data));
                AppendLine(line);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup AppendCreate: " + e.Message);
            }
        }

        public void AppendTMTransaction(long xid, byte[] data)
        {
            if (this.toUpload == null)
            {
                return;
            }
            try
            {
                SetTx(xid);
                string line = string.Format("TM||{0}||{1}", xid, ToString(data));
                AppendLine(line);
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup AppendSetData: " + e.Message);
            }
        }
        #endregion

        #region serialization
        public static void FromString(string base64String, out byte[] output)
        {
            if (String.Equals(base64String, "<null>"))
            {
                output = null;
                return;
            }
            output = Encoding.UTF8.GetBytes(base64String);
        }

        public static string ToString(byte[] data)
        {
            if (data == null)
            {
                return "<null>";
            }
            return Convert.ToBase64String(data);
        }

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
        /// Gets the disk free space ex.
        /// </summary>
        /// <param name="lpDirectoryName">Name of the lp directory.</param>
        /// <param name="lpFreeBytesAvailable">The lp free bytes available.</param>
        /// <param name="lpTotalNumberOfBytes">The lp total number of bytes.</param>
        /// <param name="lpTotalNumberOfFreeBytes">The lp total number of free bytes.</param>
        /// <returns><c>true</c> if the operation succeeded, <c>false</c> otherwise.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
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
#endregion

        private void AppendLine(string line)
        {
            lock (fileLockObj)
            {
                if (!takingSnapshot)
                {
                    if (currentCount > this.maxEvents)
                    {
                        OnTimer(null);
                    }

                    if (currentFile == null)
                    {
                        currentFile = CreateFile(out currentFileName);
                        if (resetTimer == null)
                        {
                            resetTimer = new Timer(OnTimer, null, this.maxTimeInMillis, Timeout.Infinite);
                        }
                        else
                        {
                            resetTimer.Change(this.maxTimeInMillis, Timeout.Infinite);
                        }
                    }
                    currentCount++;
                }
                currentFile.WriteLine(line);
            }
        }

        private StreamWriter CreateFile(out string currentfilename)
        {
            currentfilename = GetFileName();
            return new StreamWriter(new BufferedStream(new FileStream(currentfilename, FileMode.CreateNew), 64 * 1024));
        }

        private string GetFileName()
        {
            return Path.Combine(this.path, string.Format("wirebackup-{0}{1}{2}", lastTxId, takingSnapshot ? "-snap" : "", OngoingExtension));
        }

        public const string OngoingExtension = ".wbtmp";
        public const string ReadyExtension = ".wb";

        private void OnTimer(object ign)
        {
            try
            {
                lock (fileLockObj)
                {
                    if (resetTimer == null || currentFile == null)
                    {
                        return;
                    }

                    if (currentCount > 0)
                    {
                        currentFile.Close();
                        ArchiveFile(currentFileName);
                        currentFile = null;
                        currentFileName = null;
                        grandCount += currentCount;

                        if (this.grandCount > this.maxEventsBetweenSnapshots || DateTime.UtcNow >= this.nextSnapshotTime)
                        {
                            takingSnapshot = true;
                            currentFile = CreateFile(out currentFileName);
                            try
                            {
                                bool taken = TakeSnapshot();
                                currentFile.Close();
                                if (taken)
                                {
                                    ArchiveFile(currentFileName);
                                }
                                else
                                {
                                    File.Delete(currentFileName);
                                }
                            }
                            catch (Exception)
                            {
                                if (currentFile != null)
                                {
                                    currentFile.Close();
                                    File.Delete(currentFileName);
                                }
                                throw;
                            }
                            finally
                            {
                                takingSnapshot = false;
                                currentFile = null;
                                currentFileName = null;
                                this.grandCount = 0;
                                this.nextSnapshotTime = DateTime.UtcNow + TimeSpan.FromMilliseconds(maxTimeInMillisBetweenSnapshots);
                            }
                        }

                        currentCount = 0;
                    }

                    resetTimer.Change(this.maxTimeInMillis, Timeout.Infinite);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Ignorable exception on Wirebackup timer: " + e);
            }
        }
    }
}
