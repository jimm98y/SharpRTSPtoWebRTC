namespace CameraAPI.AAC.Syntax
{
    public class Constants
    {
        public const int MAX_ELEMENTS = 16;
        public const int BYTE_MASK = 0xFF;
        public const int MIN_INPUT_SIZE = 768; //6144 bits/channel
                                               //frame length
        public const int WINDOW_LEN_LONG = 1024;
        public const int WINDOW_LEN_SHORT = WINDOW_LEN_LONG / 8;
        public const int WINDOW_SMALL_LEN_LONG = 960;
        public const int WINDOW_SMALL_LEN_SHORT = WINDOW_SMALL_LEN_LONG / 8;
        //element types
        public const int ELEMENT_SCE = 0;
        public const int ELEMENT_CPE = 1;
        public const int ELEMENT_CCE = 2;
        public const int ELEMENT_LFE = 3;
        public const int ELEMENT_DSE = 4;
        public const int ELEMENT_PCE = 5;
        public const int ELEMENT_FIL = 6;
        public const int ELEMENT_END = 7;
        //maximum numbers
        public const int MAX_WINDOW_COUNT = 8;
        public const int MAX_WINDOW_GROUP_COUNT = MAX_WINDOW_COUNT;
        public const int MAX_LTP_SFB = 40;
        public const int MAX_SECTIONS = 120;
        public const int MAX_MS_MASK = 128;
        public const float SQRT2 = 1.414213562f;
    }
}
