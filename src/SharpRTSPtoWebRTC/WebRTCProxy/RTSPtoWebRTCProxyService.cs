using SIPSorcery.Net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharpRTSPClient;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Threading;
using SIPSorcery.Sys;

namespace SharpRTSPtoWebRTC.WebRTCProxy
{
    public class RTSPtoWebRTCProxyService 
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<RTSPtoWebRTCProxyService> _logger;
        private readonly IConfiguration _config;

        private const string CONFIG_KEY_PUBLIC_IPV4 = "PublicIPv4";
        private const string CONFIG_KEY_PUBLIC_IPV6 = "PublicIPv6";
        private readonly IPAddress _publicIPv4;
        private readonly IPAddress _publicIPv6;

        private readonly ConcurrentDictionary<string, RTCPeerConnection> _peerConnections = new ConcurrentDictionary<string, RTCPeerConnection>();
        private readonly ConcurrentDictionary<string, Lazy<ProxySession>> _rtspClients = new ConcurrentDictionary<string, Lazy<ProxySession>>();

        private const int MAX_RECONNECT_ATTEMPTS = 100;

        /// <summary>
        /// How long a peer connection may stay unanswered before it is closed.
        /// </summary>
        private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// One RTSP client and the things the service has to be able to call off when the last
        /// viewer of that camera goes away.
        /// </summary>
        private sealed class ProxySession
        {
            /// <summary>
            /// Cancels a reconnect that is waiting out its backoff, so tearing the client down does
            /// not leave a delayed retry to bring it back up with nobody watching.
            /// </summary>
            public CancellationTokenSource Reconnects { get; } = new CancellationTokenSource();

            public Task<RTSPtoWebRTCProxy> Proxy { get; set; }
        }

