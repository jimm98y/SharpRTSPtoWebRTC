namespace SharpJaad.MP4.Boxes.Impl
{
    /**
 * This box sets up default values used by the movie fragments. By setting
 * defaults in this way, space and complexity can be saved in each Track
 * Fragment Box.
 *
 * @author in-somnia
 */
    public class TrackExtendsBox : FullBox
    {
        private long _trackID;
        private long _defaultSampleDescriptionIndex, _defaultSampleDuration, _defaultSampleSize;
        private long _defaultSampleFlags;

        public TrackExtendsBox() : base("Track Extends Box")
        {  }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _trackID = input.ReadBytes(4);
            _defaultSampleDescriptionIndex = input.ReadBytes(4);
            _defaultSampleDuration = input.ReadBytes(4);
            _defaultSampleSize = input.ReadBytes(4);
            /* 6 bits reserved
             * 2 bits sampleDependsOn
             * 2 bits sampleIsDependedOn
             * 2 bits sampleHasRedundancy
             * 3 bits samplePaddingValue
             * 1 bit sampleIsDifferenceSample
             * 16 bits sampleDegradationPriority
             */
            _defaultSampleFlags = input.ReadBytes(4);
        }

        /**
         * The track ID identifies the track; this shall be the track ID of a track
         * in the Movie Box.
         *
         * @return the track ID
         */
        public long GetTrackID()
        {
            return _trackID;
        }

        /**
         * The default sample description index used in the track fragments.
         *
         * @return the default sample description index
         */
        public long GetDefaultSampleDescriptionIndex()
        {
            return _defaultSampleDescriptionIndex;
        }

        /**
         * The default sample duration used in the track fragments.
         *
         * @return the default sample duration
         */
        public long GetDefaultSampleDuration()
        {
            return _defaultSampleDuration;
        }

        /**
         * The default sample size used in the track fragments.
         *
         * @return the default sample size
         */
        public long GetDefaultSampleSize()
        {
            return _defaultSampleSize;
        }

        /**
         * The default 'sample depends on' value as defined in the
         * SampleDependencyTypeBox.
         *
         * @see SampleDependencyTypeBox#getSampleDependsOn()
         * @return the default 'sample depends on' value
         */
        public int GetSampleDependsOn()
        {
            return (int)((_defaultSampleFlags >> 24) & 3);
        }

        /**
         * The default 'sample is depended on' value as defined in the
         * SampleDependencyTypeBox.
         *
         * @see SampleDependencyTypeBox#getSampleIsDependedOn()
         * @return the default 'sample is depended on' value
         */
        public int GetSampleIsDependedOn()
        {
            return (int)((_defaultSampleFlags >> 22) & 3);
        }

        /**
         * The default 'sample has redundancy' value as defined in the
         * SampleDependencyBox.
         *
         * @see SampleDependencyTypeBox#getSampleHasRedundancy()
         * @return the default 'sample has redundancy' value
         */
        public int GetSampleHasRedundancy()
        {
            return (int)((_defaultSampleFlags >> 20) & 3);
        }

        /**
         * The default padding value as defined in the PaddingBitBox.
         *
         * @see PaddingBitBox#getPad1()
         * @return the default padding value
         */
        public int GetSamplePaddingValue()
        {
            return (int)((_defaultSampleFlags >> 17) & 7);
        }

        public bool IsSampleDifferenceSample()
        {
            return ((_defaultSampleFlags >> 16) & 1) == 1;
        }

        /**
         * The default degradation priority for the samples.
         * @return the default degradation priority
         */
        public int GetSampleDegradationPriority()
        {
            return (int)(_defaultSampleFlags & 0xFFFF);
        }
    }
}
