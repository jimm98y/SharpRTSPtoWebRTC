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
        private RTSPClient _client = null;
        private IStreamConfigurationData _videoStream = null;
        private IStreamConfigurationData _audioStream = null;

        private int _lastVideoMarkerBit = 1; // initial value 1 to make sure the first connection will send sps/pps
        private byte[] _dci = null;
        private byte[] _sps = null;
        private byte[] _pps = null;
        private byte[] _vps = null;

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
                    return new AudioFormat(AudioType, "opus", 48000, 2, null); // passing just AudioCodecsEnumExp.OPUS results in incorrect SDP
                else if(AudioCodecEnum == ProxyAudioCodecs.AAC)
                    return OpusAudioEncoder.GetOpusAudioFormat(1);
                else
                    return new AudioFormat(AudioCodecsEnum.Unknown, AudioType);
            }
        }

        public VideoFormat VideoFormat
        {
            get
            {
                if (VideoCodecEnum == ProxyVideoCodecs.H264)
                    return new VideoFormat(VideoCodecsEnum.H264, VideoType);
                else if (VideoCodecEnum == ProxyVideoCodecs.H265)
                    return new VideoFormat(VideoCodecsEnum.H265, VideoType);
                else if (VideoCodecEnum == ProxyVideoCodecs.H266)
                    return new VideoFormat(VideoType, "H266", 90000, null);
                else if (VideoCodecEnum == ProxyVideoCodecs.AV1)
                    return new VideoFormat(VideoCodecsEnum.AV1, VideoType);
                else
                    return new VideoFormat(VideoCodecsEnum.Unknown, VideoType);
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

            if (VideoCodec != "AV1")
            {
                if (_videoStream != null) // this will be null in case sprop-parameter-sets are not singalled in the SDP
                {
                    if (_videoStream is H264StreamConfigurationData h264)
                    {
                        _dci = null;
                        _vps = null;
                        _sps = h264.SPS;
                        _pps = h264.PPS;
                    }
                    else if (_videoStream is H265StreamConfigurationData h265)
                    {
                        _dci = null;
                        _vps = h265.VPS;
                        _sps = h265.SPS;
                        _pps = h265.PPS;
                    }
                    else if (_videoStream is H266StreamConfigurationData h266)
                    {
                        _dci = h266.DCI;
                        _vps = h266.VPS;
                        _sps = h266.SPS;
                        _pps = h266.PPS;
                    }
                    else
                    {
                        _logger.LogError($"Unsupported video stream");
                    }
                }
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

        public void AddPeerConnection(string id, RTCPeerConnection peerConnection)
        {
            _peerConnections.TryAdd(id, peerConnection);
        }

        public int RemovePeerConnection(string id)
        {
            _peerConnections.TryRemove(id, out _);
            return _peerConnections.Count;
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

                    foreach (KeyValuePair<string, RTCPeerConnection> peerConnection in _peerConnections)
                    {
                        if (peerConnection.Value.VideoStream.IsSecurityContextReady())
                        {                            
                            // WebRTC does not support sprop-parameter-sets in the SDP, so if SPS/PPS was delivered this way, 
                            //  we have to keep sending it in between the AUs
                            if (_lastVideoMarkerBit == 1 && !e.IsMarker)
                            {
                                if (_sps != null && _pps != null)
                                {
                                    peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _sps, e.Timestamp, 0, e.PayloadType);
                                    peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _pps, e.Timestamp, 0, e.PayloadType);
                                }
                            }

                            peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, msg, e.Timestamp, e.IsMarker ? 1 : 0, e.PayloadType);
                        }
                    }

                    _lastVideoMarkerBit = e.IsMarker ? 1 : 0;
                }
                else if (VideoCodecEnum == ProxyVideoCodecs.H265)
                {
                    int naluType = msg[0] & 0x7E;
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
                    foreach (KeyValuePair<string, RTCPeerConnection> peerConnection in _peerConnections)
                    {
                        if (peerConnection.Value.VideoStream.IsSecurityContextReady())
                        {
                            if (_lastVideoMarkerBit == 1 && !e.IsMarker)
                            {
                                if (_sps != null && _pps != null)
                                {
                                    if (_vps != null)
                                    {
                                        peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _vps, e.Timestamp, 0, e.PayloadType);
                                    }
                                    peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _sps, e.Timestamp, 0, e.PayloadType);
                                    peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _pps, e.Timestamp, 0, e.PayloadType);
                                }
                            }

                            peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, msg, e.Timestamp, e.IsMarker ? 1 : 0, e.PayloadType);
                        }
                    }

                    _lastVideoMarkerBit = e.IsMarker ? 1 : 0;
                }
                // as of 8/10/2025 H266 does not seem to be supported by any web browser
                else if(VideoCodecEnum == ProxyVideoCodecs.H266)
                {
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

                    foreach (KeyValuePair<string, RTCPeerConnection> peerConnection in _peerConnections)
                    {
                        if (peerConnection.Value.VideoStream.IsSecurityContextReady())
                        {
                            if (_lastVideoMarkerBit == 1 && !e.IsMarker)
                            {
                                if (_sps != null && _pps != null)
                                {
                                    if (_dci != null)
                                    {
                                        peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _dci, e.Timestamp, 0, e.PayloadType);
                                    }
                                    if (_vps != null)
                                    {
                                        peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _vps, e.Timestamp, 0, e.PayloadType);
                                    }
                                    peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _sps, e.Timestamp, 0, e.PayloadType);
                                    peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _pps, e.Timestamp, 0, e.PayloadType);
                                }
                            }

                            peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, msg, e.Timestamp, e.IsMarker ? 1 : 0, e.PayloadType);
                        }
                    }

                    _lastVideoMarkerBit = e.IsMarker ? 1 : 0;
                }
                else if (VideoCodecEnum == ProxyVideoCodecs.AV1)
                {
                    foreach (KeyValuePair<string, RTCPeerConnection> peerConnection in _peerConnections)
                    {
                        if (peerConnection.Value.VideoStream.IsSecurityContextReady())
                        {
                            peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, msg, e.Timestamp, e.IsMarker ? 1 : 0, e.PayloadType);
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
                    foreach (var peerConnection in _peerConnections)
                    {
                        if (peerConnection.Value.AudioStream.IsSecurityContextReady())
                        {
                            peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.audio, msg, e.Timestamp, e.IsMarker ? 1 : 0, e.PayloadType);
                        }
                    }
                }
            }
        }

        #region AAC to Opus transcoding

        private Decoder _aacDecoder = null;
        private OpusAudioEncoder _opusEncoder = null;
        private IResampler _pcmResampler = null;
        private List<short> _samples = new List<short>();
        private short[] _resampledBuffer = null;
        private int _decodedChannels = 0;   // actual channel count produced by the AAC decoder (see note in TranscodeAndSend)
        private int _decodedFrequency = 0;   // actual sample rate produced by the AAC decoder

        private readonly BlockingCollection<(byte[][] Frames, uint RtpTimestamp)> _audioQueue = new BlockingCollection<(byte[][], uint)>();
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

            if (!_audioQueue.IsAddingCompleted)
                _audioQueue.Add((frames, e.RtpTimestamp));
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
            uint scaledTimestamp = (uint)((ulong)sourceRtpTimestamp * (ulong)SampleFrequency.SAMPLE_FREQUENCY_48000.GetFrequency() / (ulong)aacFrequency);
            uint rtpTimestamp = scaledTimestamp - (uint)(_samples.Count / Math.Max(1, _decodedChannels));

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

                    // _samples is empty before the first frame, so the timestamp offset is 0 either way; this
                    //  just re-evaluates it now that the real channel count is known.
                    rtpTimestamp = scaledTimestamp - (uint)(_samples.Count / _decodedChannels);
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
                    sdata = _resampledBuffer.Take(outLen * _decodedChannels).ToArray();
                }

                // append the resampled audio to the remaining samples that did not fit into the last OPUS encoded payload
                _samples.AddRange(sdata);

                int opusFrameSize = _opusEncoder.GetFrameSize() * _decodedChannels;

                while (_samples.Count >= opusFrameSize)
                {
                    // take a single frame from the send buffer
                    sdata = _samples.Take(opusFrameSize).ToArray();
                    _samples.RemoveRange(0, opusFrameSize);

                    // encode it using OPUS
                    byte[] encoded = _opusEncoder.EncodeAudio(sdata, AudioFormat);

                    // send it to all peers
                    foreach (var peerConnection in _peerConnections)
                    {
                        if (peerConnection.Value.AudioStream.IsSecurityContextReady())
                        {
                            peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.audio, encoded, rtpTimestamp, 0, AudioFormat.FormatID);
                        }
                    }

                    // increment the RTP timestamp by the frame size
                    rtpTimestamp += (uint)_opusEncoder.GetFrameSize();
                }
            }
        }

        #endregion //  AAC to Opus transcoding
    }
}
