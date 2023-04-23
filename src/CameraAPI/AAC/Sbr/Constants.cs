namespace CameraAPI.AAC.Sbr
{
    public static class Constants
    {
        public static int[] startMinTable = {7, 7, 10, 11, 12, 16, 16, 17, 24, 32, 35, 48};
        public static int[] offsetIndexTable = {5, 5, 4, 4, 4, 3, 2, 1, 0, 6, 6, 6};
        public static int[][] OFFSET = {
			new int[] {-8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7}, //16000
			new int[] {-5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 9, 11, 13}, //22050
			new int[] {-5, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 9, 11, 13, 16}, //24000
			new int[] {-6, -4, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 9, 11, 13, 16}, //32000
			new int[] {-4, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 9, 11, 13, 16, 20}, //44100-64000
			new int[] {-2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 9, 11, 13, 16, 20, 24}, //>64000
			new int[] {0, 1, 2, 3, 4, 5, 6, 7, 9, 11, 13, 16, 20, 24, 28, 33}
		};

		public const int EXTENSION_ID_PS = 2;
		public const int MAX_NTSRHFG = 40; //maximum of number_time_slots * rate + HFGen. 16*2+8
		public const int MAX_NTSR = 32; //max number_time_slots * rate, ok for DRM and not DRM mode
		public const int MAX_M = 49; //maximum value for M
		public const int MAX_L_E = 5; //maximum value for L_E
		public const int EXT_SBR_DATA = 13;
		public const int EXT_SBR_DATA_CRC = 14;
		public const int FIXFIX = 0;
		public const int FIXVAR = 1;
		public const int VARFIX = 2;
		public const int VARVAR = 3;
		public const int LO_RES = 0;
		public const int HI_RES = 1;
		public const int NO_TIME_SLOTS_960 = 15;
		public const int NO_TIME_SLOTS = 16;
		public const int RATE = 2;
		public const int NOISE_FLOOR_OFFSET = 6;
		public const int T_HFGEN = 8;
		public const int T_HFADJ = 2;
    }
}
