namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    //TODO: use nio ByteBuffer instead of array
    public class ID3TagBox : FullBox
    {
        private string _language;
        private byte[] _id3Data;

        public ID3TagBox() : base("ID3 Tag Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _language = Utils.GetLanguageCode(input.ReadBytes(2));

            _id3Data = new byte[(int)GetLeft(input)];
            input.ReadBytes(_id3Data);
        }

        public byte[] GetID3Data()
        {
            return _id3Data;
        }

        /**
         * The language code for the following text. See ISO 639-2/T for the set of
         * three character codes.
         */
        public string GetLanguage()
        {
            return _language;
        }
    }
}
