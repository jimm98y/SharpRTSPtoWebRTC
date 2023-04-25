namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class ITunesMetadataMeanBox : FullBox
    {
        private string _domain;

        public ITunesMetadataMeanBox() : base("iTunes Metadata Mean Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _domain = input.readString((int)GetLeft(input));
        }

        public string getDomain()
        {
            return _domain;
        }
    }
}
