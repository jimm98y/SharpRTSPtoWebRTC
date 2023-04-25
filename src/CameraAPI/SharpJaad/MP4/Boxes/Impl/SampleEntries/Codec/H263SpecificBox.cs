namespace SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec
{
    public class H263SpecificBox : CodecSpecificBox
    {
        private int _level, _profile;

        public H263SpecificBox() : base("H.263 Specific Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            DecodeCommon(input);

            _level = input.Read();
            _profile = input.Read();
        }

        public int GetLevel()
        {
            return _level;
        }

        public int GetProfile()
        {
            return _profile;
        }
    }
}
