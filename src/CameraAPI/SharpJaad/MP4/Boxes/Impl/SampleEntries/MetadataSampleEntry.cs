namespace SharpJaad.MP4.Boxes.Impl.SampleEntries
{
    public abstract class MetadataSampleEntry : SampleEntry
    {
        private string _contentEncoding;

        public MetadataSampleEntry(string name) : base(name)
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _contentEncoding = input.ReadUTFString((int)GetLeft(input), MP4InputStream.UTF8);
        }

        /**
         * A string providing a MIME type which identifies the content encoding of
         * the timed metadata. If not present (an empty string is supplied) the
         * timed metadata is not encoded.
         * An example for this field is 'application/zip'.
         * @return the encoding's MIME-type
         */
        public string GetContentEncoding()
        {
            return _contentEncoding;
        }
    }
}
