namespace SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec
{
    public class QCELPSpecificBox : CodecSpecificBox
    {
        private int _framesPerSample;

        public QCELPSpecificBox() : base("QCELP Specific Box")
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
