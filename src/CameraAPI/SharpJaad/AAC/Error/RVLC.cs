using SharpJaad.AAC.Huffman;
using SharpJaad.AAC.Syntax;
using System;

namespace SharpJaad.AAC.Error
{
    public class RVLC
    {
        private const int ESCAPE_FLAG = 7;

        public void Decode(BitStream input, ICStream ics, int[][] scaleFactors)
        {
            int bits = ics.GetInfo().IsEightShortFrame() ? 11 : 9;
            bool sfConcealment = input.ReadBool();
            int revGlobalGain = input.ReadBits(8);
            int rvlcSFLen = input.ReadBits(bits);

            ICSInfo info = ics.GetInfo();
            int windowGroupCount = info.GetWindowGroupCount();
            int maxSFB = info.GetMaxSFB();
            int[][] sfbCB = null; //ics.getSectionData().getSfbCB();

            int sf = ics.GetGlobalGain();
            int intensityPosition = 0;
            int noiseEnergy = sf - 90 - 256;
            bool intensityUsed = false, noiseUsed = false;

            int sfb;
            for (int g = 0; g < windowGroupCount; g++)
            {
                for (sfb = 0; sfb < maxSFB; sfb++)
                {
                    switch (sfbCB[g][sfb])
                    {
                        case HCB.ZERO_HCB:
                            scaleFactors[g][sfb] = 0;
                            break;
                        case HCB.INTENSITY_HCB:
                        case HCB.INTENSITY_HCB2:
                            if (!intensityUsed) intensityUsed = true;
                            intensityPosition += DecodeHuffman(input);
                            scaleFactors[g][sfb] = intensityPosition;
                            break;
                        case HCB.NOISE_HCB:
                            if (noiseUsed)
                            {
                                noiseEnergy += DecodeHuffman(input);
                                scaleFactors[g][sfb] = noiseEnergy;
                            }
                            else
                            {
                                noiseUsed = true;
                                noiseEnergy = DecodeHuffman(input);
                            }
                            break;
                        default:
                            sf += DecodeHuffman(input);
                            scaleFactors[g][sfb] = sf;
                            break;
                    }
                }
            }

            int lastIntensityPosition = 0;
            if (intensityUsed) lastIntensityPosition = DecodeHuffman(input);
            noiseUsed = false;
            if (input.ReadBool()) DecodeEscapes(input, ics, scaleFactors);
        }

        private void DecodeEscapes(BitStream input, ICStream ics, int[][] scaleFactors)
        {
            ICSInfo info = ics.GetInfo();
            int windowGroupCount = info.GetWindowGroupCount();
            int maxSFB = info.GetMaxSFB();
            int[][] sfbCB = null; //ics.getSectionData().getSfbCB();

            int escapesLen = input.ReadBits(8);

            bool noiseUsed = false;

            int sfb, val;
            for (int g = 0; g < windowGroupCount; g++)
            {
                for (sfb = 0; sfb < maxSFB; sfb++)
                {
                    if (sfbCB[g][sfb] == HCB.NOISE_HCB && !noiseUsed) noiseUsed = true;
                    else if (Math.Abs(sfbCB[g][sfb]) == ESCAPE_FLAG)
                    {
                        val = DecodeHuffmanEscape(input);
                        if (sfbCB[g][sfb] == -ESCAPE_FLAG) scaleFactors[g][sfb] -= val;
                        else scaleFactors[g][sfb] += val;
                    }
                }
            }
        }

        private int DecodeHuffman(BitStream input)
        {
            int off = 0;
            int i = RVLCTables.RVLC_BOOK[off][1];
            int cw = input.ReadBits(i);

            int j;
            while (cw != RVLCTables.RVLC_BOOK[off][2] && i < 10)
            {
                off++;
                j = RVLCTables.RVLC_BOOK[off][1] - i;
                i += j;
                cw <<= j;
                cw |= input.ReadBits(j);
            }

            return RVLCTables.RVLC_BOOK[off][0];
        }

        private int DecodeHuffmanEscape(BitStream input)
        {
            int off = 0;
            int i = RVLCTables.ESCAPE_BOOK[off][1];
            int cw = input.ReadBits(i);

            int j;
            while (cw != RVLCTables.ESCAPE_BOOK[off][2] && i < 21)
            {
                off++;
                j = RVLCTables.ESCAPE_BOOK[off][1] - i;
                i += j;
                cw <<= j;
                cw |= input.ReadBits(j);
            }

            return RVLCTables.ESCAPE_BOOK[off][0];
        }
    }
}
