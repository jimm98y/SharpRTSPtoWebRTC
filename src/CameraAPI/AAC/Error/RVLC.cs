using CameraAPI.AAC.Huffman;
using CameraAPI.AAC.Syntax;
using System;

namespace CameraAPI.AAC.Error
{
    public class RVLC : RVLCTables
    {
        private const int ESCAPE_FLAG = 7;

		public void Decode(BitStream input, ICStream ics, int[][] scaleFactors) {
			int bits = (ics.getInfo().isEightShortFrame()) ? 11 : 9;
			bool sfConcealment = input.readBool();
			int revGlobalGain = input.readBits(8);
			int rvlcSFLen = input.readBits(bits);

			ICSInfo info = ics.getInfo();
			int windowGroupCount = info.getWindowGroupCount();
			int maxSFB = info.getMaxSFB();
			int[][] sfbCB = null; //ics.getSectionData().getSfbCB();

			int sf = ics.getGlobalGain();
			int intensityPosition = 0;
			int noiseEnergy = sf-90-256;
			bool intensityUsed = false, noiseUsed = false;

			int sfb;
			for(int g = 0; g<windowGroupCount; g++) {
				for(sfb = 0; sfb<maxSFB; sfb++) {
					switch(sfbCB[g][sfb]) {
						case HCB.ZERO_HCB:
							scaleFactors[g][sfb] = 0;
							break;
						case HCB.INTENSITY_HCB:
						case HCB.INTENSITY_HCB2:
							if(!intensityUsed) intensityUsed = true;
							intensityPosition += DecodeHuffman(input);
							scaleFactors[g][sfb] = intensityPosition;
							break;
						case HCB.NOISE_HCB:
							if(noiseUsed) {
								noiseEnergy += DecodeHuffman(input);
								scaleFactors[g][sfb] = noiseEnergy;
							}
							else {
								noiseUsed = true;
								noiseEnergy = DecodeHuffman(input);
							}
							break;
						default:
							sf += DecodeHuffman(input);
							scaleFactors[g][sfb] = sf;
							break;
					}
				}
			}

			int lastIntensityPosition = 0;
			if(intensityUsed) lastIntensityPosition = DecodeHuffman(input);
			noiseUsed = false;
			if(input.readBool()) DecodeEscapes(input, ics, scaleFactors);
		}

		private void DecodeEscapes(BitStream input, ICStream ics, int[][] scaleFactors) {
			ICSInfo info = ics.getInfo();
			int windowGroupCount = info.getWindowGroupCount();
			int maxSFB = info.getMaxSFB();
			int[][] sfbCB = null; //ics.getSectionData().getSfbCB();

			int escapesLen = input.readBits(8);

			bool noiseUsed = false;

			int sfb, val;
			for(int g = 0; g<windowGroupCount; g++) {
				for(sfb = 0; sfb<maxSFB; sfb++) {
					if(sfbCB[g][sfb]==HCB.NOISE_HCB&&!noiseUsed) noiseUsed = true;
					else if(Math.Abs(sfbCB[g][sfb])==ESCAPE_FLAG) {
						val = DecodeHuffmanEscape(input);
						if(sfbCB[g][sfb]==-ESCAPE_FLAG) scaleFactors[g][sfb] -= val;
						else scaleFactors[g][sfb] += val;
					}
				}
			}
		}

		private int DecodeHuffman(BitStream input) {
			int off = 0;
			int i = RVLC_BOOK[off][1];
			int cw = input.readBits(i);

			int j;
			while((cw!=RVLC_BOOK[off][2])&&(i<10)) {
				off++;
				j = RVLC_BOOK[off][1]-i;
				i += j;
				cw <<= j;
				cw |= input.readBits(j);
			}

			return RVLC_BOOK[off][0];
		}

		private int DecodeHuffmanEscape(BitStream input) {
			int off = 0;
			int i = ESCAPE_BOOK[off][1];
			int cw = input.readBits(i);

			int j;
			while((cw!=ESCAPE_BOOK[off][2])&&(i<21)) {
				off++;
				j = ESCAPE_BOOK[off][1]-i;
				i += j;
				cw <<= j;
				cw |= input.readBits(j);
			}

			return ESCAPE_BOOK[off][0];
		}
    }
}
