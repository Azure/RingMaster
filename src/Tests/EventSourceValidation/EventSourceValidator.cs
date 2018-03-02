// <copyright file="EventSourceValidator.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.EventSourceValidation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The EventSourceValidator class.
    /// </summary>
    [TestClass]
    public sealed class EventSourceValidator
    {
        /// <summary>
        /// The ring master assembly prefix
        /// </summary>
        private const string RingMasterAssemblyPrefix = "Microsoft.RingMaster";

        /// <summary>
        /// The event source type
        /// </summary>
        private static readonly Type EventSourceType = typeof(EventSource);

        /// <summary>
        /// The event source base class methods
        /// </summary>
        private static List<string> baseClassMethods = EventSourceType.GetMethods().Select(m => m.Name).ToList();

        /// <summary>
        /// Checks all events are unique in same event source.
        /// </summary>
        [TestMethod]
        public void CheckAllEventsAreUniqueInSameEventSource()
        {
            var allAssemblies = LoadAllAssemblies();
            var customEventSources = new List<Type>();
            foreach (var assembly in allAssemblies)
            {
                customEventSources.AddRange(assembly.GetTypes().Where(t => t.IsSubclassOf(EventSourceType)));
            }

            Assert.IsTrue(customEventSources.Count > 0, "No event source types found.");

            foreach (var eventSourceType in customEventSources)
            {
                Assert.IsTrue(IsValidEventSource(eventSourceType), $"{eventSourceType.FullName} is not valid event source type.");
                Console.WriteLine($"validated {eventSourceType.FullName}");
            }
        }

        /// <summary>
        /// Determines whether the given event source type is a valid one.
        /// </summary>
        /// <param name="eventSourceType">Type of the event source.</param>
        /// <returns>
        ///   <c>true</c> if the given event source type is valid; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsValidEventSource(Type eventSourceType)
        {
            var methods = eventSourceType.GetMethods().Where(m => !baseClassMethods.Contains(m.Name)).ToArray();

            if (IsEventIdUnique(eventSourceType, methods) && IsEventNameUnique(eventSourceType, methods))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether [is event name unique] [the specified event source type].
        /// </summary>
        /// <param name="eventSourceType">Type of the event source.</param>
        /// <param name="methods">The methods.</param>
        /// <returns>
        ///   <c>true</c> if [is event name unique] [the specified event source type]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsEventNameUnique(Type eventSourceType, MethodInfo[] methods)
        {
            var eventNames = new HashSet<string>();
            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<EventAttribute>();
                if (attribute != null)
                {
                    if (eventNames.Contains(method.Name))
                    {
                        Console.WriteLine($"{eventSourceType.FullName} has duplicate event name {method.Name}");
                        return false;
                    }

                    eventNames.Add(method.Name);
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether [is event identifier unique] [the specified event source type].
        /// </summary>
        /// <param name="eventSourceType">Type of the event source.</param>
        /// <param name="methods">The methods.</param>
        /// <returns>
        ///   <c>true</c> if [is event identifier unique] [the specified event source type]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsEventIdUnique(Type eventSourceType, MethodInfo[] methods)
        {
            var eventIds = new HashSet<int>();
            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<EventAttribute>();
                if (attribute != null)
                {
                    if (eventIds.Contains(attribute.EventId))
                    {
                        Console.WriteLine($"{eventSourceType.FullName} has duplicate event id {attribute.EventId}");
                        return false;
                    }

                    eventIds.Add(attribute.EventId);
                }
            }

            return true;
        }

        /// <summary>
        /// Loads all assemblies.
        /// </summary>
        /// <returns>A list of Assemblies the project referenced.</returns>
        private static IEnumerable<Assembly> LoadAllAssemblies()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var assemblyFiles = Directory.GetFiles(path, $"{RingMasterAssemblyPrefix}*.dll").ToList();
            assemblyFiles.AddRange(Directory.GetFiles(path, $"{RingMasterAssemblyPrefix}*.exe"));

            return assemblyFiles.Select(Assembly.LoadFile);
        }
    }
}
