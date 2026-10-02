// <copyright file="Program.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Vega.Performance
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Console entry point for convenience of debugging
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">List of test methods to run</param>
        private static void Main(string[] args)
        {
            var testMethods = GetAllTestMethods();
            Console.WriteLine("List of test methods:");
            Console.WriteLine(string.Join(Environment.NewLine, testMethods.Select(m => m.Name)));

            var context = new DummyTestContext();

            VegaInMemoryPerf.Setup(context);

            // CallbackBeforeCompleteTests.Setup(context);
            foreach (var methodName in args)
            {
                var method = testMethods.FirstOrDefault(m => string.Compare(m.Name, methodName, StringComparison.OrdinalIgnoreCase) == 0);
                if (method != null)
                {
                    var inst = Activator.CreateInstance(method.DeclaringType);
                    Console.WriteLine($"Start running test {method.Name}");
                    var result = method.Invoke(inst, null) as Task;
                    if (result != null)
                    {
                        result.GetAwaiter().GetResult();
                    }

                    Console.WriteLine($"Finished running test {method.Name}");
                }
            }
        }

        /// <summary>
        /// Gets the list of test methods in all test classes
        /// </summary>
        /// <returns>List of test methods</returns>
        private static System.Reflection.MethodInfo[] GetAllTestMethods()
        {
            var assembly = typeof(Program).Assembly;
            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttributes(true).Any(a => a.GetType().Name == "TestClassAttribute"));
            var testMethods = testClasses.SelectMany(c => c.GetMethods())
                .Where(m => m.GetCustomAttributes(true).Any(a => a.GetType().Name == "TestMethodAttribute"));
            return testMethods.ToArray();
        }

        /// <summary>
        /// Dummy test context to initialize the test class
        /// </summary>
        private class DummyTestContext : TestContext
        {
            /// <inheritdoc />
            public override IDictionary Properties { get; } = new Dictionary<string, object>();

            /// <inheritdoc />
            public override void AddResultFile(string fileName)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc />
            public override void Write(string message) => Console.Write(message);

            /// <inheritdoc />
            public override void Write(string format, params object[] args) => Console.Write(format, args);

            /// <inheritdoc />
            public override void WriteLine(string message) => Console.WriteLine(message);

            /// <inheritdoc />
            public override void WriteLine(string format, params object[] args) => Console.WriteLine(format, args);
        }
    }
}
