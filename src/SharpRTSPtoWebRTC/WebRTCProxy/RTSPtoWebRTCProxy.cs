using SharpRTSPtoWebRTC.Codecs;
using SharpJaad.AAC;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using SharpRTSPClient;
using Concentus;

namespace SharpRTSPtoWebRTC.WebRTCProxy
{
    public enum ProxyVideoCodecs
    {
        H264,
        H265,
        H266,
        AV1,
        VP9,
        Unknown
    }

    public enum ProxyAudioCodecs
    {
        PCMU,
        PCMA,
        AAC,
        OPUS,
        Unknown
    }

    /// <summary>
    /// Proxy that takes RTP from RTSP and passes it to WebRTC PeerConnection. If necessary,
    ///  it performs transcoding (AAC -> OPUS).
    /// </summary>
    public class RTSPtoWebRTCProxy
    {
        private readonly ILogger _logger;

        private ConcurrentDictionary<string, RTCPeerConnection> _peerConnections = new ConcurrentDictionary<string, RTCPeerConnection>();

        // The peers as a plain array, rebuilt whenever one is added or removed. Every RTP packet is
        //  offered to every peer, and enumerating the dictionary allocated an enumerator each time -
        //  per packet, per stream, for the life of the process.
        private volatile RTCPeerConnection[] _peers = new RTCPeerConnection[0];

        // Serialises rebuilding the snapshot. The dictionary is concurrent, but taking a copy of it
        //  and publishing that copy is two steps: two viewers arriving at once could both read the
        //  dictionary and the one that wrote second could publish the older of the two reads,
        //  leaving a peer that receives nothing for as long as it is connected.
        private readonly object _peersLock = new object();
        private RTSPClient _client = null;
        private IStreamConfigurationData _videoStream = null;
        private IStreamConfigurationData _audioStream = null;

        private int _lastVideoMarkerBit = 1; // initial value 1 to make sure the first connection will send sps/pps
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

        public AudioFormat AudioFormat
        {
            get
            {
                if(AudioCodecEnum == ProxyAudioCodecs.PCMU)
                    return new AudioFormat(AudioCodecsEnum.PCMU, AudioType);
                else if(AudioCodecEnum == ProxyAudioCodecs.PCMA)
                    return new AudioFormat(AudioCodecsEnum.PCMA, AudioType);
                else if(AudioCodecEnum == ProxyAudioCodecs.OPUS)
                    return new AudioFormat(AudioType, "opus", 48000, 2, OpusAudioEncoder.GetStereoFormatParameters((_audioStream as OpusStreamConfigurationData)?.SpropStereo == true)); // passing just AudioCodecsEnumExp.OPUS results in incorrect SDP
                else if(AudioCodecEnum == ProxyAudioCodecs.AAC)
                    return OpusAudioEncoder.GetOpusAudioFormat((_audioStream as AACStreamConfigurationData)?.ChannelConfiguration ?? 1, TranscodedAudioType);
                else
                    return new AudioFormat(AudioCodecsEnum.Unknown, AudioType);
            }
        }

        /// <summary>
        /// The payload type the Opus transcoded from AAC goes out on. 111 is what the browsers use
        /// for Opus, but the video keeps the camera's payload type, so it must not be that one.
        /// </summary>
        private int TranscodedAudioType => VideoType == 111 ? 110 : 111;

