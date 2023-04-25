namespace SharpJaad.MP4.Boxes.Impl
{
    /**
      * This box provides a compact marking of the random access points within the
      * stream. The table is arranged in strictly increasing order of sample number.
      *
      * If the sync sample box is not present, every sample is a random access point.
      *
      * @author in-somnia
      */
    public class SyncSampleBox : FullBox
    {
        private long[] _sampleNumbers;

        public SyncSampleBox() : base("Sync Sample Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            int entryCount = (int)input.ReadBytes(4);
            _sampleNumbers = new long[entryCount];
            for (int i = 0; i < entryCount; i++)
            {
                _sampleNumbers[i] = input.ReadBytes(4);
            }
        }

        /**
         * Gives the numbers of the samples for each entry that are random access
         * points in the stream.
         * 
         * @return a list of sample numbers
         */
        public long[] GetSampleNumbers()
        {
            return _sampleNumbers;
        }
    }
}
