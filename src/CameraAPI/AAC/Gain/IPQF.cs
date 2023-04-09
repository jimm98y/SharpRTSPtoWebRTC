namespace CameraAPI.AAC.Gain
{
    public class IPQF : GCConstants
    {
        private float[] buf;
        private float[,] tmp1, tmp2;

        public IPQF()
        {
            buf = new float[BANDS];
            tmp1 = new float[BANDS / 2,NPQFTAPS / BANDS];
            tmp2 = new float[BANDS / 2,NPQFTAPS / BANDS];
        }

        public void Process(float[][] input, int frameLen, int maxBand, float[] output)
        {
            int i, j;
            for (i = 0; i < frameLen; i++)
            {
			    output[i] = 0.0f;
            }

            for (i = 0; i < frameLen / BANDS; i++)
            {
                for (j = 0; j < BANDS; j++)
                {
                    buf[j] = input[j][i] ;
                }
                PerformSynthesis(buf, output, i * BANDS);
            }
        }

        private void PerformSynthesis(float[] input, float[] output, int outOff)
        {
            int kk = NPQFTAPS / (2 * BANDS);
            int i, n, k;
            float acc;

            for (n = 0; n < BANDS / 2; ++n)
            {
                for (k = 0; k < 2 * kk - 1; ++k)
                {
                    tmp1[n,k] = tmp1[n,k + 1];
                    tmp2[n,k] = tmp2[n,k + 1];
                }
            }

            for (n = 0; n < BANDS / 2; ++n)
            {
                acc = 0.0f;
                for (i = 0; i < BANDS; ++i)
                {
                    acc += PQFTables.COEFS_Q0[n][i] * input[i] ;
                }
                tmp1[n,2 * kk - 1] = acc;

                acc = 0.0f;
                for (i = 0; i < BANDS; ++i)
                {
                    acc += PQFTables.COEFS_Q1[n][i] * input[i] ;
                }
                tmp2[n,2 * kk - 1] = acc;
            }

            for (n = 0; n < BANDS / 2; ++n)
            {
                acc = 0.0f;
                for (k = 0; k < kk; ++k)
                {
                    acc += PQFTables.COEFS_T0[n][k] * tmp1[n,2 * kk - 1 - 2 * k];
                }
                for (k = 0; k < kk; ++k)
                {
                    acc += PQFTables.COEFS_T1[n][k] * tmp2[n,2 * kk - 2 - 2 * k];
                }
			    output[outOff+n] = acc;

                acc = 0.0f;
                for (k = 0; k < kk; ++k)
                {
                    acc += PQFTables.COEFS_T0[BANDS - 1 - n][k] * tmp1[n,2 * kk - 1 - 2 * k];
                }
                for (k = 0; k < kk; ++k)
                {
                    acc -= PQFTables.COEFS_T1[BANDS - 1 - n][k] * tmp2[n,2 * kk - 2 - 2 * k];
                }
			    output[outOff+BANDS - 1 - n] = acc;
            }
        }
    }
}
