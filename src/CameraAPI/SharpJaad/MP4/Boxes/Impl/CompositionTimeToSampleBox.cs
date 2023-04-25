namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * This box provides the offset between decoding time and composition time.
     * Since decoding time must be less than the composition time, the offsets are
     * expressed as unsigned numbers such that
     * CT(n) = DT(n) + CTTS(n)
     * where CTTS(n) is the (uncompressed) table entry for sample n.
     *
     * The composition time to sample table is optional and must only be present if
     * DT and CT differ for any samples.
     *
     * Hint tracks do not use this box.
     * 
     * @author in-somnia
     */
    public class CompositionTimeToSampleBox : FullBox
    {
        private long[] _sampleCounts, _sampleOffsets;

        public CompositionTimeToSampleBox() : base("Time To Sample Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            int entryCount = (int)input.ReadBytes(4);
            _sampleCounts = new long[entryCount];
            _sampleOffsets = new long[entryCount];

            for (int i = 0; i < entryCount; i++)
            {
                _sampleCounts[i] = input.ReadBytes(4);
                _sampleOffsets[i] = input.ReadBytes(4);
            }
        }

        public long[] GetSampleCounts()
        {
            return _sampleCounts;
        }

        public long[] GetSampleOffsets()
        {
            return _sampleOffsets;
        }
    }
}