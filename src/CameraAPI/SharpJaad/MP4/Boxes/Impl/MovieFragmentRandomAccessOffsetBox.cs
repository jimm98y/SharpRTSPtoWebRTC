namespace SharpJaad.MP4.Boxes.Impl
{
    /**
      * The Movie Fragment Random Access Offset Box provides a copy of the length
      * field from the enclosing Movie Fragment Random Access Box. It is placed last
      * within that box, so that the size field is also last in the enclosing Movie
      * Fragment Random Access Box. When the Movie Fragment Random Access Box is also
      * last in the file this permits its easy location. The size field here must be
      * correct. However, neither the presence of the Movie Fragment Random Access
      * Box, nor its placement last in the file, are assured.
      *
      * @author in-somnia
      */
    public class MovieFragmentRandomAccessOffsetBox : FullBox
    {
        private long _byteSize;

        public MovieFragmentRandomAccessOffsetBox() : base("Movie Fragment Random Access Offset Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _byteSize = input.readBytes(4);
        }

        public long GetByteSize()
        {
            return _byteSize;
        }
    }
}
