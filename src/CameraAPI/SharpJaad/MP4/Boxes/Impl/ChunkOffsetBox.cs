namespace SharpJaad.MP4.Boxes.Impl
{
    public class ChunkOffsetBox : FullBox
    {
        private long[] _chunks;

        public ChunkOffsetBox() : base("Chunk Offset Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            int len = (_type == BoxTypes.CHUNK_LARGE_OFFSET_BOX) ? 8 : 4;
            int entryCount = (int)input.ReadBytes(4);
            _chunks = new long[entryCount];

            for (int i = 0; i < entryCount; i++)
            {
                _chunks[i] = input.ReadBytes(len);
            }
        }

        public long[] GetChunks()
        {
            return _chunks;
        }
    }
}
