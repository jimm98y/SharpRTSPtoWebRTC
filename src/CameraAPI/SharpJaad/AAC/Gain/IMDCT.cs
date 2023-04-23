using System;
using SharpJaad.AAC;
using static SharpJaad.AAC.Syntax.ICSInfo;

namespace SharpJaad.AAC.Gain
{
    public class IMDCT
    {
        private static float[][] _LONG_WINDOWS = { Windows.SINE_256, Windows.KBD_256 };
        private static float[][] _SHORT_WINDOWS = { Windows.SINE_32, Windows.KBD_32 };
        private int _frameLen, _shortFrameLen, _lbLong, _lbShort, _lbMid;

        public IMDCT(int frameLen)
        {
            _frameLen = frameLen;
            _lbLong = frameLen / GCConstants.BANDS;
            _shortFrameLen = frameLen / 8;
            _lbShort = _shortFrameLen / GCConstants.BANDS;
            _lbMid = (_lbLong - _lbShort) / 2;
        }

        public void Process(float[] input, float[] output, int winShape, int winShapePrev, WindowSequence winSeq)
        {
            float[] buf = new float[_frameLen];

            int b, j, i;
            if (winSeq.Equals(WindowSequence.EIGHT_SHORT_SEQUENCE))
            {
                for (b = 0; b < GCConstants.BANDS; b++)
                {
                    for (j = 0; j < 8; j++)
                    {
                        for (i = 0; i < _lbShort; i++)
                        {
                            if (b % 2 == 0)
                                buf[_lbLong * b + _lbShort * j + i] = input[_shortFrameLen * j + _lbShort * b + i];
                            else
                                buf[_lbLong * b + _lbShort * j + i] = input[_shortFrameLen * j + _lbShort * b + _lbShort - 1 - i];
                        }
                    }
                }
            }
            else
            {
                for (b = 0; b < GCConstants.BANDS; b++)
                {
                    for (i = 0; i < _lbLong; i++)
                    {
                        if (b % 2 == 0)
                            buf[_lbLong * b + i] = input[_lbLong * b + i];
                        else
                            buf[_lbLong * b + i] = input[_lbLong * b + _lbLong - 1 - i];
                    }
                }
            }

            for (b = 0; b < GCConstants.BANDS; b++)
            {
                Process2(buf, output, winSeq, winShape, winShapePrev, b);
            }
        }

        private void Process2(float[] input, float[] output, WindowSequence winSeq, int winShape, int winShapePrev, int band)
        {
            float[] bufIn = new float[_lbLong];
            float[] bufOut = new float[_lbLong * 2];
            float[] window = new float[_lbLong * 2];
            float[] window1 = new float[_lbShort * 2];
            float[] window2 = new float[_lbShort * 2];

            //init windows
            int i;
            switch (winSeq)
            {
                case WindowSequence.ONLY_LONG_SEQUENCE:
                    for (i = 0; i < _lbLong; i++)
                    {
                        window[i] = _LONG_WINDOWS[winShapePrev][i];
                        window[_lbLong * 2 - 1 - i] = _LONG_WINDOWS[winShape][i];
                    }
                    break;
                case WindowSequence.EIGHT_SHORT_SEQUENCE:
                    for (i = 0; i < _lbShort; i++)
                    {
                        window1[i] = _SHORT_WINDOWS[winShapePrev][i];
                        window1[_lbShort * 2 - 1 - i] = _SHORT_WINDOWS[winShape][i];
                        window2[i] = _SHORT_WINDOWS[winShape][i];
                        window2[_lbShort * 2 - 1 - i] = _SHORT_WINDOWS[winShape][i];
                    }
                    break;
                case WindowSequence.LONG_START_SEQUENCE:
                    for (i = 0; i < _lbLong; i++)
                    {
                        window[i] = _LONG_WINDOWS[winShapePrev][i];
                    }
                    for (i = 0; i < _lbMid; i++)
                    {
                        window[i + _lbLong] = 1.0f;
                    }

                    for (i = 0; i < _lbShort; i++)
                    {
                        window[i + _lbMid + _lbLong] = _SHORT_WINDOWS[winShape][_lbShort - 1 - i];
                    }
                    for (i = 0; i < _lbMid; i++)
                    {
                        window[i + _lbMid + _lbLong + _lbShort] = 0.0f;
                    }
                    break;
                case WindowSequence.LONG_STOP_SEQUENCE:
                    for (i = 0; i < _lbMid; i++)
                    {
                        window[i] = 0.0f;
                    }
                    for (i = 0; i < _lbShort; i++)
                    {
                        window[i + _lbMid] = _SHORT_WINDOWS[winShapePrev][i];
                    }
                    for (i = 0; i < _lbMid; i++)
                    {
                        window[i + _lbMid + _lbShort] = 1.0f;
                    }
                    for (i = 0; i < _lbLong; i++)
                    {
                        window[i + _lbMid + _lbShort + _lbMid] = _LONG_WINDOWS[winShape][_lbLong - 1 - i];
                    }
                    break;
            }

            int j;
            if (winSeq.Equals(WindowSequence.EIGHT_SHORT_SEQUENCE))
            {
                int k;
                for (j = 0; j < 8; j++)
                {
                    for (k = 0; k < _lbShort; k++)
                    {
                        bufIn[k] = input[band * _lbLong + j * _lbShort + k];
                    }
                    if (j == 0)
                        Array.Copy(window1, 0, window, 0, _lbShort * 2);
                    else
                        Array.Copy(window2, 0, window, 0, _lbShort * 2);
                    Imdct(bufIn, bufOut, window, _lbShort);
                    for (k = 0; k < _lbShort * 2; k++)
                    {
                        output[band * _lbLong * 2 + j * _lbShort * 2 + k] = bufOut[k] / 32.0f;
                    }
                }
            }
            else
            {
                for (j = 0; j < _lbLong; j++)
                {
                    bufIn[j] = input[band * _lbLong + j];
                }
                Imdct(bufIn, bufOut, window, _lbLong);
                for (j = 0; j < _lbLong * 2; j++)
                {
                    output[band * _lbLong * 2 + j] = bufOut[j] / 256.0f;
                }
            }
        }

