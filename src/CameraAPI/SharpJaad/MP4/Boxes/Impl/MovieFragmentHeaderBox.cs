namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * The movie fragment header contains a sequence number, as a safety check. The
     * sequence number usually starts at 1 and must increase for each movie fragment
     * in the file, in the order in which they occur. This allows readers to verify
     * integrity of the sequence; it is an error to construct a file where the
     * fragments are out of sequence.
     */
    public class MovieFragmentHeaderBox : FullBox
    {
        private long _sequenceNumber;

        public MovieFragmentHeaderBox() : base("Movie Fragment Header Box")
        {  }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _sequenceNumber = input.readBytes(4);
        }

        /**
         * The ordinal number of this fragment, in increasing order.
         */
        public long GetSequenceNumber()
        {
            return _sequenceNumber;
        }
    }
}
