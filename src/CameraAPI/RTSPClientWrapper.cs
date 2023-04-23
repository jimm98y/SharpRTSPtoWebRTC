using CameraAPI.AAC;
using CameraAPI.Opus;
using Concentus.Common;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CameraAPI
{
    public class RTSPClientWrapper
    {
        private readonly byte[] _annexB = new byte[] { 0, 0, 0, 1 };

        private readonly ILogger _logger;

        private ConcurrentDictionary<string, RTCPeerConnection> _peerConnections = new ConcurrentDictionary<string, RTCPeerConnection>();
        private RTSPClient _client = null;

        private Decoder _aacDecoder = null;
        private OpusAudioEncoder _opusEncoder = null;
        private SpeexResampler _pcmResampler = null;
        private List<short> _samples = new List<short>();
        private short[] _resampledBuffer = null;

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

        public RTSPClientWrapper(ILogger logger, RTSPClient client, int audioType, string audioCodec, int videoType, string videoCodec, byte[] sps, byte[] pps, byte[] vps = null)
        {
            this._logger = logger;
            this._client = client;
            this.AudioType = audioType;
            this.VideoType = videoType;
            this.AudioCodec = audioCodec;
            this.VideoCodec = videoCodec;
            this._sps = sps;
            this._pps = pps;
            this._vps = vps;

            if (VideoType > 0)
            {
                VideoCodecEnum = GetVideoCodec(videoCodec);
            }

            if (AudioType > 0)
            {
                AudioCodecEnum = GetAudioCodec(audioCodec);
            }

            client.RtpMessageReceived += Client_RtpMessageReceived;
            client.Received_AAC += Client_Received_AAC;
            client.Received_NALs += Client_Received_NALs;
        }

        private void Client_RtpMessageReceived(byte[] rtp, uint timestamp, int markerBit, int payloadType, int skip)
        {
            // forward RTP to WebRTC "as is", just without the RTP header
            byte[] msg = rtp.Skip(skip).ToArray();

            if (payloadType == VideoType && VideoCodecEnum != VideoCodecsEnum.Unknown)
            {
                if (VideoCodecEnum == VideoCodecsEnum.H264) // H264 only, H265 requires re-packetization
                {
                    foreach (KeyValuePair<string, RTCPeerConnection> peerConnection in _peerConnections)
                    {
                        // WebRTC does not support sprop-parameter-sets in the SDP, so if SPS/PPS was delivered this way, 
                        //  we have to keep sending it in between the NALs
                        if (_lastVideoMarkerBit == 1 && markerBit == 0)
                        {
                            if (_sps != null)
                            {
                                peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _sps, timestamp, 0, payloadType);
                                peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _pps, timestamp, 0, payloadType);
                            }
                        }

                        peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, msg, timestamp, markerBit, payloadType);
                    }

                    _lastVideoMarkerBit = markerBit;
                }
                else if(VideoCodecEnum != VideoCodecsEnum.H265)
                {
                    _logger.LogDebug($"Unsupported video codec {VideoCodecEnum}");
                }
            }
            else if (payloadType == AudioType && AudioCodecEnum != AudioCodecsEnum.Unknown) // avoid sending RTP for unsupported codecs (AAC)
            {
                if (AudioCodecEnum == AudioCodecsEnum.PCMU || AudioCodecEnum == AudioCodecsEnum.PCMA)
                {
                    // forward RTP "as is", the browser should be able to decode it because PCMA and PCMU are defined as mandatory in the WebRTC specification
                    foreach (var peerConnection in _peerConnections)
                    {
                        peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.audio, msg, timestamp, markerBit, payloadType);
                    }
                }
                else if (AudioCodecEnum != AudioCodecsEnum.OPUS)
                {
                    _logger.LogDebug($"Unsupported audio codec {AudioCodecEnum}");
                }
            }
        }

        #region H265 WebRTC for Safari

        private void Client_Received_NALs(List<byte[]> nalUnits, uint rtpTimestamp)
        {
            if (VideoCodecEnum == VideoCodecsEnum.H265)
            {
                if (nalUnits.Count == 0)
                    return;

                // TODO: use VPS/SPS/PPS from the SDP in case it's not among the NALs for the IDR frame
                IEnumerable<byte> msg = new byte[0];

                // join all NAL units in this AU into a single array and prefix each NAL with Annex B
                for (int i = 0; i < nalUnits.Count; i++)
                {
                    msg = msg.Concat(_annexB).Concat(nalUnits[i]);
                }

                foreach (var peerConnection in _peerConnections)
                {
                    SendSafariH265AU(peerConnection.Value, VideoType, msg.ToArray(), rtpTimestamp);
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
            const int maxRtpPayloadLength = 1200 - 12 - 1; // 12 bytes RTP header, 1 byte is the type header
            byte auType = au[4];
            int start = -1;

            if (au.Length <= maxRtpPayloadLength)
            {
                int markerBit = 1;
                Tuple<byte, int> h265RtpHeader = GetSafariH265RtpHeader(auType, start);
                byte[] array = new byte[au.Length + 1];
                array[0] = h265RtpHeader.Item1;
                Buffer.BlockCopy(au, 0, array, 1, au.Length);
                peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, array, timestamp, markerBit, payloadTypeID);
            }
            else
            {
                for (int i = 0; i * maxRtpPayloadLength < au.Length; i++)
                {
                    int srcOffset = i * maxRtpPayloadLength;
                    int num = ((i + 1) * maxRtpPayloadLength < au.Length) ? maxRtpPayloadLength : (au.Length - i * maxRtpPayloadLength);
                    bool flag = (i + 1) * maxRtpPayloadLength >= au.Length;
                    int markerBit2 = flag ? 1 : 0;
                    Tuple<byte, int> h265RtpHeader = GetSafariH265RtpHeader(auType, start);
                    start = h265RtpHeader.Item2;
                    byte[] array2 = new byte[num + 1];
                    array2[0] = h265RtpHeader.Item1;
                    Buffer.BlockCopy(au, srcOffset, array2, 1, num);
                    peerConnection.SendRtpRaw(SDPMediaTypesEnum.video, array2, timestamp, markerBit2, payloadTypeID);
                }
            }
        }

        private static Tuple<byte, int> GetSafariH265RtpHeader(byte au0, int start)
        {
            // algorithm from here: https://github.com/AlexxIT/Blog/issues/5
            if (start == -1)
            {
                var nut = (au0 >> 1) & 0b111111;

                switch (nut)
                {
                    case 32:    // VPS
                    case 33:    // SPS
                    case 34:    // PPS
                    case 19:    // NAL_UNIT_CODED_SLICE_IDR_W_RADL
                        return new Tuple<byte, int>(3, 1);

                    default:
                        return new Tuple<byte, int>(2, 0);
                }
            }
            else
            {
                return new Tuple<byte, int>((byte)start, start);
            }
        }

        #endregion // H265 WebRTC for Safari

        #region AAC to Opus transcoding

        private void Client_Received_AAC(string format, List<byte[]> aac, uint objectType, uint frequencyIndex, uint channelConfiguration, uint timestamp, int payloadType)
        {
            // here we know we have AAC
            int channels = (int)channelConfiguration;

            // setup
            if (_aacDecoder == null)
            {
                var decoderConfig = new DecoderConfig();
                decoderConfig.SetProfile(Profile.AAC_LC); // AAC Low Complexity is most likely used, set it as default
                decoderConfig.SetSampleFrequency((SampleFrequency)frequencyIndex);
                decoderConfig.SetChannelConfiguration((ChannelConfiguration)channels);
                _aacDecoder = new Decoder(decoderConfig);

                // we only need resampling if the AAC payload is not using the 48k sampling rate already
                if ((SampleFrequency)frequencyIndex != SampleFrequency.SAMPLE_FREQUENCY_48000)
                {
                    _resampledBuffer = new short[1024 * 6 * channels]; 
                    _pcmResampler = new SpeexResampler(channels, ((SampleFrequency)frequencyIndex).GetFrequency(), 48000, 10);
                }

                _opusEncoder = new OpusAudioEncoder(channels);
            }

            // calculate the RTP timestamp based upon the current timestamp and the remainder from the last AAC payload 
            //  which did not fit into the frame size of the Opus encoded payload
            uint rtpTimestamp = (uint)(timestamp - (_samples.Count / channels));

            // single RTP can contain multiple AAC frames
            foreach (var aacFrame in aac)
            {
                SampleBuffer buffer = new SampleBuffer();

                // make sure the result is encoded as Little Endian
                buffer.SetBigEndian(false);

                // decode AAC to PCM using a port of the JAAD AAC Decoder
                _aacDecoder.DecodeFrame(aacFrame, buffer);

                // convert to signed short PCM
                short[] sdata = new short[buffer.Data.Length / sizeof(short)];
                Buffer.BlockCopy(buffer.Data, 0, sdata, 0, buffer.Data.Length);

                // if the AAC sample rate is not 48k, resample the PCM to 48k which is required by the Opus codec
                if ((SampleFrequency)frequencyIndex != SampleFrequency.SAMPLE_FREQUENCY_48000)
                {
                    int inLen = sdata.Length / channels;
                    int outLen = _resampledBuffer.Length / channels;
                    _pcmResampler.ProcessInterleaved(sdata, 0, ref inLen, _resampledBuffer, 0, ref outLen);
                    sdata = _resampledBuffer.Take(outLen * channels).ToArray();
                }

                // append the resampled audio to the remaining samples that did not fit into the last Opus encoded payload
                _samples.AddRange(sdata);

                int opusFrameSize = _opusEncoder.GetFrameSize() * channels;

                while (_samples.Count >= opusFrameSize)
                {
                    // take a single frame from the send buffer
                    sdata = _samples.Take(opusFrameSize).ToArray();
                    _samples.RemoveRange(0, opusFrameSize);

                    // encode it using Opus
                    byte[] encoded = _opusEncoder.EncodeAudio(sdata, OpusAudioEncoder.MEDIA_FORMAT_OPUS);

                    // send it to all peers
                    foreach (var peerConnection in _peerConnections)
                    {
                        peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.audio, encoded, rtpTimestamp, 0, OpusAudioEncoder.MEDIA_FORMAT_OPUS.FormatID);
                    }

                    // increment the RTP timestamp by the frame size
                    rtpTimestamp += (uint)_opusEncoder.GetFrameSize();
                }
            }
        }

        #endregion //  AAC to Opus transcoding

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
                    ret = VideoCodecsEnum.H265;
                    break;

                default:
                    // VP9/VP8 are not supported
                    ret = VideoCodecsEnum.Unknown;
                    break;
            }

            return ret;
        }

        #endregion // Codecs

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
