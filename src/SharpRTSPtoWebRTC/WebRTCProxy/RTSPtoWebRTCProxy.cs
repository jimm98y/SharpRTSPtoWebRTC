using SharpRTSPtoWebRTC.Codecs;
using SharpJaad.AAC;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SharpRTSPClient;
using Concentus;

namespace SharpRTSPtoWebRTC.WebRTCProxy
{
    /// <summary>
    /// Proxy that takes RTP from RTSP and passes it to WebRTC PeerConnection. If necessary,
    ///  it performs transcoding (AAC -> OPUS) or re-packetization (H265 RTP -> Safari WebRTC).
    /// </summary>
    public class RTSPtoWebRTCProxy
    {
        private readonly ILogger _logger;

        private ConcurrentDictionary<string, RTCPeerConnection> _peerConnections = new ConcurrentDictionary<string, RTCPeerConnection>();
        private RTSPClient _client = null;
        private IStreamConfigurationData _videoStream = null;
        private IStreamConfigurationData _audioStream = null;

        private int _lastVideoMarkerBit = 1; // initial value 1 to make sure the first connection will send sps/pps
        private byte[] _sps = null;
        private byte[] _pps = null;
        private byte[] _vps = null;

        public int AudioType { get; private set; } = -1;
        public int VideoType { get; private set; } = -1;
        public string AudioCodec { get; private set; }
        public string VideoCodec { get; private set; }
        public VideoCodecsEnum VideoCodecEnum { get; private set; } = VideoCodecsEnum.Unknown;
        public AudioCodecsEnum AudioCodecEnum { get; private set; } = AudioCodecsEnum.Unknown;

        public AudioFormat AudioFormat
        {
            get
            {
                if (_opusEncoder != null)
                {
                    return _opusEncoder.OpusAudioFormat;
                }

                return new AudioFormat(AudioCodecEnum, AudioType);
            }
        }

