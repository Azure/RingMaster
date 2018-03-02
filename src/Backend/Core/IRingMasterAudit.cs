// ***********************************************************************
// Assembly         : RingMaster
// <copyright file="IRingMasterAudit.cs" company="Microsoft">
//     Copyright ©  2015
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace Microsoft.Azure.Networking.Infrastructure.RingMaster.Backend
{
    /// <summary>
    /// Interface that must be implemented by a consumer of RingMaster audit events.
    /// </summary>
    public interface IRingMasterAudit
    {
        /// <summary>
        /// A session is being initialized.
        /// </summary>
        /// <param name="sessionId">Unique Id of the session in which the command was received</param>
        /// <param name="clientIP">Address of the client</param>
        /// <param name="clientIdentity">Identity of the client</param>
        void OnInitializeSession(ulong sessionId, string clientIP, string clientIdentity);

        /// <summary>
        /// Auth information of a session is being changed.
        /// </summary>
        /// <param name="sessionId">Unique Id of the session in which the command was received</param>
        /// <param name="clientIP">Address of the client</param>
        /// <param name="clientIdentity">Identity of the client</param>
        /// <param name="clientDigest">Client digest</param>
        /// <param name="isSuperSession">If <c>true</c> this is a session with super user privileges</param>
        void OnSetAuth(ulong sessionId, string clientIP, string clientIdentity, string clientDigest, bool isSuperSession);

        /// <summary>
        /// A request to execute a command has been received.
        /// </summary>
        /// <param name="sessionId">Unique Id of the session in which the command was received</param>
        /// <param name="clientIP">Address of the client</param>
        /// <param name="clientIdentity">Identity of the client</param>
        /// <param name="path">Path associated with the request</param>
        void OnRequestCommand(ulong sessionId, string clientIP, string clientIdentity, string path);

        /// <summary>
        /// A command is being executed.
        /// </summary>
        /// <param name="command">Command that is being executed</param>
        void OnRunCommand(string command);
    }
}