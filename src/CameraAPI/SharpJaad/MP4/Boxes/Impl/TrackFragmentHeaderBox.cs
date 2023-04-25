namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * Each movie fragment can add zero or more fragments to each track; and a track
     * fragment can add zero or more contiguous runs of samples. The track fragment
     * header sets up information and defaults used for those runs of samples.
     * 
     * @author in-somnia
     */
    public class TrackFragmentHeaderBox : FullBox
    {
        private long _trackID;
        private bool _baseDataOffsetPresent, _sampleDescriptionIndexPresent,
                _defaultSampleDurationPresent, _defaultSampleSizePresent,
                _defaultSampleFlagsPresent;
        private bool _durationIsEmpty;
        private long _baseDataOffset, _sampleDescriptionIndex, _defaultSampleDuration,
                _defaultSampleSize, _defaultSampleFlags;

        public TrackFragmentHeaderBox() : base("Track Fragment Header Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _trackID = input.ReadBytes(4);

            //optional fields
            _baseDataOffsetPresent = ((_flags & 1) == 1);
            _baseDataOffset = _baseDataOffsetPresent ? input.ReadBytes(8) : 0;

            _sampleDescriptionIndexPresent = ((_flags & 2) == 2);
            _sampleDescriptionIndex = _sampleDescriptionIndexPresent ? input.ReadBytes(4) : 0;

            _defaultSampleDurationPresent = ((_flags & 8) == 8);
            _defaultSampleDuration = _defaultSampleDurationPresent ? input.ReadBytes(4) : 0;

            _defaultSampleSizePresent = ((_flags & 16) == 16);
            _defaultSampleSize = _defaultSampleSizePresent ? input.ReadBytes(4) : 0;

            _defaultSampleFlagsPresent = ((_flags & 32) == 32);
            _defaultSampleFlags = _defaultSampleFlagsPresent ? input.ReadBytes(4) : 0;

            _durationIsEmpty = ((_flags & 0x10000) == 0x10000);
        }

        public long GetTrackID()
        {
            return _trackID;
        }

        public bool IsBaseDataOffsetPresent()
        {
            return _baseDataOffsetPresent;
        }

        public long GetBaseDataOffset()
        {
            return _baseDataOffset;
        }

        public bool IsSampleDescriptionIndexPresent()
        {
            return _sampleDescriptionIndexPresent;
        }

        public long GetSampleDescriptionIndex()
        {
            return _sampleDescriptionIndex;
        }

        public bool IsDefaultSampleDurationPresent()
        {
            return _defaultSampleDurationPresent;
        }

        public long GetDefaultSampleDuration()
        {
            return _defaultSampleDuration;
        }

        public bool IsDefaultSampleSizePresent()
        {
            return _defaultSampleSizePresent;
        }

        public long GetDefaultSampleSize()
        {
            return _defaultSampleSize;
        }

        public bool IsDefaultSampleFlagsPresent()
        {
            return _defaultSampleFlagsPresent;
        }

        public long GetDefaultSampleFlags()
        {
            return _defaultSampleFlags;
        }

        public bool IsDurationIsEmpty()
        {
            return _durationIsEmpty;
        }
    }
}