        public VideoFormat VideoFormat
        {
            get
            {
                return new VideoFormat(VideoCodecEnum, VideoType);
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

            if(_videoStream is H264StreamConfigurationData h264)
            {
                _vps = null;
                _sps = h264.SPS;
                _pps = h264.PPS;
            }
            else if (_videoStream is H265StreamConfigurationData h265)
            {
                _vps = h265.VPS;
                _sps = h265.SPS;
                _pps = h265.PPS;
            }
            else
            {
                throw new NotSupportedException("Unsupported video stream.");
            }

            if (VideoType > 0)
            {
                VideoCodecEnum = GetVideoCodec(VideoCodec);
            }

            if (AudioType > 0)
            {
                AudioCodecEnum = GetAudioCodec(AudioCodec);
            }

            client.ReceivedRawVideoRTP += Client_ReceivedRawVideoRTP;
            client.ReceivedRawAudioRTP += Client_ReceivedRawAudioRTP;

            client.ReceivedRawVideoRTCP += Client_ReceivedRawVideoRTCP;
            client.ReceivedRawAudioRTCP += Client_ReceivedRawAudioRTCP;

            client.ReceivedAudioData += Client_ReceivedAudioData;
        }

        private void Client_ReceivedRawAudioRTCP(object sender, RawRtcpDataEventArgs e)
        {
            foreach (KeyValuePair<string, RTCPeerConnection> peerConnection in _peerConnections)
            {
                if (peerConnection.Value.AudioStream.IsSecurityContextReady())
                {
                    peerConnection.Value.SendRtcpRaw(SDPMediaTypesEnum.audio, e.Data.ToArray());
                }
            }
        }

        private void Client_ReceivedRawVideoRTCP(object sender, RawRtcpDataEventArgs e)
        {
            foreach (KeyValuePair<string, RTCPeerConnection> peerConnection in _peerConnections)
            {
                if (peerConnection.Value.VideoStream.IsSecurityContextReady())
                {
                    peerConnection.Value.SendRtcpRaw(SDPMediaTypesEnum.video, e.Data.ToArray());
                }
            }
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
            _client.Stop();
        }

        #endregion // WebRTC

        #region Codecs

        private AudioCodecsEnum GetAudioCodec(string codec)
        {
            AudioCodecsEnum ret;

            switch (codec)
            {
                case "PCMA":
                    ret = AudioCodecsEnum.PCMA;
                    break;

                case "PCMU":
                    ret = AudioCodecsEnum.PCMU;
                    break;

                case "AAC":
                case "MPEG4-GENERIC": // AAC is not supported by WebRTC, it requires transcoding to PCMA/PCMU or Opus
                    ret = AudioCodecsEnum.OPUS;
                    break;

                default:
                    ret = AudioCodecsEnum.Unknown;
                    break;
            }

            return ret;
        }

        private VideoCodecsEnum GetVideoCodec(string codec)
        {
            VideoCodecsEnum ret;

            switch (codec)
            {
                case "H264":
                    ret = VideoCodecsEnum.H264;
                    break;

                case "H265":
                    ret = VideoCodecsEnum.H265; // as of May 2023 this only work in Safari with Experimantal WebRTC H265 feature flag enabled
                    break;

                default:
                    ret = VideoCodecsEnum.Unknown;
                    break;
            }

            return ret;
        }

        #endregion // Codecs

        private void Client_ReceivedRawVideoRTP(object sender, RawRtpDataEventArgs e)
        {
            if (e.PayloadType == VideoType && VideoCodecEnum != VideoCodecsEnum.Unknown)
            {
                // forward RTP to WebRTC "as is", just without the RTP header 
                // Note: e.PayloadSize is incorrect in this case, we have to calculate the correct size using 12 + e.CsrcCount * 4
                byte[] msg = e.Data.Slice(12 + e.CsrcCount * 4).ToArray();

                if (VideoCodecEnum == VideoCodecsEnum.H264) // H264 only
                {
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
                else if (VideoCodecEnum == VideoCodecsEnum.H265)
                {
                    // after this change: https://github.com/WebKit/WebKit/pull/15494/commits/93eb48d39b70248c062e90fceb4630a312e46b0d H265 uses now standard packetization
                    foreach (KeyValuePair<string, RTCPeerConnection> peerConnection in _peerConnections)
                    {
                        if (peerConnection.Value.VideoStream.IsSecurityContextReady())
                        {
                            if (_lastVideoMarkerBit == 1 && !e.IsMarker)
                            {
                                if (_vps != null && _sps != null && _pps != null)
                                {
                                    peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _vps, e.Timestamp, 0, e.PayloadType);
                                    peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _sps, e.Timestamp, 0, e.PayloadType);
                                    peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _pps, e.Timestamp, 0, e.PayloadType);
                                }
                            }

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

        private void Client_ReceivedRawAudioRTP(object sender, RawRtpDataEventArgs e)
        {
            if (e.PayloadType == AudioType && AudioCodecEnum != AudioCodecsEnum.Unknown)
            {
                if (AudioCodecEnum == AudioCodecsEnum.PCMU || AudioCodecEnum == AudioCodecsEnum.PCMA)
                {
                    // forward RTP to WebRTC "as is", just without the RTP header
                    // Note: e.PayloadSize is incorrect in this case, we have to calculate the correct size using 12 + e.CsrcCount * 4
                    byte[] msg = e.Data.Slice(12 + e.CsrcCount * 4).ToArray();

                    // forward RTP "as is", the browser should be able to decode it because PCMA and PCMU are defined as mandatory in the WebRTC specification
                    foreach (var peerConnection in _peerConnections)
                    {
                        if (peerConnection.Value.AudioStream.IsSecurityContextReady())
                        {
                            peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.audio, msg, e.Timestamp, e.IsMarker ? 1 : 0, e.PayloadType);
                        }
                    }
                }
                else if (AudioCodecEnum != AudioCodecsEnum.OPUS)
                {
                    _logger.LogDebug($"Unsupported audio codec {AudioCodecEnum}");
                }
            }
        }

        #region AAC to Opus transcoding

        private Decoder _aacDecoder = null;
        private OpusAudioEncoder _opusEncoder = null;
        private IResampler _pcmResampler = null;
        private List<short> _samples = new List<short>();
        private short[] _resampledBuffer = null;

        private void Client_ReceivedAudioData(object sender, SimpleDataEventArgs e)
        {
            var aacConfiguration = _audioStream as AACStreamConfigurationData;

            if (aacConfiguration == null)
                return;

            // here we know we have AAC
            int channels = aacConfiguration.ChannelConfiguration;

            if (_aacDecoder == null)
            {
                var decoderConfig = new DecoderConfig();
                decoderConfig.SetProfile(Profile.AAC_LC); // AAC Low Complexity is most likely used, set it as default
                decoderConfig.SetSampleFrequency((SampleFrequency)aacConfiguration.FrequencyIndex);
                decoderConfig.SetChannelConfiguration((ChannelConfiguration)channels);
                _aacDecoder = new Decoder(decoderConfig);

                // we only need resampling if the AAC payload is not using the 48k sampling rate already
                if ((SampleFrequency)aacConfiguration.FrequencyIndex != SampleFrequency.SAMPLE_FREQUENCY_48000)
                {
                    const int MAX_AAC_FRAME_SIZE = 1024; // for AAC-LC only
                    _resampledBuffer = new short[(MAX_AAC_FRAME_SIZE * SampleFrequency.SAMPLE_FREQUENCY_48000.GetFrequency() * channels) / ((SampleFrequency)aacConfiguration.FrequencyIndex).GetFrequency()];

                    const int OPUS_QUALITY = 10; // 0-10, 10 is for maximum quality
                    _pcmResampler = ResamplerFactory.CreateResampler(channels, ((SampleFrequency)aacConfiguration.FrequencyIndex).GetFrequency(), SampleFrequency.SAMPLE_FREQUENCY_48000.GetFrequency(), OPUS_QUALITY);
                }

                _opusEncoder = new OpusAudioEncoder(channels);
            }

            // calculate the RTP timestamp based upon the current timestamp and the remainder from the last AAC payload 
            //  which did not fit into the frame size of the Opus encoded payload
            uint rtpTimestamp = (uint)(e.RtpTimestamp - (_samples.Count / channels));

            // single RTP can contain multiple AAC frames
            foreach (var aacFrame in e.Data)
            {
                SampleBuffer buffer = new SampleBuffer();

                // make sure the result is encoded as Little Endian
                buffer.SetBigEndian(false);

                // decode AAC to PCM using a port of the JAAD AAC Decoder
                _aacDecoder.DecodeFrame(aacFrame.ToArray(), buffer);

                // convert to signed short PCM
                short[] sdata = new short[buffer.Data.Length / sizeof(short)];
                Buffer.BlockCopy(buffer.Data, 0, sdata, 0, buffer.Data.Length);

                // if the AAC sample rate is not 48k, resample the PCM to 48k which is required by the OPUS codec
                if ((SampleFrequency)aacConfiguration.FrequencyIndex != SampleFrequency.SAMPLE_FREQUENCY_48000)
                {
                    int inLen = sdata.Length / channels;
                    int outLen = _resampledBuffer.Length / channels;
                    _pcmResampler.ProcessInterleaved(sdata, ref inLen, _resampledBuffer, ref outLen);
                    sdata = _resampledBuffer.Take(outLen * channels).ToArray();
                }

                // append the resampled audio to the remaining samples that did not fit into the last OPUS encoded payload
                _samples.AddRange(sdata);

                int opusFrameSize = _opusEncoder.GetFrameSize() * channels;

                while (_samples.Count >= opusFrameSize)
                {
                    // take a single frame from the send buffer
                    sdata = _samples.Take(opusFrameSize).ToArray();
                    _samples.RemoveRange(0, opusFrameSize);

                    // encode it using OPUS
                    byte[] encoded = _opusEncoder.EncodeAudio(sdata, _opusEncoder.OpusAudioFormat);

                    // send it to all peers
                    foreach (var peerConnection in _peerConnections)
                    {
                        if (peerConnection.Value.AudioStream.IsSecurityContextReady())
                        {
                            peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.audio, encoded, rtpTimestamp, 0, _opusEncoder.OpusAudioFormat.FormatID);
                        }
                    }

                    // increment the RTP timestamp by the frame size
                    rtpTimestamp += (uint)_opusEncoder.GetFrameSize();
                }
            }
        }

        #endregion //  AAC to Opus transcoding

        // only useful for debugging to dump the raw PCM into a file
#if DEBUG
        private static void AppendToFile(string fileToWrite, byte[] data)
        {
            using (System.IO.FileStream fileStream = new System.IO.FileStream(fileToWrite, System.IO.File.Exists(fileToWrite) ? System.IO.FileMode.Append : System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write))
            {
                fileStream.Write(data, 0, data.Length);
                fileStream.Close();
            }
        }
#endif
    }
}
