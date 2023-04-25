using System.Collections.Generic;

namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class NeroMetadataTagsBox : BoxImpl
    {
        private readonly Dictionary<string, string> _pairs;

        public NeroMetadataTagsBox() : base("Nero Metadata Tags Box")
        {
            _pairs = new Dictionary<string, string>();
        }

        public override void decode(MP4InputStream input)
        {
            input.skipBytes(12); //meta box

            string key, val;
            int len;
            //TODO: what are the other skipped fields for?
            while (GetLeft(input) > 0 && input.read() == 0x80)
            {
                input.skipBytes(2); //x80 x00 x06/x05
                key = input.readUTFString((int)GetLeft(input), MP4InputStream.UTF8);
                input.skipBytes(4); //0x00 0x01 0x00 0x00 0x00
                len = input.read();
                val = input.readString(len);
                _pairs.Add(key, val);
            }
        }

        public Dictionary<string, string> GetPairs()
        {
            return _pairs;
        }
    }
}
