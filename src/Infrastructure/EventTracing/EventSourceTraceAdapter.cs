// <copyright file="EventSourceTraceAdapter.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Instrumentation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Tracing;
    using System.Text;

    /// <summary>
    /// Consumes messages logged to an event source and outputs them as trace messages.
    /// </summary>
    public sealed class EventSourceTraceAdapter : EventListener
    {
        private readonly Dictionary<string, TrackedEventSource> trackedEventSources = new Dictionary<string, TrackedEventSource>();

        /// <summary>
        /// Enable listening for events from an event source.
        /// </summary>
        /// <param name="name">Name of the event source</param>
        /// <param name="level"></param>
        /// <param name="friendlyName"></param>
        public void Enable(string name, EventLevel level, string friendlyName)
        {
            var trackedEventSource = new TrackedEventSource
            {
                Name = name,
                Level = level,
                FriendlyName = friendlyName
            };

            this.trackedEventSources.Add(name, trackedEventSource);

            foreach (var source in EventSource.GetSources())
            {
                if (source.Name == name)
                {
                    Trace.TraceInformation($"Enabling EventSource name={source.Name}, level={level}, friendlyName={friendlyName}");
                    this.EnableEvents(source, level);
                    break;
                }
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs args)
        {
            if (args == null)
            {
                return;
            }

            if (!this.trackedEventSources.ContainsKey(args.EventSource.Name))
            {
                return;
            }

            try
            {
                var builder = new StringBuilder();

                builder.Append(this.trackedEventSources[args.EventSource.Name].FriendlyName);
                builder.Append('.');
                builder.Append(args.EventName);
                builder.Append(' ');

                for (int i = 0; i < args.PayloadNames.Count; i++)
                {
                    if (i != 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(args.PayloadNames[i]);
                    builder.Append('=');
                    builder.Append(args.Payload[i]);
                }

                string message = builder.ToString();
                switch (args.Level)
                {
                    case EventLevel.Critical:
                    case EventLevel.Error:
                        Trace.TraceError(message);
                        return;
                    case EventLevel.Warning:
                        Trace.TraceWarning(message);
                        return;
                    default:
                        Trace.TraceInformation(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to decode event. sourceName={args.EventSource.Name}, eventName={args.EventName}, exception={ex.ToString()}");
            }
        }

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source == null)
            {
                return;
            }

            foreach (var trackedEventSource in this.trackedEventSources.Values)
            {
                if (source.Name == trackedEventSource.Name)
                {
                    EventLevel level = trackedEventSource.Level;
                    Trace.TraceInformation($"Enabling EventSource name={source.Name}, level={level}, friendlyName={trackedEventSource.FriendlyName}");
                    this.EnableEvents(source, level);
                    break;
                }
            }
        }

        private struct TrackedEventSource
        {
            public string Name;
            public EventLevel Level;
            public string FriendlyName;
        }
    }
}