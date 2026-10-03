using System;

namespace SharpRTSPtoWebRTC.Codecs
{
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
}
