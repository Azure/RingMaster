// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.CodeCoverage
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.Coverage.Analysis;

    /// <summary>
    /// the program
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: Microsoft.Vega.CodeCoverage {DirectoryContainingCoverageFile} {outputDirectory}");
            }

            DirectoryInfo directory = new DirectoryInfo(args[0]);
            FileInfo[] files = directory.GetFiles("*.coverage", SearchOption.AllDirectories);
            if (files == null || files.Length == 0)
            {
                Console.WriteLine("No coverage files found. Exiting.");
                return;
            }

            files = files.GroupBy(f => f.Name).Select(x => x.First()).ToArray();

            foreach (var file in files)
            {
                Console.WriteLine($"{file.FullName}, file size: {file.Length}");
            }

            CoverageInfo coverageInfo = CoverageInfo.CreateFromFile(files[0].FullName);
            for (int i = 1; i < files.Length; i++)
            {
                coverageInfo = CoverageInfo.Join(coverageInfo, CoverageInfo.CreateFromFile(files[i].FullName));
            }

            Directory.CreateDirectory(args[1]);

            CoverageDS data = coverageInfo.BuildDataSet();
            var mergedFile = Path.Combine(args[1], "MergedCoverage.xml");
            data.WriteXml(mergedFile);
        }
    }
}
