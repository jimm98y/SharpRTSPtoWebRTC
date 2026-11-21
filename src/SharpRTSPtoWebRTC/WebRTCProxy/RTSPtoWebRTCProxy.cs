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

using SharpRTSPtoWebRTC.Codecs;
using SharpJaad.AAC;
using SharpWebRTC;
using SharpWebRTC.SDP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharpRTSPClient;
using Concentus;

namespace SharpRTSPtoWebRTC.WebRTCProxy
{
    public enum ProxyVideoCodecs
    {
        // Unknown of 0, the default of a codec not yet told; a codec added goes last
        Unknown = 0,
        H264 = 1,
        H265 = 2,
        H266 = 3,
        AV1 = 4,
        VP9 = 5
    }

    public enum ProxyAudioCodecs
    {
        // Unknown of 0, the default of a codec not yet told; a codec added goes last
        Unknown = 0,
        PCMU = 1,
        PCMA = 2,
        AAC = 3,
        OPUS = 4
    }

    /// <summary>
    /// A viewer of a camera: its peer connection, and the tracks it was offered - null of a kind the camera has none of.
    /// </summary>
    public sealed class WebRtcViewer
    {
        public WebRtcViewer(PeerConnection connection, MediaTrack video, MediaTrack audio)
        {
            Connection = connection;
            Video = video;
            Audio = audio;
        }

        public PeerConnection Connection { get; }
        public MediaTrack Video { get; }
        public MediaTrack Audio { get; }

        // its place of the viewers being set up, given back once it is connected, or gone
        internal RTSPtoWebRTCProxyService.PendingSlot Slot;

        /// <summary>The session's secret, of its offer: its answer and candidates are taken of it alone.</summary>
        public string Token { get; } = SessionToken.Create();

        /// <summary>The user who asked for the offer, of a server of authentication; its answer and candidates are of that user alone.</summary>
        public string User { get; internal set; }

        /// <summary>Whether a request of the token and the user given is the session's own.</summary>
        public bool IsOwnedBy(string token, string user) => SessionToken.Matches(Token, token) && string.Equals(User, user, StringComparison.Ordinal);

        /// <summary>Whether its DTLS-SRTP keys are there: what it is sent once it is.</summary>
        public bool IsConnected => Connection.State == PeerConnectionState.Connected;

        /// <summary>A payload of the video, of an RTP header of the viewer's own source and of a payload type its answer took.</summary>
        public void SendVideo(ReadOnlySpan<byte> payload, uint timestamp, bool marker)
        {
            // of the payload types offered, one the answer took: of H264, the camera's or the one labelled Constrained Baseline
            if (Video != null)
                ProxyMetrics.Sent(true, Connection.SendRtp(Video, payload, timestamp, marker, Video.Formats[0].PayloadType));
        }

        /// <summary>A payload of the sound, of an RTP header of the viewer's own source and of a payload type its answer took.</summary>
        public void SendAudio(ReadOnlySpan<byte> payload, uint timestamp, bool marker)
        {
            if (Audio != null)
                ProxyMetrics.Sent(false, Connection.SendRtp(Audio, payload, timestamp, marker, Audio.Formats[0].PayloadType));
        }
    }

    /// <summary>
    /// Proxy that takes RTP from RTSP and passes it to WebRTC PeerConnection. If necessary,
    ///  it performs transcoding (AAC -> OPUS).
    /// </summary>
    public class RTSPtoWebRTCProxy
    {
        private readonly ILogger _logger;

        private ConcurrentDictionary<string, WebRtcViewer> _peerConnections = new ConcurrentDictionary<string, WebRtcViewer>();

        // The peers as a plain array, rebuilt whenever one is added or removed. Every RTP packet is
        //  offered to every peer, and enumerating the dictionary allocated an enumerator each time -
        //  per packet, per stream, for the life of the process.
        private volatile WebRtcViewer[] _peers = new WebRtcViewer[0];

        // Serialises rebuilding the snapshot. The dictionary is concurrent, but taking a copy of it
        //  and publishing that copy is two steps: two viewers arriving at once could both read the
        //  dictionary and the one that wrote second could publish the older of the two reads,
        //  leaving a peer that receives nothing for as long as it is connected.
        private readonly object _peersLock = new object();
        private RTSPClient _client = null;
        private IStreamConfigurationData _videoStream = null;
        private IStreamConfigurationData _audioStream = null;

