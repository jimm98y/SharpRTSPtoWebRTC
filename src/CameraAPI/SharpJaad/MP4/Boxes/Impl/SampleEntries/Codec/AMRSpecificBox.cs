namespace SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec
{
    public class AMRSpecificBox : CodecSpecificBox
    {
        private int modeSet, modeChangePeriod, framesPerSample;

        public AMRSpecificBox() : base("AMR Specific Box")
        { }

        public override void decode(MP4InputStream input)
        {
            DecodeCommon(input);

            modeSet = (int)input.readBytes(2);
            modeChangePeriod = input.read();
            framesPerSample = input.read();
        }

        public int GetModeSet()
        {
            return modeSet;
        }

        public int GetModeChangePeriod()
        {
            return modeChangePeriod;
        }

        public int GetFramesPerSample()
        {
            return framesPerSample;
        }
    }
}
