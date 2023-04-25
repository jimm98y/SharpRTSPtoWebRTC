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

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _trackID = input.readBytes(4);

            //optional fields
            _baseDataOffsetPresent = ((flags & 1) == 1);
            _baseDataOffset = _baseDataOffsetPresent ? input.readBytes(8) : 0;

            _sampleDescriptionIndexPresent = ((flags & 2) == 2);
            _sampleDescriptionIndex = _sampleDescriptionIndexPresent ? input.readBytes(4) : 0;

            _defaultSampleDurationPresent = ((flags & 8) == 8);
            _defaultSampleDuration = _defaultSampleDurationPresent ? input.readBytes(4) : 0;

            _defaultSampleSizePresent = ((flags & 16) == 16);
            _defaultSampleSize = _defaultSampleSizePresent ? input.readBytes(4) : 0;

            _defaultSampleFlagsPresent = ((flags & 32) == 32);
            _defaultSampleFlags = _defaultSampleFlagsPresent ? input.readBytes(4) : 0;

            _durationIsEmpty = ((flags & 0x10000) == 0x10000);
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