        private int _lastVideoMarkerBit = 1; // initial value 1 to make sure the first connection will send sps/pps

        // A new RTSP session - the client reconnected - starts the camera's timestamps at a new random value
        //  (RFC 3550). Passed on as they came, on the same SSRC, the browser took its frames for old ones and
        //  dropped them, the last picture staying. So after one, a track goes on from the last timestamp sent.
        private int _videoRestarted, _audioRestarted;
        private uint _videoOffset, _audioOffset, _lastVideoTimestamp, _lastAudioTimestamp;
        private long _lastVideoAt, _lastAudioAt;
        private byte[] _dci = null;
        private byte[] _sps = null;
        private byte[] _pps = null;
        private byte[] _vps = null;

        // The source puts a decoding order number in front of its NAL units (sprop-max-don-diff > 0),
        //  which no browser reads, so it is taken out before the payload is forwarded.
        private bool _stripDonl = false;

        public int AudioType { get; private set; } = -1;
        public int VideoType { get; private set; } = -1;
        public string AudioCodec { get; private set; }
        public string VideoCodec { get; private set; }
        public ProxyVideoCodecs VideoCodecEnum { get; private set; } = ProxyVideoCodecs.Unknown;
        public ProxyAudioCodecs AudioCodecEnum { get; private set; } = ProxyAudioCodecs.Unknown;

        /// <summary>The format a viewer is offered the sound of; null of a sound that cannot be sent.</summary>
        public RtpFormat AudioFormat
        {
            get
            {
                if(AudioCodecEnum == ProxyAudioCodecs.PCMU)
                    return new RtpFormat { PayloadType = AudioType, Name = "PCMU", ClockRate = 8000 };
                else if(AudioCodecEnum == ProxyAudioCodecs.PCMA)
                    return new RtpFormat { PayloadType = AudioType, Name = "PCMA", ClockRate = 8000 };
                else if(AudioCodecEnum == ProxyAudioCodecs.OPUS)
                    return new RtpFormat { PayloadType = AudioType, Name = "opus", ClockRate = 48000, Channels = 2, Parameters = OpusAudioEncoder.GetStereoFormatParameters((_audioStream as OpusStreamConfigurationData)?.SpropStereo == true) };
                else if(AudioCodecEnum == ProxyAudioCodecs.AAC)
                    return OpusAudioEncoder.GetOpusAudioFormat((_audioStream as AACStreamConfigurationData)?.ChannelConfiguration ?? 1, TranscodedAudioType);
                else
                    return null;
            }
        }

        /// <summary>
        /// The payload type the Opus transcoded from AAC goes out on. 111 is what the browsers use
        /// for Opus, but the video keeps the camera's payload type, so it must not be that one.
        /// </summary>
        private int TranscodedAudioType => VideoType == 111 ? 110 : 111;

        // the clock of every video payload format WebRTC has
        private const int VideoClockRate = 90000;

        /// <summary>The format a viewer is offered the video of; null of a video that cannot be sent.</summary>
        public RtpFormat VideoFormat
        {
            get
            {
                if (VideoCodecEnum == ProxyVideoCodecs.H264)
                    return new RtpFormat { PayloadType = VideoType, Name = "H264", ClockRate = VideoClockRate, Parameters = GetH264FormatParameters() };
                else if (VideoCodecEnum == ProxyVideoCodecs.H265)
                    return new RtpFormat { PayloadType = VideoType, Name = "H265", ClockRate = VideoClockRate, Parameters = GetVideoFormatParameters() };
                else if (VideoCodecEnum == ProxyVideoCodecs.H266)
                    return new RtpFormat { PayloadType = VideoType, Name = "H266", ClockRate = VideoClockRate, Parameters = GetVideoFormatParameters() };
                else if (VideoCodecEnum == ProxyVideoCodecs.AV1)
                    return new RtpFormat { PayloadType = VideoType, Name = "AV1", ClockRate = VideoClockRate, Parameters = GetVideoFormatParameters() };
                else if (VideoCodecEnum == ProxyVideoCodecs.VP9)
                    return new RtpFormat { PayloadType = VideoType, Name = "VP9", ClockRate = VideoClockRate, Parameters = GetVideoFormatParameters() };
                else
                    return null;
            }
        }

