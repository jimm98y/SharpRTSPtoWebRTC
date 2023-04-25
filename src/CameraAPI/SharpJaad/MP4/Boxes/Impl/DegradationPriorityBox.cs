namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * This box contains the degradation priority of each sample. The values are
     * stored in the table, one for each sample. Specifications derived from this
     * define the exact meaning and acceptable range of the priority field.
     * 
     * @author in-somnia
     */
    public class DegradationPriorityBox : FullBox
    {
        private int[] _priorities;

        public DegradationPriorityBox() : base("Degradation Priority Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            //get number of samples from SampleSizeBox
            int sampleCount = ((SampleSizeBox)_parent.GetChild(BoxTypes.SAMPLE_SIZE_BOX)).GetSampleCount();

            _priorities = new int[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                _priorities[i] = (int)input.ReadBytes(2);
            }
        }

        /**
         * The priority is integer specifying the degradation priority for each
         * sample.
         * @return the list of priorities
         */
        public int[] GetPriorities()
        {
            return _priorities;
        }
    }
}
