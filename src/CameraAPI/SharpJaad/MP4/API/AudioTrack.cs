using SharpJaad.MP4.Boxes;
using SharpJaad.MP4.Boxes.Impl;
using SharpJaad.MP4.Boxes.Impl.SampleEntries;
using SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec;

namespace SharpJaad.MP4.API
{
    public class AudioTrack : Track
    {
        public enum AudioCodec
        {
            AAC,
		    AC3,
		    AMR,
		    AMR_WIDE_BAND,
		    EVRC,
		    EXTENDED_AC3,
		    QCELP,
		    SMV,
		    UNKNOWN_AUDIO_CODEC
        }

        public static AudioCodec ForType(long type)
        {
            AudioCodec ac;
            if (type == BoxTypes.MP4A_SAMPLE_ENTRY) ac = AudioCodec.AAC;
            else if (type == BoxTypes.AC3_SAMPLE_ENTRY) ac = AudioCodec.AC3;
            else if (type == BoxTypes.AMR_SAMPLE_ENTRY) ac = AudioCodec.AMR;
            else if (type == BoxTypes.AMR_WB_SAMPLE_ENTRY) ac = AudioCodec.AMR_WIDE_BAND;
            else if (type == BoxTypes.EVRC_SAMPLE_ENTRY) ac = AudioCodec.EVRC;
            else if (type == BoxTypes.EAC3_SAMPLE_ENTRY) ac = AudioCodec.EXTENDED_AC3;
            else if (type == BoxTypes.QCELP_SAMPLE_ENTRY) ac = AudioCodec.QCELP;
            else if (type == BoxTypes.SMV_SAMPLE_ENTRY) ac = AudioCodec.SMV;
            else ac = AudioCodec.UNKNOWN_AUDIO_CODEC;
            return ac;
        }

        private readonly SoundMediaHeaderBox _smhd;
        private readonly AudioSampleEntry _sampleEntry;
        private AudioCodec _codec;

        public AudioTrack(Box trak, MP4InputStream input) : base(trak, input)
        {
            Box mdia = trak.GetChild(BoxTypes.MEDIA_BOX);
            Box minf = mdia.GetChild(BoxTypes.MEDIA_INFORMATION_BOX);
            _smhd = (SoundMediaHeaderBox)minf.GetChild(BoxTypes.SOUND_MEDIA_HEADER_BOX);

            Box stbl = minf.GetChild(BoxTypes.SAMPLE_TABLE_BOX);

            //sample descriptions: 'mp4a' and 'enca' have an ESDBox, all others have a CodecSpecificBox
            SampleDescriptionBox stsd = (SampleDescriptionBox)stbl.GetChild(BoxTypes.SAMPLE_DESCRIPTION_BOX);
            if (stsd.GetChildren()[0] is AudioSampleEntry) 
            {
                _sampleEntry = (AudioSampleEntry)stsd.GetChildren()[0];
                long type = _sampleEntry.GetBoxType();
                if (_sampleEntry.HasChild(BoxTypes.ESD_BOX)) FindDecoderSpecificInfo((ESDBox)_sampleEntry.GetChild(BoxTypes.ESD_BOX));
                else _decoderInfo = DecoderInfo.Parse((CodecSpecificBox)_sampleEntry.GetChildren()[0]);

                if (type == BoxTypes.ENCRYPTED_AUDIO_SAMPLE_ENTRY || type == BoxTypes.DRMS_SAMPLE_ENTRY)
                {
                    FindDecoderSpecificInfo((ESDBox)_sampleEntry.GetChild(BoxTypes.ESD_BOX));
                    _protection = Protection.Parse(_sampleEntry.GetChild(BoxTypes.PROTECTION_SCHEME_INFORMATION_BOX));
                    _codec = (AudioCodec)_protection.GetOriginalFormat();
                }
                else
                {
                    _codec = ForType(_sampleEntry.GetBoxType());
                }
            }
            else
            {
                _sampleEntry = null;
                _codec = AudioCodec.UNKNOWN_AUDIO_CODEC;
            }
	    }
        
        public override Type GetTrackType()
        {
            return Type.AUDIO;
        }

        public override System.Enum GetCodec()
        {
            return _codec;
        }

        /**
         * The balance is a floating-point number that places mono audio tracks in a
         * stereo space: 0 is centre (the normal value), full left is -1.0 and full
         * right is 1.0.
         *
         * @return the stereo balance for a this track
         */
        public double GetBalance()
        {
            return _smhd.GetBalance();
        }

        /**
         * Returns the number of channels in this audio track.
         * @return the number of channels
         */
        public int GetChannelCount()
        {
            return _sampleEntry.GetChannelCount();
        }

        /**
         * Returns the sample rate of this audio track.
         * @return the sample rate
         */
        public int GetSampleRate()
        {
            return _sampleEntry.GetSampleRate();
        }

        /**
         * Returns the sample size in bits for this track.
         * @return the sample size
         */
        public int GetSampleSize()
        {
            return _sampleEntry.GetSampleSize();
        }

        public double GetVolume()
        {
            return _tkhd.GetVolume();
        }
    }
}
