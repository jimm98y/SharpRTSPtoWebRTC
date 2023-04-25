namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * In some streams the media samples do not occupy all bits of the bytes given
     * by the sample size, and are padded at the end to a byte boundary. In some
     * cases, it is necessary to record externally the number of padding bits used.
     * This table supplies that information.
     * 
     * @author in-somnia
     */
    public class PaddingBitBox : FullBox
    {
        private int[] _pad1, _pad2;

        public PaddingBitBox() : base("Padding Bit Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            int sampleCount = (int)(input.ReadBytes(4) + 1) / 2;
            _pad1 = new int[sampleCount];
            _pad2 = new int[sampleCount];

            byte b;
            for (int i = 0; i < sampleCount; i++)
            {
                b = (byte)input.Read();
                //1 bit reserved
                //3 bits pad1
                _pad1[i] = (b >> 4) & 7;
                //1 bit reserved
                //3 bits pad2
                _pad2[i] = b & 7;
            }
        }

        /**
         * Integer values from 0 to 7, indicating the number of bits at the end of
         * sample (i*2)+1.
         */
        public int[] GetPad1()
        {
            return _pad1;
        }

        /**
         * Integer values from 0 to 7, indicating the number of bits at the end of
         * sample (i*2)+2.
         */
        public int[] GetPad2()
        {
            return _pad2;
        }
    }
}