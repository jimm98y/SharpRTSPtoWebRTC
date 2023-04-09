namespace CameraAPI.AAC.Gain
{
    public class GCConstants
    {
        public const int BANDS = 4;
        public const int MAX_CHANNELS = 5;
        public const int NPQFTAPS = 96;
        public const int NPEPARTS = 64;  //number of pre-echo inhibition parts
        public const int ID_GAIN = 16;
        public static int[] LN_GAIN = {
            -4, -3, -2, -1, 0, 1, 2, 3,
            4, 5, 6, 7, 8, 9, 10, 11
        };
    }
}
