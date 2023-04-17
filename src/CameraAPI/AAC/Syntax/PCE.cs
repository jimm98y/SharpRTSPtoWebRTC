namespace CameraAPI.AAC.Syntax
{
    public class PCE : Element
    {
		private const int MAX_FRONT_CHANNEL_ELEMENTS = 16;
		private const int MAX_SIDE_CHANNEL_ELEMENTS = 16;
		private const int MAX_BACK_CHANNEL_ELEMENTS = 16;
		private const int MAX_LFE_CHANNEL_ELEMENTS = 4;
		private const int MAX_ASSOC_DATA_ELEMENTS = 8;
		private const int MAX_VALID_CC_ELEMENTS = 16;

		public sealed class TaggedElement {

			public bool isCPE;
			public int tag;

			public TaggedElement(bool isCPE, int tag) {
				this.isCPE = isCPE;
				this.tag = tag;
			}

			public bool isIsCPE() {
				return isCPE;
			}

			public int getTag() {
				return tag;
			}
		}

		public sealed class CCE {

			public bool isIndSW;
			public int tag;

			public CCE(bool isIndSW, int tag) {
				this.isIndSW = isIndSW;
				this.tag = tag;
			}

			public bool isIsIndSW() {
				return isIndSW;
			}

			public int getTag() {
				return tag;
			}
		}

		private Profile profile;
		private SampleFrequency sampleFrequency;
		private int frontChannelElementsCount, sideChannelElementsCount, backChannelElementsCount;
		private int lfeChannelElementsCount, assocDataElementsCount;
		private int validCCElementsCount;
		private bool monoMixdown, stereoMixdown, matrixMixdownIDXPresent;
		private int monoMixdownElementNumber, stereoMixdownElementNumber, matrixMixdownIDX;
		private bool pseudoSurround;
		private TaggedElement[] frontElements, sideElements, backElements;
		private int[] lfeElementTags;
		private int[] assocDataElementTags;
		private CCE[] ccElements;
		private byte[] commentFieldData;

		public PCE() {
			frontElements = new TaggedElement[MAX_FRONT_CHANNEL_ELEMENTS];
			sideElements = new TaggedElement[MAX_SIDE_CHANNEL_ELEMENTS];
			backElements = new TaggedElement[MAX_BACK_CHANNEL_ELEMENTS];
			lfeElementTags = new int[MAX_LFE_CHANNEL_ELEMENTS];
			assocDataElementTags = new int[MAX_ASSOC_DATA_ELEMENTS];
			ccElements = new CCE[MAX_VALID_CC_ELEMENTS];
			sampleFrequency = SampleFrequency.SAMPLE_FREQUENCY_NONE;
		}

		public void decode(BitStream input) {
			readElementInstanceTag(input);

			profile = (Profile)(input.readBits(2));

			sampleFrequency = (SampleFrequency)(input.readBits(4));

			frontChannelElementsCount = input.readBits(4);
			sideChannelElementsCount = input.readBits(4);
			backChannelElementsCount = input.readBits(4);
			lfeChannelElementsCount = input.readBits(2);
			assocDataElementsCount = input.readBits(3);
			validCCElementsCount = input.readBits(4);

			if(monoMixdown = input.readBool()) {
				//Constants.LOGGER.warning("mono mixdown present, but not yet supported");
				monoMixdownElementNumber = input.readBits(4);
			}
			if(stereoMixdown = input.readBool()) {
				//Constants.LOGGER.warning("stereo mixdown present, but not yet supported");
				stereoMixdownElementNumber = input.readBits(4);
			}
			if(matrixMixdownIDXPresent = input.readBool()) {
				//Constants.LOGGER.warning("matrix mixdown present, but not yet supported");
				matrixMixdownIDX = input.readBits(2);
				pseudoSurround = input.readBool();
			}

			readTaggedElementArray(frontElements, input, frontChannelElementsCount);

			readTaggedElementArray(sideElements, input, sideChannelElementsCount);

			readTaggedElementArray(backElements, input, backChannelElementsCount);

			int i;
			for(i = 0; i<lfeChannelElementsCount; ++i) {
				lfeElementTags[i] = input.readBits(4);
			}

			for(i = 0; i<assocDataElementsCount; ++i) {
				assocDataElementTags[i] = input.readBits(4);
			}

			for(i = 0; i<validCCElementsCount; ++i) {
				ccElements[i] = new CCE(input.readBool(), input.readBits(4));
			}

            input.byteAlign();

			int commentFieldBytes = input.readBits(8);
			commentFieldData = new byte[commentFieldBytes];
			for(i = 0; i<commentFieldBytes; i++) {
				commentFieldData[i] = (byte)input.readBits(8);
			}
		}

		private void readTaggedElementArray(TaggedElement[] te, BitStream input, int len) {
			for(int i = 0; i<len; ++i) {
				te[i] = new TaggedElement(input.readBool(), input.readBits(4));
			}
		}

		public Profile getProfile() {
			return profile;
		}

		public SampleFrequency getSampleFrequency() {
			return sampleFrequency;
		}

		public int getChannelCount() {
            int count = lfeChannelElementsCount + assocDataElementsCount;

            for (int n = 0; n < frontChannelElementsCount; ++n)
                count += frontElements[n].isCPE ? 2 : 1;

            for (int n = 0; n < sideChannelElementsCount; ++n)
                count += sideElements[n].isCPE ? 2 : 1;

            for (int n = 0; n < backChannelElementsCount; ++n)
                count += backElements[n].isCPE ? 2 : 1;

            return count;
        }
    }
}
