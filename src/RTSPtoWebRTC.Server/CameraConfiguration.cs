using SharpRTSPClient;

namespace RTSPtoWebRTC.Server
{
    public class CameraConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string UserName { get; set; }
        public string Password { get; set; }

        /// <summary>
        /// Port range for the WebRTC peer connections serving this camera. Both ends must be even.
        /// </summary>
        public int StartPort { get; set; }
        public int EndPort { get; set; }

        /// <summary>
        /// How the RTP arrives from the camera. TCP interleaves it in the RTSP connection, which is
        /// what gets through a firewall; UDP keeps it off the control connection, which a camera
        /// under load usually prefers.
        /// </summary>
        public RTPTransport Transport { get; set; } = RTPTransport.TCP;

        /// <summary>
        /// Port range the UDP transports take their pairs from. Ignored for TCP, and left to the
        /// client's own default when either end is zero. Keep it clear of the WebRTC range above.
        /// </summary>
        public int RtspStartPort { get; set; }
        public int RtspEndPort { get; set; }
    }
}
