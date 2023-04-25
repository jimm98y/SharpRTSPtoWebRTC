namespace SharpJaad.MP4.Boxes.Impl.SampleEntries
{
    public class AudioSampleEntry : SampleEntry
    {
        private int _channelCount, _sampleSize, _sampleRate;

        public AudioSampleEntry(string name) : base(name)
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            input.skipBytes(8); //reserved
            _channelCount = (int)input.readBytes(2);
            _sampleSize = (int)input.readBytes(2);
            input.skipBytes(2); //pre-defined: 0
            input.skipBytes(2); //reserved
            _sampleRate = (int)input.readBytes(2);
            input.skipBytes(2); //not used by samplerate

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