        public VideoFormat VideoFormat
        {
            get
            {
                if (VideoCodecEnum == ProxyVideoCodecs.H264)
                    return new VideoFormat(VideoCodecsEnum.H264, VideoType, VideoFormat.DEFAULT_CLOCK_RATE, GetH264FormatParameters());
                else if (VideoCodecEnum == ProxyVideoCodecs.H265)
                    return new VideoFormat(VideoCodecsEnum.H265, VideoType, VideoFormat.DEFAULT_CLOCK_RATE, GetVideoFormatParameters());
                else if (VideoCodecEnum == ProxyVideoCodecs.H266)
                    return new VideoFormat(VideoType, "H266", VideoFormat.DEFAULT_CLOCK_RATE, GetVideoFormatParameters());
                else if (VideoCodecEnum == ProxyVideoCodecs.AV1)
                    return new VideoFormat(VideoCodecsEnum.AV1, VideoType, VideoFormat.DEFAULT_CLOCK_RATE, GetVideoFormatParameters());
                else if (VideoCodecEnum == ProxyVideoCodecs.VP9)
                    return new VideoFormat(VideoCodecsEnum.VP9, VideoType, VideoFormat.DEFAULT_CLOCK_RATE, GetVideoFormatParameters());
                else
                    return new VideoFormat(VideoCodecsEnum.Unknown, VideoType);
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
        }

        #region WebRTC

        /// <summary>
        /// Registers a peer to forward this stream to. False where the id is already taken, in which
        /// case the caller still owns the connection it passed in and has to close it.
        /// </summary>
        public bool AddPeerConnection(string id, RTCPeerConnection peerConnection)
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
        public int RemovePeerConnection(string id)
        {
            lock (_peersLock)
            {
                if (_peerConnections.TryRemove(id, out _))
                {
                    RebuildPeers();
                }

                return _peerConnections.Count;
            }
        }

        private void RebuildPeers()
        {
            var peers = new List<RTCPeerConnection>(_peerConnections.Count);
            foreach (var peer in _peerConnections)
            {
                peers.Add(peer.Value);
            }

            _peers = peers.ToArray();
        }

        public void Stop()
        {
            _audioQueue.CompleteAdding();
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
                byte[] msg = e.Data.Slice(e.PayloadStart).ToArray();
                if (msg.Length == 0)
                    return;

                if (VideoCodecEnum == ProxyVideoCodecs.H264) // H264 only
                {
                    int naluType = msg[0] & 0x1F;
                    if (naluType == 7) // SPS
                    {
                        _sps = msg;
                    }
                    else if (naluType == 8) // PPS
                    {
                        _pps = msg;
                    }

                    foreach (RTCPeerConnection peerConnection in _peers)
                    {
                        if (peerConnection.VideoStream.IsSecurityContextReady())
                        {                            
                            // WebRTC does not support sprop-parameter-sets in the SDP, so if SPS/PPS was delivered this way, 
                            //  we have to keep sending it in between the AUs
                            if (_lastVideoMarkerBit == 1 && !e.IsMarker)
                            {
                                if (_sps != null && _pps != null)
                                {
                                    peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, _sps, e.Timestamp, 0, e.PayloadType);
                                    peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, _pps, e.Timestamp, 0, e.PayloadType);
                                }
                            }

                            peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, msg, e.Timestamp, e.IsMarker ? 1 : 0, e.PayloadType);
                        }
                    }

                    _lastVideoMarkerBit = e.IsMarker ? 1 : 0;
                }
                else if (VideoCodecEnum == ProxyVideoCodecs.H265)
                {
                    if (_stripDonl)
                    {
                        msg = DonlRemover.StripH265(msg);
                        if (msg == null)
                            return;
                    }

                    int naluType = (msg[0] >> 1) & 0x3F;
                    if (naluType == 32) // VPS
                    {
                        _vps = msg;
                    }
                    else if (naluType == 33) // SPS
                    {
                        _sps = msg;
                    }
                    else if (naluType == 34) // PPS
                    {
                        _pps = msg;
                    }

                    // after this change: https://github.com/WebKit/WebKit/pull/15494/commits/93eb48d39b70248c062e90fceb4630a312e46b0d H265 uses now standard packetization
                    foreach (RTCPeerConnection peerConnection in _peers)
                    {
                        if (peerConnection.VideoStream.IsSecurityContextReady())
                        {
                            if (_lastVideoMarkerBit == 1 && !e.IsMarker)
                            {
                                if (_sps != null && _pps != null)
                                {
                                    if (_vps != null)
                                    {
                                        peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, _vps, e.Timestamp, 0, e.PayloadType);
                                    }
                                    peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, _sps, e.Timestamp, 0, e.PayloadType);
                                    peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, _pps, e.Timestamp, 0, e.PayloadType);
                                }
                            }

