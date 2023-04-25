namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * The Scheme Type Box identifies the protection scheme.
     * 
     * @author in-somnia
     */
    public class SchemeTypeBox : FullBox
    {
        public const long ITUNES_SCHEME = 1769239918; //itun
        private long _schemeType, _schemeVersion;
        private string _schemeURI;

        public SchemeTypeBox() : base("Scheme Type Box")
        {  }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _schemeType = input.ReadBytes(4);
            _schemeVersion = input.ReadBytes(4);
            _schemeURI = ((_flags & 1) == 1) ? input.ReadUTFString((int)GetLeft(input), MP4InputStream.UTF8) : null;
        }

        /**
         * The scheme type is the code defining the protection scheme.
         *
         * @return the scheme type
         */
        public long GetSchemeType()
        {
            return _schemeType;
        }

        /**
         * The scheme version is the version of the scheme used to create the
         * content.
         *
         * @return the scheme version
         */
        public long GetSchemeVersion()
        {
            return _schemeVersion;
        }

        /**
         * The optional scheme URI allows for the option of directing the user to a
         * web-page if they do not have the scheme installed on their system. It is
         * an absolute URI.
         * If the scheme URI is not present, this method returns null.
         *
         * @return the scheme URI or null, if no URI is present
         */
        public string GetSchemeURI()
        {
            return _schemeURI;
        }
    }
}
