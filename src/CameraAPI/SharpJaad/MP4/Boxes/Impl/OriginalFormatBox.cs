namespace SharpJaad.MP4.Boxes.Impl
{
    /**
 * The Original Format Box contains the four-character-code of the original
 * un-transformed sample description.
 *
 * @author in-somnia
 */
    public class OriginalFormatBox : BoxImpl
    {
        private long _originalFormat;

        public OriginalFormatBox() : base("Original Format Box")
        { }

        public override void decode(MP4InputStream input)
        {
            _originalFormat = input.readBytes(4);
        }

        /**
         * The original format is the four-character-code of the original
         * un-transformed sample entry (e.g. 'mp4v' if the stream contains protected
         * MPEG-4 visual material).
         *
         * @return the stream's original format
         */
        public long GetOriginalFormat()
        {
            return _originalFormat;
        }
    }
}
