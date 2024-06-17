using Concentus;
using Concentus.Enums;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace SharpRTSPtoWebRTC.Codecs
{
    /// <summary>
    /// OPUS audio encoder/decoder for sipsorcery based upon the Concentus OPUS codec implementation.
    /// </summary>
    /// <remarks>
    /// Based on this discussion: https://github.com/sipsorcery-org/sipsorcery/issues/518#issuecomment-888639894
    /// </remarks>
    internal class OpusAudioEncoder : IAudioEncoder
    {
        private static readonly ILogger log = SIPSorcery.LogFactory.CreateLogger<OpusAudioEncoder>();

        // private const int FRAME_SIZE_MILLISECONDS = 20;
        private const int OPUS_FRAME_SIZE = 960;
        private const int MAX_DECODED_FRAME_SIZE_MULT = 6; 
        private const int MAX_PACKET_SIZE = 1275;
        private const int MAX_FRAME_SIZE = MAX_DECODED_FRAME_SIZE_MULT * OPUS_FRAME_SIZE; // some buffer large enough to hold the samples
        private const int SAMPLE_RATE = 48000;

        // Chrome uses in SDP two audio channels, but if the audio itself contains only one channel, we must pass it as 2 channels in SDP but create a decoder/encoder with only one channel
        public AudioFormat OpusAudioFormat { get { return new AudioFormat(111, "opus", SAMPLE_RATE, SAMPLE_RATE, Math.Max(2, _channels), "a=fmtp:111 minptime=10;useinbandfec=1"); } }
        public List<AudioFormat> SupportedFormats => _supportedFormats;

        private AudioEncoder _audioEncoder; // the AudioEncoder available in SIPSorcery
        private List<AudioFormat> _supportedFormats;

        private int _channels = 1;
        private short[] _shortBuffer;
        private byte[] _byteBuffer;

        private IOpusEncoder _opusEncoder;
        private IOpusDecoder _opusDecoder;

        public OpusAudioEncoder(int channels)
        {
            _channels = channels;
            _audioEncoder = new AudioEncoder();

            // Add OPUS in the list of AudioFormat
            _supportedFormats = new List<AudioFormat>
            {
                OpusAudioFormat
            };

            // Add also list available in the AudioEncoder available in SIPSorcery
            _supportedFormats.AddRange(_audioEncoder.SupportedFormats);
        }

        public short[] DecodeAudio(byte[] encoded, AudioFormat format)
        {
            if (format.FormatName == "opus")
            {
                if (_opusDecoder == null)
                {
                    _opusDecoder = OpusCodecFactory.CreateDecoder(SAMPLE_RATE, _channels);
                    _shortBuffer = new short[MAX_FRAME_SIZE * _channels];
                }

                try
                {
                    int numSamplesDecoded = _opusDecoder.Decode(encoded, _shortBuffer, OPUS_FRAME_SIZE, false);

                    if (numSamplesDecoded >= 1)
                    {
                        var buffer = new short[numSamplesDecoded * _channels];
                        Buffer.BlockCopy(_shortBuffer, 0, buffer, 0, numSamplesDecoded * _channels * sizeof(short));

                        log.LogDebug($"[DecodeAudio] DecodedShort:[{numSamplesDecoded}] - EncodedBytes.Length:[{encoded.Length}]");
                        return buffer;
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message);
                }

                return new short[0];
            }
            else
            {
                return _audioEncoder.DecodeAudio(encoded, format);
            }
        }

        public byte[] EncodeAudio(short[] pcm, AudioFormat format)
        {
            if (format.FormatName == "opus")
            {
                if (_opusEncoder == null)
                {
                    _opusEncoder = OpusCodecFactory.CreateEncoder(SAMPLE_RATE, _channels, OpusApplication.OPUS_APPLICATION_AUDIO);
                    _opusEncoder.ForceMode = OpusMode.MODE_CELT_ONLY;
                    _byteBuffer = new byte[MAX_PACKET_SIZE];
                }

                try
                {
                    int frameSize = GetFrameSize();
                    int size = _opusEncoder.Encode(pcm, frameSize, _byteBuffer, _byteBuffer.Length);

                    if (size > 1)
                    {
                        byte[] result = new byte[size];
                        Buffer.BlockCopy(_byteBuffer, 0, result, 0, size);

                        log.LogDebug($"[EncodeAudio] frameSize:[{frameSize}] - DecodedShort:[{pcm.Length}] - EncodedBytes.Length:[{result.Length}]");
                        return result;
                    }
                }
                catch(Exception ex)
                {
                    log.LogError(ex.Message);
                }

                return new byte[0];
            }
            else
            {
                return _audioEncoder.EncodeAudio(pcm, format);
            }
        }

        public int GetFrameSize()
        {
            return OPUS_FRAME_SIZE;
        }
    }
}
