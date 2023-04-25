namespace SharpJaad.MP4.Boxes.Impl.SampleEntries
{
    public class RTPHintSampleEntry : SampleEntry
    {
        private int _hintTrackVersion, _highestCompatibleVersion;
        private long _maxPacketSize;

        public RTPHintSampleEntry() : base("RTP Hint Sample Entry")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _hintTrackVersion = (int)input.ReadBytes(2);
            _highestCompatibleVersion = (int)input.ReadBytes(2);
            _maxPacketSize = input.ReadBytes(4);
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
