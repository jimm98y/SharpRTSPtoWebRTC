using SharpJaad.MP4.Boxes;
using SharpJaad.MP4.Boxes.Impl;
using SharpJaad.MP4.Boxes.Impl.SampleEntries;
using SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec;

namespace SharpJaad.MP4.API
{
    public class VideoTrack : Track
    {
        public enum VideoCodec : Codec
        {
            AVC,
            H263,
            MP4_ASP,
            UNKNOWN_VIDEO_CODEC
        }

        public static VideoCodec forType(long type)
        {
            VideoCodec ac;
            if (type == BoxTypes.AVC_SAMPLE_ENTRY) ac = VideoCodec.AVC;
            else if (type == BoxTypes.H263_SAMPLE_ENTRY) ac = VideoCodec.H263;
            else if (type == BoxTypes.MP4V_SAMPLE_ENTRY) ac = VideoCodec.MP4_ASP;
            else ac = VideoCodec.UNKNOWN_VIDEO_CODEC;
            return ac;
        }

        private readonly VideoMediaHeaderBox vmhd;
        private readonly VideoSampleEntry sampleEntry;
        private readonly Codec codec;

        public VideoTrack(Box trak, MP4InputStream input) : base(trak, input)
        {
            Box minf = trak.GetChild(BoxTypes.MEDIA_BOX).GetChild(BoxTypes.MEDIA_INFORMATION_BOX);
            vmhd = (VideoMediaHeaderBox)minf.GetChild(BoxTypes.VIDEO_MEDIA_HEADER_BOX);

            Box stbl = minf.GetChild(BoxTypes.SAMPLE_TABLE_BOX);

            //sample descriptions: 'mp4v' has an ESDBox, all others have a CodecSpecificBox
            SampleDescriptionBox stsd = (SampleDescriptionBox)stbl.GetChild(BoxTypes.SAMPLE_DESCRIPTION_BOX);
            if (stsd.GetChildren()[0] is VideoSampleEntry) 
            {
                sampleEntry = (VideoSampleEntry)stsd.GetChildren()[0];
                long type = sampleEntry.getType();
                if (type == BoxTypes.MP4V_SAMPLE_ENTRY) findDecoderSpecificInfo((ESDBox)sampleEntry.GetChild(BoxTypes.ESD_BOX));
                else if (type == BoxTypes.ENCRYPTED_VIDEO_SAMPLE_ENTRY || type == BoxTypes.DRMS_SAMPLE_ENTRY)
                {
                    findDecoderSpecificInfo((ESDBox)sampleEntry.GetChild(BoxTypes.ESD_BOX));
                    protection = Protection.parse(sampleEntry.GetChild(BoxTypes.PROTECTION_SCHEME_INFORMATION_BOX));
                }
                else decoderInfo = DecoderInfo.Parse((CodecSpecificBox)sampleEntry.GetChildren().get(0));

                codec = forType(sampleEntry.getType());
            }
            else
            {
                sampleEntry = null;
                codec = VideoCodec.UNKNOWN_VIDEO_CODEC;
            }
        }

        public override Type getType()
        {
            return Type.VIDEO;
        }

        public override Codec GetCodec()
        {
            return codec;
        }

        public int GetWidth()
        {
            return (sampleEntry != null) ? sampleEntry.GetWidth() : 0;
        }

        public int GetHeight()
        {
            return (sampleEntry != null) ? sampleEntry.GetHeight() : 0;
        }

        public double GetHorizontalResolution()
        {
            return (sampleEntry != null) ? sampleEntry.GetHorizontalResolution() : 0;
        }

        public double GetVerticalResolution()
        {
            return (sampleEntry != null) ? sampleEntry.GetVerticalResolution() : 0;
        }

        public int GetFrameCount()
        {
            return (sampleEntry != null) ? sampleEntry.GetFrameCount() : 0;
        }

        public string GetCompressorName()
        {
            return (sampleEntry != null) ? sampleEntry.GetCompressorName() : "";
        }

        public int GetDepth()
        {
            return (sampleEntry != null) ? sampleEntry.GetDepth() : 0;
        }

        public int GetLayer()
        {
            return tkhd.getLayer();
        }
    }
}
