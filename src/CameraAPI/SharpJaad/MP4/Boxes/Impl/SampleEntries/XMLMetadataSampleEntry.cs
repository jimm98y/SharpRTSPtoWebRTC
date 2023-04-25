namespace SharpJaad.MP4.Boxes.Impl.SampleEntries
{
    public class XMLMetadataSampleEntry : MetadataSampleEntry
    {
        private string _ns, _schemaLocation;

        public XMLMetadataSampleEntry() : base("XML Metadata Sample Entry")
        {  }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _ns = input.readUTFString((int)GetLeft(input), MP4InputStream.UTF8);
            _schemaLocation = input.readUTFString((int)GetLeft(input), MP4InputStream.UTF8);
        }

        /**
         * Gives the namespace of the schema for the timed XML metadata. This is
         * needed for identifying the type of metadata, e.g. gBSD or AQoS
         * (MPEG-21-7) and for decoding using XML aware encoding mechanisms such as
         * BiM.
         * @return the namespace
         */
        public string GetNamespace()
        {
            return _ns;
        }

        /**
         * Optionally provides an URL to find the schema corresponding to the
         * namespace. This is needed for decoding of the timed metadata by XML aware
         * encoding mechanisms such as BiM.
         * @return the schema's URL
         */
        public string GetSchemaLocation()
        {
            return _schemaLocation;
        }
    }
}
