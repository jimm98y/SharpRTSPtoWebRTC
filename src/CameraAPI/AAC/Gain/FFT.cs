using System;

namespace CameraAPI.AAC.Gain
{
    public class FFT
    {
        public static float[][] FFT_TABLE_128 = {
            new float[] {1.0f, -0.0f},
            new float[] {0.99879545f, -0.049067676f},
            new float[] {0.9951847f, -0.09801714f},
            new float[] {0.9891765f, -0.14673047f},
            new float[] {0.98078525f, -0.19509032f},
            new float[] {0.97003126f, -0.24298018f},
            new float[] {0.95694035f, -0.29028466f},
            new float[] {0.94154406f, -0.33688986f},
            new float[] {0.9238795f, -0.38268343f},
            new float[] {0.9039893f, -0.42755508f},
            new float[] {0.8819213f, -0.47139674f},
            new float[] {0.8577286f, -0.51410276f},
            new float[] {0.8314696f, -0.55557024f},
            new float[] {0.8032075f, -0.5956993f},
            new float[] {0.77301043f, -0.6343933f},
            new float[] {0.7409511f, -0.671559f},
            new float[] {0.70710677f, -0.70710677f},
            new float[] {0.671559f, -0.7409511f},
            new float[] {0.6343933f, -0.77301043f},
            new float[] {0.5956993f, -0.8032075f},
            new float[] {0.55557024f, -0.8314696f},
            new float[] {0.51410276f, -0.8577286f},
            new float[] {0.47139674f, -0.8819213f},
            new float[] {0.42755508f, -0.9039893f},
            new float[] {0.38268343f, -0.9238795f},
            new float[] {0.33688986f, -0.94154406f},
            new float[] {0.29028466f, -0.95694035f},
            new float[] {0.24298018f, -0.97003126f},
            new float[] {0.19509032f, -0.98078525f},
            new float[] {0.14673047f, -0.9891765f},
            new float[] {0.09801714f, -0.9951847f},
            new float[] {0.049067676f, -0.99879545f},
            new float[] {6.123234E-17f, -1.0f},
            new float[] {-0.049067676f, -0.99879545f},
            new float[] {-0.09801714f, -0.9951847f},
            new float[] {-0.14673047f, -0.9891765f},
            new float[] {-0.19509032f, -0.98078525f},
            new float[] {-0.24298018f, -0.97003126f},
            new float[] {-0.29028466f, -0.95694035f},
            new float[] {-0.33688986f, -0.94154406f},
            new float[] {-0.38268343f, -0.9238795f},
            new float[] {-0.42755508f, -0.9039893f},
            new float[] {-0.47139674f, -0.8819213f},
            new float[] {-0.51410276f, -0.8577286f},
            new float[] {-0.55557024f, -0.8314696f},
            new float[] {-0.5956993f, -0.8032075f},
            new float[] {-0.6343933f, -0.77301043f},
            new float[] {-0.671559f, -0.7409511f},
            new float[] {-0.70710677f, -0.70710677f},
            new float[] {-0.7409511f, -0.671559f},
            new float[] {-0.77301043f, -0.6343933f},
            new float[] {-0.8032075f, -0.5956993f},
            new float[] {-0.8314696f, -0.55557024f},
            new float[] {-0.8577286f, -0.51410276f},
            new float[] {-0.8819213f, -0.47139674f},
            new float[] {-0.9039893f, -0.42755508f},
            new float[] {-0.9238795f, -0.38268343f},
            new float[] {-0.94154406f, -0.33688986f},
            new float[] {-0.95694035f, -0.29028466f},
            new float[] {-0.97003126f, -0.24298018f},
            new float[] {-0.98078525f, -0.19509032f},
            new float[] {-0.9891765f, -0.14673047f},
            new float[] {-0.9951847f, -0.09801714f},
            new float[] {-0.99879545f, -0.049067676f}
        };
        public static float[][] FFT_TABLE_16 = {
            new float[] {1.0f, -0.0f},
            new float[] {0.9238795f, -0.38268343f},
            new float[] {0.70710677f, -0.70710677f},
            new float[] {0.38268343f, -0.9238795f},
            new float[] {6.123234E-17f, -1.0f},
            new float[] {-0.38268343f, -0.9238795f},
            new float[] {-0.70710677f, -0.70710677f},
            new float[] {-0.9238795f, -0.38268343f}
        };

        public static void Process(float[][] input, int n)
        {
            int ln = (int)Math.Round(Math.Log(n) / Math.Log(2));
            float[][] table = (n == 128) ? FFT_TABLE_128 : FFT_TABLE_16;

            //bit-reversal
            float[][] rev = new float[n][];
            int i, ii = 0;
            for (i = 0; i < n; i++)
            {
                rev[i][0] = input[ii][0];
                rev[i][1] = input[ii][1];
                int kk = n >> 1;
                while (ii >= kk && kk > 0)
                {
                    ii -= kk;
                    kk >>= 1;
                }
                ii += kk;
            }

            for (i = 0; i < n; i++)
            {
                input[i][0] = rev[i][0];
                input[i][1] = rev[i][1];
            }

            //calculation
            int blocks = n / 2;
            int size = 2;
            int j, k, l, k0, k1, size2;
            float[] a = new float[2];
            for (i = 0; i < ln; i++)
            {
                size2 = size / 2;
                k0 = 0;
                k1 = size2;
                for (j = 0; j < blocks; ++j)
                {
                    l = 0;
                    for (k = 0; k < size2; ++k)
                    {
                        a[0] = input[k1][0]*table[l][0] - input[k1][1]*table[l][1];
                        a[1] = input[k1][0]*table[l][1] + input[k1][1]*table[l][0];
                        input[k1][0] = input[k0][0]-a[0];
                        input[k1][1] = input[k0][1]-a[1];
                        input[k0][0] += a[0];
                        input[k0][1] += a[1];
                        l += blocks;
                        k0++;
                        k1++;
                    }
                    k0 += size2;
                    k1 += size2;
                }
                blocks = blocks / 2;
                size = size * 2;
            }
        }
    }
}
