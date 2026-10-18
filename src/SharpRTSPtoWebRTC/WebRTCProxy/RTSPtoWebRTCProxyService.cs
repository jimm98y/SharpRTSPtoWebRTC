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

using SharpICE;
using SharpWebRTC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharpRTSPClient;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Threading;

namespace SharpRTSPtoWebRTC.WebRTCProxy
{
    public class RTSPtoWebRTCProxyService 
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<RTSPtoWebRTCProxyService> _logger;
        private readonly IConfiguration _config;

        private const string CONFIG_KEY_MAX_VIEWERS = "MaxViewers";
        private const int DEFAULT_MAX_VIEWERS = 100;
        private readonly int _maxViewers;

        private const string CONFIG_KEY_PUBLIC_IPV4 = "PublicIPv4";
        private const string CONFIG_KEY_PUBLIC_IPV6 = "PublicIPv6";
        private readonly IPAddress _publicIPv4;
        private readonly IPAddress _publicIPv6;

        private readonly ConcurrentDictionary<string, WebRtcViewer> _peerConnections = new ConcurrentDictionary<string, WebRtcViewer>();
        private readonly ConcurrentDictionary<string, Lazy<ProxySession>> _rtspClients = new ConcurrentDictionary<string, Lazy<ProxySession>>();

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

            // each viewer holds ports, a DTLS key and the camera's bitrate of upload: of anyone's asking, a number of them alone
            _maxViewers = int.TryParse(config[CONFIG_KEY_MAX_VIEWERS], out int maxViewers) && maxViewers > 0 ? maxViewers : DEFAULT_MAX_VIEWERS;

            if (IPAddress.TryParse(config[CONFIG_KEY_PUBLIC_IPV4], out _publicIPv4))
            {
                _logger.LogInformation($"Public IPv4 address set to {_publicIPv4}.");
            }

