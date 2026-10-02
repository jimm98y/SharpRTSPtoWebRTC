using System;

namespace SharpRTSPtoWebRTC.Codecs
{
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
