namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class ThreeGPPRecordingYearBox : FullBox
    {
        private int _year;

        public ThreeGPPRecordingYearBox() : base("3GPP Recording Year Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _year = (int)input.ReadBytes(2);
        }

        public int GetYear()
        {
            return _year;
        }
    }
}
