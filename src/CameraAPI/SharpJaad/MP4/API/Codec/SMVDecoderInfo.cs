using SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec;

namespace SharpJaad.MP4.API.Codec
{
    public class SMVDecoderInfo : DecoderInfo
    {
        private SMVSpecificBox box;

        public SMVDecoderInfo(CodecSpecificBox box)
        {
            this.box = (SMVSpecificBox)box;
        }

        public int GetDecoderVersion()
        {
            return box.GetDecoderVersion();
        }

        public long GetVendor()
        {
            return box.GetVendor();
        }

        public int GetFramesPerSample()
        {
            return box.GetFramesPerSample();
        }
    }
}
