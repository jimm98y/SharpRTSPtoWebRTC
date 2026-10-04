// SharpRTSPtoWebRTC
// Copyright (C) 2026 Lukas Volf
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

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
