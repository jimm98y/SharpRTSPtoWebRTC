namespace SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec
{
    public class AMRSpecificBox : CodecSpecificBox
    {
        private int modeSet, modeChangePeriod, framesPerSample;

        public AMRSpecificBox() : base("AMR Specific Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            DecodeCommon(input);

            modeSet = (int)input.ReadBytes(2);
            modeChangePeriod = input.Read();
            framesPerSample = input.Read();
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
