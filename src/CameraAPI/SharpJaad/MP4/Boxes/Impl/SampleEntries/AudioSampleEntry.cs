namespace SharpJaad.MP4.Boxes.Impl.SampleEntries
{
    public class AudioSampleEntry : SampleEntry
    {
        private int _channelCount, _sampleSize, _sampleRate;

        public AudioSampleEntry(string name) : base(name)
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            input.SkipBytes(8); //reserved
            _channelCount = (int)input.ReadBytes(2);
            _sampleSize = (int)input.ReadBytes(2);
            input.SkipBytes(2); //pre-defined: 0
            input.SkipBytes(2); //reserved
            _sampleRate = (int)input.ReadBytes(2);
            input.SkipBytes(2); //not used by samplerate

            ReadChildren(input);
        }

        public int GetChannelCount()
        {
            return _channelCount;
        }

        public int GetSampleRate()
        {
            return _sampleRate;
        }

        public int GetSampleSize()
        {
            return _sampleSize;
        }
    }
}