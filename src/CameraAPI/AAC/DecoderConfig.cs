using CameraAPI.AAC.Syntax;

namespace CameraAPI.AAC
{
    public class DecoderConfig : Constants
    {
        private Profile profile, extProfile;
		private SampleFrequency sampleFrequency;
		private ChannelConfiguration channelConfiguration;
		private bool frameLengthFlag;
		private bool dependsOnCoreCoder;
		private int coreCoderDelay;
		private bool extensionFlag;
		//extension: SBR
		private bool sbrPresent, downSampledSBR, sbrEnabled;
		//extension: error resilience
		private bool sectionDataResilience, scalefactorResilience, spectralDataResilience;

		public DecoderConfig() {
			profile = Profile.AAC_MAIN;
			extProfile = Profile.UNKNOWN;
			sampleFrequency = SampleFrequency.SAMPLE_FREQUENCY_NONE;
			channelConfiguration = ChannelConfiguration.CHANNEL_CONFIG_UNSUPPORTED;
			frameLengthFlag = false;
			sbrPresent = false;
			downSampledSBR = false;
			sbrEnabled = true;
			sectionDataResilience = false;
			scalefactorResilience = false;
			spectralDataResilience = false;
		}

		/* ========== gets/sets ========== */
		public ChannelConfiguration getChannelConfiguration() {
			return channelConfiguration;
		}

		public void setChannelConfiguration(ChannelConfiguration channelConfiguration) {
			this.channelConfiguration = channelConfiguration;
		}

		public int getCoreCoderDelay() {
			return coreCoderDelay;
		}

		public void setCoreCoderDelay(int coreCoderDelay) {
			this.coreCoderDelay = coreCoderDelay;
		}

		public bool isDependsOnCoreCoder() {
			return dependsOnCoreCoder;
		}

		public void setDependsOnCoreCoder(bool dependsOnCoreCoder) {
			this.dependsOnCoreCoder = dependsOnCoreCoder;
		}

		public Profile getExtObjectType() {
			return extProfile;
		}

		public void setExtObjectType(Profile extObjectType) {
			this.extProfile = extObjectType;
		}

		public int getFrameLength() {
			return frameLengthFlag ? WINDOW_SMALL_LEN_LONG : WINDOW_LEN_LONG;
		}

		public bool isSmallFrameUsed() {
			return frameLengthFlag;
		}

		public void setSmallFrameUsed(bool shortFrame) {
			this.frameLengthFlag = shortFrame;
		}

		public Profile getProfile() {
			return profile;
		}

		public void setProfile(Profile profile) {
			this.profile = profile;
		}

		public SampleFrequency getSampleFrequency() {
			return sampleFrequency;
		}

		public void setSampleFrequency(SampleFrequency sampleFrequency) {
			this.sampleFrequency = sampleFrequency;
		}

		//=========== SBR =============
		public bool isSBRPresent() {
			return sbrPresent;
		}

		public bool isSBRDownSampled() {
			return downSampledSBR;
		}

		public bool isSBREnabled() {
			return sbrEnabled;
		}

		public void setSBREnabled(bool enabled) {
			sbrEnabled = enabled;
		}

		//=========== ER =============
		public bool isScalefactorResilienceUsed() {
			return scalefactorResilience;
		}

		public bool isSectionDataResilienceUsed() {
			return sectionDataResilience;
		}

		public bool isSpectralDataResilienceUsed() {
			return spectralDataResilience;
		}

		/* ======== static builder ========= */
		/**
		 * Parses the input arrays as a DecoderSpecificInfo, as used in MP4
		 * containers.
		 * 
		 * @return a DecoderConfig
		 */
		public static DecoderConfig parseMP4DecoderSpecificInfo(byte[] data) {
			BitStream input = new BitStream(data);
			DecoderConfig config = new DecoderConfig();

			try {
				config.profile = readProfile(input);

				int sf = input.readBits(4);
				if(sf==0xF) config.sampleFrequency = SampleFrequencyExtensions.FromFrequency(input.readBits(24));
				else config.sampleFrequency = (SampleFrequency)sf;
				config.channelConfiguration = (ChannelConfiguration)(input.readBits(4));

				switch(config.profile) {
					case Profile.AAC_SBR:
						config.extProfile = config.profile;
						config.sbrPresent = true;
						sf = input.readBits(4);
						//TODO: 24 bits already read; read again?
						//if(sf==0xF) config.sampleFrequency = SampleFrequency.forFrequency(in.readBits(24));
						//if sample frequencies are the same: downsample SBR
						config.downSampledSBR = (int)config.sampleFrequency==sf;
						config.sampleFrequency = (SampleFrequency)(sf);
						config.profile = readProfile(input);
						break;
					case Profile.AAC_MAIN:
					case Profile.AAC_LC:
					case Profile.AAC_SSR:
					case Profile.AAC_LTP:
					case Profile.ER_AAC_LC:
					case Profile.ER_AAC_LTP:
					case Profile.ER_AAC_LD:
						//ga-specific info:
						config.frameLengthFlag = input.readBool();
						if(config.frameLengthFlag) throw new AACException("config uses 960-sample frames, not yet supported"); //TODO: are 960-frames working yet?
						config.dependsOnCoreCoder = input.readBool();
						if(config.dependsOnCoreCoder) config.coreCoderDelay = input.readBits(14);
						else config.coreCoderDelay = 0;
						config.extensionFlag = input.readBool();

						if(config.extensionFlag) {
							if(config.profile.IsErrorResilientProfile()) {
								config.sectionDataResilience = input.readBool();
								config.scalefactorResilience = input.readBool();
								config.spectralDataResilience = input.readBool();
							}
                            //extensionFlag3
                            input.skipBit();
						}

						if(config.channelConfiguration==ChannelConfiguration.CHANNEL_CONFIG_NONE) {
                            //TODO: is this working correct? -> ISO 14496-3 part 1: 1.A.4.3
                            input.skipBits(3); //PCE
							PCE pce = new PCE();
							pce.decode(input);
							config.profile = pce.getProfile();
							config.sampleFrequency = pce.getSampleFrequency();
							config.channelConfiguration = (ChannelConfiguration)(pce.getChannelCount());
						}

						if(input.getBitsLeft()>10) readSyncExtension(input, config);
						break;
					default:
						throw new AACException("profile not supported: "+ (int)config.profile);
				}
				return config;
			}
			finally {
                input.destroy();
			}
		}

		private static Profile readProfile(BitStream input) {
			int i = input.readBits(5);
			if(i==31) i = 32+ input.readBits(6);
			return (Profile)(i);
		}

		private static void readSyncExtension(BitStream input, DecoderConfig config) {
			int type = input.readBits(11);
			switch(type) {
				case 0x2B7:
					Profile profile = (Profile)(input.readBits(5));

					if(profile.Equals(Profile.AAC_SBR)) {
						config.sbrPresent = input.readBool();
						if(config.sbrPresent) {
							config.profile = profile;

							int tmp = input.readBits(4);

							if(tmp==(int)config.sampleFrequency) config.downSampledSBR = true;
							if(tmp==15) {
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
