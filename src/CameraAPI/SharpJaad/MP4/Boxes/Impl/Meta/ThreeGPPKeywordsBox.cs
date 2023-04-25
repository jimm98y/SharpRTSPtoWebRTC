namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class ThreeGPPKeywordsBox : ThreeGPPMetadataBox
    {
        private string[] _keywords;

        public ThreeGPPKeywordsBox() : base("3GPP Keywords Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            DecodeCommon(input);

            int count = input.Read();
            _keywords = new string[count];

            int len;
            for (int i = 0; i < count; i++) 
            {
                len = input.Read();
                _keywords[i] = input.ReadUTFString(len);
            }
        }

        public string[] GetKeywords()
        {
            return _keywords;
        }
    }
}
