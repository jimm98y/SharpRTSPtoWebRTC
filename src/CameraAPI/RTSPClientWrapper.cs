using SIPSorcery.Net;
using System.Collections.Concurrent;
using System.Linq;

namespace CameraAPI
{
    public class RTSPClientWrapper
    {
        private ConcurrentDictionary<string, RTCPeerConnection> _peerConnections = new ConcurrentDictionary<string, RTCPeerConnection>();

        public RTSPClient Client { get; private set; }
        public int AudioType { get; private set; } = -1;
        public int VideoType { get; private set; } = -1;
        public string AudioCodec { get; private set; }
        public string VideoCodec { get; private set; }

        public RTSPClientWrapper(RTSPClient client, int audioType, string audioCodec, int videoType, string videoCodec)
        {
            this.Client = client;
            this.AudioType = audioType;
            this.VideoType = videoType;
            this.AudioCodec = audioCodec;
            this.VideoCodec = videoCodec;

            client.RtpMessageReceived += (message, rtpTimestamp, markerBit, payloadType, skip) =>
            {
                // forward RTP to WebRTC "as is", just without the RTP header
                byte[] msg = message.Skip(skip).ToArray();

                if (payloadType == VideoType)
                {
                    foreach (var peerConnection in _peerConnections)
                    {
                        peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, msg, rtpTimestamp, markerBit, payloadType);
                    }
                }
                else if (payloadType == AudioType)
                {
                    foreach (var peerConnection in _peerConnections)
                    {
                        peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.audio, msg, rtpTimestamp, markerBit, payloadType);
                    }
                }
            };
        }

        public void AddPeerConnection(string id, RTCPeerConnection peerConnection)
        {
            _peerConnections.TryAdd(id, peerConnection);
        }

        public int RemovePeerConnection(string id)
        {
            _peerConnections.TryRemove(id, out _);
            return _peerConnections.Count;
        }
    }
}
