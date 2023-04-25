namespace SharpJaad.MP4.Boxes.Impl.SampleEntries
{
    public abstract class SampleEntry : BoxImpl
    {
        private long _dataReferenceIndex;

        protected SampleEntry(string name) : base(name)
        { }

        public override void Decode(MP4InputStream input)
        {
            input.SkipBytes(6); //reserved
            _dataReferenceIndex = input.ReadBytes(2);
        }

        /**
         * The data reference index is an integer that contains the index of the
         * data reference to use to retrieve data associated with samples that use
         * this sample description. Data references are stored in Data Reference
         * Boxes. The index ranges from 1 to the number of data references.
         */
        public long GetDataReferenceIndex()
        {
            return _dataReferenceIndex;
        }
    }
}
