using SharpJaad.MP4.API.DRM;
using SharpJaad.MP4.Boxes;
using SharpJaad.MP4.Boxes.Impl;

namespace SharpJaad.MP4.API
{
    /**
     * This class contains information about a DRM system.
     */
    public abstract class Protection
    {
        public enum Scheme
        {
            ITUNES_FAIR_PLAY = 1769239918,
            UNKNOWN = -1
        }

        public static Protection parse(Box sinf)
        {
            Protection p = null;
            if (sinf.HasChild(BoxTypes.SCHEME_TYPE_BOX))
            {
                SchemeTypeBox schm = (SchemeTypeBox)sinf.GetChild(BoxTypes.SCHEME_TYPE_BOX);
                long l = schm.GetSchemeType();
                if (l == Scheme.ITUNES_FAIR_PLAY.type) p = new ITunesProtection(sinf);
            }

            if (p == null) p = new UnknownProtection(sinf);
            return p;
        }

        private Codec originalFormat;

        protected Protection(Box sinf)
        {
            //original format
            long type = ((OriginalFormatBox)sinf.GetChild(BoxTypes.ORIGINAL_FORMAT_BOX)).GetOriginalFormat();
            Codec c;
            //TODO: currently it tests for audio and video codec, can do this any other way?
            if (!(c = AudioTrack.ForType(type)).equals(AudioTrack.AudioCodec.UNKNOWN_AUDIO_CODEC)) originalFormat = c;
            else if (!(c = VideoTrack.forType(type)).equals(VideoTrack.VideoCodec.UNKNOWN_VIDEO_CODEC)) originalFormat = c;
            else originalFormat = null;
        }

        public Codec getOriginalFormat()
        {
            return originalFormat;
        }

        public abstract Scheme GetScheme();

        //default implementation for unknown protection schemes
        private class UnknownProtection : Protection
        {
            public UnknownProtection(Box sinf) : base(sinf)
            { }

            public override Scheme GetScheme()
            {
                return Scheme.UNKNOWN;
            }
        }
    }
}