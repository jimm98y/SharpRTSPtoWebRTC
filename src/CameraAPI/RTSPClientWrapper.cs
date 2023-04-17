using CameraAPI.AAC;
using CameraAPI.Opus;
using Concentus.Common;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CameraAPI
{
    public class RTSPClientWrapper
    {
        private ConcurrentDictionary<string, RTCPeerConnection> _peerConnections = new ConcurrentDictionary<string, RTCPeerConnection>();
        private readonly ILogger _logger;
        private int _lastVideoMarkerBit = 1; // initial value 1 to make sure the first connection will send sps/pps

        public RTSPClient Client { get; private set; }
        public int AudioType { get; private set; } = -1;
        public int VideoType { get; private set; } = -1;
        public string AudioCodec { get; private set; }
        public string VideoCodec { get; private set; }
        public VideoCodecsEnum VideoCodecEnum { get; set; } = VideoCodecsEnum.Unknown;
        public AudioCodecsEnum AudioCodecEnum { get; set; } = AudioCodecsEnum.Unknown;
        public byte[] Sps { get; set; } = null;
        public byte[] Pps { get; set; } = null;

        Decoder AacDecoder = null;
        OpusAudioEncoder enc = new OpusAudioEncoder(2);
        SpeexResampler resampler = new SpeexResampler(2, 44100, 48000, 10);
        List<short> _samples = new List<short>();
        short[] resampled = new short[22050];

        public RTSPClientWrapper(ILogger logger, RTSPClient client, int audioType, string audioCodec, int videoType, string videoCodec, byte[] sps, byte[] pps)
        {
            this._logger = logger;
            this.Client = client;
            this.AudioType = audioType;
            this.VideoType = videoType;
            this.AudioCodec = audioCodec;
            this.VideoCodec = videoCodec;
            this.Sps = sps;
            this.Pps = pps;

            if (VideoType > 0)
            {
                switch (VideoCodec)
                {
                    case "H264":
                        VideoCodecEnum = VideoCodecsEnum.H264;
                        break;

                    case "H265":
                        VideoCodecEnum = VideoCodecsEnum.H265;
                        break;

                    default:
                        VideoCodecEnum = VideoCodecsEnum.Unknown;
                        break;
                }
            }

            if (AudioType > 0)
            {
                switch (AudioCodec)
                {
                    case "PCMA":
                        AudioCodecEnum = AudioCodecsEnum.PCMA;
                        break;

                    case "PCMU":
                        AudioCodecEnum = AudioCodecsEnum.PCMU;
                        break;

                    case "MPEG4-GENERIC": // AAC not supported currently by WebRTC, it requires transcoding to PCMA/PCMU
                                          //throw new NotSupportedException(codecs.audio);
                        AudioCodecEnum = AudioCodecsEnum.OPUS;
                        var decoderConfig = new DecoderConfig();
                        decoderConfig.setProfile(Profile.AAC_LC);
                        decoderConfig.setSampleFrequency(SampleFrequency.SAMPLE_FREQUENCY_44100);
                        decoderConfig.setChannelConfiguration(ChannelConfiguration.CHANNEL_CONFIG_STEREO);
                        AacDecoder = new Decoder(decoderConfig);
                        break;

                    default:
                        AudioCodecEnum = AudioCodecsEnum.Unknown;
                        break;
                }
            }

            client.RtpMessageReceived += (message, rtpTimestamp, markerBit, payloadType, skip) =>
            {
                // forward RTP to WebRTC "as is", just without the RTP header
                byte[] msg = message.Skip(skip).ToArray();

                if (payloadType == VideoType && VideoCodecEnum != VideoCodecsEnum.Unknown)
                {
                    foreach (KeyValuePair<string, RTCPeerConnection> peerConnection in _peerConnections)
                    {
                        if (Sps != null)
                        {
                            if (_lastVideoMarkerBit == 1 && markerBit == 0)
                            {
                                // send SPS/PPS in between NALs
                                peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, Sps, rtpTimestamp, 0, payloadType);
                                peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, Pps, rtpTimestamp, 0, payloadType);
                            }
                        }

                        peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.video, msg, rtpTimestamp, markerBit, payloadType);
                    }

                    _lastVideoMarkerBit = markerBit;
                }
                else if (payloadType == AudioType && AudioCodecEnum != AudioCodecsEnum.Unknown) // avoid sending RTP for unsupported codecs (AAC)
                {
                    if (AacDecoder == null)
                    {
                        // forward RTP
                        foreach (var peerConnection in _peerConnections)
                        {
                            peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.audio, msg, rtpTimestamp, markerBit, payloadType);
                        }
                    }
                }
            };


            client.Received_AAC += (format, aac, objectType, frequencyIndex, channelConfig, timestamp, payloadType) =>
            {
                try
                {
                    uint rtpBegin = (uint)(timestamp - (_samples.Count / 2 /* buffer.Channels */));

                    foreach (var aacFrame in aac)
                    {
                        //AppendToFile("C:\\Temp\\test.aac", aacFrame);

                        SampleBuffer buffer = new SampleBuffer();
                        buffer.SetBigEndian(false);
                        AacDecoder.decodeFrame(aacFrame, buffer);

                        //AppendToFile("C:\\Temp\\test.pcm", buffer.Data);

                        short[] sdata = new short[buffer.Data.Length / sizeof(short)];
                        Buffer.BlockCopy(buffer.Data, 0, sdata, 0, buffer.Data.Length);

                        // resample from 44100 to 48000
                        int inLen = sdata.Length / buffer.Channels;
                        int outLen = resampled.Length / buffer.Channels;
                        resampler.ProcessInterleaved(sdata, 0, ref inLen, resampled, 0, ref outLen);

                        sdata = resampled.Take(outLen * buffer.Channels).ToArray();

                        _samples.AddRange(sdata);

                        int frameSize = enc.GetFrameSize() * buffer.Channels;

                        while (_samples.Count >= frameSize)
                        {
                            sdata = _samples.Take(frameSize).ToArray();
                            _samples.RemoveRange(0, frameSize);

                            //byte[] result = new byte[sdata.Length * sizeof(short)];
                            //Buffer.BlockCopy(sdata, 0, result, 0, result.Length);
                            //AppendToFile("C:\\Temp\\testres.pcm", result);

                            byte[] encoded = enc.EncodeAudio(sdata, OpusAudioEncoder.MEDIA_FORMAT_OPUS);
                            foreach (var peerConnection in _peerConnections)
                            {
                                peerConnection.Value.SendRtpRaw(SDPMediaTypesEnum.audio, encoded, rtpBegin, 0, 111);
                            }

                            //short[] decoded = enc.DecodeAudio(encoded, OpusAudioEncoder.MEDIA_FORMAT_OPUS);
                            //byte[] decodedBytes = new byte[decoded.Length * sizeof(short)];
                            //Buffer.BlockCopy(decoded, 0, decodedBytes, 0, decodedBytes.Length);
                            //AppendToFile("C:\\Temp\\testdec.pcm", decodedBytes);

                            rtpBegin += (uint)enc.GetFrameSize();
                        }
                    }
                }
                catch (Exception ex)
                { }
            };
        }

        public static void AppendToFile(string fileToWrite, byte[] DT)
        {
            using (FileStream FS = new FileStream(fileToWrite, File.Exists(fileToWrite) ? FileMode.Append : FileMode.OpenOrCreate, FileAccess.Write))
            {
                FS.Write(DT, 0, DT.Length);
                FS.Close();
            }
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
    }
}
