namespace SharpJaad.MP4.Boxes.Impl
{
    public class BitRateBox : BoxImpl
    {
        private long _decodingBufferSize, _maxBitrate, _avgBitrate;

        public BitRateBox() : base("Bitrate Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            _decodingBufferSize = input.ReadBytes(4);
            _maxBitrate = input.ReadBytes(4);
            _avgBitrate = input.ReadBytes(4);
        }

        /**
         * Gives the size of the decoding buffer for the elementary stream in bytes.
         * @return the decoding buffer size
         */
        public long GetDecodingBufferSize()
        {
            return _decodingBufferSize;
        }

        /**
         * Gives the maximum rate in bits/second over any window of one second.
         * @return the maximum bitrate
         */
        public long GetMaximumBitrate()
        {
            return _maxBitrate;
        }

        /**
         * Gives the average rate in bits/second over the entire presentation.
         * @return the average bitrate
         */
        public long GetAverageBitrate()
        {
            return _avgBitrate;
        }
    }
}
