using SharpRTSPClient;
using System;

namespace SharpRTSPtoWebRTC.WebRTCProxy
{
    /// <summary>
    /// When a dropped RTSP stream is worth reconnecting to, and how long to wait first.
    /// </summary>
    internal static class ReconnectPolicy
    {
        public const int MAX_ATTEMPTS = 100;

        private const int MAX_DELAY_SECONDS = 30;

        /// <summary>
        /// Whether reconnecting to a stream that stopped for this reason could ever work.
        /// </summary>
        /// <remarks>
        /// Retrying a rejected password or a path the server does not have just repeats the same
        /// exchange, and against a camera that locks an account out after so many failures it does
        /// real harm. These used to be retried as hard as a dropped connection.
        /// </remarks>
        public static bool IsWorthRetrying(StoppedReason reason)
        {
            switch (reason)
            {
                case StoppedReason.Unauthorized:
                case StoppedReason.NotFound:
                case StoppedReason.UnsupportedMedia:
                case StoppedReason.EncryptionUnavailable:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// How long to wait before the given attempt: 1s, 2s, 4s ... capped at 30s.
        /// </summary>
        public static TimeSpan Delay(int attempt)
        {
            if (attempt < 1)
            {
                attempt = 1;
            }

            // capped before the shift so a long outage cannot overflow it
            double seconds = attempt >= 6 ? MAX_DELAY_SECONDS : Math.Pow(2, attempt - 1);
            return TimeSpan.FromSeconds(Math.Min(MAX_DELAY_SECONDS, seconds));
        }
    }
}
