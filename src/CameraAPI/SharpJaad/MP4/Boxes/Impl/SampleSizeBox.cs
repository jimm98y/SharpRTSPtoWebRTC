using SharpJaad.AAC.Tools;

namespace SharpJaad.MP4.Boxes.Impl
{
    public class SampleSizeBox : FullBox
    {
        private long _sampleCount;
        private long[] _sampleSizes;

        public SampleSizeBox() : base("Sample Size Box")
        {  }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            bool compact = type == BoxTypes.COMPACT_SAMPLE_SIZE_BOX;

            int sampleSize;
            if (compact) {
                input.skipBytes(3);
                sampleSize = input.read();
            }
            else sampleSize = (int)input.readBytes(4);

            _sampleCount = input.readBytes(4);
            _sampleSizes = new long[(int)_sampleCount];

            if (compact)
            {
                //compact: sampleSize can be 4, 8 or 16 bits
                if (sampleSize == 4)
                {
                    int x;
                    for (int i = 0; i < _sampleCount; i += 2)
                    {
                        x = input.read();
                        _sampleSizes[i] = (x >> 4) & 0xF;
                        _sampleSizes[i + 1] = x & 0xF;
                    }
                }
                else
                {
                    ReadSizes(input, sampleSize / 8);
                }
            }
            else if (sampleSize == 0)
            {
                ReadSizes(input, 4);
            }
            else
            {
                Arrays.Fill(_sampleSizes, sampleSize);
            }
        }

        private void ReadSizes(MP4InputStream input, int len)
        {
            for (int i = 0; i < _sampleCount; i++) 
            {
                _sampleSizes[i] = input.readBytes(len);
            }
        }

        public int GetSampleCount()
        {
            return (int)_sampleCount;
        }

        public long[] GetSampleSizes()
        {
            return _sampleSizes;
        }
    }
}
