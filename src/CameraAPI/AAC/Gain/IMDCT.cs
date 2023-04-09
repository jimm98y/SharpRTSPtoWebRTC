using System;
using static CameraAPI.AAC.Syntax.ICSInfo;

namespace CameraAPI.AAC.Gain
{
    public class IMDCT : GCConstants
    {
        private static float[][] LONG_WINDOWS = { Windows.SINE_256, Windows.KBD_256 };
		private static float[][] SHORT_WINDOWS = { Windows.SINE_32, Windows.KBD_32 };
		private int frameLen, shortFrameLen, lbLong, lbShort, lbMid;

		public IMDCT(int frameLen) {
			this.frameLen = frameLen;
			lbLong = frameLen/BANDS;
			shortFrameLen = frameLen/8;
			lbShort = shortFrameLen/BANDS;
			lbMid = (lbLong-lbShort)/2;
		}

		public void Process(float[] input, float[] output, int winShape, int winShapePrev, WindowSequence winSeq) {
			float[] buf = new float[frameLen];

			int b, j, i;
			if(winSeq.Equals(WindowSequence.EIGHT_SHORT_SEQUENCE)) {
				for(b = 0; b<BANDS; b++) {
					for(j = 0; j<8; j++) {
						for(i = 0; i<lbShort; i++) {
							if(b%2==0) buf[lbLong*b+lbShort*j+i] = input[shortFrameLen*j+lbShort*b+i];
							else buf[lbLong*b+lbShort*j+i] = input[shortFrameLen*j+lbShort*b+lbShort-1-i];
						}
					}
				}
			}
			else {
				for(b = 0; b<BANDS; b++) {
					for(i = 0; i<lbLong; i++) {
						if(b%2==0) buf[lbLong*b+i] = input[lbLong*b+i];
						else buf[lbLong*b+i] = input[lbLong*b+lbLong-1-i];
					}
				}
			}

			for(b = 0; b<BANDS; b++) {
				Process2(buf, output, winSeq, winShape, winShapePrev, b);
			}
		}

		private void Process2(float[] input, float[] output, WindowSequence winSeq, int winShape, int winShapePrev, int band) {
			float[] bufIn = new float[lbLong];
			float[] bufOut = new float[lbLong*2];
			float[] window = new float[lbLong*2];
			float[] window1 = new float[lbShort*2];
			float[] window2 = new float[lbShort*2];

			//init windows
			int i;
			switch(winSeq) {
				case WindowSequence.ONLY_LONG_SEQUENCE:
					for(i = 0; i<lbLong; i++) {
						window[i] = LONG_WINDOWS[winShapePrev][i];
						window[lbLong*2-1-i] = LONG_WINDOWS[winShape][i];
					}
					break;
				case WindowSequence.EIGHT_SHORT_SEQUENCE:
					for(i = 0; i<lbShort; i++) {
						window1[i] = SHORT_WINDOWS[winShapePrev][i];
						window1[lbShort*2-1-i] = SHORT_WINDOWS[winShape][i];
						window2[i] = SHORT_WINDOWS[winShape][i];
						window2[lbShort*2-1-i] = SHORT_WINDOWS[winShape][i];
					}
					break;
				case WindowSequence.LONG_START_SEQUENCE:
					for(i = 0; i<lbLong; i++) {
						window[i] = LONG_WINDOWS[winShapePrev][i];
					}
					for(i = 0; i<lbMid; i++) {
						window[i+lbLong] = 1.0f;
					}

					for(i = 0; i<lbShort; i++) {
						window[i+lbMid+lbLong] = SHORT_WINDOWS[winShape][lbShort-1-i];
					}
					for(i = 0; i<lbMid; i++) {
						window[i+lbMid+lbLong+lbShort] = 0.0f;
					}
					break;
				case WindowSequence.LONG_STOP_SEQUENCE:
					for(i = 0; i<lbMid; i++) {
						window[i] = 0.0f;
					}
					for(i = 0; i<lbShort; i++) {
						window[i+lbMid] = SHORT_WINDOWS[winShapePrev][i];
					}
					for(i = 0; i<lbMid; i++) {
						window[i+lbMid+lbShort] = 1.0f;
					}
					for(i = 0; i<lbLong; i++) {
						window[i+lbMid+lbShort+lbMid] = LONG_WINDOWS[winShape][lbLong-1-i];
					}
					break;
			}

			int j;
			if(winSeq.Equals(WindowSequence.EIGHT_SHORT_SEQUENCE)) {
				int k;
				for(j = 0; j<8; j++) {
					for(k = 0; k<lbShort; k++) {
						bufIn[k] = input[band*lbLong+j*lbShort+k];
					}
					if(j==0) Array.Copy(window1, 0, window, 0, lbShort*2);
					else Array.Copy(window2, 0, window, 0, lbShort*2);
					Imdct(bufIn, bufOut, window, lbShort);
					for(k = 0; k<lbShort*2; k++) {
						output[band*lbLong*2+j*lbShort*2+k] = bufOut[k]/32.0f;
					}
				}
			}
			else {
				for(j = 0; j<lbLong; j++) {
					bufIn[j] = input[band*lbLong+j];
				}
				Imdct(bufIn, bufOut, window, lbLong);
				for(j = 0; j<lbLong*2; j++) {
					output[band*lbLong*2+j] = bufOut[j]/256.0f;
				}
			}
		}

		private void Imdct(float[] input, float[] output, float[] window, int n) {
			int n2 = n/2;
			float[][] table, table2;
			if(n==256) {
				table = IMDCTTables.IMDCT_TABLE_256;
				table2 = IMDCTTables.IMDCT_POST_TABLE_256;
			}
			else if(n==32) {
				table = IMDCTTables.IMDCT_TABLE_32;
				table2 = IMDCTTables.IMDCT_POST_TABLE_32;
			}
			else throw new AACException("gain control: unexpected IMDCT length");

			float[] tmp = new float[n];
			int i;
			for(i = 0; i<n2; ++i) {
				tmp[i] = input[2*i];
			}
			for(i = n2; i<n; ++i) {
				tmp[i] = -input[2*n-1-2*i];
			}

			//pre-twiddle
			float[][] buf = new float[n2][];
            for (i = 0; i < n2; i++)
            {
                buf[i] = new float[2];
            }
            for (i = 0; i<n2; i++) {
				buf[i][0] = (table[i][0]*tmp[2*i])-(table[i][1]*tmp[2*i+1]);
				buf[i][1] = (table[i][0]*tmp[2*i+1])+(table[i][1]*tmp[2*i]);
			}

			//fft
			FFT.Process(buf, n2);

			//post-twiddle and reordering
			for(i = 0; i<n2; i++) {
				tmp[i] = table2[i][0]*buf[i][0]+table2[i][1]*buf[n2-1-i][0]
						+table2[i][2]*buf[i][1]+table2[i][3]*buf[n2-1-i][1];
				tmp[n-1-i] = table2[i][2]*buf[i][0]-table2[i][3]*buf[n2-1-i][0]
						-table2[i][0]*buf[i][1]+table2[i][1]*buf[n2-1-i][1];
			}

			//copy to output and apply window
			Array.Copy(tmp, n2, output, 0, n2);
			for(i = n2; i<n*3/2; ++i) {
				output[i] = -tmp[n*3/2-1-i];
			}
			for(i = n*3/2; i<n*2; ++i) {
				output[i] = -tmp[i-n*3/2];
			}

			for(i = 0; i<n; i++) {
				output[i] *= window[i];
			}
		}
    }
}
