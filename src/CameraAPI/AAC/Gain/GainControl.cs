using CameraAPI.AAC.Syntax;
using System;
using System.Linq;
using static CameraAPI.AAC.Syntax.ICSInfo;

namespace CameraAPI.AAC.Gain
{
    public class GainControl
    {
		private int _frameLen;
		private int _lbLong;
        private int _lbShort;
		private IMDCT _imdct;
		private IPQF _ipqf;
		private float[] _buffer1, _function;
		private float[][] _buffer2, _overlap;
		private int _maxBand;
		private int[][][] _level, _levelPrev;
		private int[][][] _location, _locationPrev;

		public GainControl(int frameLen) 
		{
			this._frameLen = frameLen;
			_lbLong = frameLen / GCConstants.BANDS;
			_lbShort = _lbLong/8;
			_imdct = new IMDCT(frameLen);
			_ipqf = new IPQF();
			_levelPrev = new int[0][][];
			_locationPrev = new int[0][][];
			_buffer1 = new float[frameLen/2];
			_buffer2 = new float[GCConstants.BANDS][];
            for (int i = 0; i < GCConstants.BANDS; i++)
            {
                _buffer2[i] = new float[_lbLong];
            }
            _function = new float[_lbLong*2];
			_overlap = new float[GCConstants.BANDS][];
            for (int i = 0; i < GCConstants.BANDS; i++)
            {
                _overlap[i] = new float[_lbLong * 2];
            }
        }

		public void Decode(BitStream input, WindowSequence winSeq) 
		{
			_maxBand = input.ReadBits(2)+1;

			int wdLen, locBits, locBits2 = 0;
			switch(winSeq) {
				case WindowSequence.ONLY_LONG_SEQUENCE:
					wdLen = 1;
					locBits = 5;
					locBits2 = 5;
					break;
				case WindowSequence.EIGHT_SHORT_SEQUENCE:
					wdLen = 8;
					locBits = 2;
					locBits2 = 2;
					break;
				case WindowSequence.LONG_START_SEQUENCE:
					wdLen = 2;
					locBits = 4;
					locBits2 = 2;
					break;
				case WindowSequence.LONG_STOP_SEQUENCE:
					wdLen = 2;
					locBits = 4;
					locBits2 = 5;
					break;
				default:
					return;
			}
			_level = new int[_maxBand][][];
            for (int i = 0; i < _maxBand; i++)
            {
                _level[i] = new int[wdLen][];
            }
            _location = new int[_maxBand][][];
            for (int i = 0; i < _maxBand; i++)
            {
                _location[i] = new int[wdLen][];
            }

            int wd, k, len, bits;
			for(int bd = 1; bd<_maxBand; bd++) 
			{
				for(wd = 0; wd<wdLen; wd++) 
				{
					len = input.ReadBits(3);
					_level[bd][wd] = new int[len];
					_location[bd][wd] = new int[len];
					for(k = 0; k<len; k++) 
					{
						_level[bd][wd][k] = input.ReadBits(4);
						bits = (wd==0) ? locBits : locBits2;
						_location[bd][wd][k] = input.ReadBits(bits);
					}
				}
			}
		}

		public void Process(float[] data, int winShape, int winShapePrev, WindowSequence winSeq)
		{
			_imdct.Process(data, _buffer1, winShape, winShapePrev, winSeq);

			for(int i = 0; i < GCConstants.BANDS; i++) 
			{
				Compensate(_buffer1, _buffer2, winSeq, i);
			}

			_ipqf.Process(_buffer2, _frameLen, _maxBand, data);
		}