        /// <summary>
        /// The fmtp of the H264 offer.
        /// </summary>
        /// <remarks>
        /// profile-level-id - All WebRTC implementations are required to specify and interpret this parameter in their SDP,
        ///  and the browsers pick their decoder by it. The SPS is what the stream really is, so it goes before what the
        ///  camera's SDP claims; 42e01f (Constrained Baseline 3.1) only where neither says.
        /// packetization-mode - All endpoints are required to support mode 1 (non-interleaved mode), and it covers the
        ///  single NAL unit packets of mode 0 as well.
        /// sprop-parameter-sets - When AVC is used with WebRTC, this information must be signaled in-band, so it is
        ///  never offered; the parameter sets are resent in the stream instead.
        /// Without an fmtp, Firefox answers with VP8 and the connection fails: https://groups.google.com/g/discuss-webrtc/c/facYnHFiY-8?pli=1
        /// </remarks>
        private string GetH264FormatParameters()
        {
            string profileLevelId = GetProfileLevelId(_sps) ?? (_videoStream as H264StreamConfigurationData)?.ProfileLevelId ?? "42e01f";
            return $"profile-level-id={profileLevelId.ToLowerInvariant()};level-asymmetry-allowed=1;packetization-mode=1";
        }

        /// <summary>
        /// The profile-level-id an H264 SPS describes: its profile_idc, constraint flags and
        /// level_idc, or null where it is not an SPS.
        /// </summary>
        internal static string GetProfileLevelId(byte[] sps)
        {
            if (sps == null || sps.Length < 4 || (sps[0] & 0x1F) != 7)
                return null;

            return $"{sps[1]:x2}{sps[2]:x2}{sps[3]:x2}";
        }

        /// <summary>
        /// The fmtp of the offer for the codecs that only negotiate a profile and a level, taken
        /// from what the camera's SDP said; null where the client could not read it.
        /// </summary>
        private string GetVideoFormatParameters()
        {
            switch (_videoStream)
            {
                case H265StreamConfigurationData h265:
                    // tx-mode=SRST: one RTP stream, which is all this forwards
                    return (h265.ProfileSpace != 0 ? $"profile-space={h265.ProfileSpace};" : "") +
                        $"profile-id={h265.ProfileId};tier-flag={h265.TierFlag};level-id={h265.LevelId};tx-mode=SRST";

                case H266StreamConfigurationData h266:
                    return $"profile-id={h266.ProfileId};tier-flag={h266.TierFlag};level-id={h266.LevelId}";

                case AV1StreamConfigurationData av1:
                    return $"profile={av1.Profile};level-idx={av1.LevelIdx};tier={av1.Tier}";

                case VP9StreamConfigurationData vp9:
                    return $"profile-id={vp9.ProfileId}";

                default:
                    return null;
            }
        }

        public RTSPtoWebRTCProxy(
                ILogger<RTSPtoWebRTCProxyService> logger,
                RTSPClient client,
                int videoPayloadType,
                string videoPayloadName,
                IStreamConfigurationData videoStream,
                int audioPayloadType,
                string audioPayloadName,
                IStreamConfigurationData audioStream)
        {
            _logger = logger;
            _client = client;
            _videoStream = videoStream;
            _audioStream = audioStream;
            AudioType = audioPayloadType;
            VideoType = videoPayloadType;
            AudioCodec = audioPayloadName;
            VideoCodec = videoPayloadName;

            // The parameter sets are null where the SDP does not carry them; they are then picked
            //  up from the stream. AV1 and VP9 have none to resend, and a configuration the client
            //  could not read is null altogether.
            if (_videoStream is H264StreamConfigurationData h264)
            {
                _sps = h264.SPS;
                _pps = h264.PPS;

                if (h264.PacketizationMode == 2)
                {
                    _logger.LogError("The H264 stream uses interleaved packetization (packetization-mode=2), which no browser supports. It will not play.");
                }
            }
            else if (_videoStream is H265StreamConfigurationData h265)
            {
                _vps = h265.VPS;
                _sps = h265.SPS;
                _pps = h265.PPS;
                _stripDonl = h265.MaxDonDiff > 0;
            }
            else if (_videoStream is H266StreamConfigurationData h266)
            {
                _dci = h266.DCI;
                _vps = h266.VPS;
                _sps = h266.SPS;
                _pps = h266.PPS;
                _stripDonl = h266.MaxDonDiff > 0;
            }

            if (VideoType > 0)
            {
                VideoCodecEnum = GetVideoCodec(VideoCodec);
            }

            if (AudioType >= 0) // PCMU: AudioType = 0
            {
                AudioCodecEnum = GetAudioCodec(AudioCodec);
            }

            client.ReceivedRawRTP += Client_ReceivedRawRTP;
            client.ReceivedData += Client_ReceivedData;
            // raised by every completed SETUP; this proxy is made after the first, so it sees only the reconnects
            client.SetupMessageCompleted += Client_SetupMessageCompleted;
        }

