namespace CameraAPI.AAC.Filterbank
{
    public class MDCT
    {
        private int _N, _N2, _N4, _N8;
		private float[][] _sincos;
		private FFT _fft;
		private float[,] _buf;
		private float[] _tmp;

		public MDCT(int length) 
		{
			_N = length;
			_N2 = length>>1;
			_N4 = length>>2;
			_N8 = length>>3;
			switch(length)
			{
				case 2048:
					_sincos = MDCTTables.MDCT_TABLE_2048;
					break;
				case 256:
					_sincos = MDCTTables.MDCT_TABLE_128;
					break;
				case 1920:
					_sincos = MDCTTables.MDCT_TABLE_1920;
					break;
				case 240:
					_sincos = MDCTTables.MDCT_TABLE_240;
					break;
				default:
					throw new AACException("unsupported MDCT length: "+length);
			}
			_fft = new FFT(_N4);
			_buf = new float[_N4,2];
            _tmp = new float[2];
		}

		public void Process(float[] input, int inOff, float[] output, int outOff) 
		{
			int k;

			//pre-IFFT complex multiplication
			for(k = 0; k<_N4; k++)
			{
				_buf[k,1] = (input[inOff+2*k]*_sincos[k][0])+(input[inOff+_N2-1-2*k]*_sincos[k][1]);
				_buf[k,0] = (input[inOff+_N2-1-2*k]*_sincos[k][0])-(input[inOff+2*k]*_sincos[k][1]);
			}

			//complex IFFT, non-scaling
			_fft.Process(_buf, false);

			//post-IFFT complex multiplication
			for(k = 0; k<_N4; k++) 
			{
				_tmp[0] = _buf[k,0];
				_tmp[1] = _buf[k,1];
				_buf[k,1] = (_tmp[1]*_sincos[k][0])+(_tmp[0]*_sincos[k][1]);
				_buf[k,0] = (_tmp[0]*_sincos[k][0])-(_tmp[1]*_sincos[k][1]);
			}

			//reordering
			for(k = 0; k<_N8; k += 2) 
			{
                output[outOff+2*k] = _buf[_N8+k,1];
                output[outOff+2+2*k] = _buf[_N8+1+k,1];

                output[outOff+1+2*k] = -_buf[_N8-1-k,0];
                output[outOff+3+2*k] = -_buf[_N8-2-k,0];

                output[outOff+_N4+2*k] = _buf[k,0];
                output[outOff+_N4+2+2*k] = _buf[1+k,0];

                output[outOff+_N4+1+2*k] = -_buf[_N4-1-k,1];
                output[outOff+_N4+3+2*k] = -_buf[_N4-2-k,1];

                output[outOff+_N2+2*k] = _buf[_N8+k,0];
                output[outOff+_N2+2+2*k] = _buf[_N8+1+k,0];

                output[outOff+_N2+1+2*k] = -_buf[_N8-1-k,1];
                output[outOff+_N2+3+2*k] = -_buf[_N8-2-k,1];

                output[outOff+_N2+_N4+2*k] = -_buf[k,1];
                output[outOff+_N2+_N4+2+2*k] = -_buf[1+k,1];

                output[outOff+_N2+_N4+1+2*k] = _buf[_N4-1-k,0];
                output[outOff+_N2+_N4+3+2*k] = _buf[_N4-2-k,0];
			}
		}

		public void ProcessForward(float[] input, float[] output)
		{
			int n, k;
			//pre-FFT complex multiplication
			for(k = 0; k<_N8; k++)
			{
				n = k<<1;
				_tmp[0] = input[_N-_N4-1-n]+ input[_N-_N4+n];
				_tmp[1] = input[_N4+n]- input[_N4-1-n];

				_buf[k,0] = (_tmp[0]*_sincos[k][0])+(_tmp[1]*_sincos[k][1]);
				_buf[k,1] = (_tmp[1]*_sincos[k][0])-(_tmp[0]*_sincos[k][1]);

				_buf[k,0] *= _N;
				_buf[k,1] *= _N;

				_tmp[0] = input[_N2-1-n]-input[n];
				_tmp[1] = input[_N2+n]+ input[_N-1-n];

				_buf[k+_N8,0] = (_tmp[0]*_sincos[k+_N8][0])+(_tmp[1]*_sincos[k+_N8][1]);
				_buf[k+_N8,1] = (_tmp[1]*_sincos[k+_N8][0])-(_tmp[0]*_sincos[k+_N8][1]);

				_buf[k+_N8,0] *= _N;
				_buf[k+_N8,1] *= _N;
			}

			//complex FFT, non-scaling
			_fft.Process(_buf, true);

			//post-FFT complex multiplication
			for(k = 0; k<_N4; k++) 
			{
				n = k<<1;

				_tmp[0] = (_buf[k,0]*_sincos[k][0])+(_buf[k,1]*_sincos[k][1]);
				_tmp[1] = (_buf[k,1]*_sincos[k][0])-(_buf[k,0]*_sincos[k][1]);

                output[n] = -_tmp[0];
                output[_N2-1-n] = _tmp[1];
                output[_N2+n] = -_tmp[1];
                output[_N-1-n] = _tmp[0];
			}
		}
    }
}