		/**
		 * gain compensation and overlap-add:
		 * - the gain control function is calculated
		 * - the gain control function applies to IMDCT output samples as a another IMDCT window
		 * - the reconstructed time domain signal produces by overlap-add
		 */
		private void Compensate(float[] input, float[][] output, WindowSequence winSeq, int band)
		{
			int j;
			if(winSeq.Equals(WindowSequence.EIGHT_SHORT_SEQUENCE)) 
			{
				int a, b;
				for(int k = 0; k<8; k++) 
				{
					//calculation
					CalculateFunctionData(_lbShort*2, band, winSeq, k);
					//applying
					for(j = 0; j<_lbShort*2; j++) 
					{
						a = band*_lbLong*2+k*_lbShort*2+j;
                        input[a] *= _function[j];
					}
					//overlapping
					for(j = 0; j<_lbShort; j++) 
					{
						a = j+_lbLong*7/16+_lbShort*k;
						b = band*_lbLong*2+k*_lbShort*2+j;
						_overlap[band][a] += input[b];
					}
					//store for next frame
					for(j = 0; j<_lbShort; j++) 
					{
						a = j+_lbLong*7/16+_lbShort*(k+1);
						b = band*_lbLong*2+k*_lbShort*2+_lbShort+j;

						_overlap[band][a] = input[b];
					}
					_locationPrev[band][0] = _location[band][k].ToArray();
					_levelPrev[band][0] = _level[band][k].ToArray();
				}
				Array.Copy(_overlap[band], 0, output[band], 0, _lbLong);
                Array.Copy(_overlap[band], _lbLong, _overlap[band], 0, _lbLong);
			}
			else 
			{
				//calculation
				CalculateFunctionData(_lbLong*2, band, winSeq, 0);
				//applying
				for(j = 0; j<_lbLong*2; j++) 
				{
                    input[band*_lbLong*2+j] *= _function[j];
				}
				//overlapping
				for(j = 0; j<_lbLong; j++) 
				{
                    output[band][j] = _overlap[band][j]+ input[band*_lbLong*2+j];
				}
				//store for next frame
				for(j = 0; j<_lbLong; j++)
				{
					_overlap[band][j] = input[band*_lbLong*2+_lbLong+j];
				}
				
				int lastBlock = winSeq.Equals(WindowSequence.ONLY_LONG_SEQUENCE) ? 1 : 0;
				_locationPrev[band][0] = _location[band][lastBlock].ToArray();
				_levelPrev[band][0] = _level[band][lastBlock].ToArray();
			}
		}

		//produces gain control function data, stores it in 'function' array
		private void CalculateFunctionData(int samples, int band, WindowSequence winSeq, int blockID)
		{
			int[] locA = new int[10];
			float[] levA = new float[10];
			float[] modFunc = new float[samples];
			float[] buf1 = new float[samples/2];
			float[] buf2 = new float[samples/2];
			float[] buf3 = new float[samples/2];

			int maxLocGain0 = 0, maxLocGain1 = 0, maxLocGain2 = 0;
			switch(winSeq) 
			{
				case WindowSequence.ONLY_LONG_SEQUENCE:
				case WindowSequence.EIGHT_SHORT_SEQUENCE:
					maxLocGain0 = maxLocGain1 = samples/2;
					maxLocGain2 = 0;
					break;
				case WindowSequence.LONG_START_SEQUENCE:
					maxLocGain0 = samples/2;
					maxLocGain1 = samples*7/32;
					maxLocGain2 = samples/16;
					break;
				case WindowSequence.LONG_STOP_SEQUENCE:
					maxLocGain0 = samples/16;
					maxLocGain1 = samples*7/32;
					maxLocGain2 = samples/2;
					break;
			}

			//calculate the fragment modification functions
			//for the first half region
			CalculateFMD(band, 0, true, maxLocGain0, samples, locA, levA, buf1);

			//for the latter half region
			int block = (winSeq.Equals(WindowSequence.EIGHT_SHORT_SEQUENCE)) ? blockID : 0;
			float secLevel = CalculateFMD(band, block, false, maxLocGain1, samples, locA, levA, buf2);

			//for the non-overlapped region
			if(winSeq.Equals(WindowSequence.LONG_START_SEQUENCE) || winSeq.Equals(WindowSequence.LONG_STOP_SEQUENCE)) 
			{
				CalculateFMD(band, 1, false, maxLocGain2, samples, locA, levA, buf3);
			}

			//calculate a gain modification function
			int i;
			int flatLen = 0;
			if(winSeq.Equals(WindowSequence.LONG_STOP_SEQUENCE))
			{
				flatLen = samples/2-maxLocGain0-maxLocGain1;
				for(i = 0; i<flatLen; i++) 
				{
					modFunc[i] = 1.0f;
				}
			}
			if(winSeq.Equals(WindowSequence.ONLY_LONG_SEQUENCE) || winSeq.Equals(WindowSequence.EIGHT_SHORT_SEQUENCE)) levA[0] = 1.0f;

			for(i = 0; i<maxLocGain0; i++) 
			{
				modFunc[i+flatLen] = levA[0]*secLevel*buf1[i];
			}
			for(i = 0; i<maxLocGain1; i++) 
			{
				modFunc[i+flatLen+maxLocGain0] = levA[0]*buf2[i];
			}

			if(winSeq.Equals(WindowSequence.LONG_START_SEQUENCE)) 
			{
				for(i = 0; i<maxLocGain2; i++)
				{
					modFunc[i+maxLocGain0+maxLocGain1] = buf3[i];
				}
				flatLen = samples/2-maxLocGain1-maxLocGain2;
				for(i = 0; i<flatLen; i++) 
				{
					modFunc[i+maxLocGain0+maxLocGain1+maxLocGain2] = 1.0f;
				}
			}
			else if(winSeq.Equals(WindowSequence.LONG_STOP_SEQUENCE)) 
			{
				for(i = 0; i<maxLocGain2; i++) 
				{
					modFunc[i+flatLen+maxLocGain0+maxLocGain1] = buf3[i];
				}
			}

			//calculate a gain control function
			for(i = 0; i<samples; i++) 
			{
				_function[i] = 1.0f/modFunc[i];
			}
		}