        private void Imdct(float[] input, float[] output, float[] window, int n)
        {
            int n2 = n / 2;
            float[][] table, table2;
            if (n == 256)
            {
                table = IMDCTTables.IMDCT_TABLE_256;
                table2 = IMDCTTables.IMDCT_POST_TABLE_256;
            }
            else if (n == 32)
            {
                table = IMDCTTables.IMDCT_TABLE_32;
                table2 = IMDCTTables.IMDCT_POST_TABLE_32;
            }
            else throw new AACException("gain control: unexpected IMDCT length");

            float[] tmp = new float[n];
            int i;
            for (i = 0; i < n2; ++i)
            {
                tmp[i] = input[2 * i];
            }
            for (i = n2; i < n; ++i)
            {
                tmp[i] = -input[2 * n - 1 - 2 * i];
            }

            //pre-twiddle
            float[][] buf = new float[n2][];
            for (i = 0; i < n2; i++)
            {
                buf[i] = new float[2];
            }
            for (i = 0; i < n2; i++)
            {
                buf[i][0] = table[i][0] * tmp[2 * i] - table[i][1] * tmp[2 * i + 1];
                buf[i][1] = table[i][0] * tmp[2 * i + 1] + table[i][1] * tmp[2 * i];
            }

            //fft
            FFT.Process(buf, n2);

            //post-twiddle and reordering
            for (i = 0; i < n2; i++)
            {
                tmp[i] = table2[i][0] * buf[i][0] + table2[i][1] * buf[n2 - 1 - i][0]
                        + table2[i][2] * buf[i][1] + table2[i][3] * buf[n2 - 1 - i][1];
                tmp[n - 1 - i] = table2[i][2] * buf[i][0] - table2[i][3] * buf[n2 - 1 - i][0]
                        - table2[i][0] * buf[i][1] + table2[i][1] * buf[n2 - 1 - i][1];
            }

            //copy to output and apply window
            Array.Copy(tmp, n2, output, 0, n2);
            for (i = n2; i < n * 3 / 2; ++i)
            {
                output[i] = -tmp[n * 3 / 2 - 1 - i];
            }
            for (i = n * 3 / 2; i < n * 2; ++i)
            {
                output[i] = -tmp[i - n * 3 / 2];
            }
            for (i = 0; i < n; i++)
            {
                output[i] *= window[i];
            }
        }
    }
}
