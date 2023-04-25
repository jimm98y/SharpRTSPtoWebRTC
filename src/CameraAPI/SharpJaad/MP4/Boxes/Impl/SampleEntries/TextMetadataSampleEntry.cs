namespace SharpJaad.MP4.Boxes.Impl.SampleEntries
{
    public class TextMetadataSampleEntry : MetadataSampleEntry
    {
        private string _mimeType;

        public TextMetadataSampleEntry() : base("Text Metadata Sample Entry")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _mimeType = input.ReadUTFString((int)GetLeft(input), MP4InputStream.UTF8);
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
