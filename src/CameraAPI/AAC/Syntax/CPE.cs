using CameraAPI.AAC.Tools;

namespace CameraAPI.AAC.Syntax
{
    public class CPE : Element
    {
        private MSMask msMask;
		private bool[] msUsed;
		private bool commonWindow;
		ICStream icsL, icsR;

		public CPE(int frameLength) {
			msUsed = new bool[Constants.MAX_MS_MASK];
			icsL = new ICStream(frameLength);
			icsR = new ICStream(frameLength);
		}

		public void decode(BitStream input, DecoderConfig conf) {
			Profile profile = conf.getProfile();
			SampleFrequency sf = conf.getSampleFrequency();
			if(sf.Equals(SampleFrequency.SAMPLE_FREQUENCY_NONE)) throw new AACException("invalid sample frequency");

			readElementInstanceTag(input);

			commonWindow = input.readBool();
			ICSInfo info = icsL.getInfo();
			if(commonWindow) {
				info.decode(input, conf, commonWindow);
				icsR.getInfo().setData(info);

				msMask = (MSMask)(input.readBits(2));
				if(msMask.Equals(MSMask.TYPE_USED)) {
					int maxSFB = info.getMaxSFB();
					int windowGroupCount = info.getWindowGroupCount();

					for(int idx = 0; idx<windowGroupCount*maxSFB; idx++) {
						msUsed[idx] = input.readBool();
					}
				}
				else if(msMask.Equals(MSMask.TYPE_ALL_1)) Arrays.Fill(msUsed, true);
				else if(msMask.Equals(MSMask.TYPE_ALL_0)) Arrays.Fill(msUsed, false);
				else throw new AACException("reserved MS mask type used");
			}
			else {
				msMask = MSMask.TYPE_ALL_0;
				Arrays.Fill(msUsed, false);
			}

			if(profile.IsErrorResilientProfile() && (info.isLTPrediction1Present())) {
				if(info.ltpData2Present = input.readBool()) info.getLTPrediction2().decode(input, info, profile);
			}

			icsL.decode(input, commonWindow, conf);
			icsR.decode(input, commonWindow, conf);
		}

		public ICStream getLeftChannel() {
			return icsL;
		}

		public ICStream getRightChannel() {
			return icsR;
		}

		public MSMask getMSMask() {
			return msMask;
		}

		public bool isMSUsed(int off) {
			return msUsed[off];
		}

		public bool isMSMaskPresent() {
			return !msMask.Equals(MSMask.TYPE_ALL_0);
		}

		public bool isCommonWindow() {
			return commonWindow;
		}
    }
}
