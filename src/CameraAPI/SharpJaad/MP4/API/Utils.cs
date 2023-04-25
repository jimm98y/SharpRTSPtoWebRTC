using System;

namespace SharpJaad.MP4.API
{
    public static class Utils
    {
        private const long DATE_OFFSET = 2082850791998;

        public static DateTime getDate(long time)
        {
            return new DateTime(time * 1000 - DATE_OFFSET);
        }
    }
}
