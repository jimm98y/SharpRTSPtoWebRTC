using CameraAPI.AAC.Syntax;
using static CameraAPI.AAC.Syntax.ICSInfo;

namespace CameraAPI.AAC.Filterbank
{
    public class FilterBank : Constants
    {
        private float[][] LONG_WINDOWS;// = {SINE_LONG, KBD_LONG};
		private float[][] SHORT_WINDOWS;// = {SINE_SHORT, KBD_SHORT};
		private int length;
		private int shortLen;
		private int mid;
		private int trans;
		private MDCT mdctShort, mdctLong;
		private float[] buf;
		private float[][] overlaps;

		public FilterBank(bool smallFrames, int channels) {
			if(smallFrames) {
				length = WINDOW_SMALL_LEN_LONG;
				shortLen = WINDOW_SMALL_LEN_SHORT;
				LONG_WINDOWS = new float[][]{ SineWindows.SINE_960, KBDWindows.KBD_960 };
				SHORT_WINDOWS = new float[][]{ SineWindows.SINE_120, KBDWindows.KBD_120 };
			}
			else {
				length = WINDOW_LEN_LONG;
				shortLen = WINDOW_LEN_SHORT;
				LONG_WINDOWS = new float[][]{ SineWindows.SINE_1024, KBDWindows.KBD_1024 };
				SHORT_WINDOWS = new float[][] { SineWindows.SINE_128, KBDWindows.KBD_128 };
			}
			mid = (length-shortLen)/2;
			trans = shortLen/2;

			mdctShort = new MDCT(shortLen*2);
			mdctLong = new MDCT(length*2);

			overlaps = new float[channels][];
			for (int i = 0; i < channels; i++)
			{
				overlaps[i] = new float[length];
			}

			buf = new float[2*length];
		}

		public void Process(WindowSequence windowSequence, int windowShape, int windowShapePrev, float[] input, float[] output, int channel) {
			int i;
			float[] overlap = overlaps[channel];
			switch(windowSequence) {
				case WindowSequence.ONLY_LONG_SEQUENCE:
					mdctLong.Process(input, 0, buf, 0);
					//add second half output of previous frame to windowed output of current frame
					for(i = 0; i<length; i++) {
                        output[i] = overlap[i]+(buf[i]*LONG_WINDOWS[windowShapePrev][i]);
					}

					//window the second half and save as overlap for next frame
					for(i = 0; i<length; i++) {
						overlap[i] = buf[length+i]*LONG_WINDOWS[windowShape][length-1-i];
					}
					break;
				case WindowSequence.LONG_START_SEQUENCE:
					mdctLong.Process(input, 0, buf, 0);
					//add second half output of previous frame to windowed output of current frame
					for(i = 0; i<length; i++) {
                        output[i] = overlap[i]+(buf[i]*LONG_WINDOWS[windowShapePrev][i]);
					}

					//window the second half and save as overlap for next frame
					for(i = 0; i<mid; i++) {
						overlap[i] = buf[length+i];
					}
					for(i = 0; i<shortLen; i++) {
						overlap[mid+i] = buf[length+mid+i]*SHORT_WINDOWS[windowShape][shortLen-i-1];
					}
					for(i = 0; i<mid; i++) {
						overlap[mid+shortLen+i] = 0;
					}
					break;
				case WindowSequence.EIGHT_SHORT_SEQUENCE:
					for(i = 0; i<8; i++) {
						mdctShort.Process(input, i*shortLen, buf, 2*i*shortLen);
					}

					//add second half output of previous frame to windowed output of current frame
					for(i = 0; i<mid; i++) {
                        output[i] = overlap[i];
					}
					for(i = 0; i<shortLen; i++) {
                        output[mid+i] = overlap[mid+i]+(buf[i]*SHORT_WINDOWS[windowShapePrev][i]);
                        output[mid+1*shortLen+i] = overlap[mid+shortLen*1+i]+(buf[shortLen*1+i]*SHORT_WINDOWS[windowShape][shortLen-1-i])+(buf[shortLen*2+i]*SHORT_WINDOWS[windowShape][i]);
                        output[mid+2*shortLen+i] = overlap[mid+shortLen*2+i]+(buf[shortLen*3+i]*SHORT_WINDOWS[windowShape][shortLen-1-i])+(buf[shortLen*4+i]*SHORT_WINDOWS[windowShape][i]);
                        output[mid+3*shortLen+i] = overlap[mid+shortLen*3+i]+(buf[shortLen*5+i]*SHORT_WINDOWS[windowShape][shortLen-1-i])+(buf[shortLen*6+i]*SHORT_WINDOWS[windowShape][i]);
						if(i<trans) output[mid+4*shortLen+i] = overlap[mid+shortLen*4+i]+(buf[shortLen*7+i]*SHORT_WINDOWS[windowShape][shortLen-1-i])+(buf[shortLen*8+i]*SHORT_WINDOWS[windowShape][i]);
					}

					//window the second half and save as overlap for next frame
					for(i = 0; i<shortLen; i++) {
						if(i>=trans) overlap[mid+4*shortLen+i-length] = (buf[shortLen*7+i]*SHORT_WINDOWS[windowShape][shortLen-1-i])+(buf[shortLen*8+i]*SHORT_WINDOWS[windowShape][i]);
						overlap[mid+5*shortLen+i-length] = (buf[shortLen*9+i]*SHORT_WINDOWS[windowShape][shortLen-1-i])+(buf[shortLen*10+i]*SHORT_WINDOWS[windowShape][i]);
						overlap[mid+6*shortLen+i-length] = (buf[shortLen*11+i]*SHORT_WINDOWS[windowShape][shortLen-1-i])+(buf[shortLen*12+i]*SHORT_WINDOWS[windowShape][i]);
						overlap[mid+7*shortLen+i-length] = (buf[shortLen*13+i]*SHORT_WINDOWS[windowShape][shortLen-1-i])+(buf[shortLen*14+i]*SHORT_WINDOWS[windowShape][i]);
						overlap[mid+8*shortLen+i-length] = (buf[shortLen*15+i]*SHORT_WINDOWS[windowShape][shortLen-1-i]);
					}
					for(i = 0; i<mid; i++) {
						overlap[mid+shortLen+i] = 0;
					}
					break;
				case WindowSequence.LONG_STOP_SEQUENCE:
					mdctLong.Process(input, 0, buf, 0);
					//add second half output of previous frame to windowed output of current frame
					//construct first half window using padding with 1's and 0's
					for(i = 0; i<mid; i++) {
                        output[i] = overlap[i];
					}
					for(i = 0; i<shortLen; i++) {
                        output[mid+i] = overlap[mid+i]+(buf[mid+i]*SHORT_WINDOWS[windowShapePrev][i]);
					}
					for(i = 0; i<mid; i++) {
						output[mid+shortLen+i] = overlap[mid+shortLen+i]+buf[mid+shortLen+i];
					}
					//window the second half and save as overlap for next frame
					for(i = 0; i<length; i++) {
						overlap[i] = buf[length+i]*LONG_WINDOWS[windowShape][length-1-i];
					}
					break;
			}
		}

