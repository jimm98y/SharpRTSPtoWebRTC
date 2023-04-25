namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    /**
     * This box is used in custom metadata tags (within the box-type '----'). It
     * contains the name of the custom tag, whose data is stored in the 'data'-box.
     *
     * @author in-somnia
     */
    public class ITunesMetadataNameBox : FullBox
    {
        private string _metaName;

        public ITunesMetadataNameBox() : base("iTunes Metadata Name Box")
        {

        }

        public override void Decode(MP4InputStream input)
        {
            base.decode(input);

            _metaName = input.readString((int)GetLeft(input));
        }

        public string getMetaName()
        {
            return _metaName;
        }
    }
}
