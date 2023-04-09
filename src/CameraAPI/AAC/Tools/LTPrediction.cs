using CameraAPI.AAC.Filterbank;
using CameraAPI.AAC.Syntax;
using System;
using System.Linq;

namespace CameraAPI.AAC.Tools
{
    public class LTPrediction : Constants
    {
		private static readonly float[] CODEBOOK = {
			0.570829f,
			0.696616f,
			0.813004f,
			0.911304f,
			0.984900f,
			1.067894f,
			1.194601f,
			1.369533f
		};
		private int frameLength;
		private int[] states;
		private int coef, lag, lastBand;
		private bool lagUpdate;
		private bool[] shortUsed, shortLagPresent, longUsed;
		private int[] shortLag;

		public LTPrediction(int frameLength) {
			this.frameLength = frameLength;
			states = new int[4*frameLength];
		}

		public void decode(BitStream input, ICSInfo info, Profile profile) {
			lag = 0;
			if(profile.Equals(Profile.AAC_LD)) {
				lagUpdate = input.readBool();
				if(lagUpdate) lag = input.readBits(10);
			}
			else lag = input.readBits(11);
			if(lag>(frameLength<<1)) throw new AACException("LTP lag too large: "+lag);
			coef = input.readBits(3);

			int windowCount = info.getWindowCount();

			if(info.isEightShortFrame()) {
				shortUsed = new bool[windowCount];
				shortLagPresent = new bool[windowCount];
				shortLag = new int[windowCount];
				for(int w = 0; w<windowCount; w++) {
					if((shortUsed[w] = input.readBool())) {
						shortLagPresent[w] = input.readBool();
						if(shortLagPresent[w]) shortLag[w] = input.readBits(4);
					}
				}
			}
			else {
				lastBand = Math.Min(info.getMaxSFB(), MAX_LTP_SFB);
				longUsed = new bool[lastBand];
				for(int i = 0; i<lastBand; i++) {
					longUsed[i] = input.readBool();
				}
			}
		}

		public void setPredictionUnused(int sfb) {
			if(longUsed!=null) longUsed[sfb] = false;
		}

		public void process(ICStream ics, float[] data, FilterBank filterBank, SampleFrequency sf) {
			ICSInfo info = ics.getInfo();

			if(!info.isEightShortFrame()) {
				int samples = frameLength<<1;
				float[] input = new float[2048];
				float[] output = new float[2048];

				for(int i = 0; i<samples; i++) {
                    input[i] = states[samples+i-lag]*CODEBOOK[coef];
				}

				filterBank.ProcessLTP(info.getWindowSequence(), info.getWindowShape(ICSInfo.CURRENT),
						info.getWindowShape(ICSInfo.PREVIOUS), input, output);

				if(ics.isTNSDataPresent()) ics.getTNS().process(ics, output, sf, true);

				int[] swbOffsets = info.getSWBOffsets();
				int swbOffsetMax = info.getSWBOffsetMax();
				int low, high, bin;
				for(int sfb = 0; sfb<lastBand; sfb++) {
					if(longUsed[sfb]) {
						low = swbOffsets[sfb];
						high = Math.Min(swbOffsets[sfb+1], swbOffsetMax);

						for(bin = low; bin<high; bin++) {
							data[bin] += output[bin];
						}
					}
				}
			}
		}

		public void updateState(float[] time, float[] overlap, Profile profile) {
			int i;
			if(profile.Equals(Profile.AAC_LD)) {
				for(i = 0; i<frameLength; i++) {
					states[i] = states[i+frameLength];
					states[frameLength+i] = states[i+(frameLength*2)];
					states[(frameLength*2)+i] = (int)Math.Round(time[i]);
					states[(frameLength*3)+i] = (int)Math.Round(overlap[i]);
				}
			}
			else {
				for(i = 0; i<frameLength; i++) {
					states[i] = states[i+frameLength];
					states[frameLength+i] = (int)Math.Round(time[i]);
					states[(frameLength*2)+i] = (int)Math.Round(overlap[i]);
				}
			}
		}

		public static bool isLTPProfile(Profile profile) {
			return profile.Equals(Profile.AAC_LTP)||profile.Equals(Profile.ER_AAC_LTP)||profile.Equals(Profile.AAC_LD);
		}

		public void copy(LTPrediction ltp) {
			Array.Copy(ltp.states, 0, states, 0, states.Length);
			coef = ltp.coef;
			lag = ltp.lag;
			lastBand = ltp.lastBand;
			lagUpdate = ltp.lagUpdate;
			shortUsed = ltp.shortUsed.ToArray();
			shortLagPresent = ltp.shortLagPresent.ToArray();
			shortLag = ltp.shortLag.ToArray();
			longUsed = ltp.longUsed.ToArray();
		}
    }
}