		/*
		 * calculates a fragment modification function by interpolating the gain
		 * values of the gain change positions
		 */
		private float CalculateFMD(int bd, int wd, bool prev, int maxLocGain, int samples, int[] loc, float[] lev, float[] fmd) 
		{
			 int[] m = new int[samples/2];
			 int[] lct = prev ? _locationPrev[bd][wd] : _location[bd][wd];
			 int[] lvl = prev ? _levelPrev[bd][wd] : _level[bd][wd];
			 int length = lct.Length;

			int lngain;
			int i;
			for(i = 0; i<length; i++) 
			{
				loc[i+1] = 8*lct[i]; //gainc
				lngain = GetGainChangePointID(lvl[i]); //gainc
				if(lngain<0)
					lev[i+1] = 1.0f/(float) Math.Pow(2, -lngain);
				else
					lev[i+1] = (float) Math.Pow(2, lngain);
			}

			//set start point values
			loc[0] = 0;
			if(length==0) 
				lev[0] = 1.0f;
			else 
				lev[0] = lev[1];
			float secLevel = lev[0];

			//set end point values
			loc[length+1] = maxLocGain;
			lev[length+1] = 1.0f;

			int j;
			for(i = 0; i<maxLocGain; i++) 
			{
				m[i] = 0;
				for(j = 0; j<=length+1; j++) 
				{
					if(loc[j]<=i) m[i] = j;
				}
			}

			for(i = 0; i<maxLocGain; i++) 
			{
				if((i>=loc[m[i]])&&(i<=loc[m[i]]+7))
					fmd[i] = InterpolateGain(lev[m[i]], lev[m[i]+1], i-loc[m[i]]);
				else 
					fmd[i] = lev[m[i]+1];
			}

			return secLevel;
		}

		/**
		 * transformes the exponent value of the gain to the id of the gain change
		 * point
		 */
		private int GetGainChangePointID(int lngain)
		{
			for(int i = 0; i < GCConstants.ID_GAIN; i++) 
			{
				if(lngain == GCConstants.LN_GAIN[i]) 
					return i;
			}
			return 0; //shouldn't happen
		}

		/**
		 * calculates a fragment modification function
		 * the interpolated gain value between the gain values of two gain change
		 * positions is calculated by the formula:
		 * f(a,b,j) = 2^(((8-j)log2(a)+j*log2(b))/8)
		 */
		private float InterpolateGain(float alev0, float alev1, int iloc)
		{
			float a0 = (float) (Math.Log(alev0)/Math.Log(2));
			float a1 = (float) (Math.Log(alev1)/Math.Log(2));
			return (float) Math.Pow(2.0f, (((8-iloc)*a0+iloc*a1)/8));
		}
    }
}
