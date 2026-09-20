using System;

namespace SharpRTSPtoWebRTC.Codecs
{
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
}
