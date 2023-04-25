namespace SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec
{
    public class EVRCSpecificBox : CodecSpecificBox
    {
        private int _framesPerSample;

        public EVRCSpecificBox() : base("EVCR Specific Box")
        { }

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