        private void Client_SetupMessageCompleted(object sender, EventArgs e)
        {
            _logger.LogInformation("The RTSP session was set up again, the stream to the viewers goes on from where it was.");
            Interlocked.Exchange(ref _videoRestarted, 1);
            Interlocked.Exchange(ref _audioRestarted, 1);
        }

        /// <summary>
        /// The timestamp to send a packet of the camera's with: of its own, of an offset made after a new session, so that
        /// the track goes on from the last timestamp sent by the time that passed - at least 20 ms, so that the next frame
        /// is a new one, and at most 30 s, so that a machine that slept for hours does not move the stream on by hours.
        /// </summary>
        private static uint Continue(uint source, int clockRate, ref int restarted, ref uint offset, ref uint last, ref long lastAt)
        {
            long now = Stopwatch.GetTimestamp();
            if (Interlocked.Exchange(ref restarted, 0) == 1 && lastAt != 0)
            {
                double elapsed = Math.Max(0.02, Math.Min(30, (double)(now - lastAt) / Stopwatch.Frequency));
                offset = unchecked(last + (uint)Math.Round(elapsed * clockRate) - source);
            }

            last = unchecked(source + offset);
            lastAt = now;
            return last;
        }

        #region WebRTC

        /// <summary>
        /// Registers a peer to forward this stream to. False where the id is already taken, in which
        /// case the caller still owns the connection it passed in and has to close it.
        /// </summary>
        public bool AddPeerConnection(string id, WebRtcViewer peerConnection)
        {
            lock (_peersLock)
            {
                if (!_peerConnections.TryAdd(id, peerConnection))
                    return false;

                RebuildPeers();
                return true;
            }
        }

        /// <summary>
        /// Drops a peer and returns how many are left, so the caller can tear the RTSP client down
        /// once the last one has gone.
        /// </summary>
        public int RemovePeerConnection(string id, WebRtcViewer viewer)
        {
            lock (_peersLock)
            {
                // that viewer alone: a later one of the same id - of a page that asked again - is not the one leaving
                if (((ICollection<KeyValuePair<string, WebRtcViewer>>)_peerConnections).Remove(new KeyValuePair<string, WebRtcViewer>(id, viewer)))
                {
                    RebuildPeers();
                }

                return _peerConnections.Count;
            }
        }

        private void RebuildPeers()
        {
            var peers = new List<WebRtcViewer>(_peerConnections.Count);
            foreach (var peer in _peerConnections)
            {
                peers.Add(peer.Value);
            }

            _peers = peers.ToArray();
        }

        public void Stop()
        {
            _audioStopped = true; // what is still queued is of viewers going away: dropped, not transcoded
            _client.Stop();
        }

        #endregion // WebRTC

        #region Codecs

        private ProxyAudioCodecs GetAudioCodec(string codec)
        {
            ProxyAudioCodecs ret;

            switch (codec)
            {
                case "PCMA":
                    ret = ProxyAudioCodecs.PCMA;
                    break;

                case "PCMU":
                    ret = ProxyAudioCodecs.PCMU;
                    break;

                case "OPUS":
                    ret = ProxyAudioCodecs.OPUS;
                    break;

                case "AAC":
                case "MPEG4-GENERIC": // AAC is not supported by WebRTC, it requires transcoding to PCMA/PCMU or Opus
                    ret = ProxyAudioCodecs.AAC;
                    break;

                default:
                    ret = ProxyAudioCodecs.Unknown;
                    break;
            }

            return ret;
        }

        private ProxyVideoCodecs GetVideoCodec(string codec)
        {
            ProxyVideoCodecs ret;

            switch (codec)
            {
                case "H264":
                    ret = ProxyVideoCodecs.H264;
                    break;

                case "H265":
                    ret = ProxyVideoCodecs.H265; // as of April 2025 this works in Safari and Chrome Canary 136
                    break;

                case "H266":
                    ret = ProxyVideoCodecs.H266; // not supported by any browser yet
                    break;

                case "AV1":
                    ret = ProxyVideoCodecs.AV1;
                    break;

                case "VP9":
                    ret = ProxyVideoCodecs.VP9;
                    break;

                default:
                    ret = ProxyVideoCodecs.Unknown;
                    break;
            }

            return ret;
        }

