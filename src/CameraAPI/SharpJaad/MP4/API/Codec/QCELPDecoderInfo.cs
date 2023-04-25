using SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec;

namespace SharpJaad.MP4.API.Codec
{
    public class QCELPDecoderInfo : DecoderInfo
    {
        private QCELPSpecificBox box;

        public QCELPDecoderInfo(CodecSpecificBox box)
        {
            this.box = (QCELPSpecificBox)box;
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
