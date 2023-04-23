using CameraAPI.AAC.Syntax;
using static CameraAPI.AAC.Syntax.ICSInfo;

namespace CameraAPI.AAC.Filterbank
{
    public class FilterBank
    {
        private float[][] _LONG_WINDOWS;// = {SINE_LONG, KBD_LONG};
		private float[][] _SHORT_WINDOWS;// = {SINE_SHORT, KBD_SHORT};
		private int _length;
		private int _shortLen;
		private int _mid;
		private int _trans;
		private MDCT _mdctShort, _mdctLong;
		private float[] _buf;
		private float[][] _overlaps;

		public FilterBank(bool smallFrames, int channels) 
		{
			if(smallFrames)
			{
				_length = Constants.WINDOW_SMALL_LEN_LONG;
				_shortLen = Constants.WINDOW_SMALL_LEN_SHORT;
				_LONG_WINDOWS = new float[][]{ SineWindows.SINE_960, KBDWindows.KBD_960 };
				_SHORT_WINDOWS = new float[][]{ SineWindows.SINE_120, KBDWindows.KBD_120 };
			}
			else 
			{
				_length = Constants.WINDOW_LEN_LONG;
				_shortLen = Constants.WINDOW_LEN_SHORT;
				_LONG_WINDOWS = new float[][]{ SineWindows.SINE_1024, KBDWindows.KBD_1024 };
				_SHORT_WINDOWS = new float[][] { SineWindows.SINE_128, KBDWindows.KBD_128 };
			}
			_mid = (_length-_shortLen)/2;
			_trans = _shortLen/2;

			_mdctShort = new MDCT(_shortLen*2);
			_mdctLong = new MDCT(_length*2);

			_overlaps = new float[channels][];
			for (int i = 0; i < channels; i++)
			{
				_overlaps[i] = new float[_length];
			}

			_buf = new float[2*_length];
		}

		public void Process(WindowSequence windowSequence, int windowShape, int windowShapePrev, float[] input, float[] output, int channel) 
		{
			int i;
			float[] overlap = _overlaps[channel];
			switch(windowSequence) {
				case WindowSequence.ONLY_LONG_SEQUENCE:
					_mdctLong.Process(input, 0, _buf, 0);
					//add second half output of previous frame to windowed output of current frame
					for(i = 0; i<_length; i++)
					{
                        output[i] = overlap[i]+(_buf[i]*_LONG_WINDOWS[windowShapePrev][i]);
					}

					//window the second half and save as overlap for next frame
					for(i = 0; i<_length; i++)
					{
						overlap[i] = _buf[_length+i]*_LONG_WINDOWS[windowShape][_length-1-i];
					}
					break;
				case WindowSequence.LONG_START_SEQUENCE:
					_mdctLong.Process(input, 0, _buf, 0);
					//add second half output of previous frame to windowed output of current frame
					for(i = 0; i<_length; i++) 
					{
                        output[i] = overlap[i]+(_buf[i]*_LONG_WINDOWS[windowShapePrev][i]);
					}

					//window the second half and save as overlap for next frame
					for(i = 0; i<_mid; i++) 
					{
						overlap[i] = _buf[_length+i];
					}
					for(i = 0; i<_shortLen; i++)
					{
						overlap[_mid+i] = _buf[_length+_mid+i]*_SHORT_WINDOWS[windowShape][_shortLen-i-1];
					}
					for(i = 0; i<_mid; i++)
					{
						overlap[_mid+_shortLen+i] = 0;
					}
					break;
				case WindowSequence.EIGHT_SHORT_SEQUENCE:
					for(i = 0; i<8; i++)
					{
						_mdctShort.Process(input, i*_shortLen, _buf, 2*i*_shortLen);
					}

					//add second half output of previous frame to windowed output of current frame
					for(i = 0; i<_mid; i++) 
					{
                        output[i] = overlap[i];
					}
					for(i = 0; i<_shortLen; i++) 
					{
                        output[_mid+i] = overlap[_mid+i]+(_buf[i]*_SHORT_WINDOWS[windowShapePrev][i]);
                        output[_mid+1*_shortLen+i] = overlap[_mid+_shortLen*1+i]+(_buf[_shortLen*1+i]*_SHORT_WINDOWS[windowShape][_shortLen-1-i])+(_buf[_shortLen*2+i]*_SHORT_WINDOWS[windowShape][i]);
                        output[_mid+2*_shortLen+i] = overlap[_mid+_shortLen*2+i]+(_buf[_shortLen*3+i]*_SHORT_WINDOWS[windowShape][_shortLen-1-i])+(_buf[_shortLen*4+i]*_SHORT_WINDOWS[windowShape][i]);
                        output[_mid+3*_shortLen+i] = overlap[_mid+_shortLen*3+i]+(_buf[_shortLen*5+i]*_SHORT_WINDOWS[windowShape][_shortLen-1-i])+(_buf[_shortLen*6+i]*_SHORT_WINDOWS[windowShape][i]);
						if(i<_trans) output[_mid+4*_shortLen+i] = overlap[_mid+_shortLen*4+i]+(_buf[_shortLen*7+i]*_SHORT_WINDOWS[windowShape][_shortLen-1-i])+(_buf[_shortLen*8+i]*_SHORT_WINDOWS[windowShape][i]);
					}

					//window the second half and save as overlap for next frame
					for(i = 0; i<_shortLen; i++)
					{
						if(i>=_trans) overlap[_mid+4*_shortLen+i-_length] = (_buf[_shortLen*7+i]*_SHORT_WINDOWS[windowShape][_shortLen-1-i])+(_buf[_shortLen*8+i]*_SHORT_WINDOWS[windowShape][i]);
						overlap[_mid+5*_shortLen+i-_length] = (_buf[_shortLen*9+i]*_SHORT_WINDOWS[windowShape][_shortLen-1-i])+(_buf[_shortLen*10+i]*_SHORT_WINDOWS[windowShape][i]);
						overlap[_mid+6*_shortLen+i-_length] = (_buf[_shortLen*11+i]*_SHORT_WINDOWS[windowShape][_shortLen-1-i])+(_buf[_shortLen*12+i]*_SHORT_WINDOWS[windowShape][i]);
						overlap[_mid+7*_shortLen+i-_length] = (_buf[_shortLen*13+i]*_SHORT_WINDOWS[windowShape][_shortLen-1-i])+(_buf[_shortLen*14+i]*_SHORT_WINDOWS[windowShape][i]);
						overlap[_mid+8*_shortLen+i-_length] = (_buf[_shortLen*15+i]*_SHORT_WINDOWS[windowShape][_shortLen-1-i]);
					}
					for(i = 0; i<_mid; i++)
					{
						overlap[_mid+_shortLen+i] = 0;
					}
					break;
				case WindowSequence.LONG_STOP_SEQUENCE:
					_mdctLong.Process(input, 0, _buf, 0);
					//add second half output of previous frame to windowed output of current frame
					//construct first half window using padding with 1's and 0's
					for(i = 0; i<_mid; i++)
					{
                        output[i] = overlap[i];
					}
					for(i = 0; i<_shortLen; i++) 
					{
                        output[_mid+i] = overlap[_mid+i]+(_buf[_mid+i]*_SHORT_WINDOWS[windowShapePrev][i]);
					}
					for(i = 0; i<_mid; i++) 
					{
						output[_mid+_shortLen+i] = overlap[_mid+_shortLen+i]+_buf[_mid+_shortLen+i];
					}
					//window the second half and save as overlap for next frame
					for(i = 0; i<_length; i++) 
					{
						overlap[i] = _buf[_length+i]*_LONG_WINDOWS[windowShape][_length-1-i];
					}
					break;
			}
		}

