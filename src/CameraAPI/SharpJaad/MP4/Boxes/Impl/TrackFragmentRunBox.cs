namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * Within the Track Fragment Box, there are zero or more Track Run Boxes. If the
     * duration-is-empty flag is set in the track fragment box, there are no track
     * runs. A track run documents a contiguous set of samples for a track.
     *
     * If the data-offset is not present, then the data for this run starts
     * immediately after the data of the previous run, or at the base-data-offset
     * defined by the track fragment header if this is the first run in a track
     * fragment.
     * If the data-offset is present, it is relative to the base-data-offset
     * established in the track fragment header.
     * 
     * @author in-somnia
     */
    public class TrackFragmentRunBox : FullBox
    {
        private int _sampleCount;
        private bool _dataOffsetPresent, _firstSampleFlagsPresent;
        private long _dataOffset, _firstSampleFlags;
        private bool _sampleDurationPresent, _sampleSizePresent, _sampleFlagsPresent, _sampleCompositionTimeOffsetPresent;
        private long[] _sampleDuration, _sampleSize, _sampleFlags, _sampleCompositionTimeOffset;

        public TrackFragmentRunBox() : base("Track Fragment Run Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _sampleCount = (int)input.readBytes(4);

            //optional fields
            _dataOffsetPresent = ((flags & 1) == 1);
            if (_dataOffsetPresent) _dataOffset = input.readBytes(4);

            _firstSampleFlagsPresent = ((flags & 4) == 4);
            if (_firstSampleFlagsPresent) _firstSampleFlags = input.readBytes(4);

            //all fields are optional
            _sampleDurationPresent = ((flags & 0x100) == 0x100);
            if (_sampleDurationPresent) _sampleDuration = new long[_sampleCount];
            _sampleSizePresent = ((flags & 0x200) == 0x200);
            if (_sampleSizePresent) _sampleSize = new long[_sampleCount];
            _sampleFlagsPresent = ((flags & 0x400) == 0x400);
            if (_sampleFlagsPresent) _sampleFlags = new long[_sampleCount];
            _sampleCompositionTimeOffsetPresent = ((flags & 0x800) == 0x800);
            if (_sampleCompositionTimeOffsetPresent) _sampleCompositionTimeOffset = new long[_sampleCount];

            for (int i = 0; i < _sampleCount && GetLeft(input) > 0; i++)
            {
                if (_sampleDurationPresent) _sampleDuration[i] = input.readBytes(4);
                if (_sampleSizePresent) _sampleSize[i] = input.readBytes(4);
                if (_sampleFlagsPresent) _sampleFlags[i] = input.readBytes(4);
                if (_sampleCompositionTimeOffsetPresent) _sampleCompositionTimeOffset[i] = input.readBytes(4);
            }
        }

        public int GetSampleCount()
        {
            return _sampleCount;
        }

        public bool IsDataOffsetPresent()
        {
            return _dataOffsetPresent;
        }

        public long GetDataOffset()
        {
            return _dataOffset;
        }

        public bool IsFirstSampleFlagsPresent()
        {
            return _firstSampleFlagsPresent;
        }

        public long GetFirstSampleFlags()
        {
            return _firstSampleFlags;
        }

        public bool IsSampleDurationPresent()
        {
            return _sampleDurationPresent;
        }

        public long[] GetSampleDuration()
        {
            return _sampleDuration;
        }

        public bool IsSampleSizePresent()
        {
            return _sampleSizePresent;
        }

        public long[] GetSampleSize()
        {
            return _sampleSize;
        }

        public bool IsSampleFlagsPresent()
        {
            return _sampleFlagsPresent;
        }

        public long[] GetSampleFlags()
        {
            return _sampleFlags;
        }

        public bool IsSampleCompositionTimeOffsetPresent()
        {
            return _sampleCompositionTimeOffsetPresent;
        }

        public long[] GetSampleCompositionTimeOffset()
        {
            return _sampleCompositionTimeOffset;
        }
    }
}
