using SharpJaad.MP4.Boxes;
using SharpJaad.MP4.Boxes.Impl;
using SharpJaad.MP4.Boxes.Impl.SampleEntries;
using SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec;

namespace SharpJaad.MP4.API
{
    public class VideoTrack : Track
    {
        public enum VideoCodec
        {
            AVC,
            H263,
            MP4_ASP,
            UNKNOWN_VIDEO_CODEC
        }

        public static VideoCodec ForType(long type)
        {
            VideoCodec ac;
            if (type == BoxTypes.AVC_SAMPLE_ENTRY) ac = VideoCodec.AVC;
            else if (type == BoxTypes.H263_SAMPLE_ENTRY) ac = VideoCodec.H263;
            else if (type == BoxTypes.MP4V_SAMPLE_ENTRY) ac = VideoCodec.MP4_ASP;
            else ac = VideoCodec.UNKNOWN_VIDEO_CODEC;
            return ac;
        }

        private readonly VideoMediaHeaderBox _vmhd;
        private readonly VideoSampleEntry _sampleEntry;
        private readonly VideoCodec _codec;

        public VideoTrack(Box trak, MP4InputStream input) : base(trak, input)
        {
            Box minf = trak.GetChild(BoxTypes.MEDIA_BOX).GetChild(BoxTypes.MEDIA_INFORMATION_BOX);
            _vmhd = (VideoMediaHeaderBox)minf.GetChild(BoxTypes.VIDEO_MEDIA_HEADER_BOX);

            Box stbl = minf.GetChild(BoxTypes.SAMPLE_TABLE_BOX);

            //sample descriptions: 'mp4v' has an ESDBox, all others have a CodecSpecificBox
            SampleDescriptionBox stsd = (SampleDescriptionBox)stbl.GetChild(BoxTypes.SAMPLE_DESCRIPTION_BOX);
            if (stsd.GetChildren()[0] is VideoSampleEntry) 
            {
                _sampleEntry = (VideoSampleEntry)stsd.GetChildren()[0];
                long type = _sampleEntry.GetBoxType();
                if (type == BoxTypes.MP4V_SAMPLE_ENTRY) FindDecoderSpecificInfo((ESDBox)_sampleEntry.GetChild(BoxTypes.ESD_BOX));
                else if (type == BoxTypes.ENCRYPTED_VIDEO_SAMPLE_ENTRY || type == BoxTypes.DRMS_SAMPLE_ENTRY)
                {
                    FindDecoderSpecificInfo((ESDBox)_sampleEntry.GetChild(BoxTypes.ESD_BOX));
                    _protection = Protection.Parse(_sampleEntry.GetChild(BoxTypes.PROTECTION_SCHEME_INFORMATION_BOX));
                }
                else _decoderInfo = DecoderInfo.Parse((CodecSpecificBox)_sampleEntry.GetChildren()[0]);

                _codec = ForType(_sampleEntry.GetBoxType());
            }
            else
            {
                _sampleEntry = null;
                _codec = VideoCodec.UNKNOWN_VIDEO_CODEC;
            }
        }

        public override Type GetTrackType()
        {
            return Type.VIDEO;
        }

        public override System.Enum GetCodec()
        {
            return _codec;
        }

        public int GetWidth()
        {
            return (_sampleEntry != null) ? _sampleEntry.GetWidth() : 0;
        }

        public int GetHeight()
        {
            return (_sampleEntry != null) ? _sampleEntry.GetHeight() : 0;
        }

        public double GetHorizontalResolution()
        {
            return (_sampleEntry != null) ? _sampleEntry.GetHorizontalResolution() : 0;
        }

        public double GetVerticalResolution()
        {
            return (_sampleEntry != null) ? _sampleEntry.GetVerticalResolution() : 0;
        }

        public int GetFrameCount()
        {
            return (_sampleEntry != null) ? _sampleEntry.GetFrameCount() : 0;
        }

        public string GetCompressorName()
        {
            return (_sampleEntry != null) ? _sampleEntry.GetCompressorName() : "";
        }

        public int GetDepth()
        {
            return (_sampleEntry != null) ? _sampleEntry.GetDepth() : 0;
        }

        public int GetLayer()
        {
            return _tkhd.GetLayer();
        }
    }
}
