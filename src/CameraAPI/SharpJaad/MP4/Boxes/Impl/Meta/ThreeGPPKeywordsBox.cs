namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class ThreeGPPKeywordsBox : ThreeGPPMetadataBox
    {
        private string[] keywords;

        public ThreeGPPKeywordsBox() : base("3GPP Keywords Box")
        { }

        public override void decode(MP4InputStream input)
        {
            DecodeCommon(input);

            int count = input.read();
            keywords = new string[count];

            int len;
            for (int i = 0; i < count; i++) 
            {
                len = input.read();
                keywords[i] = input.readUTFString(len);
            }
        }

        public string[] GetKeywords()
        {
            return keywords;
        }
    }
}
