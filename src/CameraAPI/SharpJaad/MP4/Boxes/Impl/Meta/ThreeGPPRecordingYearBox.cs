namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class ThreeGPPRecordingYearBox : FullBox
    {
        private int _year;

        public ThreeGPPRecordingYearBox() : base("3GPP Recording Year Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _year = (int)input.readBytes(2);
        }

        public int GetYear()
        {
            return _year;
        }
    }
}
