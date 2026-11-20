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
using SharpWebRTC.SDP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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

        // Admission: at most so many viewers being set up at once - of an offer until connected, failed or timed out - and so
        //  many of one address. A burst of offers is set up a few at a time rather than all at once, which took the CPU the
        //  handshakes needed and none connected; and of one client, or of many, the setups cannot all be taken.
        private const string CONFIG_KEY_MAX_PENDING_VIEWERS = "MaxPendingViewers";
        private const int DEFAULT_MAX_PENDING_VIEWERS = 32;
        private const string CONFIG_KEY_MAX_PENDING_PER_ADDRESS = "MaxPendingViewersPerAddress";
        private const int DEFAULT_MAX_PENDING_PER_ADDRESS = 8;
        private const string CONFIG_KEY_PENDING_WAIT = "PendingWaitSeconds";
        private const double DEFAULT_PENDING_WAIT_SECONDS = 10;
        private readonly SemaphoreSlim _pending;
        private readonly int _maxPendingPerAddress;
        private readonly TimeSpan _pendingWait;
        private readonly Dictionary<IPAddress, int> _pendingOf = new Dictionary<IPAddress, int>();

        /// <summary>
        /// Of the viewers being set up, the one place of each, given back once - of connected, closed, or an offer that failed.
        /// </summary>
        internal sealed class PendingSlot
        {
            internal IPAddress Client;
            internal int Released;
        }

        private const string CONFIG_KEY_PUBLIC_IPV4 = "PublicIPv4";
        private const string CONFIG_KEY_PUBLIC_IPV6 = "PublicIPv6";
        private readonly IPAddress _publicIPv4;
        private readonly IPAddress _publicIPv6;

        // the certificate of DTLS of every viewer's connection, a new one a day before it expires: of one each, every viewer
        //  paid the generation of a key
        private PeerConnectionCertificate _certificate;
        private readonly object _certificateLock = new object();

        private PeerConnectionCertificate Certificate()
        {
            lock (_certificateLock)
            {
                if (_certificate == null || _certificate.Expires - DateTime.UtcNow < TimeSpan.FromDays(1))
                    _certificate = PeerConnectionCertificate.Generate();
                return _certificate;
            }
        }

        private readonly ConcurrentDictionary<string, WebRtcViewer> _peerConnections = new ConcurrentDictionary<string, WebRtcViewer>();
        private readonly ConcurrentDictionary<string, Lazy<ProxySession>> _rtspClients = new ConcurrentDictionary<string, Lazy<ProxySession>>();

        /// <summary>
        /// How long a peer connection may stay unanswered before it is closed.
        /// </summary>
        private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(30);
        // of no answer to its offer in this long, a viewer is closed - "AnswerTimeoutSeconds" - rather than of HandshakeTimeout
        private const string CONFIG_KEY_ANSWER_TIMEOUT = "AnswerTimeoutSeconds";
        // of the offer made, not received: of a host this busy that the offer reaches its client seconds later, the client
        //  still has time to answer - of 10 s, of 200 viewers at once, 23 were closed before they had their offers
        private const double DEFAULT_ANSWER_TIMEOUT_SECONDS = 20;
        private readonly TimeSpan _answerTimeout;

        /// <summary>
        /// How long a camera may take to set up its first session: of a camera that refuses, its reconnects of up to 30 s each
        /// held the viewer's request for the best part of an hour.
        /// </summary>
        internal static TimeSpan ConnectTimeout = TimeSpan.FromSeconds(15);

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
            ProxyMetrics.Observe(() => _peerConnections.Values);

            // each viewer holds ports, a DTLS key and the camera's bitrate of upload: of anyone's asking, a number of them alone
            _maxViewers = int.TryParse(config[CONFIG_KEY_MAX_VIEWERS], out int maxViewers) && maxViewers > 0 ? maxViewers : DEFAULT_MAX_VIEWERS;
            int maxPending = int.TryParse(config[CONFIG_KEY_MAX_PENDING_VIEWERS], out int mp) && mp > 0 ? mp : DEFAULT_MAX_PENDING_VIEWERS;
            _pending = new SemaphoreSlim(maxPending, maxPending);
            _maxPendingPerAddress = int.TryParse(config[CONFIG_KEY_MAX_PENDING_PER_ADDRESS], out int mpa) && mpa > 0 ? mpa : DEFAULT_MAX_PENDING_PER_ADDRESS;
            _pendingWait = TimeSpan.FromSeconds(double.TryParse(config[CONFIG_KEY_PENDING_WAIT], NumberStyles.Float, CultureInfo.InvariantCulture, out double pw) && pw >= 0 ? pw : DEFAULT_PENDING_WAIT_SECONDS);
            _answerTimeout = TimeSpan.FromSeconds(double.TryParse(config[CONFIG_KEY_ANSWER_TIMEOUT], NumberStyles.Float, CultureInfo.InvariantCulture, out double at) && at > 0 ? at : DEFAULT_ANSWER_TIMEOUT_SECONDS);

            if (IPAddress.TryParse(config[CONFIG_KEY_PUBLIC_IPV4], out _publicIPv4))
            {
                _logger.LogInformation($"Public IPv4 address set to {_publicIPv4}.");
            }

            if (IPAddress.TryParse(config[CONFIG_KEY_PUBLIC_IPV6], out _publicIPv6))
            {
                _logger.LogInformation($"Public IPv6 address set to {_publicIPv6}.");
            }
        }

        public Task<SessionDescriptionInit> GetOfferAsync(
            string id,
            string url,
            string userName = null,
            string password = null,
            int startPort = 0,
            int endPort = 0,
            RTPTransport transport = RTPTransport.TCP,
            int rtspStartPort = 0,
            int rtspEndPort = 0,
            string user = null)
        {
            return GetOfferAsync(id, url, userName, password, startPort, endPort, transport, rtspStartPort, rtspEndPort, user, null);
        }

        /// <summary>
        /// As the other, of the address of the client asking: of each, at most "MaxPendingViewersPerAddress" viewers set up
        /// at once (<see cref="TooManyPendingException"/>); of null, of no limit of its own. Of all, at most "MaxPendingViewers":
        /// one more waits "PendingWaitSeconds" for a place, else <see cref="ServerBusyException"/>.
        /// </summary>
        public async Task<SessionDescriptionInit> GetOfferAsync(
            string id,
            string url,
            string userName,
            string password,
            int startPort,
            int endPort,
            RTPTransport transport,
            int rtspStartPort,
            int rtspEndPort,
            string user,
            IPAddress client)
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

            if (client != null)
            {
                lock (_pendingOf)
                {
                    _pendingOf.TryGetValue(client, out int ofClient);
                    if (ofClient >= _maxPendingPerAddress)
                        throw new TooManyPendingException(_maxPendingPerAddress);
                    _pendingOf[client] = ofClient + 1;
                }
            }

            if (!await _pending.WaitAsync(_pendingWait))
            {
                ReleaseAddress(client);
                throw new ServerBusyException();
            }

            var slot = new PendingSlot { Client = client };
            try
            {
                return await GetAdmittedOfferAsync(id, url, userName, password, startPort, endPort, transport, rtspStartPort, rtspEndPort, user, slot);
            }
            catch
            {
                Release(slot);
                throw;
            }
        }

        private void ReleaseAddress(IPAddress client)
        {
            if (client == null)
                return;
            lock (_pendingOf)
            {
                if (_pendingOf.TryGetValue(client, out int ofClient) && ofClient > 1)
                    _pendingOf[client] = ofClient - 1;
                else
                    _pendingOf.Remove(client);
            }
        }

        // the place of a viewer set up - connected - or given up: once, whichever comes first
        private void Release(PendingSlot slot)
        {
            if (slot == null || Interlocked.Exchange(ref slot.Released, 1) != 0)
                return;
            _pending.Release();
            ReleaseAddress(slot.Client);
        }

        private async Task<SessionDescriptionInit> GetAdmittedOfferAsync(
            string id, string url, string userName, string password, int startPort, int endPort, RTPTransport transport,
            int rtspStartPort, int rtspEndPort, string user, PendingSlot slot)
        {
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
                Certificate = Certificate(),
                Ice = ice,
                // the camera's bitrate is its own: an estimate below it would only hold the video back, not make it less
                PaceAtSourceRate = true,
                // of trace logging alone: the connection and its ICE agent make no message of none
                Trace = _logger.IsEnabled(LogLevel.Trace) ? (Action<string>)(message => _logger.LogTrace($"WebRTC {SanitizeForLog(id)}: {message}")) : null,
            });
            WebRtcViewer viewer = AddTracks(peerConnection, proxy);
            viewer.User = user;
            viewer.Slot = slot;

            peerConnection.LocalCandidate += (s, candidate) => _logger.LogDebug($"Local ICE candidate {candidate}.");
            peerConnection.KeyFrameRequested += (s, track) =>
                _logger.LogDebug($"Peer connection {SanitizeForLog(id)} asks for a key frame of its {track.Kind}: the camera's next one is what it gets.");
            peerConnection.RtcpPacketReceived += (s, e) =>
            {
                // the message is made only when it is logged: this runs for every RTCP packet
                if (!_logger.IsEnabled(LogLevel.Debug))
                    return;
                foreach (var (reporter, block) in e.ReportBlocks)
                {
                    _logger.LogDebug($"RTCP receiver report of {reporter:X8} for {block.SSRC:X8}: {block.FractionLost / 2.56:F1}% lost, {block.CumulativeLost} in all, jitter {block.Jitter}.");
                }
            };

            peerConnection.StateChanged += (s, state) =>
            {
                _logger.LogDebug($"Peer connection {SanitizeForLog(id)} state changed to {state}.");

                // not of Disconnected, which ICE comes back from - of 10 s of no consent; of 30 s, Failed: closed of a blip, a
                //  viewer was lost for good, and of a stall of the host's, every viewer at once
                if (state == PeerConnectionState.Closed || state == PeerConnectionState.Failed)
                {
                    ClosePeerConnection(id, url, viewer);
                }
                else if (state == PeerConnectionState.Connected)
                {
                    _logger.LogDebug("Peer connection connected.");
                    Release(viewer.Slot);
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

            // of a range of ports all taken - the camera's StartPort to EndPort - no candidate: an offer of none never connects
            if (!offer.Contains("a=candidate:"))
            {
                proxy.RemovePeerConnection(id, viewer);
                peerConnection.Close();
                throw new TooManySessionsException($"No port from {startPort} to {endPort} is free for another viewer of {Redact(url)}.");
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
            Release(viewer.Slot);

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
                _logger.LogDebug($"RTSPClient for {Redact(url)} stopped.");
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
            // of no answer in "AnswerTimeoutSeconds", closed then: of a client that takes offers and answers none, its places
            //  were held the whole HandshakeTimeout
            Task.Delay(_answerTimeout).ContinueWith(_ =>
            {
                if (viewer.Connection.RemoteDescription == null && _peerConnections.TryGetValue(id, out var current) && current == viewer)
                {
                    _logger.LogWarning($"Peer connection {SanitizeForLog(id)} was not answered within {_answerTimeout.TotalSeconds}s, closing it.");
                    ClosePeerConnection(id, url, viewer);
                }
            }, TaskScheduler.Default);

            Task.Delay(HandshakeTimeout).ContinueWith(_ =>
            {
                if (viewer.Connection.State == PeerConnectionState.Connected || viewer.Connection.State == PeerConnectionState.Disconnected)
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
                // Firefox takes Constrained Baseline alone, and its OpenH264 decodes the other profiles too: the stream is
                //  offered of it as well, on a payload type of the top of the range, out of the way of RTX's from 96 up
                const string baseline = "profile-level-id=42e01f;level-asymmetry-allowed=1;packetization-mode=1";
                int baselineType = 127;
                while (baselineType == proxy.VideoType || baselineType == proxy.AudioType || baselineType == 110 || baselineType == 111)
                    baselineType--;
                video = videoFormat.Name == "H264" && videoFormat.Parameters != baseline
                    ? peerConnection.AddTrack("video", videoFormat, new RtpFormat { PayloadType = baselineType, Name = "H264", ClockRate = videoFormat.ClockRate, Parameters = baseline })
                    : peerConnection.AddTrack("video", videoFormat);
            }

            var audioFormat = proxy.AudioCodecEnum != ProxyAudioCodecs.Unknown ? proxy.AudioFormat : null;
            if (audioFormat != null)
            {
                audio = peerConnection.AddTrack("audio", audioFormat);
            }

            return new WebRtcViewer(peerConnection, video, audio);
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
                    // the frames of the payloads are of AAC alone, which is transcoded; the rest goes on as RTP -
                    //  the video's too, so its frames are not put together (ProcessRTP would, of every packet)
                    client.ProcessAudioRTP = audioName == "AAC" || audioName == "MPEG4-GENERIC";
                }
            };
            client.ProcessRTP = false;

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

            if (await Task.WhenAny(result.Task, Task.Delay(ConnectTimeout)) != result.Task)
            {
                session.Reconnects.Cancel();
                client.Stop();
                throw new TimeoutException($"The RTSP server {Redact(url)} did not set up a session in {ConnectTimeout.TotalSeconds} s.");
            }
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

        internal TooManySessionsException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// A viewer refused, of as many of its client's address being set up already ("MaxPendingViewersPerAddress").
    /// </summary>
    public class TooManyPendingException : Exception
    {
        public TooManyPendingException(int max)
            : base($"There are {max} viewers of this address being set up already.")
        {
        }
    }

    /// <summary>
    /// A viewer refused, of as many being set up already ("MaxPendingViewers") for as long as it waits ("PendingWaitSeconds").
    /// </summary>
    public class ServerBusyException : Exception
    {
        public ServerBusyException()
            : base("Too many viewers are being set up, try again shortly.")
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
