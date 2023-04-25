using SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec;

namespace SharpJaad.MP4.API.Codec
{
    public class EVRCDecoderInfo : DecoderInfo
    {
        private EVRCSpecificBox box;

        public EVRCDecoderInfo(CodecSpecificBox box)
        {
            this.box = (EVRCSpecificBox)box;
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
