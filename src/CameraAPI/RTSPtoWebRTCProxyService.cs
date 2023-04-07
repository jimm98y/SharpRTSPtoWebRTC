using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CameraAPI
{
    public class RTSPtoWebRTCProxyService : IHostedService
    {
        private readonly ILogger<RTSPtoWebRTCProxyService> _logger;

        private readonly ConcurrentDictionary<string, RTCPeerConnection> _peerConnections = new ConcurrentDictionary<string, RTCPeerConnection>();
        private readonly ConcurrentDictionary<string, RTSPClient> _rtspClients = new ConcurrentDictionary<string, RTSPClient>();

        public RTSPtoWebRTCProxyService(ILogger<RTSPtoWebRTCProxyService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task<RTCSessionDescriptionInit> GetOfferAsync(string id, string url, string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id", "A unique ID parameter must be supplied when creating a new peer connection.");
            }
            else if (_peerConnections.ContainsKey(id))
            {
                throw new ArgumentNullException("id", "The specified peer connection ID is already in use.");
            }

            TaskCompletionSource<(string video, int videoType, string audio, int audioType)> result = new TaskCompletionSource<(string video, int videoType, string audio, int audioType)>();

            // session must be created in advance in order to know which codec to use
            var client = new RTSPClient(_logger);
            client.SetupCompleted += (video, videoType, audio, audioType) =>
            {
                result.SetResult((video, videoType, audio, audioType));
            };
            client.Connect(url, RTSPClient.RTP_TRANSPORT.TCP, userName, password);

            var codecs = await result.Task;

            VideoCodecsEnum videoCodecEnum = VideoCodecsEnum.Unknown;

            switch (codecs.video)
            {
                case "H264":
                    videoCodecEnum = VideoCodecsEnum.H264;
                    break;

                case "H265":
                    videoCodecEnum = VideoCodecsEnum.H265;
                    break;

                default:
                    throw new NotSupportedException(codecs.video);
            }

            AudioCodecsEnum audioCodecEnum = AudioCodecsEnum.Unknown;

            switch (codecs.audio)
            {
                case "PCMA":
                    audioCodecEnum = AudioCodecsEnum.PCMA;
                    break;

                case "PCMU":
                    audioCodecEnum = AudioCodecsEnum.PCMU;
                    break;

                case "MPEG4-GENERIC": // AAC not supported currently by SipSorcery
                    //throw new NotSupportedException(codecs.audio);

                default:
                    audioCodecEnum = AudioCodecsEnum.Unknown;
                    break;
            }

            RTCPeerConnection peerConnection = new RTCPeerConnection(null);

            if (videoCodecEnum != VideoCodecsEnum.Unknown)
            {
                SDPAudioVideoMediaFormat videoFormat = new SDPAudioVideoMediaFormat(new VideoFormat(videoCodecEnum, codecs.videoType));
                MediaStreamTrack videoTrack = new MediaStreamTrack(SDPMediaTypesEnum.video, false, new List<SDPAudioVideoMediaFormat> { videoFormat }, MediaStreamStatusEnum.SendOnly);
                peerConnection.addTrack(videoTrack);
            }

            if(audioCodecEnum != AudioCodecsEnum.Unknown)
            {
                SDPAudioVideoMediaFormat audioFormat = new SDPAudioVideoMediaFormat(new AudioFormat(audioCodecEnum, codecs.audioType));
                MediaStreamTrack audioTrack = new MediaStreamTrack(SDPMediaTypesEnum.audio, false, new List<SDPAudioVideoMediaFormat> { audioFormat }, MediaStreamStatusEnum.SendOnly);
                peerConnection.addTrack(audioTrack);
            }

            peerConnection.onicecandidateerror += 
                (candidate, error) => _logger.LogWarning($"Error adding remote ICE candidate. {error} {candidate}");
            peerConnection.oniceconnectionstatechange += 
                (state) => _logger.LogDebug($"ICE connection state change to {state}.");
            peerConnection.OnRtcpBye += 
                (reason) => _logger.LogDebug($"RTCP BYE receive, reason: {(string.IsNullOrWhiteSpace(reason) ? "<none>" : reason)}.");
            peerConnection.OnRtpClosed += 
                (reason) => _logger.LogDebug($"Peer connection closed, reason: {(string.IsNullOrWhiteSpace(reason) ? "<none>" : reason)}.");

            peerConnection.onconnectionstatechange += (state) =>
            {
                _logger.LogDebug($"Peer connection {id} state changed to {state}.");

                if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.disconnected || state == RTCPeerConnectionState.failed)
                {
                    _peerConnections.TryRemove(id, out _);
                    
                    if(_rtspClients.TryRemove(id, out var rtspClient))
                    {
                        rtspClient.Stop();
                    }
                }
                else if (state == RTCPeerConnectionState.connected)
                {
                    _logger.LogDebug("Peer connection connected.");
                }
            };

            client.RtpMessageReceived += (message, rtpTimestamp, markerBit, payloadType, skip) =>
            {
                // forward RTP to WebRTC "as is", just without the RTP header
                byte[] msg = message.Skip(skip).ToArray();

                if (payloadType == codecs.videoType)
                {
                    peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, msg, rtpTimestamp, markerBit, payloadType);
                }
                else if(payloadType == codecs.audioType)
                {
                    peerConnection.SendRtpRaw(SDPMediaTypesEnum.audio, msg, rtpTimestamp, markerBit, payloadType);
                }
                else
                {
                    _logger.LogDebug($"Unknown type {payloadType}.");
                }
            };

            var offerInit = peerConnection.createOffer();
            
            if (!offerInit.sdp.Contains($"a=fmtp:{codecs.videoType}") && offerInit.sdp.Contains($"a=rtpmap:{codecs.videoType} H264/90000\r\n"))
            {
                // mungle SDP for Firefox, otherwise Firefox answers with VP8 and WebRTC connection fails: https://groups.google.com/g/discuss-webrtc/c/facYnHFiY-8?pli=1
                offerInit.sdp = offerInit.sdp.Replace($"a=rtpmap:{codecs.videoType} H264/90000\r\n", $"a=rtpmap:{codecs.videoType} H264/90000\r\na=fmtp:{codecs.videoType} profile-level-id=42e01f;level-asymmetry-allowed=1;packetization-mode=1\r\n");
            }

            await peerConnection.setLocalDescription(offerInit);

            _peerConnections.TryAdd(id, peerConnection);
            _rtspClients.TryAdd(id, client);

            return offerInit;
        }

        public void SetAnswer(string id, RTCSessionDescriptionInit description)
        {
            if (!_peerConnections.TryGetValue(id, out var peerConnection))
            {
                throw new ApplicationException("No peer connection is available for the specified id.");
            }
            else
            {
                _logger.LogDebug($"Answer SDP: {description.sdp}");
                peerConnection.setRemoteDescription(description);
            }
        }

        public void AddIceCandidate(string id, RTCIceCandidateInit iceCandidate)
        {
            if (!_peerConnections.TryGetValue(id, out var peerConnection))
            {
                throw new ApplicationException("No peer connection is available for the specified id.");
            }
            else
            {
                _logger.LogDebug($"ICE Candidate: {iceCandidate.candidate}");
                peerConnection.addIceCandidate(iceCandidate);
            }
        }
    }
}