		//only for LTP: no overlapping, no short blocks
		public void ProcessLTP(WindowSequence windowSequence, int windowShape, int windowShapePrev, float[] input, float[] output) 
		{
			int i;

			switch(windowSequence) 
			{
				case WindowSequence.ONLY_LONG_SEQUENCE:
					for(i = _length-1; i>=0; i--) 
					{
						_buf[i] = input[i]*_LONG_WINDOWS[windowShapePrev][i];
						_buf[i+_length] = input[i+_length]*_LONG_WINDOWS[windowShape][_length-1-i];
					}
					break;

				case WindowSequence.LONG_START_SEQUENCE:
					for(i = 0; i<_length; i++) 
					{
						_buf[i] = input[i]*_LONG_WINDOWS[windowShapePrev][i];
					}
					for(i = 0; i<_mid; i++) 
					{
						_buf[i+_length] = input[i+_length];
					}
					for(i = 0; i<_shortLen; i++)
					{
						_buf[i+_length+_mid] = input[i+_length+_mid]*_SHORT_WINDOWS[windowShape][_shortLen-1-i];
					}
					for(i = 0; i<_mid; i++) 
					{
						_buf[i+_length+_mid+_shortLen] = 0;
					}
					break;

				case WindowSequence.LONG_STOP_SEQUENCE:
					for(i = 0; i<_mid; i++) 
					{
						_buf[i] = 0;
					}
					for(i = 0; i<_shortLen; i++)
					{
						_buf[i+_mid] = input[i+_mid]*_SHORT_WINDOWS[windowShapePrev][i];
					}
					for(i = 0; i<_mid; i++) 
					{
						_buf[i+_mid+_shortLen] = input[i+_mid+_shortLen];
					}
					for(i = 0; i<_length; i++) 
					{
						_buf[i+_length] = input[i+_length]*_LONG_WINDOWS[windowShape][_length-1-i];
					}
					break;
			}
			_mdctLong.ProcessForward(_buf, output);
		}

		public float[] GetOverlap(int channel) 
		{
			return _overlaps[channel];
		}
    }
}
