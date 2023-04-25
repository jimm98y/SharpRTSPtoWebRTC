namespace SharpJaad.MP4.Boxes
{
    public static class Utils
    {
        private const long UNDETERMINED = 4294967295;

        public static string getLanguageCode(long l)
        {
            //1 bit padding, 5*3 bits language code (ISO-639-2/T)
            char[] c = new char[3];
            c[0] = (char)(((l >> 10) & 31) + 0x60);
            c[1] = (char)(((l >> 5) & 31) + 0x60);
            c[2] = (char)((l & 31) + 0x60);
            return new string(c);
        }

        public static long detectUndetermined(long l)
        {
            long x;
            if (l == UNDETERMINED) x = -1;
            else x = l;
            return x;
        }
    }
}
