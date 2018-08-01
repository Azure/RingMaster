// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Test
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Vega Code Benchmark Console Program
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            Action<string> log = Console.WriteLine;

            log($"# Vega Benchmark");
            log($"Test environment and parameters:");
            log($"- OS Version: {Environment.OSVersion}");
            log($"- Number of processors: {Environment.ProcessorCount}\n");

            var benches = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.GetInterface(typeof(IBench).Name) != null && !t.IsAbstract);

            log("List of benchmark suites: " + string.Join(", ", benches.Select(b => b.Name)) + "\n");

            foreach (var b in benches)
            {
                var match = true;
                if (args.Length > 0)
                {
                    match = args.Any(a => Regex.Match(b.Name, a).Success);
                }

                if (match)
                {
                    var inst = Activator.CreateInstance(b) as IBench;
                    inst.Run(log);
                }
            }
        }
    }
}
