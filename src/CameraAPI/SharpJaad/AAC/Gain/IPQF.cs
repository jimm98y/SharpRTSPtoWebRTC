namespace SharpJaad.AAC.Gain
{
    public class IPQF
    {
        private float[] _buf;
        private float[,] _tmp1, _tmp2;

        public IPQF()
        {
            _buf = new float[GCConstants.BANDS];
            _tmp1 = new float[GCConstants.BANDS / 2, GCConstants.NPQFTAPS / GCConstants.BANDS];
            _tmp2 = new float[GCConstants.BANDS / 2, GCConstants.NPQFTAPS / GCConstants.BANDS];
        }

        public void Process(float[][] input, int frameLen, int maxBand, float[] output)
        {
            int i, j;
            for (i = 0; i < frameLen; i++)
            {
                output[i] = 0.0f;
            }

            for (i = 0; i < frameLen / GCConstants.BANDS; i++)
            {
                for (j = 0; j < GCConstants.BANDS; j++)
                {
                    _buf[j] = input[j][i];
                }
                PerformSynthesis(_buf, output, i * GCConstants.BANDS);
            }
        }

        private void PerformSynthesis(float[] input, float[] output, int outOff)
        {
            int kk = GCConstants.NPQFTAPS / (2 * GCConstants.BANDS);
            int i, n, k;
            float acc;

            for (n = 0; n < GCConstants.BANDS / 2; ++n)
            {
                for (k = 0; k < 2 * kk - 1; ++k)
                {
                    _tmp1[n, k] = _tmp1[n, k + 1];
                    _tmp2[n, k] = _tmp2[n, k + 1];
                }
            }

            for (n = 0; n < GCConstants.BANDS / 2; ++n)
            {
                acc = 0.0f;
                for (i = 0; i < GCConstants.BANDS; ++i)
                {
                    acc += PQFTables.COEFS_Q0[n][i] * input[i];
                }
                _tmp1[n, 2 * kk - 1] = acc;

                acc = 0.0f;
                for (i = 0; i < GCConstants.BANDS; ++i)
                {
                    acc += PQFTables.COEFS_Q1[n][i] * input[i];
                }
                _tmp2[n, 2 * kk - 1] = acc;
            }

            for (n = 0; n < GCConstants.BANDS / 2; ++n)
            {
                acc = 0.0f;
                for (k = 0; k < kk; ++k)
                {
                    acc += PQFTables.COEFS_T0[n][k] * _tmp1[n, 2 * kk - 1 - 2 * k];
                }
                for (k = 0; k < kk; ++k)
                {
                    acc += PQFTables.COEFS_T1[n][k] * _tmp2[n, 2 * kk - 2 - 2 * k];
                }
                output[outOff + n] = acc;

                acc = 0.0f;
                for (k = 0; k < kk; ++k)
                {
                    acc += PQFTables.COEFS_T0[GCConstants.BANDS - 1 - n][k] * _tmp1[n, 2 * kk - 1 - 2 * k];
                }
                for (k = 0; k < kk; ++k)
                {
                    acc -= PQFTables.COEFS_T1[GCConstants.BANDS - 1 - n][k] * _tmp2[n, 2 * kk - 2 - 2 * k];
                }
                output[outOff + GCConstants.BANDS - 1 - n] = acc;
            }
        }
    }
}
