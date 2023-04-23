using CameraAPI.AAC.Huffman;
using CameraAPI.AAC.Syntax;
using System;

namespace CameraAPI.AAC.Error
{
    public class HCR
    {
        private class Codeword
        {
            public int _cb;
            public int _decoded;
            public int _sp_offset;
            public BitsBuffer _bits;

            public void Fill(int sp, int cb)
            {
                _sp_offset = sp;
                this._cb = cb;
                _decoded = 0;
                _bits = new BitsBuffer();
            }
        }

        private static readonly int NUM_CB = 6;
        private static readonly int NUM_CB_ER = 22;
        //private static readonly int MAX_CB = 32;
        private static readonly int VCB11_FIRST = 16;
        private static readonly int VCB11_LAST = 31;
        private static readonly int[] PRE_SORT_CB_STD = { 11, 9, 7, 5, 3, 1 };
        private static readonly int[] PRE_SORT_CB_ER = { 11, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 9, 7, 5, 3, 1 };
        private static readonly int[] MAX_CW_LEN = {0, 11, 9, 20, 16, 13, 11, 14, 12, 17, 14, 49,
            0, 0, 0, 0, 14, 17, 21, 21, 25, 25, 29, 29, 29, 29, 33, 33, 33, 37, 37, 41};
        //bit-twiddling helpers
        private static readonly int[] S = { 1, 2, 4, 8, 16 };
        private static readonly int[] B = { 0x55555555, 0x33333333, 0x0F0F0F0F, 0x00FF00FF, 0x0000FFFF };

        //32 bit rewind and reverse
        private static int RewindReverse(int v, int len)
        {
            v = ((v >> S[0]) & B[0]) | ((v << S[0]) & ~B[0]);
            v = ((v >> S[1]) & B[1]) | ((v << S[1]) & ~B[1]);
            v = ((v >> S[2]) & B[2]) | ((v << S[2]) & ~B[2]);
            v = ((v >> S[3]) & B[3]) | ((v << S[3]) & ~B[3]);
            v = ((v >> S[4]) & B[4]) | ((v << S[4]) & ~B[4]);

            //shift off low bits
            v >>= (32 - len);

            return v;
        }

        //64 bit rewind and reverse
        public static int[] RewindReverse64(int hi, int lo, int len)
        {
            int[] i = new int[2];
            if (len <= 32)
            {
                i[0] = 0;
                i[1] = RewindReverse(lo, len);
            }
            else
            {
                lo = ((lo >> S[0]) & B[0]) | ((lo << S[0]) & ~B[0]);
                hi = ((hi >> S[0]) & B[0]) | ((hi << S[0]) & ~B[0]);
                lo = ((lo >> S[1]) & B[1]) | ((lo << S[1]) & ~B[1]);
                hi = ((hi >> S[1]) & B[1]) | ((hi << S[1]) & ~B[1]);
                lo = ((lo >> S[2]) & B[2]) | ((lo << S[2]) & ~B[2]);
                hi = ((hi >> S[2]) & B[2]) | ((hi << S[2]) & ~B[2]);
                lo = ((lo >> S[3]) & B[3]) | ((lo << S[3]) & ~B[3]);
                hi = ((hi >> S[3]) & B[3]) | ((hi << S[3]) & ~B[3]);
                lo = ((lo >> S[4]) & B[4]) | ((lo << S[4]) & ~B[4]);
                hi = ((hi >> S[4]) & B[4]) | ((hi << S[4]) & ~B[4]);

                //shift off low bits
                i[1] = (hi >> (64 - len)) | (lo << (len - 32));
                i[1] = lo >> (64 - len);
            }
            return i;
        }

        private static bool IsGoodCB(int cb, int sectCB)
        {
            bool b = false;
            if ((sectCB > HCB.ZERO_HCB && sectCB <= HCB.ESCAPE_HCB) || (sectCB >= VCB11_FIRST && sectCB <= VCB11_LAST))
            {
                if (cb < HCB.ESCAPE_HCB) b = ((sectCB == cb) || (sectCB == cb + 1));
                else b = (sectCB == cb);
            }
            return b;
        }

