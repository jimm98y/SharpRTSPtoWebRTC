namespace SharpJaad.AAC
{
    public enum Profile : int
    {
        UNKNOWN = -1,
        AAC_MAIN = 1,
        AAC_LC = 2,
        AAC_SSR = 3,
        AAC_LTP = 4,
        AAC_SBR = 5,
        AAC_SCALABLE = 6,
        TWIN_VQ = 7,
        AAC_LD = 11,
        ER_AAC_LC = 17,
        ER_AAC_SSR = 18,
        ER_AAC_LTP = 19,
        ER_AAC_SCALABLE = 20,
        ER_TWIN_VQ = 21,
        ER_BSAC = 22,
        ER_AAC_LD = 23
    }

    public static class ProfileExtensions
    {
        public static bool IsErrorResilientProfile(this Profile profile)
        {
            return (int)profile > 16;
        }

        public static bool IsDecodingSupported(this Profile profile)
        {
            return profile == Profile.AAC_MAIN || profile == Profile.AAC_LC || profile == Profile.AAC_SBR || profile == Profile.ER_AAC_LC || profile == Profile.AAC_LTP || profile == Profile.ER_AAC_LTP;
        }
    }
}
