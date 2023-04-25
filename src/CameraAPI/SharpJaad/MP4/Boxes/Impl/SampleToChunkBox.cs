namespace SharpJaad.MP4.Boxes.Impl
{
    public class SampleToChunkBox : FullBox
    {
        private long[] _firstChunks, _samplesPerChunk, _sampleDescriptionIndex;

        public SampleToChunkBox() : base("Sample To Chunk Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            int entryCount = (int)input.readBytes(4);
            _firstChunks = new long[entryCount];
            _samplesPerChunk = new long[entryCount];
            _sampleDescriptionIndex = new long[entryCount];

            for (int i = 0; i < entryCount; i++)
            {
                _firstChunks[i] = input.readBytes(4);
                _samplesPerChunk[i] = input.readBytes(4);
                _sampleDescriptionIndex[i] = input.readBytes(4);
            }
        }

        public long[] GetFirstChunks()
        {
            return _firstChunks;
        }

        public long[] GetSamplesPerChunk()
        {
            return _samplesPerChunk;
        }

        public long[] GetSampleDescriptionIndex()
        {
            return _sampleDescriptionIndex;
        }
    }
}
