namespace SharpJaad.MP4.Boxes.Impl.SampleEntries
{
    public class TextMetadataSampleEntry : MetadataSampleEntry
    {
        private string _mimeType;

        public TextMetadataSampleEntry() : base("Text Metadata Sample Entry")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _mimeType = input.readUTFString((int)GetLeft(input), MP4InputStream.UTF8);
        }

        /**
         * Provides a MIME type which identifies the content format of the timed
         * metadata. Examples for this field are 'text/html' and 'text/plain'.
         * 
         * @return the content's MIME type
         */
        public string GetMimeType()
        {
            return _mimeType;
        }
    }
}
