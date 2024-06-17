using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharpRTSPClient;

namespace SharpRTSPtoWebRTC.WebRTCProxy
{
    public class RTSPtoWebRTCProxyService 
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<RTSPtoWebRTCProxyService> _logger;
        private readonly ConcurrentDictionary<string, RTCPeerConnection> _peerConnections = new ConcurrentDictionary<string, RTCPeerConnection>();
        private readonly ConcurrentDictionary<string, Task<RTSPtoWebRTCProxy>> _rtspClients = new ConcurrentDictionary<string, Task<RTSPtoWebRTCProxy>>();

        public RTSPtoWebRTCProxyService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<RTSPtoWebRTCProxyService>();
            SIPSorcery.LogFactory.Set(loggerFactory); // get the logs from the SIP Sorcery
        }

        public async Task<RTCSessionDescriptionInit> GetOfferAsync(string id, string url, string userName = null, string password = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id), "A unique ID parameter must be supplied when creating a new peer connection.");
            }
            else if (_peerConnections.ContainsKey(id))
            {
                throw new ArgumentNullException(nameof(id), "The specified peer connection ID is already in use.");
            }
            else if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                throw new ArgumentException(nameof(url), "Invalid camera URL.");
            }

            // session must be created in advance in order to know which codec to use
            RTSPtoWebRTCProxy proxy = await GetOrCreateClientAsync(_loggerFactory, url, userName, password);

            RTCPeerConnection peerConnection = new RTCPeerConnection(null);

            if (proxy.VideoCodecEnum != VideoCodecsEnum.Unknown)
            {
                SDPAudioVideoMediaFormat videoFormat = new SDPAudioVideoMediaFormat(proxy.VideoFormat);
                MediaStreamTrack videoTrack = new MediaStreamTrack(SDPMediaTypesEnum.video, false, new List<SDPAudioVideoMediaFormat> { videoFormat }, MediaStreamStatusEnum.SendOnly);
                peerConnection.addTrack(videoTrack);
            }

            if (proxy.AudioCodecEnum != AudioCodecsEnum.Unknown)
            {
                SDPAudioVideoMediaFormat audioFormat = new SDPAudioVideoMediaFormat(proxy.AudioFormat);
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

            peerConnection.OnReceiveReport += (re, media, rr) => _logger.LogDebug($"RTCP Receive for {media} from {re}\n{rr.GetDebugSummary()}");
            peerConnection.OnSendReport += (media, sr) => _logger.LogDebug($"RTCP Send for {media}\n{sr.GetDebugSummary()}");

            peerConnection.onconnectionstatechange += (state) =>
            {
                _logger.LogDebug($"Peer connection {id} state changed to {state}.");

                if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.disconnected || state == RTCPeerConnectionState.failed)
                {
                    if (_peerConnections.TryRemove(id, out _))
                    {
                        if (_rtspClients.TryGetValue(url, out var rtspClient))
                        {
                            if (rtspClient.Result.RemovePeerConnection(id) == 0)
                            {
                                if (_rtspClients.TryRemove(url, out _))
                                {
                                    rtspClient.Result.Stop();
                                    _logger.LogDebug($"RTSPClient for {url} stopped.");
                                }
                            }
                        }
                    }
                }
                else if (state == RTCPeerConnectionState.connected)
                {
                    _logger.LogDebug("Peer connection connected.");
                }
            };

            proxy.AddPeerConnection(id, peerConnection);

            var offerInit = peerConnection.createOffer();
            offerInit.sdp = MungleSDP(offerInit.sdp, proxy);
            await peerConnection.setLocalDescription(offerInit);

            _peerConnections.TryAdd(id, peerConnection);

            return offerInit;
        }

        private static string MungleSDP(string sdp, RTSPtoWebRTCProxy client)
        {
            if (!sdp.Contains($"a=fmtp:{client.VideoType}") && sdp.Contains($"a=rtpmap:{client.VideoType} H264/90000\r\n"))
            {
                // packetization-mode - All endpoints are required to support mode 1 (non-interleaved mode). Support for other packetization modes is optional, and the parameter itself is not required to be specified.
                // profile-level-id - All WebRTC implementations are required to specify and interpret this parameter in their SDP, identifying the sub-profile used by the codec. The specific value that is set is not defined; what matters is that the parameter be used at all.This is useful to note, since in RFC 6184("RTP Payload Format for H.264 Video"), profile-level-id is entirely optional.
                // sprop-parameter-sets - Sequence and picture information for AVC can be sent either in-band or out-of - band. When AVC is used with WebRTC, this information must be signaled in-band; the sprop-parameter-sets parameter must therefore not be included in the SDP.

                // mungle SDP for Firefox, otherwise Firefox answers with VP8 and WebRTC connection fails: https://groups.google.com/g/discuss-webrtc/c/facYnHFiY-8?pli=1
                sdp = sdp.Replace($"a=rtpmap:{client.VideoType} H264/90000\r\n", $"a=rtpmap:{client.VideoType} H264/90000\r\na=fmtp:{client.VideoType} profile-level-id=42e01f;level-asymmetry-allowed=1;packetization-mode=1\r\n");
            }
            //else if (!sdp.Contains($"a=fmtp:{client.VideoType}") && sdp.Contains($"a=rtpmap:{client.VideoType} H265/90000\r\n"))
            //{
            //    sdp = sdp.Replace($"a=rtpmap:{client.VideoType} H265/90000\r\n", $"a=rtpmap:{client.VideoType} H265/90000\r\na=fmtp:{client.VideoType} profile-id=1;tier-flag=0;level-id=120\r\n");
            //}
            return sdp;
        }

        private Task<RTSPtoWebRTCProxy> GetOrCreateClientAsync(ILoggerFactory loggerFactory, string url, string userName, string password)
        {
            return _rtspClients.GetOrAdd(url, (u) =>
            {
                return CreateClientAsync(loggerFactory, url, userName, password);
            });
        }

        private async Task<RTSPtoWebRTCProxy> CreateClientAsync(ILoggerFactory loggerFactory, string url, string userName, string password)
        {
            TaskCompletionSource<bool> result = new TaskCompletionSource<bool>();
            var client = new RTSPClient(loggerFactory);

            IStreamConfigurationData videoStream = null;
            int videoType = -1;
            string videoName = "";
            client.NewVideoStream += (o, e) =>
            {
                videoStream = e.StreamConfigurationData;
                videoType = e.PayloadType;
                videoName = e.StreamType;
            };

            IStreamConfigurationData audioStream = null;
            int audioType = -1;
            string audioName = "";
            client.NewAudioStream += (o, e) =>
            {
                audioStream = e.StreamConfigurationData;
                audioType = e.PayloadType;
                audioName = e.StreamType;
            };

            client.SetupMessageCompleted += (o, e) =>
            {
                result.SetResult(true);
            };

            client.Connect(url, RTPTransport.TCP, userName, password);

            var codecs = await result.Task;
            return new RTSPtoWebRTCProxy(_logger, client, videoType, videoName, videoStream, audioType, audioName, audioStream);
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
