// <copyright file="ContentType.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.CommunicationProtocol
{
    using Data;
    using Requests;

    /// <summary>
    /// Type of content stored in a serialized Request or response.
    /// </summary>
    internal enum ContentType : byte
    {
        /// <summary>
        /// Unknown content
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A <see cref="IRingMasterRequest"/> object.
        /// </summary>
        Request = 1,

        /// <summary>
        /// A <see cref="WatcherCall"/> object.
        /// </summary>
        WatcherCall = 2,

        /// <summary>
        /// An array of strings.
        /// </summary>
        StringArray = 3,

        /// <summary>
        /// A list of strings.
        /// </summary>
        ListOfString = 4,

        /// <summary>
        /// A string.
        /// </summary>
        String = 5,

        /// <summary>
        /// A <see cref="Stat"/> object.
        /// </summary>
        Stat = 6,

        /// <summary>
        /// A byte array.
        /// </summary>
        ByteArray = 7,

        /// <summary>
        /// A list of <see cref="Acl"/>s.
        /// </summary>
        AclList = 8,

        /// <summary>
        /// A list of <see cref="OpResult"/>s.
        /// </summary>
        OpResultList = 9,

        /// <summary>
        /// A <see cref="RedirectSuggested"/> object.
        /// </summary>
        Redirect = 10,

        /// <summary>
        /// An object.
        /// </summary>
        AnyObject = 255,
    }
}