        #endregion // Codecs

        // The client reports every track through a single event now, so the kind tells video from
        //  audio. Only the first track of each kind is set up (see AcceptTrack in the service), so
        //  the kind alone identifies the track.
        private void Client_ReceivedRawRTP(object sender, TrackRawRtpEventArgs e)
        {
            if (e.Kind == TrackKind.Video)
            {
                Client_ReceivedRawVideoRTP(e.Data);
            }
            else if (e.Kind == TrackKind.Audio)
            {
                Client_ReceivedRawAudioRTP(e.Data);
            }
        }

        private void Client_ReceivedData(object sender, TrackDataEventArgs e)
        {
            if (e.Kind == TrackKind.Audio)
            {
                Client_ReceivedAudioData(e.Data);
            }
        }

        private void Client_ReceivedRawVideoRTP(RawRtpDataEventArgs e)
        {
            if (VideoCodecEnum != ProxyVideoCodecs.Unknown)
            {
                // forward RTP to WebRTC "as is", just without the RTP header 
                ReadOnlySpan<byte> msg = e.Data.Span.Slice(e.PayloadStart); // forwarded as it is: only the parameter sets kept are copied
                if (msg.Length == 0)
                    return;
                ProxyMetrics.Received(true);

                uint timestamp = Continue(e.Timestamp, VideoClockRate, ref _videoRestarted, ref _videoOffset, ref _lastVideoTimestamp, ref _lastVideoAt);

                if (VideoCodecEnum == ProxyVideoCodecs.H264) // H264 only
                {
                    int naluType = msg[0] & 0x1F;
                    if (naluType == 7) // SPS
                    {
                        _sps = msg.ToArray();
                    }
                    else if (naluType == 8) // PPS
                    {
                        _pps = msg.ToArray();
                    }

                    SendToViewers(msg, timestamp, e.IsMarker, true, _lastVideoMarkerBit == 1 && !e.IsMarker);

                    _lastVideoMarkerBit = e.IsMarker ? 1 : 0;
                }
                else if (VideoCodecEnum == ProxyVideoCodecs.H265)
                {
                    if (_stripDonl)
                    {
                        byte[] stripped = DonlRemover.StripH265(msg.ToArray());
                        if (stripped == null)
                            return;
                        msg = stripped;
                    }

                    int naluType = (msg[0] >> 1) & 0x3F;
                    if (naluType == 32) // VPS
                    {
                        _vps = msg.ToArray();
                    }
                    else if (naluType == 33) // SPS
                    {
                        _sps = msg.ToArray();
                    }
                    else if (naluType == 34) // PPS
                    {
                        _pps = msg.ToArray();
                    }

                    // after this change: https://github.com/WebKit/WebKit/pull/15494/commits/93eb48d39b70248c062e90fceb4630a312e46b0d H265 uses now standard packetization
                    SendToViewers(msg, timestamp, e.IsMarker, true, _lastVideoMarkerBit == 1 && !e.IsMarker);

                    _lastVideoMarkerBit = e.IsMarker ? 1 : 0;
                }
                // as of 8/10/2025 H266 does not seem to be supported by any web browser
                else if(VideoCodecEnum == ProxyVideoCodecs.H266)
                {
                    if (_stripDonl)
                    {
                        byte[] stripped = DonlRemover.StripH266(msg.ToArray());
                        if (stripped == null)
                            return;
                        msg = stripped;
                    }

                    int naluType = (msg[1] & 0xF8) >> 3;
                    if (naluType == 13) // DCI
                    {
                        _dci = msg.ToArray();
                    }
                    else if (naluType == 14) // VPS
                    {
                        _vps = msg.ToArray();
                    }
                    else if (naluType == 15) // SPS
                    {
                        _sps = msg.ToArray();
                    }
                    else if (naluType == 16) // PPS
                    {
                        _pps = msg.ToArray();
                    }

                    SendToViewers(msg, timestamp, e.IsMarker, true, _lastVideoMarkerBit == 1 && !e.IsMarker);

                    _lastVideoMarkerBit = e.IsMarker ? 1 : 0;
                }
                // what a decoder needs is in the key frames themselves, so the packets go through as they came
                else if (VideoCodecEnum == ProxyVideoCodecs.AV1 || VideoCodecEnum == ProxyVideoCodecs.VP9)
                {
                    SendToViewers(msg, timestamp, e.IsMarker, true, false);

                    _lastVideoMarkerBit = e.IsMarker ? 1 : 0;
                }
                else
                {
                    _logger.LogDebug($"Unsupported video codec {VideoCodecEnum}");
                }
            }
        }

