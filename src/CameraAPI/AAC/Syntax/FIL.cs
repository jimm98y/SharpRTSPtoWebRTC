namespace CameraAPI.AAC.Syntax
{
    public class FIL
    {
        public class DynamicRangeInfo {

			public const int MAX_NBR_BANDS = 7;
            public bool[] excludeMask;
            public bool[] additionalExcludedChannels;
            public bool pceTagPresent;
            public int pceInstanceTag;
            public int tagReservedBits;
            public bool excludedChannelsPresent;
            public bool bandsPresent;
            public int bandsIncrement, interpolationScheme;
            public int[] bandTop;
            public bool progRefLevelPresent;
            public int progRefLevel, progRefLevelReservedBits;
            public bool[] dynRngSgn;
            public int[] dynRngCtl;

			public DynamicRangeInfo() {
				excludeMask = new bool[MAX_NBR_BANDS];
				additionalExcludedChannels = new bool[MAX_NBR_BANDS];
			}
		}

		private const int TYPE_FILL = 0;
		private const int TYPE_FILL_DATA = 1;
		private const int TYPE_EXT_DATA_ELEMENT = 2;
		private const int TYPE_DYNAMIC_RANGE = 11;
		private const int TYPE_SBR_DATA = 13;
		private const int TYPE_SBR_DATA_CRC = 14;
		private bool downSampledSBR;
		private DynamicRangeInfo dri;

		public FIL(bool downSampledSBR) {
			this.downSampledSBR = downSampledSBR;
		}

		public void decode(BitStream input, Element prev, SampleFrequency sf, bool sbrEnabled, bool smallFrames) {
			int count = input.readBits(4);
			if(count==15) count += input.readBits(8)-1;
			count *= 8; //convert to bits

			int cpy = count;
			int pos = input.getPosition();

			while(count>0) {
				count = decodeExtensionPayload(input, count, prev, sf, sbrEnabled, smallFrames);
			}

			int pos2 = input.getPosition()-pos;
			int bitsLeft = cpy-pos2;
			if(bitsLeft>0) input.skipBits(pos2);
			else if(bitsLeft<0) throw new AACException("FIL element overread: "+bitsLeft);
		}

		private int decodeExtensionPayload(BitStream input, int count, Element prev, SampleFrequency sf, bool sbrEnabled, bool smallFrames) {
			int type = input.readBits(4);
			int ret = count - 4;
			switch(type) {
				case TYPE_DYNAMIC_RANGE:
					ret = decodeDynamicRangeInfo(input, ret);
					break;
				case TYPE_SBR_DATA:
				case TYPE_SBR_DATA_CRC:
					if(sbrEnabled) {
						if(prev is SCE_LFE||prev is CPE||prev is CCE) {
							prev.decodeSBR(input, sf, ret, (prev is CPE), (type==TYPE_SBR_DATA_CRC), downSampledSBR, smallFrames);
							ret = 0;
							break;
						}
						else throw new AACException("SBR applied on unexpected element: "+prev);
					}
					else {
                        input.skipBits(ret);
						ret = 0;
					}
					break;
				case TYPE_FILL:
				case TYPE_FILL_DATA:
				case TYPE_EXT_DATA_ELEMENT:
				default:
                    input.skipBits(ret);
					ret = 0;
					break;
			}
			return ret;
		}

		private int decodeDynamicRangeInfo(BitStream input, int count) {
			if(dri==null) dri = new DynamicRangeInfo();
			int ret = count;

			int bandCount = 1;

			//pce tag
			if(dri.pceTagPresent = input.readBool()) {
				dri.pceInstanceTag = input.readBits(4);
				dri.tagReservedBits = input.readBits(4);
			}

			//excluded channels
			if(dri.excludedChannelsPresent = input.readBool()) {
				ret -= decodeExcludedChannels(input);
			}

			//bands
			if(dri.bandsPresent = input.readBool()) {
				dri.bandsIncrement = input.readBits(4);
				dri.interpolationScheme = input.readBits(4);
				ret -= 8;
				bandCount += dri.bandsIncrement;
				dri.bandTop = new int[bandCount];
				for(int i = 0; i<bandCount; i++) {
					dri.bandTop[i] = input.readBits(8);
					ret -= 8;
				}
			}

			//prog ref level
			if(dri.progRefLevelPresent = input.readBool()) {
				dri.progRefLevel = input.readBits(7);
				dri.progRefLevelReservedBits = input.readBits(1);
				ret -= 8;
			}

			dri.dynRngSgn = new bool[bandCount];
			dri.dynRngCtl = new int[bandCount];
			for(int i = 0; i<bandCount; i++) {
				dri.dynRngSgn[i] = input.readBool();
				dri.dynRngCtl[i] = input.readBits(7);
				ret -= 8;
			}
			return ret;
		}

		private int decodeExcludedChannels(BitStream input) {
			int i;
			int exclChs = 0;

			do {
				for(i = 0; i < 7; i++) {
					dri.excludeMask[exclChs] = input.readBool();
					exclChs++;
				}
			}
			while(exclChs < 57 && input.readBool());

			return (exclChs / 7) * 8;
		}
    }
}
