// <copyright file="SerializationFormatVersions.cs" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading.Tasks;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster;
    using Microsoft.Azure.Networking.Infrastructure.RingMaster.Data;

    /// <summary>
    /// Serialization format versions.
    /// </summary>
    public class SerializationFormatVersions
    {
        /// <summary>
        /// The minimum supported serialization version.
        /// </summary>
        public const uint MinimumSupportedVersion = Version13;

        /// <summary>
        /// The maximum supported serialization version.
        /// </summary>
        public const uint MaximumSupportedVersion = Version25;

        /// <summary>
        /// The very first protocol version.
        /// </summary>
        public const uint Version1 = 1;

        /// <summary>
        /// GetData allows fault back option from this version.
        /// </summary>
        public const uint Version2 = 2;

        /// <summary>
        /// ResponsePath is included in the results from this version.
        /// CreateMode.AllowPathCreationFlag is allowed on create.
        /// OpResult.CreateResult contains a stat.
        /// </summary>
        public const uint Version3 = 3;

        /// <summary>
        /// Added support for data commands on set data, and delete can contain
        /// isCascade to indicate cascade deletions.
        /// </summary>
        public const uint Version4 = 4;

        /// <summary>
        /// Multi can complete synchronously.
        /// </summary>
        public const uint Version5 = 5;

        /// <summary>
        /// <c>Auth</c> can be a field of any request.
        /// </summary>
        public const uint Version6 = 6;

        /// <summary>
        /// Added length to the requests.
        /// </summary>
        public const uint Version7 = 7;

        /// <summary>
        /// Added length to the responses.
        /// </summary>
        public const uint Version8 = 8; // add length to the responses

        /// <summary>
        /// Added GetDataOptions.
        /// </summary>
        public const uint Version9 = 9;

        /// <summary>
        /// Added on <c>Session.Init</c> the ability to request read operations to go lock-free.
        /// </summary>
        public const uint Version10 = 10;

        /// <summary>
        /// Added optional OperationOverrides.
        /// </summary>
        public const uint Version11 = 11;

        /// <summary>
        /// Added on check the ability to request a particular uniqueIncarnationId in addition to the version.
        /// </summary>
        public const uint Version12 = 12;

        /// <summary>
        /// Added redirection policy on session initialization.
        /// </summary>
        public const uint Version13 = 13;

        /// <summary>
        /// Added GetData options argument.
        /// </summary>
        public const uint Version14 = 14;

        /// <summary>
        /// Added Batch command.
        /// </summary>
        public const uint Version15 = 15;

        /// <summary>
        /// Added Check options for <c>cversion</c> and <c>aversion</c>.
        /// </summary>
        public const uint Version16 = 16;

        /// <summary>
        /// Added ExecutionQueue and ExecutionQueueTimeout to any request.
        /// </summary>
        public const uint Version17 = 17;

        /// <summary>
        /// Added DeleteMode to delete command
        /// </summary>
        public const uint Version18 = 18;

        /// <summary>
        /// Added Move to delete command
        /// </summary>
        public const uint Version19 = 19;

        /// <summary>
        /// Added $scheduled feature to multi command
        /// </summary>
        public const uint Version20 = 20;

        /// <summary>
        /// Added TimeSeriesId to request.
        /// </summary>
        public const uint Version21 = 21;

        /// <summary>
        /// Added nullable string on multi.create op.
        /// </summary>
        public const uint Version22 = 22;

        /// <summary>
        /// Added Data and Stat in WatchedEvent
        /// </summary>
        public const uint Version23 = 23;

        /// <summary>
        /// Added Stat in multi.check op
        /// Added GetChildren op in multi
        /// </summary>
        public const uint Version24 = 24;

        /// <summary>
        /// Added GetSubtree request
        /// Added Exists, Sync, GetSubtree ops in multi/batch
        /// </summary>
        public const uint Version25 = 25;
    }
}