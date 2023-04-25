namespace SharpJaad.MP4.Boxes.Impl.SampleEntries
{
    public class RTPHintSampleEntry : SampleEntry
    {
        private int _hintTrackVersion, _highestCompatibleVersion;
        private long _maxPacketSize;

        public RTPHintSampleEntry() : base("RTP Hint Sample Entry")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _hintTrackVersion = (int)input.readBytes(2);
            _highestCompatibleVersion = (int)input.readBytes(2);
            _maxPacketSize = input.readBytes(4);
        }

        /**
         * The maximum packet size indicates the size of the largest packet that
         * this track will generate.
         *
         * @return the maximum packet size
         */
        public long GetMaxPacketSize()
        {
            return _maxPacketSize;
        }
    }
}
