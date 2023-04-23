using SharpJaad.AAC;
using SharpJaad.AAC.Syntax;
using System;

namespace SharpJaad.AAC.Huffman
{
    public static class HuffmanDec
    {
        private static readonly bool[] _UNSIGNED = { false, false, true, true, false, false, true, true, true, true, true };
        private static int _QUAD_LEN = 4, _PAIR_LEN = 2;

        private static int FindOffset(BitStream input, int[][] table)
        {
            int off = 0;
            int len = table[off][0];
            int cw = input.ReadBits(len);
            int j;
            while (cw != table[off][1])
            {
                off++;
                j = table[off][0] - len;
                len = table[off][0];
                cw <<= j;
                cw |= input.ReadBits(j);
            }
            return off;
        }

        private static void SignValues(BitStream input, int[] data, int off, int len)
        {
            for (int i = off; i < off + len; i++)
            {
                if (data[i] != 0)
                {
                    if (input.ReadBool()) data[i] = -data[i];
                }
            }
        }

        private static int GetEscape(BitStream input, int s)
        {
            bool neg = s < 0;

            int i = 4;
            while (input.ReadBool())
            {
                i++;
            }
            int j = input.ReadBits(i) | 1 << i;

            return neg ? -j : j;
        }

        public static int DecodeScaleFactor(BitStream input)
        {
            int offset = FindOffset(input, Codebooks.HCB_SF);
            return Codebooks.HCB_SF[offset][2];
        }

        public static void DecodeSpectralData(BitStream input, int cb, int[] data, int off)
        {
            int[][] HCB = Codebooks.CODEBOOKS[cb - 1];

            //find index
            int offset = FindOffset(input, HCB);

            //copy data
            data[off] = HCB[offset][2];
            data[off + 1] = HCB[offset][3];
            if (cb < 5)
            {
                data[off + 2] = HCB[offset][4];
                data[off + 3] = HCB[offset][5];
            }

            //sign & escape
            if (cb < 11)
            {
                if (_UNSIGNED[cb - 1]) SignValues(input, data, off, cb < 5 ? _QUAD_LEN : _PAIR_LEN);
            }
            else if (cb == 11 || cb > 15)
            {
                SignValues(input, data, off, cb < 5 ? _QUAD_LEN : _PAIR_LEN); //virtual codebooks are always unsigned
                if (Math.Abs(data[off]) == 16) data[off] = GetEscape(input, data[off]);
                if (Math.Abs(data[off + 1]) == 16) data[off + 1] = GetEscape(input, data[off + 1]);
            }
            else throw new AACException("Huffman: unknown spectral codebook: " + cb);
        }
    }
}
