namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * The Sub-Sample Information box is designed to contain sub-sample information.
     * A sub-sample is a contiguous range of bytes of a sample. The specific
     * definition of a sub-sample shall be supplied for a given coding system (e.g.
     * for ISO/IEC 14496-10, Advanced Video Coding). In the absence of such a
     * specific definition, this box shall not be applied to samples using that
     * coding system.
     * The table is sparsely coded; the table identifies which samples have
     * sub-sample structure by recording the difference in sample-number between
     * each entry. The first entry in the table records the sample number of the
     * first sample having sub-sample information.
     *
     * @author in-somnia
     */
    public class SubSampleInformationBox : FullBox
    {
        private long[] _sampleDelta;
        private long[][] _subsampleSize;
        private int[][] _subsamplePriority;
        private bool[][] _discardable;

        public SubSampleInformationBox() : base("Sub Sample Information Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            int len = (version == 1) ? 4 : 2;
            int entryCount = (int)input.readBytes(4);
            _sampleDelta = new long[entryCount];
            _subsampleSize = new long[entryCount][];
            _subsamplePriority = new int[entryCount][];
            _discardable = new bool[entryCount][];

            int j, subsampleCount;
            for (int i = 0; i < entryCount; i++)
            {
                _sampleDelta[i] = input.readBytes(4);
                subsampleCount = (int)input.readBytes(2);
                _subsampleSize[i] = new long[subsampleCount];
                _subsamplePriority[i] = new int[subsampleCount];
                _discardable[i] = new bool[subsampleCount];

                for (j = 0; j < subsampleCount; j++)
                {
                    _subsampleSize[i][j] = input.readBytes(len);
                    _subsamplePriority[i][j] = input.read();
                    _discardable[i][j] = (input.read() & 1) == 1;
                    input.skipBytes(4); //reserved
                }
            }
        }

        /**
         * The sample delta for each entry is an integer that specifies the sample 
         * number of the sample having sub-sample structure. It is coded as the 
         * difference between the desired sample number, and the sample number
         * indicated in the previous entry. If the current entry is the first entry,
         * the value indicates the sample number of the first sample having
         * sub-sample information, that is, the value is the difference between the
         * sample number and zero.
         *
         * @return the sample deltas for all entries
         */
        public long[] GetSampleDelta()
        {
            return _sampleDelta;
        }

        /**
         * The subsample size is an integer that specifies the size, in bytes, of a
         * specific sub-sample in a specific entry.
         *
         * @return the sizes of all subsamples
         */
        public long[][] GetSubsampleSize()
        {
            return _subsampleSize;
        }

        /**
         * The subsample priority is an integer specifying the degradation priority
         * for a specific sub-sample in a specific entry. Higher values indicate
         * sub-samples which are important to, and have a greater impact on, the
         * decoded quality.
         *
         * @return all subsample priorities
         */
        public int[][] GetSubsamplePriority()
        {
            return _subsamplePriority;
        }

        /**
         * If true, the sub-sample is required to decode the current sample, while
         * false means the sub-sample is not required to decode the current sample 
         * but may be used for enhancements, e.g., the sub-sample consists of
         * supplemental enhancement information (SEI) messages.
         *
         * @return a list of flags indicating if a specific subsample is discardable
         */
        public bool[][] GetDiscardable()
        {
            return _discardable;
        }
    }
}
