namespace SharpJaad.MP4.Boxes.Impl
{
    public class AppleLosslessBox : FullBox
    {
        private long _maxSamplePerFrame, _maxCodedFrameSize, _bitRate, _sampleRate;
        private int _sampleSize, _historyMult, _initialHistory, _kModifier, _channels;

        public AppleLosslessBox() : base("Apple Lossless Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _maxSamplePerFrame = input.readBytes(4);
            input.skipBytes(1); //?
            _sampleSize = input.read();
            _historyMult = input.read();
            _initialHistory = input.read();
            _kModifier = input.read();
            _channels = input.read();
            input.skipBytes(2); //?
            _maxCodedFrameSize = input.readBytes(4);
            _bitRate = input.readBytes(4);
            _sampleRate = input.readBytes(4);
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