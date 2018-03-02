// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="RetriableOperationException.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    using System;

    /// <summary>
    /// abstracts an exception that causes this operation to fail, but it is likely to succeed if tried again
    /// </summary>
    [Serializable]
    public class RetriableOperationException : Exception
    {
        /// <summary>
        /// the constructor, taking the message
        /// </summary>
        /// <param name="msg"></param>
        public RetriableOperationException(string msg)
            : base(msg)
        {
        }
    }
}