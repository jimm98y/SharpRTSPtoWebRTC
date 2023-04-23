namespace SharpJaad.AAC.Syntax
{
    public class DSE : Element
    {
        private byte[] _dataStreamBytes;

        public void Decode(BitStream input)
        {
            ReadElementInstanceTag(input);

            bool byteAlign = input.ReadBool();
            int count = input.ReadBits(8);
            if (count == 255) count += input.ReadBits(8);

            if (byteAlign) input.ByteAlign();

            _dataStreamBytes = new byte[count];
            for (int i = 0; i < count; i++)
            {
                _dataStreamBytes[i] = (byte)input.ReadBits(8);
            }
        }
    }
}
