using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using System.Collections.Concurrent;
using System.Linq;

namespace CameraAPI
{
    public class RTSPClientWrapper
    {
        private ConcurrentDictionary<string, RTCPeerConnection> _peerConnections = new ConcurrentDictionary<string, RTCPeerConnection>();
        private readonly ILogger _logger;
        private int _lastVideoMarkerBit = 1; // initial value 1 to make sure the first connection will send sps/pps

        public RTSPClient Client { get; private set; }
        public int AudioType { get; private set; } = -1;
        public int VideoType { get; private set; } = -1;
        public string AudioCodec { get; private set; }
        public string VideoCodec { get; private set; }
        public VideoCodecsEnum VideoCodecEnum { get; set; } = VideoCodecsEnum.Unknown;
        public AudioCodecsEnum AudioCodecEnum { get; set; } = AudioCodecsEnum.Unknown;
        public byte[] Sps { get; set; } = null;
        public byte[] Pps { get; set; } = null;

        public RTSPClientWrapper(ILogger logger, RTSPClient client, int audioType, string audioCodec, int videoType, string videoCodec, byte[] sps, byte[] pps)
        {
            this._logger = logger;
            this.Client = client;
            this.AudioType = audioType;
            this.VideoType = videoType;
            this.AudioCodec = audioCodec;
            this.VideoCodec = videoCodec;
            this.Sps = sps;
            this.Pps = pps;

            if (VideoType > 0)
            {
                switch (VideoCodec)
                {
                    case "H264":
                        VideoCodecEnum = VideoCodecsEnum.H264;
                        break;

                    case "H265":
                        VideoCodecEnum = VideoCodecsEnum.H265;
                        break;

                    default:
                        VideoCodecEnum = VideoCodecsEnum.Unknown;
                        break;
                }
            }

            if (AudioType > 0)
            {
                switch (AudioCodec)
                {
                    case "PCMA":
                        AudioCodecEnum = AudioCodecsEnum.PCMA;
                        break;

                    case "PCMU":
                        AudioCodecEnum = AudioCodecsEnum.PCMU;
                        break;

                    case "MPEG4-GENERIC": // AAC not supported currently by WebRTC, it requires transcoding to PCMA/PCMU
                                          //throw new NotSupportedException(codecs.audio);

                    default:
                        AudioCodecEnum = AudioCodecsEnum.Unknown;
                        break;
                }
            }

            client.RtpMessageReceived += (message, rtpTimestamp, markerBit, payloadType, skip) =>
            {
                // forward RTP to WebRTC "as is", just without the RTP header
                byte[] msg = message.Skip(skip).ToArray();

                if (payloadType == VideoType && VideoCodecEnum != VideoCodecsEnum.Unknown)
                {
                    foreach (System.Collections.Generic.KeyValuePair<string, RTCPeerConnection> peerConnection in _peerConnections)
                    {
                        if (Sps != null)
                        {
                            if (_lastVideoMarkerBit == 1 && markerBit == 0)
                            {
                                // send SPS/PPS in between NALs
                                peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, Sps, rtpTimestamp, 0, payloadType);
                                peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, Pps, rtpTimestamp, 0, payloadType);
                            }
                        }

                        peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, msg, rtpTimestamp, markerBit, payloadType);
                    }

                    _lastVideoMarkerBit = markerBit;
                }
                else if (payloadType == AudioType && AudioCodecEnum != AudioCodecsEnum.Unknown) // avoid sending RTP for unsupported codecs (AAC)
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