        private void Client_ReceivedRawAudioRTP(RawRtpDataEventArgs e)
        {
            if (e.PayloadType == AudioType && AudioCodecEnum != ProxyAudioCodecs.Unknown)
            {
                ProxyMetrics.Received(false);
                if(AudioCodecEnum == ProxyAudioCodecs.AAC)
                { 
                    // transcoding AAC to Opus will happen in the Audio callback
                }
                else if(AudioCodecEnum == ProxyAudioCodecs.Unknown)
                {
                    _logger.LogDebug($"Unsupported audio codec {AudioCodecEnum}");
                }
                else
                {
                    // forward RTP to WebRTC "as is", just without the RTP header
                    ReadOnlySpan<byte> msg = e.Data.Span.Slice(e.PayloadStart); // forwarded as it is: only the parameter sets kept are copied
                    uint timestamp = Continue(e.Timestamp, AudioFormat?.ClockRate > 0 ? AudioFormat.ClockRate : 8000, ref _audioRestarted, ref _audioOffset, ref _lastAudioTimestamp, ref _lastAudioAt);

                    // forward RTP "as is", the browser should be able to decode it because PCMA, PCMU adn Opus are defined as mandatory in the WebRTC specification
                    SendToViewers(msg, timestamp, e.IsMarker, false, false);
                }
            }
        }

        // of a stream of this many viewers or more, its packets sent of several threads at once, each of a part of the viewers;
        //  the thread of the packet waits for all, so that each viewer has them in their order. Of one thread, of 1000 viewers a
        //  key frame's sends - 30 packets, 30,000 sends - held it so long that the camera's connection backed up and dropped
        //  frames, and the viewers' NACKs and key frame requests of the loss made it fall further behind; of 4K, 1,560 packets
        //  a second, one thread of 64 sends no more than 50 viewers' - a part of each 8 of them, 96 viewers had all of it
        private const int ParallelViewers = 8;

        private void SendToViewers(ReadOnlySpan<byte> payload, uint timestamp, bool marker, bool video, bool withParameterSets)
        {
            WebRtcViewer[] peers = _peers;
            if (peers.Length < ParallelViewers)
            {
                foreach (WebRtcViewer viewer in peers)
                    SendTo(viewer, payload, timestamp, marker, video, withParameterSets);
                return;
            }
            // of its own: the payload is of the receiver's buffer, of the receiving thread alone
            byte[] copy = payload.ToArray();
            int parts = Math.Min(Environment.ProcessorCount, (peers.Length + ParallelViewers - 1) / ParallelViewers);
            Parallel.For(0, parts, part =>
            {
                for (int i = part; i < peers.Length; i += parts)
                    SendTo(peers[i], copy, timestamp, marker, video, withParameterSets);
            });
        }

        private void SendTo(WebRtcViewer viewer, ReadOnlySpan<byte> payload, uint timestamp, bool marker, bool video, bool withParameterSets)
        {
            if (!viewer.IsConnected)
                return;
            if (!video)
            {
                viewer.SendAudio(payload, timestamp, marker);
                return;
            }
            // WebRTC does not support sprop-parameter-sets in the SDP, so if they were delivered this way, they are sent in between
            //  the AUs (VPS of H265 and H266, DCI of H266)
            if (withParameterSets && _sps != null && _pps != null)
            {
                if (_dci != null)
                    viewer.SendVideo(_dci, timestamp, false);
                if (_vps != null)
                    viewer.SendVideo(_vps, timestamp, false);
                viewer.SendVideo(_sps, timestamp, false);
                viewer.SendVideo(_pps, timestamp, false);
            }
            viewer.SendVideo(payload, timestamp, marker);
        }

        #region AAC to Opus transcoding

        private Decoder _aacDecoder = null;
        private OpusAudioEncoder _opusEncoder = null;
        private IResampler _pcmResampler = null;
        private readonly PcmSampleBuffer _samples = new PcmSampleBuffer();
        private OpusClockScaler _opusClock = null;

        // One 20ms frame: the rounding of the source clock is a sample either way, a real gap is more.
        private readonly OpusTimeline _opusTimeline = new OpusTimeline(960);

