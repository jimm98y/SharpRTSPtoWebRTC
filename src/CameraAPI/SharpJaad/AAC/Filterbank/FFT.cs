using SharpJaad.AAC;

namespace SharpJaad.AAC.Filterbank
{
    public class FFT
    {
        private int _length;
        private float[,] _roots;
        private float[,] _rev;
        private float[] _a, _b, _c, _d, _e1, _e2;

        public FFT(int length)
        {
            _length = length;

            switch (length)
            {
                case 64:
                    _roots = FFTables.FFT_TABLE_64;
                    break;
                case 512:
                    _roots = FFTables.FFT_TABLE_512;
                    break;
                case 60:
                    _roots = FFTables.FFT_TABLE_60;
                    break;
                case 480:
                    _roots = FFTables.FFT_TABLE_480;
                    break;
                default:
                    throw new AACException("unexpected FFT length: " + length);
            }

            //processing buffers
            _rev = new float[length, 2];
            _a = new float[2];
            _b = new float[2];
            _c = new float[2];
            _d = new float[2];
            _e1 = new float[2];
            _e2 = new float[2];
        }

        public void Process(float[,] input, bool forward)
        {
            int imOff = forward ? 2 : 1;
            int scale = 1;
            //bit-reversal
            int ii = 0;
            for (int i = 0; i < _length; i++)
            {
                _rev[i, 0] = input[ii, 0];
                _rev[i, 1] = input[ii, 1];
                int k = _length >> 1;
                while (ii >= k && k > 0)
                {
                    ii -= k;
                    k >>= 1;
                }
                ii += k;
            }
            for (int i = 0; i < _length; i++)
            {
                input[i, 0] = _rev[i, 0];
                input[i, 1] = _rev[i, 1];
            }

            //bottom base-4 round
            for (int i = 0; i < _length; i += 4)
            {
                _a[0] = input[i, 0] + input[i + 1, 0];
                _a[1] = input[i, 1] + input[i + 1, 1];
                _b[0] = input[i + 2, 0] + input[i + 3, 0];
                _b[1] = input[i + 2, 1] + input[i + 3, 1];
                _c[0] = input[i, 0] - input[i + 1, 0];
                _c[1] = input[i, 1] - input[i + 1, 1];
                _d[0] = input[i + 2, 0] - input[i + 3, 0];
                _d[1] = input[i + 2, 1] - input[i + 3, 1];
                input[i, 0] = _a[0] + _b[0];
                input[i, 1] = _a[1] + _b[1];
                input[i + 2, 0] = _a[0] - _b[0];
                input[i + 2, 1] = _a[1] - _b[1];

                _e1[0] = _c[0] - _d[1];
                _e1[1] = _c[1] + _d[0];
                _e2[0] = _c[0] + _d[1];
                _e2[1] = _c[1] - _d[0];
                if (forward)
                {
                    input[i + 1, 0] = _e2[0];
                    input[i + 1, 1] = _e2[1];
                    input[i + 3, 0] = _e1[0];
                    input[i + 3, 1] = _e1[1];
                }
                else
                {
                    input[i + 1, 0] = _e1[0];
                    input[i + 1, 1] = _e1[1];
                    input[i + 3, 0] = _e2[0];
                    input[i + 3, 1] = _e2[1];
                }
            }

            //iterations from bottom to top
            int shift, m, km;
            float rootRe, rootIm, zRe, zIm;
            for (int i = 4; i < _length; i <<= 1)
            {
                shift = i << 1;
                m = _length / shift;
                for (int j = 0; j < _length; j += shift)
                {
                    for (int k = 0; k < i; k++)
                    {
                        km = k * m;
                        rootRe = _roots[km, 0];
                        rootIm = _roots[km, imOff];
                        zRe = input[i + j + k, 0] * rootRe - input[i + j + k, 1] * rootIm;
                        zIm = input[i + j + k, 0] * rootIm + input[i + j + k, 1] * rootRe;

                        input[i + j + k, 0] = (input[j + k, 0] - zRe) * scale;
                        input[i + j + k, 1] = (input[j + k, 1] - zIm) * scale;
                        input[j + k, 0] = (input[j + k, 0] + zRe) * scale;
                        input[j + k, 1] = (input[j + k, 1] + zIm) * scale;
                    }
                }
            }
        }
    }
}
