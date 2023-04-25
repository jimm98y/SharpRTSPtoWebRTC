namespace SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec
{
    //defined in ISO 14496-15 as 'AVC Configuration Record'
    public class AVCSpecificBox : CodecSpecificBox
    {
        private int _configurationVersion, _profile, _level, _lengthSize;
        private byte _profileCompatibility;
        private byte[][] _sequenceParameterSetNALUnit, _pictureParameterSetNALUnit;

        public AVCSpecificBox() : base("AVC Specific Box")
        { }

        public override void decode(MP4InputStream input)
        {
            _configurationVersion = input.read();
            _profile = input.read();
            _profileCompatibility = (byte)input.read();
            _level = input.read();
            //6 bits reserved, 2 bits 'length size minus one'
            _lengthSize = (input.read() & 3) + 1;

            int len;
            //3 bits reserved, 5 bits number of sequence parameter sets
            int sequenceParameterSets = input.read() & 31;

            _sequenceParameterSetNALUnit = new byte[sequenceParameterSets][];
            for (int i = 0; i < sequenceParameterSets; i++)
            {
                len = (int)input.readBytes(2);
                _sequenceParameterSetNALUnit[i] = new byte[len];
                input.readBytes(_sequenceParameterSetNALUnit[i]);
            }

            int pictureParameterSets = input.read();

            _pictureParameterSetNALUnit = new byte[pictureParameterSets][];
            for (int i = 0; i < pictureParameterSets; i++)
            {
                len = (int)input.readBytes(2);
                _pictureParameterSetNALUnit[i] = new byte[len];
                input.readBytes(_pictureParameterSetNALUnit[i]);
            }
        }

        public int GetConfigurationVersion()
        {
            return _configurationVersion;
        }

        /**
         * The AVC profile code as defined in ISO/IEC 14496-10.
         *
         * @return the AVC profile
         */
        public int GetProfile()
        {
            return _profile;
        }

        /**
         * The profileCompatibility is a byte defined exactly the same as the byte
         * which occurs between the profileIDC and levelIDC in a sequence parameter
         * set (SPS), as defined in ISO/IEC 14496-10.
         *
         * @return the profile compatibility byte
         */
        public byte GetProfileCompatibility()
        {
            return _profileCompatibility;
        }

        public int GetLevel()
        {
            return _level;
        }

        /**
         * The length in bytes of the NALUnitLength field in an AVC video sample or
         * AVC parameter set sample of the associated stream. The value of this
         * field 1, 2, or 4 bytes.
         *
         * @return the NALUnitLength length in bytes
         */
        public int GetLengthSize()
        {
            return _lengthSize;
        }

        /**
         * The SPS NAL units, as specified in ISO/IEC 14496-10. SPSs shall occur in
         * order of ascending parameter set identifier with gaps being allowed.
         *
         * @return all SPS NAL units
         */
        public byte[][] GetSequenceParameterSetNALUnits()
        {
            return _sequenceParameterSetNALUnit;
        }

        /**
         * The PPS NAL units, as specified in ISO/IEC 14496-10. PPSs shall occur in
         * order of ascending parameter set identifier with gaps being allowed.
         *
         * @return all PPS NAL units
         */
        public byte[][] GetPictureParameterSetNALUnits()
        {
            return _pictureParameterSetNALUnit;
        }
    }
}
