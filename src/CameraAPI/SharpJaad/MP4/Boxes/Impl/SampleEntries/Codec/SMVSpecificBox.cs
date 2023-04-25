namespace SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec
{
    public class SMVSpecificBox : CodecSpecificBox
    {
        private int _framesPerSample;

        public SMVSpecificBox() : base("SMV Specific Structure")
        {  }

        public override void Decode(MP4InputStream input)
        {
            DecodeCommon(input);

            _framesPerSample = input.Read();
        }

        public int GetFramesPerSample()
        {
            return _framesPerSample;
        }
    }
}