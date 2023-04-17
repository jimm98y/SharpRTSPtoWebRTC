using CameraAPI.AAC;
using CameraAPI.Opus;
using Concentus.Common;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CameraAPI
{
    public class RTSPClientWrapper
    {
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

        public int AudioType { get; private set; } = -1;
        public int VideoType { get; private set; } = -1;
        public string AudioCodec { get; private set; }
        public string VideoCodec { get; private set; }
        public VideoCodecsEnum VideoCodecEnum { get; private set; } = VideoCodecsEnum.Unknown;
        public AudioCodecsEnum AudioCodecEnum { get; private set; } = AudioCodecsEnum.Unknown;

        public RTSPClientWrapper(ILogger logger, RTSPClient client, int audioType, string audioCodec, int videoType, string videoCodec, byte[] sps, byte[] pps)
        {
            this._logger = logger;
            this._client = client;
            this.AudioType = audioType;
            this.VideoType = videoType;
            this.AudioCodec = audioCodec;
            this.VideoCodec = videoCodec;
            this._sps = sps;
            this._pps = pps;

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
        }

        private void Client_Received_AAC(string format, List<byte[]> aac, uint objectType, uint frequencyIndex, uint channelConfiguration, uint timestamp, int payloadType)
        {
            // here we know we have AAC
            int channels = (int)channelConfiguration;

            // setup
            if (_aacDecoder == null)
            {
                var decoderConfig = new DecoderConfig();
                decoderConfig.setProfile(Profile.AAC_LC); // AAC Low Complexity is most likely used, set it as default
                decoderConfig.setSampleFrequency((SampleFrequency)frequencyIndex);
                decoderConfig.setChannelConfiguration((ChannelConfiguration)channels);
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
                _aacDecoder.decodeFrame(aacFrame, buffer);

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

        private void Client_RtpMessageReceived(byte[] rtp, uint timestamp, int markerBit, int payloadType, int skip)
        {
            // forward RTP to WebRTC "as is", just without the RTP header
            byte[] msg = rtp.Skip(skip).ToArray();

            if (payloadType == VideoType && VideoCodecEnum != VideoCodecsEnum.Unknown)
            {
                foreach (KeyValuePair<string, RTCPeerConnection> peerConnection in _peerConnections)
                {
                    if (_sps != null)
                    {
                        // WebRTC does not support sprop-parameter-sets in the SDP, so if SPS/PPS was delivered this way, 
                        //  we have to keep sending it in between the NALs
                        if (_lastVideoMarkerBit == 1 && markerBit == 0)
                        {
                            peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _sps, timestamp, 0, payloadType);
                            peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, _pps, timestamp, 0, payloadType);
                        }
                    }

                    peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, msg, timestamp, markerBit, payloadType);
                }

                _lastVideoMarkerBit = markerBit;
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
                else if(AudioCodecEnum != AudioCodecsEnum.OPUS)
                {
                    Debug.WriteLine($"Unsupported audio codec {AudioCodecEnum}");
                }
            }
        }

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