            if (IPAddress.TryParse(config[CONFIG_KEY_PUBLIC_IPV6], out _publicIPv6))
            {
                _logger.LogInformation($"Public IPv6 address set to {_publicIPv6}.");
            }
        }

        public async Task<SessionDescriptionInit> GetOfferAsync(
            string id,
            string url,
            string userName = null,
            string password = null,
            int startPort = 0,
            int endPort = 0,
            RtpTransport transport = RtpTransport.TCP,
            int rtspStartPort = 0,
            int rtspEndPort = 0,
            string user = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id), "A unique ID parameter must be supplied when creating a new peer connection.");
            }
            else if (_peerConnections.ContainsKey(id))
            {
                throw new DuplicateSessionException(id);
            }
            else if (_peerConnections.Count >= _maxViewers)
            {
                throw new TooManySessionsException(_maxViewers);
            }
            else if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                throw new ArgumentException(nameof(url), "Invalid camera URL.");
            }

            // session must be created in advance in order to know which codec to use
            RTSPtoWebRTCProxy proxy = await GetOrCreateClientAsync(_loggerFactory, url, userName, password, transport, rtspStartPort, rtspEndPort);

            // One socket of each local address, each of a port of the range: rtcp-mux leaves no RTCP port to keep even.
            var ice = new IceAgentOptions();
            if (startPort > 0 && endPort > startPort && endPort <= IPEndPoint.MaxPort)
            {
                _logger.LogDebug($"The peer connection for {Redact(url)} takes its ports from {startPort} to {endPort}.");
                ice.PortMin = startPort;
                ice.PortMax = endPort;
            }

            // A host behind a NAT of one to one - a cloud host - is reachable of its public addresses, on its sockets' ports.
            if (_publicIPv4 != null)
            {
                ice.PublicAddresses.Add(_publicIPv4);
            }

            if (_publicIPv6 != null)
            {
                ice.PublicAddresses.Add(_publicIPv6);
            }

            var peerConnection = new PeerConnection(new PeerConnectionOptions
            {
                Ice = ice,
                Trace = message => _logger.LogTrace($"WebRTC {SanitizeForLog(id)}: {message}"),
            });
            WebRtcViewer viewer = AddTracks(peerConnection, proxy);
            viewer.User = user;

            peerConnection.LocalCandidate += (s, candidate) => _logger.LogDebug($"Local ICE candidate {candidate}.");
            peerConnection.KeyFrameRequested += (s, track) =>
                _logger.LogDebug($"Peer connection {SanitizeForLog(id)} asks for a key frame of its {track.Kind}: the camera's next one is what it gets.");
            peerConnection.RtcpPacketReceived += (s, e) =>
            {
                foreach (var (reporter, block) in e.ReportBlocks)
                {
                    _logger.LogDebug($"RTCP receiver report of {reporter:X8} for {block.Ssrc:X8}: {block.FractionLost / 2.56:F1}% lost, {block.CumulativeLost} in all, jitter {block.Jitter}.");
                }
            };

            peerConnection.StateChanged += (s, state) =>
            {
                _logger.LogDebug($"Peer connection {SanitizeForLog(id)} state changed to {state}.");

                if (state == PeerConnectionState.Closed || state == PeerConnectionState.Disconnected || state == PeerConnectionState.Failed)
                {
                    ClosePeerConnection(id, url, viewer);
                }
                else if (state == PeerConnectionState.Connected)
                {
                    _logger.LogDebug("Peer connection connected.");
                }
            };

            // Claim the id before handing the connection anywhere. The check at the top of this
            //  method is only a fast path: two requests carrying the same id both got past it, and
            //  the loser left a peer connection that nothing tracked and nothing could ever close.
            if (!proxy.AddPeerConnection(id, viewer))
            {
                peerConnection.Close();
                throw new DuplicateSessionException(id);
            }

            string offer;
            try
            {
                offer = await peerConnection.CreateOfferAsync();
            }
            catch
            {
                proxy.RemovePeerConnection(id, viewer);
                peerConnection.Close();
                throw;
            }

            if (!_peerConnections.TryAdd(id, viewer))
            {
                proxy.RemovePeerConnection(id, viewer);
                peerConnection.Close();
                throw new DuplicateSessionException(id);
            }

            ScheduleHandshakeTimeout(id, url, viewer);

            return new SessionDescriptionInit { Type = "offer", Sdp = offer, Token = viewer.Token };
        }

        /// <summary>
        /// Drops a peer connection, closes it, and stops the RTSP client once its last viewer is gone.
        /// </summary>
        /// <remarks>
        /// The removal from <see cref="_peerConnections"/> is the gate: whoever takes it does the
        /// closing, so the state change handler and the handshake timeout cannot both tear the same
        /// connection down, and close() raising another state change cannot recurse.
        /// </remarks>
        private void ClosePeerConnection(string id, string url, WebRtcViewer viewer)
        {
            // that viewer alone, of its id: one that left and a later one of the same id are two, and the leaving one's timer or
            // state is not to close - or leak - the other
            if (!((ICollection<KeyValuePair<string, WebRtcViewer>>)_peerConnections).Remove(new KeyValuePair<string, WebRtcViewer>(id, viewer)))
            {
                viewer.Connection.Close();
                return;
            }

            // Removing it from the map used to be all that happened, which left the ports and the
            //  DTLS state to the garbage collector. Close() is a no-op if it is closed already.
            viewer.Connection.Close();

            if (!_rtspClients.TryGetValue(url, out Lazy<ProxySession> lazy) || !lazy.IsValueCreated)
            {
                return;
            }

            ProxySession session = lazy.Value;

            if (session.Proxy.Status != TaskStatus.RanToCompletion)
            {
                return;
            }

            if (session.Proxy.Result.RemovePeerConnection(id, viewer) == 0 && DropClient(url, lazy))
            {
                session.Reconnects.Cancel();
                session.Proxy.Result.Stop();
                _logger.LogDebug($"RtspClient for {Redact(url)} stopped.");
            }
        }

        /// <summary>
        /// Closes a peer connection that never finished its handshake.
        /// </summary>
        /// <remarks>
        /// Without an answer there is no remote description, so ICE never starts its checks and
        /// never fails - the connection sits in 'new' holding its ports and keeping the camera
        /// streaming until the process ends. An offer nobody answers is the
        /// normal outcome of a page being closed on the way in.
        /// </remarks>
        private static string SanitizeForLog(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Replace("\r", string.Empty).Replace("\n", string.Empty);
        }

        private void ScheduleHandshakeTimeout(string id, string url, WebRtcViewer viewer)
        {
            Task.Delay(HandshakeTimeout).ContinueWith(_ =>
            {
                if (viewer.Connection.State == PeerConnectionState.Connected)
                {
                    return;
                }

                if (_peerConnections.TryGetValue(id, out var current) && current == viewer)
                {
                    string safeId = SanitizeForLog(id);
                    _logger.LogWarning($"Peer connection {safeId} did not complete its handshake within {HandshakeTimeout.TotalSeconds}s, closing it.");
                    ClosePeerConnection(id, url, viewer);
                }
            }, TaskScheduler.Default);
        }

        /// <summary>
        /// Adds the send-only tracks a viewer of this proxy is offered: of the video and the sound the camera sends, those a
        /// browser can play. No telephone-event is offered: nothing here sends DTMF, and the payload type it would take may
        /// well be the one a camera gave its video.
        /// </summary>
        internal static WebRtcViewer AddTracks(PeerConnection peerConnection, RTSPtoWebRTCProxy proxy)
        {
            MediaTrack video = null;
            MediaTrack audio = null;

            var videoFormat = proxy.VideoCodecEnum != ProxyVideoCodecs.Unknown ? proxy.VideoFormat : null;
            if (videoFormat != null)
            {
                video = peerConnection.AddTrack("video", videoFormat);
            }

            var audioFormat = proxy.AudioCodecEnum != ProxyAudioCodecs.Unknown ? proxy.AudioFormat : null;
            if (audioFormat != null)
            {
                audio = peerConnection.AddTrack("audio", audioFormat);
            }

            return new WebRtcViewer(peerConnection, video, audio);
        }

        private Task<RTSPtoWebRTCProxy> GetOrCreateClientAsync(ILoggerFactory loggerFactory, string url, string userName, string password, RtpTransport transport, int rtspStartPort, int rtspEndPort)
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

        private async Task<RTSPtoWebRTCProxy> CreateClientAsync(ILoggerFactory loggerFactory, string url, string userName, string password, ProxySession session, RtpTransport transport, int rtspStartPort, int rtspEndPort)
        {
            TaskCompletionSource<bool> result = new TaskCompletionSource<bool>();
            var client = new RtspClient(loggerFactory);

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
                if (!ReconnectPolicy.IsWorthRetrying(e.Reason))
                {
                    _logger.LogError($"RTSP client for {Redact(url)} stopped: {e.Reason}. Not reconnecting.");
                    result.TrySetResult(false);
                    return;
                }

                if (++reconnectAttempts > ReconnectPolicy.MAX_ATTEMPTS)
                {
                    _logger.LogError($"RTSP client for {Redact(url)} gave up after {ReconnectPolicy.MAX_ATTEMPTS} reconnect attempts.");
                    result.TrySetResult(false);
                    return;
                }

                _logger.LogDebug($"RTSP client for {Redact(url)} stopped: {e.Reason}. Reconnect attempt {reconnectAttempts}.");

                // Backed off and off the receive thread. Reconnecting straight from the handler
                //  retried as fast as the connection could fail, which hammers the camera.
                Task.Delay(ReconnectPolicy.Delay(reconnectAttempts), session.Reconnects.Token)
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
            client.AcceptTrack = t => RtspClient.FirstOfEachKind(t) && t.Kind != TrackKind.Application;

            if (transport != RtpTransport.TCP && rtspStartPort > 0 && rtspEndPort > 0)
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
                // of the URL its credentials taken out: the message is logged, and of a server of developer pages, shown
                throw new Exception($"Failed to connect to RTSP server {Redact(url)}.");
            }
            return new RTSPtoWebRTCProxy(_logger, client, videoType, videoName, videoStream, audioType, audioName, audioStream);
        }

        /// <summary>
        /// Applies the answer to a pending offer. False where no session has that id - a stale page
        /// answering after its connection was cleaned up, which is a 404 and not a server fault.
        /// </summary>
        /// <exception cref="ArgumentException">Of an answer that is not one to this offer: it is closed.</exception>
        public bool SetAnswer(string id, string token, string user, SessionDescriptionInit description)
        {
            // of the session's token and user alone: of anyone else - who guessed its id - as of no session at all, so that
            //  the answer tells nothing of which ids are in use
            if (!_peerConnections.TryGetValue(id, out var viewer) || !viewer.IsOwnedBy(token, user))
            {
                return false;
            }

            _logger.LogDebug($"Answer SDP: {description.Sdp}");
            // an answer again - of another page that knows the id - is refused, the session left as it is
            if (viewer.Connection.RemoteDescription != null)
            {
                throw new ArgumentException("The session has its answer already.", nameof(description));
            }
            try
            {
                viewer.Connection.SetAnswer(description.Sdp);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is InvalidOperationException || ex is FormatException)
            {
                _logger.LogWarning($"The answer of {SanitizeForLog(id)} is refused: {ex.Message}");
                viewer.Connection.Close();
                throw new ArgumentException(ex.Message, nameof(description), ex);
            }

            foreach (var track in new[] { viewer.Video, viewer.Audio })
            {
                if (track != null && track.IsRejected)
                {
                    _logger.LogWarning($"The viewer {SanitizeForLog(id)} refuses the {track.Kind} of {track.Formats[0].Name}: it cannot play it.");
                }
            }
            return true;
        }

        /// <summary>
        /// Adds a remote ICE candidate. False where no session has that id.
        /// </summary>
        public bool AddIceCandidate(string id, string token, string user, IceCandidateInit iceCandidate)
        {
            if (!_peerConnections.TryGetValue(id, out var viewer) || !viewer.IsOwnedBy(token, user))
            {
                return false;
            }

            _logger.LogDebug($"ICE Candidate: {iceCandidate.Candidate}");
            if (!viewer.Connection.AddRemoteCandidate(iceCandidate.Candidate))
            {
                _logger.LogWarning($"Error adding remote ICE candidate {SanitizeForLog(iceCandidate.Candidate)}.");
            }
            return true;
        }
    }

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

    /// <summary>
    /// Thrown when an offer is asked for under a session id that is already in use.
    /// </summary>
    /// <remarks>
    /// Its own type so the caller can answer 409 rather than the 500 an ArgumentNullException used
    /// to produce - the request is refused, but nothing about it was null.
    /// </remarks>
    /// <summary>
    /// A viewer refused, of as many as the service takes already ("MaxViewers" of its configuration).
    /// </summary>
    public class TooManySessionsException : Exception
    {
        public TooManySessionsException(int max)
            : base($"There are {max} viewers already.")
        {
        }
    }

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
