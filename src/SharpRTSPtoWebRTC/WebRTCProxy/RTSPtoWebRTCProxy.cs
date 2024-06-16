using SharpRTSPtoWebRTC.Codecs;
using SharpJaad.AAC;
using Concentus.Common;
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

            client.ReceivedVideoData += Client_ReceivedVideoData;
            client.ReceivedAudioData += Client_Received_AAC;
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
                byte[] msg = e.Data.Slice(e.Data.Length - e.PayloadSize).ToArray();

                if (VideoCodecEnum == VideoCodecsEnum.H264) // H264 only, H265 requires re-packetization of the NALs
                {
                    foreach (KeyValuePair<string, RTCPeerConnection> peerConnection in _peerConnections)
                    {
                        // WebRTC does not support sprop-parameter-sets in the SDP, so if SPS/PPS was delivered this way, 
                        //  we have to keep sending it in between the AUs
                        if (_lastVideoMarkerBit == 1 && !e.IsMarker)
                        {
                            if (_sps != null)
                            {
                                peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _sps, e.Timestamp, 0, e.PayloadType);
                                peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _pps, e.Timestamp, 0, e.PayloadType);
                            }
                        }

                        peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, msg, e.Timestamp, e.IsMarker ? 1 : 0, e.PayloadType);
                    }

                    _lastVideoMarkerBit = e.IsMarker ? 1 : 0;
                }
                else if (VideoCodecEnum != VideoCodecsEnum.H265)
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
                    byte[] msg = e.Data.Slice(e.Data.Length - e.PayloadSize).ToArray();

                    // forward RTP "as is", the browser should be able to decode it because PCMA and PCMU are defined as mandatory in the WebRTC specification
                    foreach (var peerConnection in _peerConnections)
                    {
                        peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.audio, msg, e.Timestamp, e.IsMarker ? 1 : 0, e.PayloadType);
                    }
                }
                else if (AudioCodecEnum != AudioCodecsEnum.OPUS)
                {
                    _logger.LogDebug($"Unsupported audio codec {AudioCodecEnum}");
                }
            }
        }

        #region H265 WebRTC for Safari

        /// <summary>
        /// Annex-B prefix to separate the NAL units.
        /// </summary>
        private readonly byte[] _annexB = new byte[] { 0, 0, 0, 1 };

        private const byte NALU_VPS = 32;
        private const byte NALU_SPS = 33;
        private const byte NALU_PPS = 34;
        private const byte NALU_IDR = 19;

        private struct SafariH265Header
        {
            /// <summary>
            /// Current RTP header.
            /// </summary>
            public byte Current;

            /// <summary>
            /// Next RTP header for this AU.
            /// </summary>
            public int Next;

            public SafariH265Header(byte current, int next)
            {
                this.Current = current;
                this.Next = next;
            }
        }

        private void Client_ReceivedVideoData(object sender, SimpleDataEventArgs e)
        {
            if (VideoCodecEnum == VideoCodecsEnum.H265)
            {
                if (e.Data == null || e.Data.Count() == 0)
                    return;

                var nalUnits = e.Data.ToArray();

                // TODO: use VPS/SPS/PPS from the SDP in case it's not among the NALs for the IDR frame
                IEnumerable<byte> msg = new byte[0];

                // join all NAL units in this AU into a single array and prefix each NAL with Annex B
                for (int i = 0; i < nalUnits.Length; i++)
                {
                    msg = msg.Concat(_annexB).Concat(nalUnits[i].ToArray());
                }

                foreach (var peerConnection in _peerConnections)
                {
                    SendSafariH265AU(peerConnection.Value, VideoType, msg.ToArray(), e.RtpTimestamp);
                }
            }
        }

        private static void SendSafariH265AU(RTCPeerConnection peerConnection, int payloadTypeID, byte[] au, uint timestamp)
        {
            /*
            You need a correct H265 stream: VPS, SPS, PPS, I-frame, P-frame(s).
            You need it with Annex-B headers 00 00 00 01 before each NAL unit.
            You need split your stream on RTP payloads with one byte header:
                03 for payload with VPS, SPS, PPS, I-frame start
                01 for all next packets from this I-frame
                02 for payload with P-frame
                00 for all next packets from this P-frame
            Don't forget set marker flag only for last packet of each Access Units
            */
            const int maxRtpPayloadLength = 1200 - 12 - 1; // 12 bytes RTP header, 1 byte is the type header that we have to add in GetSafariH265RtpHeader
            byte auType = au[4];
            int start = -1;

            if (au.Length <= maxRtpPayloadLength)
            {
                int markerBit = 1;
                SafariH265Header h265RtpHeader = GetSafariH265RtpHeader(auType);
                byte[] array = new byte[au.Length + 1];
                array[0] = h265RtpHeader.Current;
                Buffer.BlockCopy(au, 0, array, 1, au.Length);
                peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, array, timestamp, markerBit, payloadTypeID);
            }
            else
            {
                for (int i = 0; i * maxRtpPayloadLength < au.Length; i++)
                {
                    int srcOffset = i * maxRtpPayloadLength;
                    int num = (i + 1) * maxRtpPayloadLength < au.Length ? maxRtpPayloadLength : au.Length - i * maxRtpPayloadLength;
                    bool flag = (i + 1) * maxRtpPayloadLength >= au.Length;
                    int markerBit2 = flag ? 1 : 0;
                    SafariH265Header h265RtpHeader = GetSafariH265RtpHeader(auType, start);
                    start = h265RtpHeader.Next;
                    byte[] array2 = new byte[num + 1];
                    array2[0] = h265RtpHeader.Current;
                    Buffer.BlockCopy(au, srcOffset, array2, 1, num);
                    peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, array2, timestamp, markerBit2, payloadTypeID);
                }
            }
        }

        private static SafariH265Header GetSafariH265RtpHeader(byte au0, int start = -1)
        {
            // algorithm from here: https://github.com/AlexxIT/Blog/issues/5
            if (start == -1)
            {
                var nut = au0 >> 1 & 0b111111;

                switch (nut)
                {
                    case NALU_VPS:
                    case NALU_SPS:
                    case NALU_PPS:
                    case NALU_IDR:
                        return new SafariH265Header(3, 1);

                    default:
                        return new SafariH265Header(2, 0);
                }
            }
            else
            {
                return new SafariH265Header((byte)start, start);
            }
        }

        #endregion // H265 WebRTC for Safari

        #region AAC to Opus transcoding

        private Decoder _aacDecoder = null;
        private OpusAudioEncoder _opusEncoder = null;
        private IResampler _pcmResampler = null;
        private List<short> _samples = new List<short>();
        private short[] _resampledBuffer = null;

        private void Client_Received_AAC(object sender, SimpleDataEventArgs e)
        {
            var aacConfiguration = _audioStream as AACStreamConfigurationData;

            if (aacConfiguration == null)
                return;

            // here we know we have AAC
            int channels = aacConfiguration.ChannelConfiguration;

            // setup
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
                    const int MAX_DECODED_FRAME_SIZE_MULT = 6;
                    const int MAX_AAC_FRAME_SIZE = 1024; // for AAC-LC only
                    _resampledBuffer = new short[MAX_AAC_FRAME_SIZE * MAX_DECODED_FRAME_SIZE_MULT * channels];

                    const int OPUS_QUALITY = 10; // 0-10, 10 is for maximum quality
                    _pcmResampler = ResamplerFactory.CreateResampler(channels, ((SampleFrequency)aacConfiguration.FrequencyIndex).GetFrequency(), SampleFrequency.SAMPLE_FREQUENCY_48000.GetFrequency(), OPUS_QUALITY);
                }

                _opusEncoder = new OpusAudioEncoder(channels);
            }

            // calculate the RTP timestamp based upon the current timestamp and the remainder from the last AAC payload 
            //  which did not fit into the frame size of the Opus encoded payload
            uint rtpTimestamp = (uint)(e.RtpTimestamp - _samples.Count / channels);

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
                        peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.audio, encoded, rtpTimestamp, 0, _opusEncoder.OpusAudioFormat.FormatID);
                    }

                    // increment the RTP timestamp by the frame size
                    rtpTimestamp += (uint)_opusEncoder.GetFrameSize();
                }
            }
        }

        #endregion //  AAC to Opus transcoding

        /*
        // only useful for debugging to dump the raw PCM into a file
#if DEBUG
        private static void AppendToFile(string fileToWrite, byte[] data)
        {
            using (FileStream fileStream = new FileStream(fileToWrite, File.Exists(fileToWrite) ? FileMode.Append : FileMode.OpenOrCreate, FileAccess.Write))
            {
                fileStream.Write(data, 0, data.Length);
                fileStream.Close();
            }
        }
#endif
        */
    }
}
