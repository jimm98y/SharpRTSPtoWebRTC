using CameraAPI.AAC.Tools;

namespace CameraAPI.AAC.Sbr
{
    public class AnalysisFilterbank
    {
        private float[] _x; //x is implemented as double ringbuffer
        private int _xIndex; //ringbuffer index
        private int _channels;

        public AnalysisFilterbank(int channels)
        {
            this._channels = channels;
            _x = new float[2 * channels * 10];
            _xIndex = 0;
        }

        public void Reset()
        {
            Arrays.Fill(_x, 0);
        }

        public void SbrQmfAnalysis32(SBR sbr, float[] input, float[,,,] X, int ch, int offset, int kx)
        {
            float[] u = new float[64];
            float[] in_real = new float[32], in_imag = new float[32];
            float[] out_real = new float[32], out_imag = new float[32];
            int iin = 0;
            int l;

            /* qmf subsample l */
            for (l = 0; l < sbr._numTimeSlotsRate; l++)
            {
                int n;

                /* shift input buffer x */
                /* input buffer is not shifted anymore, x is implemented as double ringbuffer */
                //memmove(qmfa.x + 32, qmfa.x, (320-32)*sizeof(real_t));

                /* add new samples to input buffer x */
                for (n = 32 - 1; n >= 0; n--)
                {
                    this._x[this._xIndex + n] = this._x[this._xIndex + n + 320] = input[iin++];
                }

                /* window and summation to create array u */
                for (n = 0; n < 64; n++)
                {
                    u[n] = (this._x[this._xIndex + n] * FilterbankTable.qmf_c[2 * n])
                        + (this._x[this._xIndex + n + 64] * FilterbankTable.qmf_c[2 * (n + 64)])
                        + (this._x[this._xIndex + n + 128] * FilterbankTable.qmf_c[2 * (n + 128)])
                        + (this._x[this._xIndex + n + 192] * FilterbankTable.qmf_c[2 * (n + 192)])
                        + (this._x[this._xIndex + n + 256] * FilterbankTable.qmf_c[2 * (n + 256)]);
                }

                /* update ringbuffer index */
                this._xIndex -= 32;
                if (this._xIndex < 0)
                    this._xIndex = (320 - 32);

                /* calculate 32 subband samples by introducing X */
                // Reordering of data moved from DCT_IV to here
                in_imag[31] = u[1];
                in_real[0] = u[0];
                for (n = 1; n < 31; n++)
                {
                    in_imag[31 - n] = u[n + 1];
                    in_real[n] = -u[64 - n];
                }
                in_imag[0] = u[32];
                in_real[31] = -u[33];

                // dct4_kernel is DCT_IV without reordering which is done before and after FFT
                DCT.Dct4Kernel(in_real, in_imag, out_real, out_imag);

                // Reordering of data moved from DCT_IV to here
                for (n = 0; n < 16; n++)
                {
                    if (2 * n + 1 < kx)
                    {
                        X[ch,l + offset,2 * n,0] = 2.0f * out_real[n];
                        X[ch, l + offset,2 * n,1] = 2.0f * out_imag[n];
                        X[ch, l + offset,2 * n + 1,0] = -2.0f * out_imag[31 - n];
                        X[ch, l + offset,2 * n + 1,1] = -2.0f * out_real[31 - n];
                    }
                    else
                    {
                        if (2 * n < kx)
                        {
                            X[ch,l + offset,2 * n,0] = 2.0f * out_real[n];
                            X[ch,l + offset,2 * n,1] = 2.0f * out_imag[n];
                        }
                        else
                        {
                            X[ch,l + offset,2 * n,0] = 0;
                            X[ch,l + offset,2 * n,1] = 0;
                        }
                        X[ch,l + offset,2 * n + 1,0] = 0;
                        X[ch,l + offset,2 * n + 1,1] = 0;
                    }
                }
            }
        }
    }
}
