namespace SharpJaad.MP4.Boxes.Impl
{
    /**
   * This box contains the sample dependencies for each switching sample. The
   * dependencies are stored in the table, one record for each sample. The size of
   * the table is taken from the the Sample Size Box ('stsz') or Compact Sample
   * Size Box ('stz2').
   *
   * @author in-somnia
   */
    public class SampleDependencyBox : FullBox
    {
        private int[] _dependencyCount;
        private int[][] _relativeSampleNumber;

        public SampleDependencyBox() : base("Sample Dependency Box")
        { }

        public void decode(MP4InputStream input)
        {
            base.Decode(input);

            int sampleCount = ((SampleSizeBox)_parent.GetChild(BoxTypes.SAMPLE_SIZE_BOX)).GetSampleCount();

            _dependencyCount = new int[sampleCount];
            _relativeSampleNumber = new int[sampleCount][];

            int j;
            for (int i = 0; i < sampleCount; i++)
            {
                _dependencyCount[i] = (int)input.ReadBytes(2);
                _relativeSampleNumber[i] = new int[_dependencyCount[i]];
                for (j = 0; j < _dependencyCount[i]; j++)
                {
                    _relativeSampleNumber[i][j] = (int)input.ReadBytes(2);
                }
            }
        }

        /**
         * The dependency count is an integer that counts the number of samples
         * in the source track on which this switching sample directly depends.
         *
         * @return all dependency counts
         */
        public int[] GetDependencyCount()
        {
            return _dependencyCount;
        }

        /**
         * The relative sample number is an integer that identifies a sample in
         * the source track. The relative sample numbers are encoded as follows.
         * If there is a sample in the source track with the same decoding time,
         * it has a relative sample number of 0. Whether or not this sample
         * exists, the sample in the source track which immediately precedes the
         * decoding time of the switching sample has relative sample number –1,
         * the sample before that –2, and so on. Similarly, the sample in the
         * source track which immediately follows the decoding time of the
         * switching sample has relative sample number +1, the sample after that
         * +2, and so on.
         *
         * @return all relative sample numbers
         */
        public int[][] GetRelativeSampleNumber()
        {
            return _relativeSampleNumber;
        }
    }
}
