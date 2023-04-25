namespace SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec
{
    public class SMVSpecificBox : CodecSpecificBox
    {
        private int _framesPerSample;

        public SMVSpecificBox() : base("SMV Specific Structure")
        {  }

        public override void decode(MP4InputStream input)
        {
            DecodeCommon(input);

            _framesPerSample = input.read();
        }

        public int GetFramesPerSample()
        {
            return _framesPerSample;
        }
    }
}