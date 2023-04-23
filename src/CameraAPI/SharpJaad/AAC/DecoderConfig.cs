using SharpJaad.AAC.Syntax;

namespace SharpJaad.AAC
{
    public class DecoderConfig
    {
        private Profile _profile, _extProfile;
        private SampleFrequency _sampleFrequency;
        private ChannelConfiguration _channelConfiguration;
        private bool _frameLengthFlag;
        private bool _dependsOnCoreCoder;
        private int _coreCoderDelay;
        private bool _extensionFlag;
        //extension: SBR
        private bool _sbrPresent, _downSampledSBR, _sbrEnabled;
        //extension: error resilience
        private bool _sectionDataResilience, _scalefactorResilience, _spectralDataResilience;

        public DecoderConfig()
        {
            _profile = Profile.AAC_MAIN;
            _extProfile = Profile.UNKNOWN;
            _sampleFrequency = SampleFrequency.SAMPLE_FREQUENCY_NONE;
            _channelConfiguration = ChannelConfiguration.CHANNEL_CONFIG_UNSUPPORTED;
            _frameLengthFlag = false;
            _sbrPresent = false;
            _downSampledSBR = false;
            _sbrEnabled = true;
            _sectionDataResilience = false;
            _scalefactorResilience = false;
            _spectralDataResilience = false;
        }

        public void SetSBRPresent(bool sbr)
        {
            _sbrPresent = sbr;
        }

        public void SetSBRDownsampled(bool sbr)
        {
            _downSampledSBR = sbr;
        }

        /* ========== gets/sets ========== */
        public ChannelConfiguration GetChannelConfiguration()
        {
            return _channelConfiguration;
        }

        public void SetChannelConfiguration(ChannelConfiguration channelConfiguration)
        {
            _channelConfiguration = channelConfiguration;
        }

        public int GetCoreCoderDelay()
        {
            return _coreCoderDelay;
        }

        public void SetCoreCoderDelay(int coreCoderDelay)
        {
            _coreCoderDelay = coreCoderDelay;
        }

        public bool IsDependsOnCoreCoder()
        {
            return _dependsOnCoreCoder;
        }

        public void SetDependsOnCoreCoder(bool dependsOnCoreCoder)
        {
            _dependsOnCoreCoder = dependsOnCoreCoder;
        }

        public Profile GetExtObjectType()
        {
            return _extProfile;
        }

        public void SetExtObjectType(Profile extObjectType)
        {
            _extProfile = extObjectType;
        }

        public int GetFrameLength()
        {
            return _frameLengthFlag ? Constants.WINDOW_SMALL_LEN_LONG : Constants.WINDOW_LEN_LONG;
        }

        public bool IsSmallFrameUsed()
        {
            return _frameLengthFlag;
        }

        public void SetSmallFrameUsed(bool shortFrame)
        {
            _frameLengthFlag = shortFrame;
        }

        public Profile GetProfile()
        {
            return _profile;
        }

        public void SetProfile(Profile profile)
        {
            _profile = profile;
        }

        public SampleFrequency GetSampleFrequency()
        {
            return _sampleFrequency;
        }

        public void SetSampleFrequency(SampleFrequency sampleFrequency)
        {
            _sampleFrequency = sampleFrequency;
        }

        //=========== SBR =============
        public bool IsSBRPresent()
        {
            return _sbrPresent;
        }

        public bool IsSBRDownSampled()
        {
            return _downSampledSBR;
        }

        public bool IsSBREnabled()
        {
            return _sbrEnabled;
        }

        public void IetSBREnabled(bool enabled)
        {
            _sbrEnabled = enabled;
        }

        //=========== ER =============
        public bool IsScalefactorResilienceUsed()
        {
            return _scalefactorResilience;
        }

        public bool IsSectionDataResilienceUsed()
        {
            return _sectionDataResilience;
        }

        public bool IsSpectralDataResilienceUsed()
        {
            return _spectralDataResilience;
        }

        /* ======== static builder ========= */

