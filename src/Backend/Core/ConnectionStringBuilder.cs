// <copyright file="ConnectionStringBuilder.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// the builder of connection strings.
    /// </summary>
    public class ConnectionStringBuilder
    {
        private int numConnections = 1;
        private HashSet<string> endpoints = new HashSet<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStringBuilder"/> class.
        /// </summary>
        public ConnectionStringBuilder()
        {
        }

        /// <summary>
        /// adds an endpoint to the list in the connection string
        /// </summary>
        /// <param name="endpoint">endpoint to add</param>
        public void AddEndpoint(string endpoint)
        {
            if (endpoint == null || endpoint.Split(':').Length != 2)
            {
                throw new ArgumentException("endpoint must be <host>:<port> and it is " + endpoint);
            }

            this.endpoints.Add(endpoint);
        }

        /// <summary>
        /// sets the max number of connections for this connection string
        /// </summary>
        /// <param name="num">number of connections</param>
        public void SetNumConnections(int num)
        {
            if (num <= 0)
            {
                throw new ArgumentException("num must be >0 and it is " + num);
            }

            if (num > 15)
            {
                throw new ArgumentException("num must be <=15 and it is " + num);
            }

            this.numConnections = num;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (this.endpoints.Count == 0)
            {
                throw new ArgumentException("at least one endpoint is needed");
            }

            if (this.numConnections > 1)
            {
                sb.AppendFormat("numconnections={0}|", this.numConnections);
            }

            string sep = string.Empty;
            foreach (string ep in this.endpoints)
            {
                sb.AppendFormat("{0}{1}", sep, ep);
                sep = ";";
            }

            return sb.ToString();
        }
    }
}