        private int BufferedSamples => _samples.Count;
        private short[] _resampledBuffer = null;
        private short[] _pcmBuffer = null;   // the decoded frame as 16-bit PCM, kept: a frame allocates nothing
        private readonly SampleBuffer _sampleBuffer = new SampleBuffer();
        private int _decodedChannels = 0;   // the channels resampled and encoded: the AAC decoder's, or 1 of a mono source (see TranscodeAndSend)
        private bool _monoSource = false;   // of a mono source, the decoder's two channels the same: one of them encoded
        private bool _stereoSeen = false;   // of a frame of two channels that differ: stereo from then on
        private int _decodedFrequency = 0;   // actual sample rate produced by the AAC decoder

        private volatile bool _audioStopped = false;

        private void Client_ReceivedAudioData(SimpleDataEventArgs e)
        {
            if (!(_audioStream is AACStreamConfigurationData))
                return;

            // Copy the AAC frames out of the payload processor's pooled buffers (they are returned to the
            //  pool as soon as this handler returns).
            byte[][] frames = e.Data.Select(f => f.ToArray()).ToArray();
            if (frames.Length == 0)
                return;

            if (_audioStopped)
                return;

            // Transcoded here, on the stream's receive loop, its frames in their order: of a work item of the thread pool of
            //  each frame, of a server of 500 streams the frames waited behind the pool's queue for seconds and most were
            //  dropped. Of a transcoding behind, the stream is read the slower and the camera's server says so.
            try
            {
                TranscodeAndSend(frames, e.RtpTimestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AAC to Opus transcoding stopped unexpectedly.");
                _audioStopped = true;
            }
        }

        private void TranscodeAndSend(byte[][] aacFrames, uint sourceRtpTimestamp)
        {
            var aacConfiguration = _audioStream as AACStreamConfigurationData;

            if (aacConfiguration == null)
                return;

            if (_aacDecoder == null)
            {
                var decoderConfig = new DecoderConfig();
                decoderConfig.SetProfile(Profile.AAC_LC); // AAC Low Complexity is most likely used, set it as default
                decoderConfig.SetSampleFrequency((SampleFrequency)aacConfiguration.FrequencyIndex);
                decoderConfig.SetChannelConfiguration((ChannelConfiguration)aacConfiguration.ChannelConfiguration);
                _aacDecoder = new Decoder(decoderConfig);
            }

            int aacFrequency = ((SampleFrequency)aacConfiguration.FrequencyIndex).GetFrequency();

            if (_opusClock == null)
            {
                _opusClock = new OpusClockScaler(aacFrequency, SampleFrequency.SAMPLE_FREQUENCY_48000.GetFrequency());
            }

            // Where the source says the next Opus frame starts: this AAC frame's time, less what is
            //  still buffered from the last. Only followed when it is out by more than a frame - see
            //  OpusTimeline for why counting on is what keeps the audio smooth.
            uint scaledTimestamp = _opusClock.Next(sourceRtpTimestamp);

            // Of no viewer connected - all of them still being set up, or gone - nothing is sent, so nothing is decoded
            //  nor encoded: of 200 streams set up at once, transcoding each for no one took the CPU their handshakes
            //  needed. What is buffered is dropped; the timeline follows the source again when a viewer connects.
            bool watched = false;
            foreach (WebRtcViewer viewer in _peers)
            {
                if (viewer.IsConnected)
                {
                    watched = true;
                    break;
                }
            }
            if (!watched)
            {
                _samples.Advance(BufferedSamples);
                return;
            }

            _opusTimeline.Align(scaledTimestamp - (uint)(BufferedSamples / Math.Max(1, _decodedChannels)));

            // single RTP can contain multiple AAC frames
            foreach (var aacFrame in aacFrames)
            {
                SampleBuffer buffer = _sampleBuffer;

                // make sure the result is encoded as Little Endian
                buffer.SetBigEndian(false);

                // decode AAC to PCM using a port of the JAAD AAC Decoder
                _aacDecoder.DecodeFrame(aacFrame, buffer);

                // Configure the resampler/encoder from the ACTUAL decoded format rather than from the SDP
                //  config. The JAAD decoder always emits interleaved stereo (it up-mixes mono to 2 channels),
                //  and the decoded sample rate is authoritative. Using aacConfiguration.ChannelConfiguration
                //  here (which reports mono for this stream) would make us treat interleaved stereo as mono:
                //  the resampler would consume only half of every frame and the rest would be dropped, badly
                //  distorting the audio.
                // Of a mono source the decoder still emits two channels, the same: one is resampled and encoded, of half the
                //  work. Parametric stereo can make a stream of a mono configuration stereo, so each frame is checked, and
                //  of one whose channels differ the stereo is encoded from then on.
                if (_monoSource && !SameChannels(buffer.Data))
                {
                    _logger.LogInformation("The audio of a mono configuration is stereo, encoding both channels from now on.");
                    _monoSource = false;
                    _stereoSeen = true;
                    _opusEncoder = null;
                    _samples.Advance(BufferedSamples);
                }

                if (_opusEncoder == null)
                {
                    _monoSource = !_stereoSeen && (buffer.Channels == 2 && aacConfiguration.ChannelConfiguration == 1 && SameChannels(buffer.Data));
                    _decodedChannels = _monoSource ? 1 : buffer.Channels;
                    _decodedFrequency = buffer.SampleRate;
                    _pcmResampler = null;

                    if (_decodedFrequency != SampleFrequency.SAMPLE_FREQUENCY_48000.GetFrequency())
                    {
                        const int RESAMPLER_QUALITY = 4; // 0-10; 4 is still transparent for 44.1k->48k, of a filter of 64 taps rather than the 80 of 5
                        _pcmResampler = ResamplerFactory.CreateResampler(_decodedChannels, _decodedFrequency, SampleFrequency.SAMPLE_FREQUENCY_48000.GetFrequency(), RESAMPLER_QUALITY);
                    }

                    _opusEncoder = new OpusAudioEncoder(_decodedChannels, _logger);
                }

                // convert to signed short PCM: of a mono source the left channel alone
                int pcmLength = buffer.Data.Length / sizeof(short) / (_monoSource ? 2 : 1);
                if (_pcmBuffer == null || _pcmBuffer.Length < pcmLength)
                    _pcmBuffer = new short[pcmLength];
                short[] sdata = _pcmBuffer;
                if (_monoSource)
                {
                    byte[] d = buffer.Data;
                    for (int j = 0; j < pcmLength; j++)
                        sdata[j] = (short)(d[4 * j] | (d[4 * j + 1] << 8));
                }
                else
                {
                    Buffer.BlockCopy(buffer.Data, 0, sdata, 0, pcmLength * sizeof(short));
                }

                // if the decoded sample rate is not 48k, resample the PCM to 48k which is required by the OPUS codec
                if (_decodedFrequency != SampleFrequency.SAMPLE_FREQUENCY_48000.GetFrequency())
                {
                    int inLen = pcmLength / _decodedChannels;
                    int neededPerChannel = (inLen * SampleFrequency.SAMPLE_FREQUENCY_48000.GetFrequency() / _decodedFrequency) + 1;
                    if (_resampledBuffer == null || _resampledBuffer.Length < neededPerChannel * _decodedChannels)
                        _resampledBuffer = new short[neededPerChannel * _decodedChannels];

                    int outLen = _resampledBuffer.Length / _decodedChannels;
                    _pcmResampler.ProcessInterleaved(sdata.AsSpan(0, pcmLength), ref inLen, _resampledBuffer, ref outLen);

                    // append straight out of the resampler's buffer; copying it to a right sized array
                    //  first only to append it was an allocation per frame
                    _samples.Append(_resampledBuffer, outLen * _decodedChannels);
                }
                else
                {
                    _samples.Append(sdata, pcmLength);
                }

                int opusFrameSize = _opusEncoder.GetFrameSize() * _decodedChannels;

                while (BufferedSamples >= opusFrameSize)
                {
                    // encode one frame straight out of the buffer, then step the read cursor past it
                    ReadOnlySpan<byte> encoded = _opusEncoder.EncodeOpus(_samples.Peek(opusFrameSize));
                    _samples.Advance(opusFrameSize);

                    uint rtpTimestamp = _opusTimeline.Take(_opusEncoder.GetFrameSize());

                    // send it to all peers
                    SendToViewers(encoded, rtpTimestamp, false, false, false);
                }
            }
        }

        // of little endian 16-bit stereo, whether the two channels are the same
        private static bool SameChannels(byte[] data)
        {
            for (int i = 0; i + 3 < data.Length; i += 4)
            {
                if (data[i] != data[i + 2] || data[i + 1] != data[i + 3])
                    return false;
            }
            return true;
        }

        #endregion //  AAC to Opus transcoding
    }
}
