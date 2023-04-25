using SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec;

namespace SharpJaad.MP4.API.Codec
{
    public class H263DecoderInfo : DecoderInfo
    {
        private H263SpecificBox box;

        public H263DecoderInfo(CodecSpecificBox box)
        {
            this.box = (H263SpecificBox)box;
        }

        public int GetDecoderVersion()
        {
            return box.GetDecoderVersion();
        }

        public long GetVendor()
        {
            return box.GetVendor();
        }

        public int GetLevel()
        {
            return box.GetLevel();
        }

        public int GetProfile()
        {
            return box.GetProfile();
        }
    }
}
