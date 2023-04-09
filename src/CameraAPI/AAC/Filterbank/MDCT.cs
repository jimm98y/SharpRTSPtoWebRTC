namespace CameraAPI.AAC.Filterbank
{
    public class MDCT : MDCTTables
    {
        private int N, N2, N4, N8;
		private float[][] sincos;
		private FFT fft;
		private float[,] buf;
		private float[] tmp;

		public MDCT(int length) {
			N = length;
			N2 = length>>1;
			N4 = length>>2;
			N8 = length>>3;
			switch(length) {
				case 2048:
					sincos = MDCT_TABLE_2048;
					break;
				case 256:
					sincos = MDCT_TABLE_128;
					break;
				case 1920:
					sincos = MDCT_TABLE_1920;
					break;
				case 240:
					sincos = MDCT_TABLE_240;
					break;
				default:
					throw new AACException("unsupported MDCT length: "+length);
			}
			fft = new FFT(N4);
			buf = new float[N4,2];
            tmp = new float[2];
		}

		public void Process(float[] input, int inOff, float[] output, int outOff) {
			int k;

			//pre-IFFT complex multiplication
			for(k = 0; k<N4; k++) {
				buf[k,1] = (input[inOff+2*k]*sincos[k][0])+(input[inOff+N2-1-2*k]*sincos[k][1]);
				buf[k,0] = (input[inOff+N2-1-2*k]*sincos[k][0])-(input[inOff+2*k]*sincos[k][1]);
			}

			//complex IFFT, non-scaling
			fft.Process(buf, false);

			//post-IFFT complex multiplication
			for(k = 0; k<N4; k++) {
				tmp[0] = buf[k,0];
				tmp[1] = buf[k,1];
				buf[k,1] = (tmp[1]*sincos[k][0])+(tmp[0]*sincos[k][1]);
				buf[k,0] = (tmp[0]*sincos[k][0])-(tmp[1]*sincos[k][1]);
			}

			//reordering
			for(k = 0; k<N8; k += 2) {
                output[outOff+2*k] = buf[N8+k,1];
                output[outOff+2+2*k] = buf[N8+1+k,1];

                output[outOff+1+2*k] = -buf[N8-1-k,0];
                output[outOff+3+2*k] = -buf[N8-2-k,0];

                output[outOff+N4+2*k] = buf[k,0];
                output[outOff+N4+2+2*k] = buf[1+k,0];

                output[outOff+N4+1+2*k] = -buf[N4-1-k,1];
                output[outOff+N4+3+2*k] = -buf[N4-2-k,1];

                output[outOff+N2+2*k] = buf[N8+k,0];
                output[outOff+N2+2+2*k] = buf[N8+1+k,0];

                output[outOff+N2+1+2*k] = -buf[N8-1-k,1];
                output[outOff+N2+3+2*k] = -buf[N8-2-k,1];

                output[outOff+N2+N4+2*k] = -buf[k,1];
                output[outOff+N2+N4+2+2*k] = -buf[1+k,1];

                output[outOff+N2+N4+1+2*k] = buf[N4-1-k,0];
                output[outOff+N2+N4+3+2*k] = buf[N4-2-k,0];
			}
		}

		public void ProcessForward(float[] input, float[] output) {
			int n, k;
			//pre-FFT complex multiplication
			for(k = 0; k<N8; k++) {
				n = k<<1;
				tmp[0] = input[N-N4-1-n]+ input[N-N4+n];
				tmp[1] = input[N4+n]- input[N4-1-n];

				buf[k,0] = (tmp[0]*sincos[k][0])+(tmp[1]*sincos[k][1]);
				buf[k,1] = (tmp[1]*sincos[k][0])-(tmp[0]*sincos[k][1]);

				buf[k,0] *= N;
				buf[k,1] *= N;

				tmp[0] = input[N2-1-n]-input[n];
				tmp[1] = input[N2+n]+ input[N-1-n];

				buf[k+N8,0] = (tmp[0]*sincos[k+N8][0])+(tmp[1]*sincos[k+N8][1]);
				buf[k+N8,1] = (tmp[1]*sincos[k+N8][0])-(tmp[0]*sincos[k+N8][1]);

				buf[k+N8,0] *= N;
				buf[k+N8,1] *= N;
			}

			//complex FFT, non-scaling
			fft.Process(buf, true);

			//post-FFT complex multiplication
			for(k = 0; k<N4; k++) {
				n = k<<1;

				tmp[0] = (buf[k,0]*sincos[k][0])+(buf[k,1]*sincos[k][1]);
				tmp[1] = (buf[k,1]*sincos[k][0])-(buf[k,0]*sincos[k][1]);

                output[n] = -tmp[0];
                output[N2-1-n] = tmp[1];
                output[N2+n] = -tmp[1];
                output[N-1-n] = tmp[0];
			}
		}
    }
}
