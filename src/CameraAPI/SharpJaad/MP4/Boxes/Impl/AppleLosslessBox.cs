namespace SharpJaad.MP4.Boxes.Impl
{
    public class AppleLosslessBox : FullBox
    {
        private long _maxSamplePerFrame, _maxCodedFrameSize, _bitRate, _sampleRate;
        private int _sampleSize, _historyMult, _initialHistory, _kModifier, _channels;

        public AppleLosslessBox() : base("Apple Lossless Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _maxSamplePerFrame = input.ReadBytes(4);
            input.SkipBytes(1); //?
            _sampleSize = input.Read();
            _historyMult = input.Read();
            _initialHistory = input.Read();
            _kModifier = input.Read();
            _channels = input.Read();
            input.SkipBytes(2); //?
            _maxCodedFrameSize = input.ReadBytes(4);
            _bitRate = input.ReadBytes(4);
            _sampleRate = input.ReadBytes(4);
        }

        public long GetMaxSamplePerFrame()
        {
            return _maxSamplePerFrame;
        }

        public int GetSampleSize()
        {
            return _sampleSize;
        }

        public int GetHistoryMult()
        {
            return _historyMult;
        }

        public int GetInitialHistory()
        {
            return _initialHistory;
        }

        public int GetkModifier()
        {
            return _kModifier;
        }

        public int GetChannels()
        {
            return _channels;
        }

        public long GetMaxCodedFrameSize()
        {
            return _maxCodedFrameSize;
        }

        public long GetBitRate()
        {
            return _bitRate;
        }

        public long GetSampleRate()
        {
            return _sampleRate;
        }
    }
}