using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using Concentus.Structs;
using Concentus.Enums;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CameraAPI.Opus
{
    // Based on this discussion: https://github.com/sipsorcery-org/sipsorcery/issues/518#issuecomment-888639894
    public class OpusAudioEncoder : IAudioEncoder
    {
        //Chrome use in SDP two audio channels, but the audio inself contains only one channel, so we must pass it as 2 channels in SDP but create a decoder/encoder with only one channel
        public static readonly AudioFormat MEDIA_FORMAT_OPUS = new AudioFormat(111, "opus", SAMPLE_RATE, SAMPLE_RATE, 2, "a=fmtp:111 minptime=10;useinbandfec=1");

        private static readonly ILogger log = SIPSorcery.LogFactory.CreateLogger<OpusAudioEncoder>();

        private AudioEncoder _audioEncoder; // The AudioEncoder available in SIPSorcery
        private List<AudioFormat> _supportedFormats;

        private const int FRAME_SIZE_MILLISECONDS = 20;
        private const int MAX_DECODED_FRAME_SIZE_MULT = 6;
        private const int MAX_PACKET_SIZE = 4000;
        private const int MAX_FRAME_SIZE = MAX_DECODED_FRAME_SIZE_MULT * 960;
        private const int SAMPLE_RATE = 48000;

        private int _channels = 1;
        private short[] _shortBuffer;
        private byte[] _byteBuffer;

        private OpusEncoder _opusEncoder;
        private OpusDecoder _opusDecoder;

        public OpusAudioEncoder(int channels)
        {
            _channels = channels;
            _audioEncoder = new AudioEncoder();

            // Add OPUS in the list of AudioFormat
            _supportedFormats = new List<AudioFormat>();
            _supportedFormats.Add(MEDIA_FORMAT_OPUS);

            // Add also list available in the AudioEncoder available in SIPSorcery
            _supportedFormats.AddRange(_audioEncoder.SupportedFormats);
        }

        public List<AudioFormat> SupportedFormats => _supportedFormats;

        public short[] DecodeAudio(byte[] encodedSample, AudioFormat format)
        {
            if (format.FormatName == "opus")
            {
                if (_opusDecoder == null)
                {
                    _opusDecoder = new OpusDecoder(SAMPLE_RATE, _channels);
                    _shortBuffer = new short[MAX_FRAME_SIZE * _channels];
                }

                try
                {
                    int numSamplesDecoded = _opusDecoder.Decode(encodedSample, 0, encodedSample.Length, _shortBuffer, 0, _shortBuffer.Length, false);

                    if (numSamplesDecoded >= 1)
                    {
                        var buffer = new short[numSamplesDecoded * _channels];
                        Buffer.BlockCopy(_shortBuffer, 0, buffer, 0, numSamplesDecoded * _channels * sizeof(short));

                        //log.LogDebug($"[DecodeAudio1] DecodedShort:[{numSamplesDecoded}] - EncodedByte.Length:[{encodedSample.Length}]");
                        return buffer;
                    }
                }
                catch
                {

                }
                return new short[0];
            }
            else
                return _audioEncoder.DecodeAudio(encodedSample, format);
        }

        public byte[] EncodeAudio(short[] pcm, AudioFormat format)
        {
            if (format.FormatName == "opus")
            {
                if (_opusEncoder == null)
                {
                    _opusEncoder = new OpusEncoder(SAMPLE_RATE, _channels, OpusApplication.OPUS_APPLICATION_AUDIO);
                    _opusEncoder.ForceMode = OpusMode.MODE_CELT_ONLY;
                    _byteBuffer = new byte[MAX_PACKET_SIZE];
                }

                try
                {
                    int frameSize = GetFrameSize();
                    int size = _opusEncoder.Encode(pcm, 0, frameSize, _byteBuffer, 0, _byteBuffer.Length);

                    if (size > 1)
                    {
                        byte[] result = new byte[size];
                        Buffer.BlockCopy(_byteBuffer, 0, result, 0, size);

                        log.LogDebug($"[EncodeAudioTest] frameSize:[{frameSize}] - DecodedShort:[{pcm.Length}] - EncodedByte.Length:[{result.Length}]");
                        return result;
                    }
                }
                catch
                {

                }
                return new byte[0];
            }
            else
                return _audioEncoder.EncodeAudio(pcm, format);
        }

        public int GetFrameSize()
        {
            return 960;
            //return (int)(SAMPLE_RATE * FRAME_SIZE_MILLISECONDS / 1000);
        }
    }
}
