﻿namespace SharpJaad.MP4.Boxes.Impl
{
    /**
      * This table can be used to find the group that a sample belongs to and the
      * associated description of that sample group. The table is compactly coded
      * with each entry giving the index of the first sample of a run of samples with
      * the same sample group descriptor. The sample group description ID is an index
      * that refers to a SampleGroupDescription box, which contains entries
      * describing the characteristics of each sample group.
      *
      * There may be multiple instances of this box if there is more than one sample
      * grouping for the samples in a track. Each instance of the SampleToGroup box
      * has a type code that distinguishes different sample groupings. Within a
      * track, there shall be at most one instance of this box with a particular
      * grouping type. The associated SampleGroupDescription shall indicate the same
      * value for the grouping type.
      */
    public class SampleToGroupBox : FullBox
    {
        private long _groupingType;
        private long[] _sampleCount, _groupDescriptionIndex;

        public SampleToGroupBox() : base("Sample To Group Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _groupingType = input.readBytes(4);
            int entryCount = (int)input.readBytes(4);
            _sampleCount = new long[entryCount];
            _groupDescriptionIndex = new long[entryCount];

            for (int i = 0; i < entryCount; i++)
            {
                _sampleCount[i] = input.readBytes(4);
                _groupDescriptionIndex[i] = input.readBytes(4);
            }
        }

        /**
         * The grouping type is an integer that identifies the type (i.e. criterion
         * used to form the sample groups) of the sample grouping and links it to
         * its sample group description table with the same value for grouping type.
         * At most one occurrence of this box with the same value for 'grouping
         * type' shall exist for a track.
         */
        public long GetGroupingType()
        {
            return _groupingType;
        }

        /**
         * The sample count is an integer that gives the number of consecutive
         * samples with the same sample group descriptor for a specific entry. If
         * the sum of the sample count in this box is less than the total sample
         * count, then the reader should effectively extend it with an entry that
         * associates the remaining samples with no group.
         * It is an error for the total in this box to be greater than the sample
         * count documented elsewhere, and the reader behaviour would then be
         * undefined.
         */
        public long[] GetSampleCount()
        {
            return _sampleCount;
        }

        /**
         * The group description index is an integer that gives the index of the
         * sample group entry which describes the samples in this group for a
         * specific entry. The index ranges from 1 to the number of sample group
         * entries in the SampleGroupDescriptionBox, or takes the value 0 to
         * indicate that this sample is a member of no group of this type.
         */
        public long[] GetGroupDescriptionIndex()
        {
            return _groupDescriptionIndex;
        }
    }
}
