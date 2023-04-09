namespace CameraAPI.AAC.Ps
{
    public static class HuffmanTables
    {
        /* binary lookup huffman tables */
        public static int[][] f_huff_iid_def = {
			new int[] { /*0*/-31, 1}, /* index 0: 1 bits: x */
			new int[] {2, 3}, /* index 1: 2 bits: 1x */
			new int[] { /*1*/-30, /*-1*/ -32}, /* index 2: 3 bits: 10x */
			new int[] {4, 5}, /* index 3: 3 bits: 11x */
			new int[] { /*2*/-29, /*-2*/ -33}, /* index 4: 4 bits: 110x */
			new int[] {6, 7}, /* index 5: 4 bits: 111x */
			new int[] { /*3*/-28, /*-3*/ -34}, /* index 6: 5 bits: 1110x */
			new int[] {8, 9}, /* index 7: 5 bits: 1111x */
			new int[] { /*-4*/-35, /*4*/ -27}, /* index 8: 6 bits: 11110x */
			new int[] { /*5*/-26, 10}, /* index 9: 6 bits: 11111x */
			new int[] { /*-5*/-36, 11}, /* index 10: 7 bits: 111111x */
			new int[] { /*6*/-25, 12}, /* index 11: 8 bits: 1111111x */
			new int[] { /*-6*/-37, 13}, /* index 12: 9 bits: 11111111x */
			new int[] { /*-7*/-38, 14}, /* index 13: 10 bits: 111111111x */
			new int[] { /*7*/-24, 15}, /* index 14: 11 bits: 1111111111x */
			new int[] {16, 17}, /* index 15: 12 bits: 11111111111x */
			new int[] { /*8*/-23, /*-8*/ -39}, /* index 16: 13 bits: 111111111110x */
			new int[] {18, 19}, /* index 17: 13 bits: 111111111111x */
			new int[] { /*9*/-22, /*10*/ -21}, /* index 18: 14 bits: 1111111111110x */
			new int[] {20, 21}, /* index 19: 14 bits: 1111111111111x */
			new int[] { /*-9*/-40, /*11*/ -20}, /* index 20: 15 bits: 11111111111110x */
			new int[] {22, 23}, /* index 21: 15 bits: 11111111111111x */
			new int[] { /*-10*/-41, 24}, /* index 22: 16 bits: 111111111111110x */
			new int[] {25, 26}, /* index 23: 16 bits: 111111111111111x */
			new int[] { /*-11*/-42, /*-14*/ -45}, /* index 24: 17 bits: 1111111111111101x */
			new int[] { /*-13*/-44, /*-12*/ -43}, /* index 25: 17 bits: 1111111111111110x */
			new int[] { /*12*/-19, 27}, /* index 26: 17 bits: 1111111111111111x */
			new int[] { /*13*/-18, /*14*/ -17} /* index 27: 18 bits: 11111111111111111x */};

        public static int[][] t_huff_iid_def = {
			new int[] { /*0*/-31, 1}, /* index 0: 1 bits: x */
			new int[] { /*-1*/-32, 2}, /* index 1: 2 bits: 1x */
			new int[] { /*1*/-30, 3}, /* index 2: 3 bits: 11x */
			new int[] { /*-2*/-33, 4}, /* index 3: 4 bits: 111x */
			new int[] { /*2*/-29, 5}, /* index 4: 5 bits: 1111x */
			new int[] { /*-3*/-34, 6}, /* index 5: 6 bits: 11111x */
			new int[] { /*3*/-28, 7}, /* index 6: 7 bits: 111111x */
			new int[] { /*-4*/-35, 8}, /* index 7: 8 bits: 1111111x */
			new int[] { /*4*/-27, 9}, /* index 8: 9 bits: 11111111x */
			new int[] { /*-5*/-36, 10}, /* index 9: 10 bits: 111111111x */
			new int[] { /*5*/-26, 11}, /* index 10: 11 bits: 1111111111x */
			new int[] { /*-6*/-37, 12}, /* index 11: 12 bits: 11111111111x */
			new int[] { /*6*/-25, 13}, /* index 12: 13 bits: 111111111111x */
			new int[] { /*7*/-24, 14}, /* index 13: 14 bits: 1111111111111x */
			new int[] { /*-7*/-38, 15}, /* index 14: 15 bits: 11111111111111x */
			new int[] {16, 17}, /* index 15: 16 bits: 111111111111111x */
			new int[] { /*8*/-23, /*-8*/ -39}, /* index 16: 17 bits: 1111111111111110x */
			new int[] {18, 19}, /* index 17: 17 bits: 1111111111111111x */
			new int[] {20, 21}, /* index 18: 18 bits: 11111111111111110x */
			new int[] {22, 23}, /* index 19: 18 bits: 11111111111111111x */
			new int[] { /*9*/-22, /*-14*/ -45}, /* index 20: 19 bits: 111111111111111100x */
			new int[] { /*-13*/-44, /*-12*/ -43}, /* index 21: 19 bits: 111111111111111101x */
			new int[] {24, 25}, /* index 22: 19 bits: 111111111111111110x */
			new int[] {26, 27}, /* index 23: 19 bits: 111111111111111111x */
			new int[] { /*-11*/-42, /*-10*/ -41}, /* index 24: 20 bits: 1111111111111111100x */
			new int[] { /*-9*/-40, /*10*/ -21}, /* index 25: 20 bits: 1111111111111111101x */
			new int[] { /*11*/-20, /*12*/ -19}, /* index 26: 20 bits: 1111111111111111110x */
			new int[] { -18, -17 } /* index 27: 20 bits: 1111111111111111111x */};

        public static int[][] f_huff_iid_fine = {
			new int[] {1, /*0*/ -31}, /* index 0: 1 bits: x */
			new int[] {2, 3}, /* index 1: 2 bits: 0x */
			new int[] {4, /*-1*/ -32}, /* index 2: 3 bits: 00x */
			new int[] { /*1*/-30, 5}, /* index 3: 3 bits: 01x */
			new int[] { /*-2*/-33, /*2*/ -29}, /* index 4: 4 bits: 000x */
			new int[] {6, 7}, /* index 5: 4 bits: 011x */
			new int[] { /*-3*/-34, /*3*/ -28}, /* index 6: 5 bits: 0110x */
			new int[] {8, 9}, /* index 7: 5 bits: 0111x */
			new int[] { /*-4*/-35, /*4*/ -27}, /* index 8: 6 bits: 01110x */
			new int[] {10, 11}, /* index 9: 6 bits: 01111x */
			new int[] { /*-5*/-36, /*5*/ -26}, /* index 10: 7 bits: 011110x */
			new int[] {12, 13}, /* index 11: 7 bits: 011111x */
			new int[] { /*-6*/-37, /*6*/ -25}, /* index 12: 8 bits: 0111110x */
			new int[] {14, 15}, /* index 13: 8 bits: 0111111x */
			new int[] { /*7*/-24, 16}, /* index 14: 9 bits: 01111110x */
			new int[] {17, 18}, /* index 15: 9 bits: 01111111x */
			new int[] {19, /*-8*/ -39}, /* index 16: 10 bits: 011111101x */
			new int[] { /*8*/-23, 20}, /* index 17: 10 bits: 011111110x */
			new int[] {21, /*-7*/ -38}, /* index 18: 10 bits: 011111111x */
			new int[] { /*10*/-21, 22}, /* index 19: 11 bits: 0111111010x */
			new int[] {23, /*-9*/ -40}, /* index 20: 11 bits: 0111111101x */
			new int[] { /*9*/-22, 24}, /* index 21: 11 bits: 0111111110x */
			new int[] { /*-11*/-42, /*11*/ -20}, /* index 22: 12 bits: 01111110101x */
			new int[] {25, 26}, /* index 23: 12 bits: 01111111010x */
			new int[] {27, /*-10*/ -41}, /* index 24: 12 bits: 01111111101x */
			new int[] {28, /*-12*/ -43}, /* index 25: 13 bits: 011111110100x */
			new int[] { /*12*/-19, 29}, /* index 26: 13 bits: 011111110101x */
			new int[] {30, 31}, /* index 27: 13 bits: 011111111010x */
			new int[] {32, /*-14*/ -45}, /* index 28: 14 bits: 0111111101000x */
			new int[] { /*14*/-17, 33}, /* index 29: 14 bits: 0111111101011x */
			new int[] {34, /*-13*/ -44}, /* index 30: 14 bits: 0111111110100x */
			new int[] { /*13*/-18, 35}, /* index 31: 14 bits: 0111111110101x */
			new int[] {36, 37}, /* index 32: 15 bits: 01111111010000x */
			new int[] {38, /*-15*/ -46}, /* index 33: 15 bits: 01111111010111x */
			new int[] { /*15*/-16, 39}, /* index 34: 15 bits: 01111111101000x */
			new int[] {40, 41}, /* index 35: 15 bits: 01111111101011x */
			new int[] {42, 43}, /* index 36: 16 bits: 011111110100000x */
			new int[] { /*-17*/-48, /*17*/ -14}, /* index 37: 16 bits: 011111110100001x */
			new int[] {44, 45}, /* index 38: 16 bits: 011111110101110x */
			new int[] {46, 47}, /* index 39: 16 bits: 011111111010001x */
			new int[] {48, 49}, /* index 40: 16 bits: 011111111010110x */
			new int[] { /*-16*/-47, /*16*/ -15}, /* index 41: 16 bits: 011111111010111x */
			new int[] { /*-21*/-52, /*21*/ -10}, /* index 42: 17 bits: 0111111101000000x */
			new int[] { /*-19*/-50, /*19*/ -12}, /* index 43: 17 bits: 0111111101000001x */
			new int[] { /*-18*/-49, /*18*/ -13}, /* index 44: 17 bits: 0111111101011100x */
			new int[] {50, 51}, /* index 45: 17 bits: 0111111101011101x */
			new int[] {52, 53}, /* index 46: 17 bits: 0111111110100010x */
			new int[] {54, 55}, /* index 47: 17 bits: 0111111110100011x */
			new int[] {56, 57}, /* index 48: 17 bits: 0111111110101100x */
			new int[] {58, 59}, /* index 49: 17 bits: 0111111110101101x */
			new int[] { /*-26*/-57, /*-25*/ -56}, /* index 50: 18 bits: 01111111010111010x */
			new int[] { /*-28*/-59, /*-27*/ -58}, /* index 51: 18 bits: 01111111010111011x */
			new int[] { /*-22*/-53, /*22*/ -9}, /* index 52: 18 bits: 01111111101000100x */
			new int[] { /*-24*/-55, /*-23*/ -54}, /* index 53: 18 bits: 01111111101000101x */
			new int[] { /*25*/-6, /*26*/ -5}, /* index 54: 18 bits: 01111111101000110x */
			new int[] { /*23*/-8, /*24*/ -7}, /* index 55: 18 bits: 01111111101000111x */
			new int[] { /*29*/-2, /*30*/ -1}, /* index 56: 18 bits: 01111111101011000x */
			new int[] { /*27*/-4, /*28*/ -3}, /* index 57: 18 bits: 01111111101011001x */
			new int[] { /*-30*/-61, /*-29*/ -60}, /* index 58: 18 bits: 01111111101011010x */
			new int[] { -51, -11 } /* index 59: 18 bits: 01111111101011011x */};

        public static int[][] t_huff_iid_fine = {
			new int[] {1, /*0*/ -31}, /* index 0: 1 bits: x */
			new int[] { /*1*/-30, 2}, /* index 1: 2 bits: 0x */
			new int[] {3, /*-1*/ -32}, /* index 2: 3 bits: 01x */
			new int[] {4, 5}, /* index 3: 4 bits: 010x */
			new int[] {6, 7}, /* index 4: 5 bits: 0100x */
			new int[] { /*-2*/-33, /*2*/ -29}, /* index 5: 5 bits: 0101x */
			new int[] {8, /*-3*/ -34}, /* index 6: 6 bits: 01000x */
			new int[] { /*3*/-28, 9}, /* index 7: 6 bits: 01001x */
			new int[] { /*-4*/-35, /*4*/ -27}, /* index 8: 7 bits: 010000x */
			new int[] {10, 11}, /* index 9: 7 bits: 010011x */
			new int[] { /*5*/-26, 12}, /* index 10: 8 bits: 0100110x */
			new int[] {13, 14}, /* index 11: 8 bits: 0100111x */
			new int[] { /*-6*/-37, /*6*/ -25}, /* index 12: 9 bits: 01001101x */
			new int[] {15, 16}, /* index 13: 9 bits: 01001110x */
			new int[] {17, /*-5*/ -36}, /* index 14: 9 bits: 01001111x */
			new int[] {18, /*-7*/ -38}, /* index 15: 10 bits: 010011100x */
			new int[] { /*7*/-24, 19}, /* index 16: 10 bits: 010011101x */
			new int[] {20, 21}, /* index 17: 10 bits: 010011110x */
			new int[] { /*9*/-22, 22}, /* index 18: 11 bits: 0100111000x */
			new int[] {23, 24}, /* index 19: 11 bits: 0100111011x */
			new int[] { /*-8*/-39, /*8*/ -23}, /* index 20: 11 bits: 0100111100x */
			new int[] {25, 26}, /* index 21: 11 bits: 0100111101x */
			new int[] { /*11*/-20, 27}, /* index 22: 12 bits: 01001110001x */
			new int[] {28, 29}, /* index 23: 12 bits: 01001110110x */
			new int[] { /*-10*/-41, /*10*/ -21}, /* index 24: 12 bits: 01001110111x */
			new int[] {30, 31}, /* index 25: 12 bits: 01001111010x */
			new int[] {32, /*-9*/ -40}, /* index 26: 12 bits: 01001111011x */
			new int[] {33, /*-13*/ -44}, /* index 27: 13 bits: 010011100011x */
			new int[] { /*13*/-18, 34}, /* index 28: 13 bits: 010011101100x */
			new int[] {35, 36}, /* index 29: 13 bits: 010011101101x */
			new int[] {37, /*-12*/ -43}, /* index 30: 13 bits: 010011110100x */
			new int[] { /*12*/-19, 38}, /* index 31: 13 bits: 010011110101x */
			new int[] {39, /*-11*/ -42}, /* index 32: 13 bits: 010011110110x */
			new int[] {40, 41}, /* index 33: 14 bits: 0100111000110x */
			new int[] {42, 43}, /* index 34: 14 bits: 0100111011001x */
			new int[] {44, 45}, /* index 35: 14 bits: 0100111011010x */
			new int[] {46, /*-15*/ -46}, /* index 36: 14 bits: 0100111011011x */
			new int[] { /*15*/-16, 47}, /* index 37: 14 bits: 0100111101000x */
			new int[] { /*-14*/-45, /*14*/ -17}, /* index 38: 14 bits: 0100111101011x */
			new int[] {48, 49}, /* index 39: 14 bits: 0100111101100x */
			new int[] { /*-21*/-52, /*-20*/ -51}, /* index 40: 15 bits: 01001110001100x */
			new int[] { /*18*/-13, /*19*/ -12}, /* index 41: 15 bits: 01001110001101x */
			new int[] { /*-19*/-50, /*-18*/ -49}, /* index 42: 15 bits: 01001110110010x */
			new int[] {50, 51}, /* index 43: 15 bits: 01001110110011x */
			new int[] {52, 53}, /* index 44: 15 bits: 01001110110100x */
			new int[] {54, 55}, /* index 45: 15 bits: 01001110110101x */
			new int[] {56, /*-17*/ -48}, /* index 46: 15 bits: 01001110110110x */
			new int[] { /*17*/-14, 57}, /* index 47: 15 bits: 01001111010001x */
			new int[] {58, /*-16*/ -47}, /* index 48: 15 bits: 01001111011000x */
			new int[] { /*16*/-15, 59}, /* index 49: 15 bits: 01001111011001x */
			new int[] { /*-26*/-57, /*26*/ -5}, /* index 50: 16 bits: 010011101100110x */
			new int[] { /*-28*/-59, /*-27*/ -58}, /* index 51: 16 bits: 010011101100111x */
			new int[] { /*29*/-2, /*30*/ -1}, /* index 52: 16 bits: 010011101101000x */
			new int[] { /*27*/-4, /*28*/ -3}, /* index 53: 16 bits: 010011101101001x */
			new int[] { /*-30*/-61, /*-29*/ -60}, /* index 54: 16 bits: 010011101101010x */
			new int[] { /*-25*/-56, /*25*/ -6}, /* index 55: 16 bits: 010011101101011x */
			new int[] { /*-24*/-55, /*24*/ -7}, /* index 56: 16 bits: 010011101101100x */
			new int[] { /*-23*/-54, /*23*/ -8}, /* index 57: 16 bits: 010011110100011x */
			new int[] { /*-22*/-53, /*22*/ -9}, /* index 58: 16 bits: 010011110110000x */
		    new int[] { -11, -10 } /* index 59: 16 bits: 010011110110011x */};

        public static int[][] f_huff_icc = {
			new int[] { /*0*/-31, 1}, /* index 0: 1 bits: x */
			new int[] { /*1*/-30, 2}, /* index 1: 2 bits: 1x */
			new int[] { /*-1*/-32, 3}, /* index 2: 3 bits: 11x */
			new int[] { /*2*/-29, 4}, /* index 3: 4 bits: 111x */
			new int[] { /*-2*/-33, 5}, /* index 4: 5 bits: 1111x */
			new int[] { /*3*/-28, 6}, /* index 5: 6 bits: 11111x */
			new int[] { /*-3*/-34, 7}, /* index 6: 7 bits: 111111x */
			new int[] { /*4*/-27, 8}, /* index 7: 8 bits: 1111111x */
			new int[] { /*5*/-26, 9}, /* index 8: 9 bits: 11111111x */
			new int[] { /*-4*/-35, 10}, /* index 9: 10 bits: 111111111x */
			new int[] { /*6*/-25, 11}, /* index 10: 11 bits: 1111111111x */
			new int[] { /*-5*/-36, 12}, /* index 11: 12 bits: 11111111111x */
			new int[] { /*7*/-24, 13}, /* index 12: 13 bits: 111111111111x */
			new int[] { -37, -38 } /* index 13: 14 bits: 1111111111111x */};

        public static int[][] t_huff_icc = {
			new int[] { /*0*/-31, 1}, /* index 0: 1 bits: x */
			new int[] { /*1*/-30, 2}, /* index 1: 2 bits: 1x */
			new int[] { /*-1*/-32, 3}, /* index 2: 3 bits: 11x */
			new int[] { /*2*/-29, 4}, /* index 3: 4 bits: 111x */
			new int[] { /*-2*/-33, 5}, /* index 4: 5 bits: 1111x */
			new int[] { /*3*/-28, 6}, /* index 5: 6 bits: 11111x */
			new int[] { /*-3*/-34, 7}, /* index 6: 7 bits: 111111x */
			new int[] { /*4*/-27, 8}, /* index 7: 8 bits: 1111111x */
			new int[] { /*-4*/-35, 9}, /* index 8: 9 bits: 11111111x */
			new int[] { /*5*/-26, 10}, /* index 9: 10 bits: 111111111x */
			new int[] { /*-5*/-36, 11}, /* index 10: 11 bits: 1111111111x */
			new int[] { /*6*/-25, 12}, /* index 11: 12 bits: 11111111111x */
			new int[] { /*-6*/-37, 13}, /* index 12: 13 bits: 111111111111x */
			new int[] { -38, -24 } /* index 13: 14 bits: 1111111111111x */};

        public static int[][] f_huff_ipd = {
			new int[] {1, /*0*/ -31}, /* index 0: 1 bits: x */
			new int[] {2, 3}, /* index 1: 2 bits: 0x */
			new int[] { /*1*/-30, 4}, /* index 2: 3 bits: 00x */
			new int[] {5, 6}, /* index 3: 3 bits: 01x */
			new int[] { /*4*/-27, /*5*/ -26}, /* index 4: 4 bits: 001x */
			new int[] { /*3*/-28, /*6*/ -25}, /* index 5: 4 bits: 010x */
			new int[] { /*2*/-29, /*7*/ -24} /* index 6: 4 bits: 011x */};

        public static int[][] t_huff_ipd = {
			new int[] {1, /*0*/ -31}, /* index 0: 1 bits: x */
			new int[] {2, 3}, /* index 1: 2 bits: 0x */
			new int[] {4, 5}, /* index 2: 3 bits: 00x */
			new int[] { /*1*/-30, /*7*/ -24}, /* index 3: 3 bits: 01x */
			new int[] { /*5*/-26, 6}, /* index 4: 4 bits: 000x */
			new int[] { /*2*/-29, /*6*/ -25}, /* index 5: 4 bits: 001x */
			new int[] { /*4*/-27, /*3*/ -28} /* index 6: 5 bits: 0001x */};

        public static int[][] f_huff_opd = {
			new int[] {1, /*0*/ -31}, /* index 0: 1 bits: x */
			new int[] {2, 3}, /* index 1: 2 bits: 0x */
			new int[] { /*7*/-24, /*1*/ -30}, /* index 2: 3 bits: 00x */
			new int[] {4, 5}, /* index 3: 3 bits: 01x */
			new int[] { /*3*/-28, /*6*/ -25}, /* index 4: 4 bits: 010x */
			new int[] { /*2*/-29, 6}, /* index 5: 4 bits: 011x */
			new int[] { /*5*/-26, /*4*/ -27} /* index 6: 5 bits: 0111x */};

        public static int[][] t_huff_opd = {
			new int[] {1, /*0*/ -31}, /* index 0: 1 bits: x */
			new int[] {2, 3}, /* index 1: 2 bits: 0x */
			new int[] {4, 5}, /* index 2: 3 bits: 00x */
			new int[] { /*1*/-30, /*7*/ -24}, /* index 3: 3 bits: 01x */
			new int[] { /*5*/-26, /*2*/ -29}, /* index 4: 4 bits: 000x */
			new int[] { /*6*/-25, 6}, /* index 5: 4 bits: 001x */
			new int[] { /*4*/-27, /*3*/ -28} /* index 6: 5 bits: 0011x */};
    }
}
