using SharpJaad.AAC.Huffman;
using System;

namespace SharpJaad.AAC.Syntax
{
    public class CCE : Element
    {
        public const int BEFORE_TNS = 0;
        public const int AFTER_TNS = 1;
        public const int AFTER_IMDCT = 2;
        private static readonly float[] CCE_SCALE =
        {
            1.09050773266525765921f,
            1.18920711500272106672f,
            1.4142135623730950488016887f,
            2f
        };
        private readonly ICStream _ics;
        //private float[] iqData;
        private int _couplingPoint;
        private int _coupledCount;
        private readonly bool[] _channelPair;
        private readonly int[] _idSelect;
        private readonly int[] _chSelect;
        /*[0] shared list of gains; [1] list of gains for right channel;
		 *[2] list of gains for left channel; [3] lists of gains for both channels
		 */
        private readonly float[,] _gain;

        public CCE(DecoderConfig config)
        {
            _ics = new ICStream(config);
            _channelPair = new bool[8];
            _idSelect = new int[8];
            _chSelect = new int[8];
            _gain = new float[16, 120];
        }

        public int GetCouplingPoint()
        {
            return _couplingPoint;
        }

        public int GetCoupledCount()
        {
            return _coupledCount;
        }

        public bool IsChannelPair(int index)
        {
            return _channelPair[index];
        }

        public int GetIDSelect(int index)
        {
            return _idSelect[index];
        }

        public int GetCHSelect(int index)
        {
            return _chSelect[index];
        }

        public void Decode(BitStream input, DecoderConfig conf)
        {
            ReadElementInstanceTag(input);
            _couplingPoint = 2 * input.ReadBit();
            _coupledCount = input.ReadBits(3);
            int gainCount = 0;
            int i;
            for (i = 0; i <= _coupledCount; i++)
            {
                gainCount++;
                _channelPair[i] = input.ReadBool();
                _idSelect[i] = input.ReadBits(4);
                if (_channelPair[i])
                {
                    _chSelect[i] = input.ReadBits(2);
                    if (_chSelect[i] == 3) gainCount++;
                }
                else _chSelect[i] = 2;
            }
            _couplingPoint += input.ReadBit();
            _couplingPoint |= _couplingPoint >> 1;

            bool sign = input.ReadBool();
            double scale = CCE_SCALE[input.ReadBits(2)];

            _ics.Decode(input, false, conf);
            ICSInfo info = _ics.GetInfo();
            int windowGroupCount = info.GetWindowGroupCount();
            int maxSFB = info.GetMaxSFB();

            int[] sfbCB = _ics.getSfbCB();
            for (i = 0; i < gainCount; i++)
            {
                int idx = 0;
                int cge = 1;
                int xg = 0;
                float gainCache = 1.0f;
                if (i > 0)
                {
                    cge = _couplingPoint == 2 ? 1 : input.ReadBit();
                    xg = cge == 0 ? 0 : HuffmanDec.DecodeScaleFactor(input) - 60;
                    gainCache = (float)Math.Pow(scale, -xg);
                }
                if (_couplingPoint == 2) _gain[i, 0] = gainCache;
                else
                {
                    int sfb;
                    for (int g = 0; g < windowGroupCount; g++)
                    {
                        for (sfb = 0; sfb < maxSFB; sfb++, idx++)
                        {
                            if (sfbCB[idx] != HCB.ZERO_HCB)
                            {
                                if (cge == 0)
                                {
                                    int t = HuffmanDec.DecodeScaleFactor(input) - 60;
                                    if (t != 0)
                                    {
                                        int s = 1;
                                        t = xg += t;
                                        if (!sign)
                                        {
                                            s -= 2 * (t & 0x1);
                                            t >>= 1;
                                        }
                                        gainCache = (float)(Math.Pow(scale, -t) * s);
                                    }
                                }
                                _gain[i, idx] = gainCache;
                            }
                        }
                    }
                }
            }
        }

        public void Process()
        {
            //iqData = ics.getInvQuantData();
        }

        public void ApplyIndependentCoupling(int index, float[] data)
        {
            double g = _gain[index, 0];
            float[] iqData = _ics.GetInvQuantData();
            for (int i = 0; i < data.Length; i++)
            {
                data[i] += (float)(g * iqData[i]);
            }
        }

        public void ApplyDependentCoupling(int index, float[] data)
        {
            ICSInfo info = _ics.GetInfo();
            int[] swbOffsets = info.GetSWBOffsets();
            int windowGroupCount = info.GetWindowGroupCount();
            int maxSFB = info.GetMaxSFB();
            int[] sfbCB = _ics.getSfbCB();
            float[] iqData = _ics.GetInvQuantData();

            int srcOff = 0;
            int dstOff = 0;

            int len, sfb, group, k, idx = 0;
            float x;
            for (int g = 0; g < windowGroupCount; g++)
            {
                len = info.GetWindowGroupLength(g);
                for (sfb = 0; sfb < maxSFB; sfb++, idx++)
                {
                    if (sfbCB[idx] != HCB.ZERO_HCB)
                    {
                        x = _gain[index, idx];
                        for (group = 0; group < len; group++)
                        {
                            for (k = swbOffsets[sfb]; k < swbOffsets[sfb + 1]; k++)
                            {
                                data[dstOff + group * 128 + k] += x * iqData[srcOff + group * 128 + k];
                            }
                        }
                    }
                }
                dstOff += len * 128;
                srcOff += len * 128;
            }
        }
    }
}
