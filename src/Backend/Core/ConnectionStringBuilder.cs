// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="ConnectionStringBuilder.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>
// <summary>an abstraction to construct connection strings</summary>
// ***********************************************************************

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

        /// <summary>
        /// returns the string for the connection string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (endpoints.Count == 0)
            {
                throw new ArgumentException("at least one endpoint is needed");
            }

            if (numConnections > 1)
            {
                sb.AppendFormat("numconnections={0}|", numConnections);
            }

            string sep = String.Empty;
            foreach (string ep in endpoints)
            {
                sb.AppendFormat("{0}{1}", sep, ep);
                sep = ";";
            }
            return sb.ToString();
        }
    }
}
