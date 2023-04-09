using CameraAPI.AAC.Tools;
using System.Linq;

namespace CameraAPI.AAC.Syntax
{
    public class ICSInfo : Constants
    {
        public const int WINDOW_SHAPE_SINE = 0;
		public const int WINDOW_SHAPE_KAISER = 1;
		public const int PREVIOUS = 0;
		public const int CURRENT = 1;

		public enum WindowSequence {

			ONLY_LONG_SEQUENCE = 0,
			LONG_START_SEQUENCE = 1,
			EIGHT_SHORT_SEQUENCE = 2,
			LONG_STOP_SEQUENCE = 3
		}

		private int frameLength;
		private WindowSequence windowSequence;
		private int[] windowShape;
		private int maxSFB;
		//prediction
		private bool predictionDataPresent;
		private ICPrediction icPredict;
		public bool ltpData1Present;
		public bool ltpData2Present;
		private LTPrediction ltPredict1, ltPredict2;
		//windows/sfbs
		private int windowCount;
		private int windowGroupCount;
		private int[] windowGroupLength;
		private int swbCount;
		private int[] swbOffsets;

		public ICSInfo(int frameLength) {
			this.frameLength = frameLength;
			windowShape = new int[2];
			windowSequence = WindowSequence.ONLY_LONG_SEQUENCE;
			windowGroupLength = new int[MAX_WINDOW_GROUP_COUNT];
			ltpData1Present = false;
			ltpData2Present = false;
		}

		/* ========== decoding ========== */
		public void decode(BitStream input, DecoderConfig conf, bool commonWindow) {
			SampleFrequency sf = conf.getSampleFrequency();
			if(sf.Equals(SampleFrequency.SAMPLE_FREQUENCY_NONE)) throw new AACException("invalid sample frequency");

			input.skipBit(); //reserved
			windowSequence = (WindowSequence)(input.readBits(2));
			windowShape[PREVIOUS] = windowShape[CURRENT];
			windowShape[CURRENT] = input.readBit();

			windowGroupCount = 1;
			windowGroupLength[0] = 1;
			if(windowSequence.Equals(WindowSequence.EIGHT_SHORT_SEQUENCE)) {
				maxSFB = input.readBits(4);
				int i;
				for(i = 0; i<7; i++) {
					if(input.readBool()) windowGroupLength[windowGroupCount-1]++;
					else {
						windowGroupCount++;
						windowGroupLength[windowGroupCount-1] = 1;
					}
				}
				windowCount = 8;
				swbOffsets = ScaleFactorBands.SWB_OFFSET_SHORT_WINDOW[(int)sf];
				swbCount = ScaleFactorBands.SWB_SHORT_WINDOW_COUNT[(int)sf];
				predictionDataPresent = false;
			}
			else {
				maxSFB = input.readBits(6);
				windowCount = 1;
				swbOffsets = ScaleFactorBands.SWB_OFFSET_LONG_WINDOW[(int)sf];
				swbCount = ScaleFactorBands.SWB_LONG_WINDOW_COUNT[(int)sf];
				predictionDataPresent = input.readBool();
				if(predictionDataPresent) readPredictionData(input, conf.getProfile(), sf, commonWindow);
			}
		}

		private void readPredictionData(BitStream input, Profile profile, SampleFrequency sf, bool commonWindow) {
			switch(profile) {
				case Profile.AAC_MAIN:
					if(icPredict==null) icPredict = new ICPrediction();
					icPredict.decode(input, maxSFB, sf);
					break;
				case Profile.AAC_LTP:
					if(ltpData1Present = input.readBool()) {
						if(ltPredict1==null) ltPredict1 = new LTPrediction(frameLength);
						ltPredict1.decode(input, this, profile);
					}
					if(commonWindow) {
						if(ltpData2Present = input.readBool()) {
							if(ltPredict2==null) ltPredict2 = new LTPrediction(frameLength);
							ltPredict2.decode(input, this, profile);
						}
					}
					break;
				case Profile.ER_AAC_LTP:
					if(!commonWindow) {
						if(ltpData1Present = input.readBool()) {
							if(ltPredict1==null) ltPredict1 = new LTPrediction(frameLength);
							ltPredict1.decode(input, this, profile);
						}
					}
					break;
				default:
					throw new AACException("unexpected profile for LTP: "+profile);
			}
		}

		/* =========== gets ============ */
		public int getMaxSFB() {
			return maxSFB;
		}

		public int getSWBCount() {
			return swbCount;
		}

		public int[] getSWBOffsets() {
			return swbOffsets;
		}

		public int getSWBOffsetMax() {
			return swbOffsets[swbCount];
		}

		public int getWindowCount() {
			return windowCount;
		}

		public int getWindowGroupCount() {
			return windowGroupCount;
		}

		public int getWindowGroupLength(int g) {
			return windowGroupLength[g];
		}

		public WindowSequence getWindowSequence() {
			return windowSequence;
		}

		public bool isEightShortFrame() {
			return windowSequence.Equals(WindowSequence.EIGHT_SHORT_SEQUENCE);
		}

		public int getWindowShape(int index) {
			return windowShape[index];
		}

		public bool isICPredictionPresent() {
			return predictionDataPresent;
		}

		public ICPrediction getICPrediction() {
			return icPredict;
		}

		public bool isLTPrediction1Present() {
			return ltpData1Present;
		}

		public LTPrediction getLTPrediction1() {
			return ltPredict1;
		}

		public bool isLTPrediction2Present() {
			return ltpData2Present;
		}

		public LTPrediction getLTPrediction2() {
			return ltPredict2;
		}

		public void unsetPredictionSFB(int sfb) {
			if(predictionDataPresent) icPredict.setPredictionUnused(sfb);
			if(ltpData1Present) ltPredict1.setPredictionUnused(sfb);
			if(ltpData2Present) ltPredict2.setPredictionUnused(sfb);
		}

		public void setData(ICSInfo info) {
			windowSequence = info.windowSequence;
			windowShape[PREVIOUS] = windowShape[CURRENT];
			windowShape[CURRENT] = info.windowShape[CURRENT];
			maxSFB = info.maxSFB;
			predictionDataPresent = info.predictionDataPresent;
			if(predictionDataPresent) icPredict = info.icPredict;
			ltpData1Present = info.ltpData1Present;
			if(ltpData1Present) {
				ltPredict1.copy(info.ltPredict1);
				ltPredict2.copy(info.ltPredict2);
			}
			windowCount = info.windowCount;
			windowGroupCount = info.windowGroupCount;
			windowGroupLength = info.windowGroupLength.ToArray();
			swbCount = info.swbCount;
			swbOffsets = info.swbOffsets.ToArray();
		}
    }
}