        /// <summary>
        /// Parses the input arrays as a DecoderSpecificInfo, as used in MP4 containers.
        /// </summary>
        /// <param name="data">Data.</param>
        /// <returns>a DecoderConfig</returns>
        /// <exception cref="AACException"></exception>
        public static DecoderConfig ParseMP4DecoderSpecificInfo(byte[] data)
        {
            BitStream input = new BitStream(data);
            DecoderConfig config = new DecoderConfig();

            try
            {
                config._profile = ReadProfile(input);

                int sf = input.ReadBits(4);
                if (sf == 0xF) config._sampleFrequency = SampleFrequencyExtensions.FromFrequency(input.ReadBits(24));
                else config._sampleFrequency = (SampleFrequency)sf;
                config._channelConfiguration = (ChannelConfiguration)input.ReadBits(4);

                switch (config._profile)
                {
                    case Profile.AAC_SBR:
                        config._extProfile = config._profile;
                        config._sbrPresent = true;
                        sf = input.ReadBits(4);
                        //TODO: 24 bits already read; read again?
                        //if(sf==0xF) config.sampleFrequency = SampleFrequency.forFrequency(in.readBits(24));
                        //if sample frequencies are the same: downsample SBR
                        config._downSampledSBR = (int)config._sampleFrequency == sf;
                        config._sampleFrequency = (SampleFrequency)sf;
                        config._profile = ReadProfile(input);
                        break;
                    case Profile.AAC_MAIN:
                    case Profile.AAC_LC:
                    case Profile.AAC_SSR:
                    case Profile.AAC_LTP:
                    case Profile.ER_AAC_LC:
                    case Profile.ER_AAC_LTP:
                    case Profile.ER_AAC_LD:
                        //ga-specific info:
                        config._frameLengthFlag = input.ReadBool();
                        if (config._frameLengthFlag) throw new AACException("config uses 960-sample frames, not yet supported"); //TODO: are 960-frames working yet?
                        config._dependsOnCoreCoder = input.ReadBool();
                        if (config._dependsOnCoreCoder) config._coreCoderDelay = input.ReadBits(14);
                        else config._coreCoderDelay = 0;
                        config._extensionFlag = input.ReadBool();

                        if (config._extensionFlag)
                        {
                            if (config._profile.IsErrorResilientProfile())
                            {
                                config._sectionDataResilience = input.ReadBool();
                                config._scalefactorResilience = input.ReadBool();
                                config._spectralDataResilience = input.ReadBool();
                            }
                            //extensionFlag3
                            input.SkipBit();
                        }

                        if (config._channelConfiguration == ChannelConfiguration.CHANNEL_CONFIG_NONE)
                        {
                            //TODO: is this working correct? -> ISO 14496-3 part 1: 1.A.4.3
                            input.SkipBits(3); //PCE
                            PCE pce = new PCE();
                            pce.Decode(input);
                            config._profile = pce.GetProfile();
                            config._sampleFrequency = pce.GetSampleFrequency();
                            config._channelConfiguration = (ChannelConfiguration)pce.GetChannelCount();
                        }

                        if (input.GetBitsLeft() > 10) ReadSyncExtension(input, config);
                        break;
                    default:
                        throw new AACException("profile not supported: " + (int)config._profile);
                }
                return config;
            }
            finally
            {
                input.Destroy();
            }
        }

        private static Profile ReadProfile(BitStream input)
        {
            int i = input.ReadBits(5);
            if (i == 31) i = 32 + input.ReadBits(6);
            return (Profile)i;
        }

        private static void ReadSyncExtension(BitStream input, DecoderConfig config)
        {
            int type = input.ReadBits(11);
            switch (type)
            {
                case 0x2B7:
                    Profile profile = (Profile)input.ReadBits(5);

                    if (profile.Equals(Profile.AAC_SBR))
                    {
                        config._sbrPresent = input.ReadBool();
                        if (config._sbrPresent)
                        {
                            config._profile = profile;

                            int tmp = input.ReadBits(4);

                            if (tmp == (int)config._sampleFrequency) config._downSampledSBR = true;
                            if (tmp == 15)
                            {
                                throw new AACException("sample rate specified explicitly, not supported yet!");
                                //tmp = in.readBits(24);
                            }
                        }
                    }
                    break;
            }
        }
    }
}
