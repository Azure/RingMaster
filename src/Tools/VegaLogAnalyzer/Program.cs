// <copyright file="Program.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.VegaLogAnalyzer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    /// the program
    /// </summary>
    public class Program
    {
        private const string ProcessLoadCompleted = "ProcessLoad_Completed";

        private const string CompleteRebuildConnectWithParent = "CompleteRebuild_ConnectWithParent";

        private const string LoadTreeStarted = "LoadTreeStarted";

        private const string LoadTreeCompleted = "LoadTreeCompleted";

        private const int TaskNameIndex = 13;

        private const int MessageIndex = 17;

        private static string numChildrenRegex = $"numChildren=\"\"([^\"]+)\"\"";

        private static string idRegex = $"id=\"\"([^\"]+)\"\"";

        private static string parentIdRegex = $"parentId=\"\"([^\"]+)\"\"";

        private static string nameRegex = $"name=\"\"([^\"]+)\"\"";

        private static string parentNameRegex = $"parentName=\"\"([^\"]+)\"\"";

        private static string logFilePath = @"E:\data\Logs_2018_06_21_19_01.csv";

        private static Dictionary<long, TreeNode> dict = new Dictionary<long, TreeNode>();

        private static int totalMismatchCount = 0;

        private static Action<string> log = s => Console.WriteLine(s);

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            int totalCount = 0;
            int validRowCount = 0;
            int processLoadCompletedCount = 0;
            int completeRebuildConnectWithParentCount = 0;
            string line;
            StreamReader file = new StreamReader(logFilePath);
            bool isValidRow = false;

            while ((line = file.ReadLine()) != null)
            {
                totalCount++;
                string[] parts = line.Split(",");
                if (parts[TaskNameIndex] == LoadTreeStarted)
                {
                    isValidRow = true;
                    continue;
                }

                if (parts[TaskNameIndex] == LoadTreeCompleted)
                {
                    isValidRow = false;
                    break;
                }

                if (!isValidRow)
                {
                    continue;
                }

                validRowCount++;
                if (parts[TaskNameIndex] == ProcessLoadCompleted)
                {
                    processLoadCompletedCount++;
                    ProcessLoadCompletedLog(parts[MessageIndex]);
                }
                else if (parts[TaskNameIndex] == CompleteRebuildConnectWithParent)
                {
                    completeRebuildConnectWithParentCount++;
                    CompleteRebuildConnectWithParentLog(parts[MessageIndex]);
                }
            }

            file.Close();
            log($"Finished reading log file. totalLine: {totalCount}, number of {ProcessLoadCompleted} task: {processLoadCompletedCount}, " +
                $"number of {CompleteRebuildConnectWithParent} task: {completeRebuildConnectWithParentCount}");

            ValidateTree(dict[0]);
            ValidateNames(dict[1]);
            FindOrphanNodes();

            log($"total mismatch count: {totalMismatchCount}");
        }

        private static void CompleteRebuildConnectWithParentLog(string message)
        {
            var nameMatch = Regex.Match(message, nameRegex);
            Debug.Assert(nameMatch.Success, "cannot parse the field");
            var name = nameMatch.Groups[1].Value;

            var parentNameMatch = Regex.Match(message, parentNameRegex);
            Debug.Assert(parentNameMatch.Success, "cannot parse the field");
            var parentName = parentNameMatch.Groups[1].Value;

            var nodeIdMatch = Regex.Match(message, idRegex);
            Debug.Assert(nodeIdMatch.Success, "cannot parse the field");
            var nodeId = long.Parse(nodeIdMatch.Groups[1].Value);

            var parentIdMatch = Regex.Match(message, parentIdRegex);
            Debug.Assert(parentIdMatch.Success, "cannot parse the field");
            var parentId = long.Parse(parentIdMatch.Groups[1].Value);

            Debug.Assert(dict[nodeId].ParentId == parentId, "parent id mismatch");
            Debug.Assert(dict[parentId].Children.Exists(n => n.Id == nodeId), "cannot find child");

            if (!string.IsNullOrEmpty(dict[nodeId].Name))
            {
                Debug.Assert(dict[nodeId].Name == name, "name mismatch");
            }

            if (!string.IsNullOrEmpty(dict[parentId].Name))
            {
                Debug.Assert(dict[parentId].Name == parentName, "parent name mismatch");
            }

            dict[nodeId].Name = name;
            dict[parentId].Name = parentName;
        }

        private static void ProcessLoadCompletedLog(string message)
        {
            var nodeIdMatch = Regex.Match(message, idRegex);
            Debug.Assert(nodeIdMatch.Success, "cannot parse the field");
            var nodeId = long.Parse(nodeIdMatch.Groups[1].Value);

            var parentIdMatch = Regex.Match(message, parentIdRegex);
            Debug.Assert(parentIdMatch.Success, "cannot parse the field");
            var parentId = long.Parse(parentIdMatch.Groups[1].Value);

            var childrenMatch = Regex.Match(message, numChildrenRegex);
            Debug.Assert(childrenMatch.Success, "cannot parse the field");
            var numChildren = int.Parse(childrenMatch.Groups[1].Value);

            if (!dict.ContainsKey(nodeId))
            {
                dict[nodeId] = new TreeNode(nodeId);
            }
            else
            {
                log($"Duplicate found: node id {nodeId}");
            }

            dict[nodeId].NumChildren = numChildren;
            dict[nodeId].ParentId = parentId;

            if (!dict.ContainsKey(parentId))
            {
                dict[parentId] = new TreeNode(parentId);
            }

            dict[parentId].Children.Add(dict[nodeId]);
        }

        private static void ValidateTree(TreeNode root)
        {
            if (root.NumChildren != root.Children.Count)
            {
                totalMismatchCount++;
                log($"Num of children count mismatch found! Node Id {root.Id}. Count in Stat: {root.NumChildren}, actual count: {root.Children.Count}");
            }

            foreach (var child in root.Children)
            {
                ValidateTree(child);
            }
        }

        private static void ValidateNames(TreeNode root)
        {
            Queue<TreeNode> queue = new Queue<TreeNode>();
            queue.Enqueue(root);
            int level = 0;
            while (queue.Count != 0)
            {
                int size = queue.Count;
                level++;
                for (int i = 0; i < size; i++)
                {
                    TreeNode curr = queue.Dequeue();
                    foreach (var n in curr.Children)
                    {
                        queue.Enqueue(n);
                    }

                    if (!IsNodeNameValid(curr.Name, level))
                    {
                        log($"Found unusual names at level {level}: {curr.Name}, {curr.Id}");
                    }
                }
            }
        }

        private static bool IsNodeNameValid(string name, int level)
        {
            if (level == 1)
            {
                return name == "/";
            }
            else if (level == 2)
            {
                return name.StartsWith("MadariUserData") || name.StartsWith("$");
            }
            else if (level == 3)
            {
                return name.StartsWith("%2Fvnets%2F");
            }
            else if (level == 4)
            {
                return name.StartsWith("mappings") || name.StartsWith("privateip");
            }
            else if (level == 5)
            {
                return name.StartsWith("lnmid") || name.StartsWith("v4ca") || name.StartsWith("v6ca") || name.StartsWith("lnms");
            }
            else if (level == 6)
            {
                var parts = name.Split(".");
                if (parts.Length == 4)
                {
                    // assume the name is valid IP address
                    return true;
                }
                else
                {
                    // name is Guid
                    parts = name.Split("-");
                    return parts.Length >= 5;
                }
            }
            else if (level == 7)
            {
                return name == "ca";
            }

            return false;
        }

        private static void FindOrphanNodes()
        {
            foreach (var pair in dict)
            {
                var node = pair.Value;
                var parentId = node.ParentId;

                if (!dict.ContainsKey(parentId) && node.Id != 0)
                {
                    log($"Found orphan node. Node id {pair.Key}, parent id {parentId}, " +
                        $"num of children (from stat): {node.NumChildren}, actual num: {node.Children.Count}, total number of children {GetAllChildrenCount(node)}");
                }
            }
        }

        private static int GetAllChildrenCount(TreeNode root)
        {
            if (root == null || root.Children.Count == 0)
            {
                return 0;
            }

            int num = root.Children.Count;
            foreach (var child in root.Children)
            {
                num += GetAllChildrenCount(child);
            }

            return num;
        }
    }
}
