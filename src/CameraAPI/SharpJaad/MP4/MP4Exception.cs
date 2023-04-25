using System.IO;

namespace SharpJaad.MP4
{
    public class MP4Exception : IOException
    {
        public MP4Exception(string message) : base(message)
        { }
    }
}
