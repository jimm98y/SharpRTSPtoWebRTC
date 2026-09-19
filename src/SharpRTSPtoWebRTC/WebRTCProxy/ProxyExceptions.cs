using System;

namespace SharpRTSPtoWebRTC.WebRTCProxy
{
    /// <summary>
    /// Thrown when an offer is asked for under a session id that is already in use.
    /// </summary>
    /// <remarks>
    /// Its own type so the caller can answer 409 rather than the 500 an ArgumentNullException used
    /// to produce - the request is refused, but nothing about it was null.
    /// </remarks>
    public class DuplicateSessionException : Exception
    {
        public DuplicateSessionException(string id)
            : base($"The session id '{id}' is already in use.")
        {
            SessionId = id;
        }

        public string SessionId { get; }
    }
}
