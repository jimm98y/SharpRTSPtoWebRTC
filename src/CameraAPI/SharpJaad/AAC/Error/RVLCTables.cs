namespace SharpJaad.AAC.Error
{
    public static class RVLCTables
    {
        //index,length,codeword
        public static int[][] RVLC_BOOK = new int[][] {
            new int[] {0, 1, 0}, /*         0 */
		    new int[] { -1, 3, 5 }, /*       101 */
		    new int[] { 1, 3, 7 }, /*       111 */
		    new int[] { -2, 4, 9 }, /*      1001 */
		    new int[] { -3, 5, 17 }, /*     10001 */
		    new int[] { 2, 5, 27 }, /*     11011 */
		    new int[] { -4, 6, 33 }, /*    100001 */
		    new int[] { 99, 6, 50 }, /*    110010 */
		    new int[] { 3, 6, 51 }, /*    110011 */
		    new int[] { 99, 6, 52 }, /*    110100 */
		    new int[] { -7, 7, 65 }, /*   1000001 */
		    new int[] { 99, 7, 96 }, /*   1100000 */
		    new int[] { 99, 7, 98 }, /*   1100010 */
		    new int[] { 7, 7, 99 }, /*   1100011 */
		    new int[] { 4, 7, 107 }, /*   1101011 */
		    new int[] { -5, 8, 129 }, /*  10000001 */
		    new int[] { 99, 8, 194 }, /*  11000010 */
		    new int[] { 5, 8, 195 }, /*  11000011 */
		    new int[] { 99, 8, 212 }, /*  11010100 */
		    new int[] { 99, 9, 256 }, /* 100000000 */
		    new int[] { -6, 9, 257 }, /* 100000001 */
		    new int[] { 99, 9, 426 }, /* 110101010 */
		    new int[] { 6, 9, 427 }, /* 110101011 */
		    new int[] { 99, 10, 0 }
        };
        public static int[][] ESCAPE_BOOK = {
            new int[] { 1, 2, 0 },
            new int[] { 0, 2, 2 },
            new int[] { 3, 3, 2 },
            new int[] { 2, 3, 6 },
            new int[] { 4, 4, 14 },
            new int[] { 7, 5, 13 },
            new int[] { 6, 5, 15 },
            new int[] { 5, 5, 31 },
            new int[] { 11, 6, 24 },
            new int[] { 10, 6, 25 },
            new int[] { 9, 6, 29 },
            new int[] { 8, 6, 61 },
            new int[] { 13, 7, 56 },
            new int[] { 12, 7, 120 },
            new int[] { 15, 8, 114 },
            new int[] { 14, 8, 242 },
            new int[] { 17, 9, 230 },
            new int[] { 16, 9, 486 },
            new int[] { 19, 10, 463 },
            new int[] { 18, 10, 974 },
            new int[] { 22, 11, 925 },
            new int[] { 20, 11, 1950 },
            new int[] { 21, 11, 1951 },
            new int[] { 23, 12, 1848 },
            new int[] { 25, 13, 3698 },
            new int[] { 24, 14, 7399 },
            new int[] { 26, 15, 14797 },
            new int[] { 49, 19, 236736 },
            new int[] { 50, 19, 236737 },
            new int[] { 51, 19, 236738 },
            new int[] { 52, 19, 236739 },
            new int[] { 53, 19, 236740 },
            new int[] { 27, 20, 473482 },
            new int[] { 28, 20, 473483 },
            new int[] { 29, 20, 473484 },
            new int[] { 30, 20, 473485 },
            new int[] { 31, 20, 473486 },
            new int[] { 32, 20, 473487 },
            new int[] { 33, 20, 473488 },
            new int[] { 34, 20, 473489 },
            new int[] { 35, 20, 473490 },
            new int[] { 36, 20, 473491 },
            new int[] { 37, 20, 473492 },
            new int[] { 38, 20, 473493 },
            new int[] { 39, 20, 473494 },
            new int[] { 40, 20, 473495 },
            new int[] { 41, 20, 473496 },
            new int[] { 42, 20, 473497 },
            new int[] { 43, 20, 473498 },
            new int[] { 44, 20, 473499 },
            new int[] { 45, 20, 473500 },
            new int[] { 46, 20, 473501 },
            new int[] { 47, 20, 473502 },
            new int[] { 48, 20, 473503 },
            new int[] { 99, 21, 0 }
        };
    }
}