        private static TimeSpan ReconnectDelay(int attempt)
        {
            // 1s, 2s, 4s ... capped at 30s
            double seconds = Math.Min(30d, Math.Pow(2, Math.Min(attempt, 5) - 1));
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>
        /// Whether reconnecting to a stream that stopped for this reason could ever work.
        /// </summary>
        /// <remarks>
        /// Retrying a rejected password or a URL the server does not have just repeats the same
        /// exchange, and against a camera that locks an account out after so many failures it does
        /// real harm. These used to be retried as hard as a dropped connection.
        /// </remarks>
        private static bool IsWorthRetrying(StoppedReason reason)
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
        /// A camera URL with any embedded credentials taken out, for logging.
        /// </summary>
        private static string Redact(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri) && !string.IsNullOrEmpty(uri.UserInfo))
            {
                return uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.UserInfo, UriFormat.UriEscaped);
            }

            return url;
        }

        public RTSPtoWebRTCProxyService(ILoggerFactory loggerFactory, IConfiguration config)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<RTSPtoWebRTCProxyService>();
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (IPAddress.TryParse(config[CONFIG_KEY_PUBLIC_IPV4], out _publicIPv4))
            {
                _logger.LogInformation($"Public IPv4 address set to {_publicIPv4}.");
            }

            if (IPAddress.TryParse(config[CONFIG_KEY_PUBLIC_IPV6], out _publicIPv6))
            {
                _logger.LogInformation($"Public IPv6 address set to {_publicIPv6}.");
            }

            SIPSorcery.LogFactory.Set(loggerFactory); // get the logs from the SIP Sorcery
        }

        public async Task<RTCSessionDescriptionInit> GetOfferAsync(
            string id,
            string url,
            string userName = null,
            string password = null,
            int startPort = 0,
            int endPort = 0,
            RTPTransport transport = RTPTransport.TCP,
            int rtspStartPort = 0,
            int rtspEndPort = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id), "A unique ID parameter must be supplied when creating a new peer connection.");
            }
            else if (_peerConnections.ContainsKey(id))
            {
                throw new DuplicateSessionException(id);
            }
            else if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                throw new ArgumentException(nameof(url), "Invalid camera URL.");
            }

            // session must be created in advance in order to know which codec to use
            RTSPtoWebRTCProxy proxy = await GetOrCreateClientAsync(_loggerFactory, url, userName, password, transport, rtspStartPort, rtspEndPort);

            PortRange portRange = null;
            if(startPort >= 0 && endPort > 0 && endPort > startPort && startPort <= IPEndPoint.MaxPort && endPort <= IPEndPoint.MaxPort)
            {
                if (startPort % 2 != 0 || endPort % 2 != 0)
                {
                    _logger.LogDebug($"Start and end port must be even numbers. StartPort: {startPort}, EndPort: {endPort}.");
                }
                else
                {
                    _logger.LogDebug($"RTCPeerConnection for {url} is set to use the port range from {startPort} to {endPort}.");
                    portRange = new PortRange(startPort, endPort, true);
                }
            }

            RTCPeerConnection peerConnection = new RTCPeerConnection(null, 0, portRange);

            if (_publicIPv4 != null)
            {
                var rtpPort = peerConnection.GetRtpChannel().RTPPort;
                var publicIPv4Candidate = new RTCIceCandidate(RTCIceProtocol.udp, _publicIPv4, (ushort)rtpPort, RTCIceCandidateType.host);
                peerConnection.addLocalIceCandidate(publicIPv4Candidate);
                _logger.LogDebug($"Added public IPv4 candidate: {_publicIPv4.ToString()}:{rtpPort}.");
            }

            if (_publicIPv6 != null)
            {
                var rtpPort = peerConnection.GetRtpChannel().RTPPort;
                var publicIPv6Candidate = new RTCIceCandidate(RTCIceProtocol.udp, _publicIPv6, (ushort)rtpPort, RTCIceCandidateType.host);
                peerConnection.addLocalIceCandidate(publicIPv6Candidate);
                _logger.LogDebug($"Added public IPv6 candidate: {_publicIPv6.ToString()}:{rtpPort}.");
            }

            if (proxy.VideoCodecEnum != ProxyVideoCodecs.Unknown)
            {
                SDPAudioVideoMediaFormat videoFormat = new SDPAudioVideoMediaFormat(proxy.VideoFormat);
                MediaStreamTrack videoTrack = new MediaStreamTrack(SDPMediaTypesEnum.video, false, new List<SDPAudioVideoMediaFormat> { videoFormat }, MediaStreamStatusEnum.SendOnly);
                peerConnection.addTrack(videoTrack);
            }

            if (proxy.AudioCodecEnum != ProxyAudioCodecs.Unknown)
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
                    ClosePeerConnection(id, url, peerConnection);
                }
                else if (state == RTCPeerConnectionState.connected)
                {
                    _logger.LogDebug("Peer connection connected.");
                }
            };

            // Claim the id before handing the connection anywhere. The check at the top of this
            //  method is only a fast path: two requests carrying the same id both got past it, and
            //  the loser left a peer connection that nothing tracked and nothing could ever close.
            if (!proxy.AddPeerConnection(id, peerConnection))
            {
                peerConnection.close();
                throw new DuplicateSessionException(id);
            }

            var offerInit = peerConnection.createOffer();
            offerInit.sdp = MungleSDP(offerInit.sdp, proxy);
            await peerConnection.setLocalDescription(offerInit);

            if (!_peerConnections.TryAdd(id, peerConnection))
            {
                proxy.RemovePeerConnection(id);
                peerConnection.close();
                throw new DuplicateSessionException(id);
            }

            ScheduleHandshakeTimeout(id, url, peerConnection);

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

            return sdp;
        }

        /// <summary>
        /// Drops a peer connection, closes it, and stops the RTSP client once its last viewer is gone.
        /// </summary>
        /// <remarks>
        /// The removal from <see cref="_peerConnections"/> is the gate: whoever takes it does the
        /// closing, so the state change handler and the handshake timeout cannot both tear the same
        /// connection down, and close() raising another state change cannot recurse.
        /// </remarks>
        private void ClosePeerConnection(string id, string url, RTCPeerConnection peerConnection)
        {
            if (!_peerConnections.TryRemove(id, out _))
            {
                return;
            }

            // Removing it from the map used to be all that happened, which left the ports and the
            //  DTLS state to the garbage collector. close() is a no-op if it is closed already.
            peerConnection.close();

            if (!_rtspClients.TryGetValue(url, out Lazy<ProxySession> lazy) || !lazy.IsValueCreated)
            {
                return;
            }

            ProxySession session = lazy.Value;

            if (session.Proxy.Status != TaskStatus.RanToCompletion)
            {
                return;
            }

            if (session.Proxy.Result.RemovePeerConnection(id) == 0 && DropClient(url, lazy))
            {
                session.Reconnects.Cancel();
                session.Proxy.Result.Stop();
                _logger.LogDebug($"RTSPClient for {Redact(url)} stopped.");
            }
        }

        /// <summary>
        /// Closes a peer connection that never finished its handshake.
        /// </summary>
        /// <remarks>
        /// Without an answer there is no remote description, so ICE never starts its checks and
        /// sipsorcery's own failure timeout never runs - the connection sits in 'new' holding a port
        /// and keeping the camera streaming until the process ends. An offer nobody answers is the
        /// normal outcome of a page being closed on the way in.
        /// </remarks>
        private void ScheduleHandshakeTimeout(string id, string url, RTCPeerConnection peerConnection)
        {
            Task.Delay(HandshakeTimeout).ContinueWith(_ =>
            {
                if (peerConnection.connectionState == RTCPeerConnectionState.connected)
                {
                    return;
                }

                if (_peerConnections.ContainsKey(id))
                {
                    _logger.LogWarning($"Peer connection {id} did not complete its handshake within {HandshakeTimeout.TotalSeconds}s, closing it.");
                    ClosePeerConnection(id, url, peerConnection);
                }
            }, TaskScheduler.Default);
        }

        private Task<RTSPtoWebRTCProxy> GetOrCreateClientAsync(ILoggerFactory loggerFactory, string url, string userName, string password, RTPTransport transport, int rtspStartPort, int rtspEndPort)
        {
            // Lazy, because ConcurrentDictionary.GetOrAdd may run its factory on several threads at
            //  once and keep only one result. Every other run had already called Connect, so two
            //  viewers arriving together opened a second RTSP session that nothing would ever stop.
            Lazy<ProxySession> lazy = _rtspClients.GetOrAdd(url, _ => new Lazy<ProxySession>(
                () =>
                {
                    var created = new ProxySession();
                    created.Proxy = CreateClientAsync(loggerFactory, url, userName, password, created, transport, rtspStartPort, rtspEndPort);
                    return created;
                },
                LazyThreadSafetyMode.ExecutionAndPublication));

            return AwaitClientAsync(url, lazy);
        }

        private async Task<RTSPtoWebRTCProxy> AwaitClientAsync(string url, Lazy<ProxySession> lazy)
        {
            try
            {
                return await lazy.Value.Proxy;
            }
            catch
            {
                // A failed connection must not stay in the map: the faulted task was handed to every
                //  later viewer, so one refusal took the camera out until the process restarted.
                DropClient(url, lazy);
                throw;
            }
        }

        /// <summary>
        /// Removes a session, but only if it is still the one registered for that URL.
        /// </summary>
        private bool DropClient(string url, Lazy<ProxySession> lazy)
        {
            // The pair overload of TryRemove is not in netstandard2.0; this is the same compare and
            //  remove, and is atomic on ConcurrentDictionary.
            return ((ICollection<KeyValuePair<string, Lazy<ProxySession>>>)_rtspClients)
                .Remove(new KeyValuePair<string, Lazy<ProxySession>>(url, lazy));
        }

        private async Task<RTSPtoWebRTCProxy> CreateClientAsync(ILoggerFactory loggerFactory, string url, string userName, string password, ProxySession session, RTPTransport transport, int rtspStartPort, int rtspEndPort)
        {
            TaskCompletionSource<bool> result = new TaskCompletionSource<bool>();
            var client = new RTSPClient(loggerFactory);

            IStreamConfigurationData videoStream = null;
            int videoType = -1;
            string videoName = "";

            IStreamConfigurationData audioStream = null;
            int audioType = -1;
            string audioName = "";

            client.NewTrack += (o, e) =>
            {
                if (e.Kind == TrackKind.Video)
                {
                    videoStream = e.StreamConfigurationData;
                    videoType = e.PayloadType;
                    videoName = e.Codec;
                }
                else if (e.Kind == TrackKind.Audio)
                {
                    audioStream = e.StreamConfigurationData;
                    audioType = e.PayloadType;
                    audioName = e.Codec;
                }
            };

            int reconnectAttempts = 0;
            client.SetupMessageCompleted += (o, e) =>
            {
                reconnectAttempts = 0;

                // TrySetResult, because this is raised by every completed SETUP and not just the
                //  first: a reconnect raises it again. SetResult threw on the already completed
                //  source, and the client turns an exception out of a handler into a teardown plus
                //  another Stopped, which reconnected and threw again - so one dropped stream put
                //  the proxy into a reconnect loop it never came out of.
                result.TrySetResult(true);
            };

            client.Stopped += (o, e) =>
            {
                if (!IsWorthRetrying(e.Reason))
                {
                    _logger.LogError($"RTSP client for {Redact(url)} stopped: {e.Reason}. Not reconnecting.");
                    result.TrySetResult(false);
                    return;
                }

                if (++reconnectAttempts > MAX_RECONNECT_ATTEMPTS)
                {
                    _logger.LogError($"RTSP client for {Redact(url)} gave up after {MAX_RECONNECT_ATTEMPTS} reconnect attempts.");
                    result.TrySetResult(false);
                    return;
                }

                _logger.LogDebug($"RTSP client for {Redact(url)} stopped: {e.Reason}. Reconnect attempt {reconnectAttempts}.");

                // Backed off and off the receive thread. Reconnecting straight from the handler
                //  retried as fast as the connection could fail, which hammers the camera.
                Task.Delay(ReconnectDelay(reconnectAttempts), session.Reconnects.Token)
                    .ContinueWith(
                        _ =>
                        {
                            try
                            {
                                client.TryReconnect();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Reconnecting to {Redact(url)} failed.");
                            }
                        },
                        session.Reconnects.Token,
                        TaskContinuationOptions.OnlyOnRanToCompletion,
                        TaskScheduler.Default);
            };

            // The proxy carries a single video track and a single audio track, so take the first of
            //  each kind and nothing else. Without this the client would also set up the first
            //  metadata track, which this proxy has nothing to do with.
            client.AcceptTrack = t => RTSPClient.FirstOfEachKind(t) && t.Kind != TrackKind.Application;

            if (transport != RTPTransport.TCP && rtspStartPort > 0 && rtspEndPort > 0)
            {
                // Only meaningful for the UDP transports; TCP interleaves the RTP in the RTSP
                //  connection and binds nothing.
                client.SetRtpPortRange(rtspStartPort, rtspEndPort);
                _logger.LogDebug($"RTSP client for {Redact(url)} takes its UDP ports from {rtspStartPort}-{rtspEndPort}.");
            }

            _logger.LogDebug($"Connecting to {Redact(url)} over {transport}.");
            client.Connect(url, transport, userName, password, false, null, true);

            bool isConnected = await result.Task;
            if(!isConnected)
            {
                throw new Exception($"Failed to connect to RTSP server {url}.");
            }
            return new RTSPtoWebRTCProxy(_logger, client, videoType, videoName, videoStream, audioType, audioName, audioStream);
        }

        /// <summary>
        /// Applies the answer to a pending offer. False where no session has that id - a stale page
        /// answering after its connection was cleaned up, which is a 404 and not a server fault.
        /// </summary>
        public bool SetAnswer(string id, RTCSessionDescriptionInit description)
        {
            if (!_peerConnections.TryGetValue(id, out var peerConnection))
            {
                return false;
            }

            _logger.LogDebug($"Answer SDP: {description.sdp}");
            peerConnection.setRemoteDescription(description);
            return true;
        }

        /// <summary>
        /// Adds a remote ICE candidate. False where no session has that id.
        /// </summary>
        public bool AddIceCandidate(string id, RTCIceCandidateInit iceCandidate)
        {
            if (!_peerConnections.TryGetValue(id, out var peerConnection))
            {
                return false;
            }

            _logger.LogDebug($"ICE Candidate: {iceCandidate.candidate}");
            peerConnection.addIceCandidate(iceCandidate);
            return true;
        }
    }
}
