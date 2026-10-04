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
        private const int DEFAULT_FORMAT_ID = 111;

        // Chrome uses in SDP two audio channels, but if the audio itself contains only one channel, we must pass it as 2 channels in SDP but create a decoder/encoder with only one channel
        public static AudioFormat GetOpusAudioFormat(int channels, int formatId = DEFAULT_FORMAT_ID)
        {
            // Just the parameters: sipsorcery writes the "a=fmtp:<id> " prefix itself, so spelling it
            //  out here produced "a=fmtp:111 a=fmtp:111 minptime=10..." in the offer and the browser
            //  had no usable fmtp line at all.
            string stereo = GetStereoFormatParameters(channels >= 2);
            return new AudioFormat(formatId, "opus", SAMPLE_RATE, SAMPLE_RATE, Math.Max(2, channels), "minptime=10;useinbandfec=1" + (stereo == null ? "" : ";" + stereo));
        }

        /// <summary>
        /// What an Opus offer has to say for the browser to play stereo, or null for mono.
        /// </summary>
        /// <remarks>
        /// The rtpmap of Opus always says two channels, so it tells the browser nothing: Chrome
        /// decodes to mono unless the offer has stereo=1. sprop-stereo=1 says it is what is sent.
        /// </remarks>
        public static string GetStereoFormatParameters(bool stereo)
        {
            return stereo ? "stereo=1;sprop-stereo=1" : null;
        }

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
                GetOpusAudioFormat(_channels)
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

        /// <summary>
        /// Encodes exactly one OPUS frame of interleaved PCM.
        /// </summary>
        /// <remarks>
        /// Takes a span so a caller holding a longer buffer can encode a frame out of the middle of it
        ///  without copying the frame out first.
        /// </remarks>
        public byte[] EncodeOpus(ReadOnlySpan<short> pcm)
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
                    return result;
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }

            return new byte[0];
        }

        public byte[] EncodeAudio(short[] pcm, AudioFormat format)
        {
            if (format.FormatName == "opus")
            {
                return EncodeOpus(pcm);
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

    /// <summary>
    /// Carries RTP timestamps from a source clock over to the one OPUS is sent on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Scales the step between packets rather than the timestamp itself. RTP timestamps wrap at
    /// 2^32, and scaling the absolute value does not survive that: at 44.1kHz the source wraps about
    /// every 27 hours, and the scaled value jumped from roughly 380 million back to nearly zero - a
    /// backwards step of some two hours on the output clock, where a wrap should have been a step of
    /// one frame. A camera left running hits this.
    /// </para>
    /// <para>
    /// Stepping means the division has to carry its remainder: 1024 source samples at 44.1kHz are
    /// worth 1114.6 output ticks, and truncating that on every frame would lose about 43 seconds a
    /// day.
    /// </para>
    /// </remarks>
    internal sealed class OpusClockScaler
    {
        /// <summary>
        /// A step larger than this is a new stream rather than a wrap - after a reconnect the camera
        /// picks a fresh random start, and the two values have nothing to do with one another.
        /// </summary>
        private const int DISCONTINUITY_SECONDS = 30;

        private readonly ulong _sourceClock;
        private readonly ulong _outputClock;

        private bool _started;
        private uint _lastSource;
        private uint _output;
        private ulong _remainder;

        public OpusClockScaler(int sourceClock, int outputClock)
        {
            if (sourceClock <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceClock));

            if (outputClock <= 0)
                throw new ArgumentOutOfRangeException(nameof(outputClock));

            _sourceClock = (ulong)sourceClock;
            _outputClock = (ulong)outputClock;
        }

        /// <summary>
        /// The output timestamp for a source timestamp, which must be given in arrival order.
        /// </summary>
        public uint Next(uint sourceTimestamp)
        {
            if (!_started)
            {
                _started = true;
                _lastSource = sourceTimestamp;
                _output = (uint)((ulong)sourceTimestamp * _outputClock / _sourceClock);
                _remainder = 0;
                return _output;
            }

            // unchecked, so a wrap is the small step it should be rather than a huge negative one
            uint delta = unchecked(sourceTimestamp - _lastSource);
            _lastSource = sourceTimestamp;

            if (delta > _sourceClock * DISCONTINUITY_SECONDS)
            {
                // carry the output clock straight on rather than stepping it by however far apart
                //  two unrelated random values happened to be
                _remainder = 0;
                return _output;
            }

            ulong scaled = (delta * _outputClock) + _remainder;
            _output = unchecked(_output + (uint)(scaled / _sourceClock));
            _remainder = scaled % _sourceClock;

            return _output;
        }
    }

    /// <summary>
    /// Hands out the RTP timestamps of consecutive Opus frames, so each is exactly one frame after
    /// the one before.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Where a frame starts can be worked out afresh for every AAC frame that comes in - its source
    /// timestamp scaled to 48kHz, less what is still buffered - but that lands a sample either side
    /// of where the frames already sent put it: 1024 samples at 44.1kHz are 1114.56 at 48kHz, and
    /// the resampler hands them out a whole sample at a time. The frames then went out 959 and 961
    /// apart rather than 960, and the receiver heard a sample's gap or overlap between them as one
    /// to conceal - which, on almost every other frame, came out as choppy audio.
    /// </para>
    /// <para>
    /// So the frames are counted on from the first, and the source is only followed when it moves
    /// away by more than a frame: a gap in the stream, a reconnect, or drift built up over a long
    /// time - not the rounding.
    /// </para>
    /// </remarks>
    internal sealed class OpusTimeline
    {
        private readonly int _tolerance;

        private bool _started;
        private uint _next;

        /// <param name="tolerance">How far, in samples, where the source says the next frame starts
        /// may be from where counting on puts it before the source is followed instead.</param>
        public OpusTimeline(int tolerance)
        {
            if (tolerance < 0)
                throw new ArgumentOutOfRangeException(nameof(tolerance));

            _tolerance = tolerance;
        }

        /// <summary>
        /// Lines the next frame up with where the source says it starts, unless that is within the
        /// tolerance of where it is anyway.
        /// </summary>
        public void Align(uint expected)
        {
            // unchecked, so a wrap of the 32 bit clock is the small difference it is
            int difference = unchecked((int)(expected - _next));

            if (!_started || difference > _tolerance || difference < -_tolerance)
            {
                _next = expected;
                _started = true;
            }
        }

        /// <summary>
        /// The timestamp of the next frame, which then moves the timeline on by its length.
        /// </summary>
        public uint Take(int frameSamples)
        {
            uint timestamp = _next;
            _next = unchecked(_next + (uint)frameSamples);
            return timestamp;
        }
    }

    /// <summary>
    /// Holds decoded interleaved PCM until there is a whole encoder frame of it.
    /// </summary>
    /// <remarks>
    /// A buffer with a read cursor rather than a List that frames are taken off the front of:
    /// removing from the front of a List shifts everything left of it down, and that happened for
    /// every one of the ~50 frames a second. Compacts before it grows, so a steady stream settles on
    /// one allocation rather than growing without limit as the cursor walks forward.
    /// </remarks>
    internal sealed class PcmSampleBuffer
    {
        private const int MIN_CAPACITY = 4096;

        private short[] _samples = new short[0];
        private int _head; // first sample not yet read
        private int _tail; // one past the last sample written

        /// <summary>
        /// How many samples are waiting to be read.
        /// </summary>
        public int Count => _tail - _head;

        /// <summary>
        /// The buffer's current capacity. Here so a test can show it stops growing.
        /// </summary>
        public int Capacity => _samples.Length;

        public void Append(short[] data, int count)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (count < 0 || count > data.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count == 0)
                return;

            if (_tail + count > _samples.Length)
            {
                int live = Count;

                if (live + count <= _samples.Length)
                {
                    // what is already there is enough room once the read part is dropped
                    Array.Copy(_samples, _head, _samples, 0, live);
                }
                else
                {
                    var grown = new short[Math.Max(live + count, Math.Max(_samples.Length * 2, MIN_CAPACITY))];
                    Array.Copy(_samples, _head, grown, 0, live);
                    _samples = grown;
                }

                _head = 0;
                _tail = live;
            }

            Array.Copy(data, 0, _samples, _tail, count);
            _tail += count;
        }

        /// <summary>
        /// The next <paramref name="count"/> samples, without consuming them.
        /// </summary>
        /// <remarks>
        /// The span points into the buffer, so it is only good until the next <see cref="Append"/>.
        /// Encode out of it and <see cref="Advance"/> past it before adding anything else.
        /// </remarks>
        public ReadOnlySpan<short> Peek(int count)
        {
            if (count < 0 || count > Count)
                throw new ArgumentOutOfRangeException(nameof(count));

            return new ReadOnlySpan<short>(_samples, _head, count);
        }

        public void Advance(int count)
        {
            if (count < 0 || count > Count)
                throw new ArgumentOutOfRangeException(nameof(count));

            _head += count;
        }
    }
}
