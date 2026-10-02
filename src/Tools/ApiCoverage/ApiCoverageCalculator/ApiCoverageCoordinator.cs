// <copyright file="ApiCoverageCoordinator.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace ApiCoverageCalculator
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using ApiCoverageCalculator.ApiCoverageCalculateRequestModel;
    using CoverageInfoExtractor.MethodCoverageInfoModel;
    using Newtonsoft.Json;

#pragma warning disable CA1303 //  Retrieve the following string(s) from a resource table instead

    /// <summary>
    /// Coordinator for API coverage calculation requests
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ApiCoverageCoordinator
    {
        private readonly HashSet<MethodCoverageInfoKey> total;
        private readonly HashSet<MethodCoverageInfoKey> covered;
        private readonly List<ApiCoverageCalculatorWorker> calculators;
        private readonly string resultFileName = "result.csv";
        private readonly string requestFileName = "CalculationRequests.json";

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiCoverageCoordinator"/> class
        /// </summary>
        public ApiCoverageCoordinator()
        {
            this.calculators = new List<ApiCoverageCalculatorWorker>();
            this.total = new HashSet<MethodCoverageInfoKey>();
            this.covered = new HashSet<MethodCoverageInfoKey>();
        }

        /// <summary>
        /// Start calculation process
        /// </summary>
        public void Start()
        {
            try
            {
                var request = this.ReadRequest();
                this.ScheduleCalculation(request);
                this.Calculate();
                this.MergeResult();
                this.WriteToLocal();

                Console.WriteLine($"Result - Total API: {this.total.Count}; Covered API: {this.covered.Count}");
            }
            catch (AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    // Extract LoaderExceptions to indicate which type failed to load
                    if (innerException is ReflectionTypeLoadException loadException)
                    {
                        foreach (var e in loadException.LoaderExceptions)
                        {
                            Console.WriteLine(e.ToString());
                        }
                    }
                    else
                    {
                        Console.WriteLine(innerException.ToString());
                    }
                }
            }
        }

        private void ScheduleCalculation(ApiCoverageCalculateRequest request)
        {
            foreach (var calcRequest in request.CalculateRequests)
            {
                this.calculators.Add(new ApiCoverageCalculatorWorker(calcRequest.AssemblyFolder, calcRequest.DllFileName, calcRequest.CoverageFileName, request.InterfaceWrappers));
            }
        }

        private void Calculate()
        {
            Parallel.ForEach(this.calculators, calculator => calculator.Calculate());
        }

        private ApiCoverageCalculateRequest ReadRequest()
        {
            string fullPath = Path.Combine(new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName, this.requestFileName);
            using (FileStream stream = new FileStream(fullPath, FileMode.Open))
            using (TextReader reader = new StreamReader(stream))
            {
                Console.WriteLine($"Reading request from {fullPath}");
                ApiCoverageCalculateRequest request = JsonConvert.DeserializeObject<ApiCoverageCalculateRequest>(reader.ReadToEnd());
                Console.WriteLine("Request loaded.");
                Console.WriteLine();

                if (request == null || request.CalculateRequests.Count == 0)
                {
                    throw new ArgumentException("No calculation request specified.");
                }

                Console.WriteLine("Assemblies to calculate:");
                foreach (var calcRequest in request.CalculateRequests)
                {
                    Console.WriteLine($"- Assembly Name: {calcRequest.DllFileName}");
                    Console.WriteLine($"--- Resource Folder: {calcRequest.AssemblyFolder}");
                    Console.WriteLine($"--- Coverage File Location : {calcRequest.CoverageFileName}");
                }

                Console.WriteLine();

                if (request.InterfaceWrappers != null && request.InterfaceWrappers.Count > 0)
                {
                    Console.WriteLine("Non-ServiceContract interfaces mapping:");
                    foreach (var interfaceMap in request.InterfaceWrappers)
                    {
                        Console.WriteLine($"- Interface: {interfaceMap.Key} => API Interface: {interfaceMap.Value}");
                    }

                    Console.WriteLine();
                }

                return request;
            }
        }

        private void MergeResult()
        {
            foreach (var calculator in this.calculators)
            {
                this.total.UnionWith(calculator.AllApiInfoKeys);
                this.covered.UnionWith(calculator.CoveredApiInfoKeys);
            }
        }

        private void WriteToLocal()
        {
            string fullPath = Path.Combine(new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName, this.resultFileName);
            using (FileStream stream = new FileStream(fullPath, FileMode.Create))
            using (TextWriter writer = new StreamWriter(stream))
            {
                foreach (var key in this.total)
                {
                    writer.WriteLine($"{key.ClassName},{key.MethodName},{this.covered.Contains(key)},\"{string.Join("; ", key.Parameters)}\"");
                }

                foreach (var key in this.covered)
                {
                    if (!this.total.Contains(key))
                    {
                        writer.WriteLine($"{key.ClassName},{key.MethodName},");
                    }
                }

                writer.Flush();
            }

            Console.WriteLine($"Written detail report to file: {fullPath}");
        }
    }
}