		//only for LTP: no overlapping, no short blocks
		public void ProcessLTP(WindowSequence windowSequence, int windowShape, int windowShapePrev, float[] input, float[] output) {
			int i;

			switch(windowSequence) {
				case WindowSequence.ONLY_LONG_SEQUENCE:
					for(i = length-1; i>=0; i--) {
						buf[i] = input[i]*LONG_WINDOWS[windowShapePrev][i];
						buf[i+length] = input[i+length]*LONG_WINDOWS[windowShape][length-1-i];
					}
					break;

				case WindowSequence.LONG_START_SEQUENCE:
					for(i = 0; i<length; i++) {
						buf[i] = input[i]*LONG_WINDOWS[windowShapePrev][i];
					}
					for(i = 0; i<mid; i++) {
						buf[i+length] = input[i+length];
					}
					for(i = 0; i<shortLen; i++) {
						buf[i+length+mid] = input[i+length+mid]*SHORT_WINDOWS[windowShape][shortLen-1-i];
					}
					for(i = 0; i<mid; i++) {
						buf[i+length+mid+shortLen] = 0;
					}
					break;

				case WindowSequence.LONG_STOP_SEQUENCE:
					for(i = 0; i<mid; i++) {
						buf[i] = 0;
					}
					for(i = 0; i<shortLen; i++) {
						buf[i+mid] = input[i+mid]*SHORT_WINDOWS[windowShapePrev][i];
					}
					for(i = 0; i<mid; i++) {
						buf[i+mid+shortLen] = input[i+mid+shortLen];
					}
					for(i = 0; i<length; i++) {
						buf[i+length] = input[i+length]*LONG_WINDOWS[windowShape][length-1-i];
					}
					break;
			}
			mdctLong.ProcessForward(buf, output);
		}

		public float[] GetOverlap(int channel) {
			return overlaps[channel];
		}
    }
}