        //sectionDataResilience = hDecoder->aacSectionDataResilienceFlag
        public static void DecodeReorderedSpectralData(ICStream ics, BitStream input, short[] spectralData, bool sectionDataResilience)
        {
            ICSInfo info = ics.GetInfo();
            int windowGroupCount = info.GetWindowGroupCount();
            int maxSFB = info.GetMaxSFB();
            int[] swbOffsets = info.GetSWBOffsets();
            int swbOffsetMax = info.GetSWBOffsetMax();
            //TODO:
            //final SectionData sectData = ics.getSectionData();
            int[][] sectStart = null; //sectData.getSectStart();
            int[][] sectEnd = null; //sectData.getSectEnd();
            int[] numSec = null; //sectData.getNumSec();
            int[][] sectCB = null; //sectData.getSectCB();
            int[][] sectSFBOffsets = null; //info.getSectSFBOffsets();

            //check parameter
            int spDataLen = ics.GetReorderedSpectralDataLength();
            if (spDataLen == 0) return;

            int longestLen = ics.GetLongestCodewordLength();
            if (longestLen == 0 || longestLen >= spDataLen) throw new AACException("length of longest HCR codeword out of range");

            //create spOffsets
            int[] spOffsets = new int[8];
            int shortFrameLen = spectralData.Length / 8;
            spOffsets[0] = 0;
            int g;
            for (g = 1; g < windowGroupCount; g++)
            {
                spOffsets[g] = spOffsets[g - 1] + shortFrameLen * info.GetWindowGroupLength(g - 1);
            }

            Codeword[] codeword = new Codeword[512];
            BitsBuffer[] segment = new BitsBuffer[512];

            int lastCB;
            int[] preSortCB;
            if (sectionDataResilience)
            {
                preSortCB = PRE_SORT_CB_ER;
                lastCB = NUM_CB_ER;
            }
            else
            {
                preSortCB = PRE_SORT_CB_STD;
                lastCB = NUM_CB;
            }

            int PCWs_done = 0;
            int segmentsCount = 0;
            int numberOfCodewords = 0;
            int bitsread = 0;

            int sfb, w_idx, i, thisCB, thisSectCB, cws;
            //step 1: decode PCW's (set 0), and stuff data in easier-to-use format
            for (int sortloop = 0; sortloop < lastCB; sortloop++)
            {
                //select codebook to process this pass
                thisCB = preSortCB[sortloop];

                for (sfb = 0; sfb < maxSFB; sfb++)
                {
                    for (w_idx = 0; 4 * w_idx < (Math.Min(swbOffsets[sfb + 1], swbOffsetMax) - swbOffsets[sfb]); w_idx++)
                    {
                        for (g = 0; g < windowGroupCount; g++)
                        {
                            for (i = 0; i < numSec[g]; i++)
                            {
                                if ((sectStart[g][i] <= sfb) && (sectEnd[g][i] > sfb))
                                {
                                    /* check whether codebook used here is the one we want to process */
                                    thisSectCB = sectCB[g][i];

                                    if (IsGoodCB(thisCB, thisSectCB))
                                    {
                                        //precalculation
                                        int sect_sfb_size = sectSFBOffsets[g][sfb + 1] - sectSFBOffsets[g][sfb];
                                        int inc = (thisSectCB < HCB.FIRST_PAIR_HCB) ? 4 : 2;
                                        int group_cws_count = (4 * info.GetWindowGroupLength(g)) / inc;
                                        int segwidth = Math.Min(MAX_CW_LEN[thisSectCB], longestLen);

                                        //read codewords until end of sfb or end of window group
                                        for (cws = 0; (cws < group_cws_count) && ((cws + w_idx * group_cws_count) < sect_sfb_size); cws++)
                                        {
                                            int sp = spOffsets[g] + sectSFBOffsets[g][sfb] + inc * (cws + w_idx * group_cws_count);

                                            //read and decode PCW
                                            if (PCWs_done == 0)
                                            {
                                                //read in normal segments
                                                if (bitsread + segwidth <= spDataLen)
                                                {
                                                    segment[segmentsCount].ReadSegment(segwidth, input);
                                                    bitsread += segwidth;

                                                    //Huffman.decodeSpectralDataER(segment[segmentsCount], thisSectCB, spectralData, sp);

                                                    //keep leftover bits
                                                    segment[segmentsCount].RewindReverse();

                                                    segmentsCount++;
                                                }
                                                else
                                                {
                                                    //remaining after last segment
                                                    if (bitsread < spDataLen)
                                                    {
                                                        int additional_bits = spDataLen - bitsread;

                                                        segment[segmentsCount].ReadSegment(additional_bits, input);
                                                        segment[segmentsCount]._len += segment[segmentsCount - 1]._len;
                                                        segment[segmentsCount].RewindReverse();

                                                        if (segment[segmentsCount - 1]._len > 32)
                                                        {
                                                            segment[segmentsCount - 1]._bufb = segment[segmentsCount]._bufb
                                                                    + segment[segmentsCount - 1].ShowBits(segment[segmentsCount - 1]._len - 32);
                                                            segment[segmentsCount - 1]._bufa = segment[segmentsCount]._bufa
                                                                    + segment[segmentsCount - 1].ShowBits(32);
                                                        }
                                                        else
                                                        {
                                                            segment[segmentsCount - 1]._bufa = segment[segmentsCount]._bufa
                                                                    + segment[segmentsCount - 1].ShowBits(segment[segmentsCount - 1]._len);
                                                            segment[segmentsCount - 1]._bufb = segment[segmentsCount]._bufb;
                                                        }
                                                        segment[segmentsCount - 1]._len += additional_bits;
                                                    }
                                                    bitsread = spDataLen;
                                                    PCWs_done = 1;

                                                    codeword[0].Fill(sp, thisSectCB);
                                                }
                                            }
                                            else
                                            {
                                                codeword[numberOfCodewords - segmentsCount].Fill(sp, thisSectCB);
                                            }
                                            numberOfCodewords++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (segmentsCount == 0) throw new AACException("no segments in HCR");

            int numberOfSets = numberOfCodewords / segmentsCount;

            //step 2: decode nonPCWs
            int trial, codewordBase, segmentID, codewordID;
            for (int set = 1; set <= numberOfSets; set++)
            {
                for (trial = 0; trial < segmentsCount; trial++)
                {
                    for (codewordBase = 0; codewordBase < segmentsCount; codewordBase++)
                    {
                        segmentID = (trial + codewordBase) % segmentsCount;
                        codewordID = codewordBase + set * segmentsCount - segmentsCount;

                        //data up
                        if (codewordID >= numberOfCodewords - segmentsCount) break;

                        if ((codeword[codewordID]._decoded == 0) && (segment[segmentID]._len > 0))
                        {
                            if (codeword[codewordID]._bits._len != 0) segment[segmentID].ConcatBits(codeword[codewordID]._bits);

                            int tmplen = segment[segmentID]._len;
                            /*int ret = Huffman.decodeSpectralDataER(segment[segmentID], codeword[codewordID].cb,
									spectralData, codeword[codewordID].sp_offset);

							if(ret>=0) codeword[codewordID].decoded = 1;
							else {
								codeword[codewordID].bits = segment[segmentID];
								codeword[codewordID].bits.len = tmplen;
							}*/

                        }
                    }
                }
                for (i = 0; i < segmentsCount; i++)
                {
                    segment[i].RewindReverse();
                }
            }
        }
    }
}