                            peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, msg, e.Timestamp, e.IsMarker ? 1 : 0, e.PayloadType);
                        }
                    }

                    _lastVideoMarkerBit = e.IsMarker ? 1 : 0;
                }
                // as of 8/10/2025 H266 does not seem to be supported by any web browser
                else if(VideoCodecEnum == ProxyVideoCodecs.H266)
                {
                    if (_stripDonl)
                    {
                        msg = DonlRemover.StripH266(msg);
                        if (msg == null)
                            return;
                    }

                    int naluType = (msg[1] & 0xF8) >> 3;
                    if (naluType == 13) // DCI
                    {
                        _dci = msg;
                    }
                    else if (naluType == 14) // VPS
                    {
                        _vps = msg;
                    }
                    else if (naluType == 15) // SPS
                    {
                        _sps = msg;
                    }
                    else if (naluType == 16) // PPS
                    {
                        _pps = msg;
                    }

                    foreach (RTCPeerConnection peerConnection in _peers)
                    {
                        if (peerConnection.VideoStream.IsSecurityContextReady())
                        {
                            if (_lastVideoMarkerBit == 1 && !e.IsMarker)
                            {
                                if (_sps != null && _pps != null)
                                {
                                    if (_dci != null)
                                    {
                                        peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, _dci, e.Timestamp, 0, e.PayloadType);
                                    }
                                    if (_vps != null)
                                    {
                                        peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, _vps, e.Timestamp, 0, e.PayloadType);
                                    }
                                    peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, _sps, e.Timestamp, 0, e.PayloadType);
                                    peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, _pps, e.Timestamp, 0, e.PayloadType);
                                }
                            }

                            peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, msg, e.Timestamp, e.IsMarker ? 1 : 0, e.PayloadType);
                        }
                    }

                    _lastVideoMarkerBit = e.IsMarker ? 1 : 0;
                }
                // what a decoder needs is in the key frames themselves, so the packets go through as they came
                else if (VideoCodecEnum == ProxyVideoCodecs.AV1 || VideoCodecEnum == ProxyVideoCodecs.VP9)
                {
                    foreach (RTCPeerConnection peerConnection in _peers)
                    {
                        if (peerConnection.VideoStream.IsSecurityContextReady())
                        {
                            peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, msg, e.Timestamp, e.IsMarker ? 1 : 0, e.PayloadType);
                        }
                    }

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
                    byte[] msg = e.Data.Slice(e.PayloadStart).ToArray();

                    // forward RTP "as is", the browser should be able to decode it because PCMA, PCMU adn Opus are defined as mandatory in the WebRTC specification
                    foreach (RTCPeerConnection peerConnection in _peers)
                    {
                        if (peerConnection.AudioStream.IsSecurityContextReady())
                        {
                            peerConnection.SendRtpRaw(SDPMediaTypesEnum.audio, msg, e.Timestamp, e.IsMarker ? 1 : 0, e.PayloadType);
                        }
                    }
                }
            }
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
        private int _decodedChannels = 0;   // actual channel count produced by the AAC decoder (see note in TranscodeAndSend)
        private int _decodedFrequency = 0;   // actual sample rate produced by the AAC decoder

        // Bounded: this is live audio, so if the transcoder cannot keep up the right answer is to drop
        //  frames rather than to queue them for ever. 100 AAC frames is roughly two seconds.
        private const int MAX_QUEUED_AUDIO_FRAMES = 100;

        private readonly BlockingCollection<(byte[][] Frames, uint RtpTimestamp)> _audioQueue =
            new BlockingCollection<(byte[][], uint)>(MAX_QUEUED_AUDIO_FRAMES);
        private Thread _audioWorker = null;

        private void Client_ReceivedAudioData(SimpleDataEventArgs e)
        {
            if (!(_audioStream is AACStreamConfigurationData))
                return;

            // Copy the AAC frames out of the payload processor's pooled buffers (they are returned to the
            //  pool as soon as this handler returns) and hand them to the worker thread for transcoding.
            byte[][] frames = e.Data.Select(f => f.ToArray()).ToArray();
            if (frames.Length == 0)
                return;

            if (_audioWorker == null)
            {
                // the receive callback is always raised from the same read-loop thread, so this is safe
                _audioWorker = new Thread(ProcessAudioQueue) { IsBackground = true, Name = "AAC-to-Opus transcoder" };
                _audioWorker.Start();
            }

            try
            {
                if (!_audioQueue.IsAddingCompleted && !_audioQueue.TryAdd((frames, e.RtpTimestamp)))
                {
                    _logger.LogWarning("AAC to Opus transcoding is behind, dropping an audio frame.");
                }
            }
            catch (InvalidOperationException)
            {
                // Stop() completed the queue between the check and the add; nothing left to do.
            }
        }

        private void ProcessAudioQueue()
        {
            try
            {
                foreach (var item in _audioQueue.GetConsumingEnumerable())
                {
                    TranscodeAndSend(item.Frames, item.RtpTimestamp);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AAC to Opus transcoding worker stopped unexpectedly.");
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
            _opusTimeline.Align(scaledTimestamp - (uint)(BufferedSamples / Math.Max(1, _decodedChannels)));

            // single RTP can contain multiple AAC frames
            foreach (var aacFrame in aacFrames)
            {
                SampleBuffer buffer = new SampleBuffer();

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
                if (_opusEncoder == null)
                {
                    _decodedChannels = buffer.Channels;
                    _decodedFrequency = buffer.SampleRate;

                    if (_decodedFrequency != SampleFrequency.SAMPLE_FREQUENCY_48000.GetFrequency())
                    {
                        const int RESAMPLER_QUALITY = 5; // 0-10; 5 is transparent for 44.1k->48k and far cheaper than 10, which keeps the real-time worker comfortably ahead
                        _pcmResampler = ResamplerFactory.CreateResampler(_decodedChannels, _decodedFrequency, SampleFrequency.SAMPLE_FREQUENCY_48000.GetFrequency(), RESAMPLER_QUALITY);
                    }

                    _opusEncoder = new OpusAudioEncoder(_decodedChannels);
                }

                // convert to signed short PCM
                short[] sdata = new short[buffer.Data.Length / sizeof(short)];
                Buffer.BlockCopy(buffer.Data, 0, sdata, 0, buffer.Data.Length);

                // if the decoded sample rate is not 48k, resample the PCM to 48k which is required by the OPUS codec
                if (_decodedFrequency != SampleFrequency.SAMPLE_FREQUENCY_48000.GetFrequency())
                {
                    int inLen = sdata.Length / _decodedChannels;
                    int neededPerChannel = (inLen * SampleFrequency.SAMPLE_FREQUENCY_48000.GetFrequency() / _decodedFrequency) + 1;
                    if (_resampledBuffer == null || _resampledBuffer.Length < neededPerChannel * _decodedChannels)
                        _resampledBuffer = new short[neededPerChannel * _decodedChannels];

                    int outLen = _resampledBuffer.Length / _decodedChannels;
                    _pcmResampler.ProcessInterleaved(sdata, ref inLen, _resampledBuffer, ref outLen);

                    // append straight out of the resampler's buffer; copying it to a right sized array
                    //  first only to append it was an allocation per frame
                    _samples.Append(_resampledBuffer, outLen * _decodedChannels);
                }
                else
                {
                    _samples.Append(sdata, sdata.Length);
                }

                int opusFrameSize = _opusEncoder.GetFrameSize() * _decodedChannels;

                while (BufferedSamples >= opusFrameSize)
                {
                    // encode one frame straight out of the buffer, then step the read cursor past it
                    byte[] encoded = _opusEncoder.EncodeOpus(_samples.Peek(opusFrameSize));
                    _samples.Advance(opusFrameSize);

                    uint rtpTimestamp = _opusTimeline.Take(_opusEncoder.GetFrameSize());

                    // send it to all peers
                    foreach (RTCPeerConnection peerConnection in _peers)
                    {
                        if (peerConnection.AudioStream.IsSecurityContextReady())
                        {
                            peerConnection.SendRtpRaw(SDPMediaTypesEnum.audio, encoded, rtpTimestamp, 0, AudioFormat.FormatID);
                        }
                    }
                }
            }
        }

        #endregion //  AAC to Opus transcoding
    }
}
