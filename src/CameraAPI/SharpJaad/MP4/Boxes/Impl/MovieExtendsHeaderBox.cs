namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * The Movie Extends Header is optional, and provides the overall duration,
     * including fragments, of a fragmented movie. If this box is not present, the
     * overall duration must be computed by examining each fragment.
     * 
     * @author in-somnia
     */
    public class MovieExtendsHeaderBox : FullBox
    {
        private long _fragmentDuration;

        public MovieExtendsHeaderBox() : base("Movie Extends Header Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            int len = (version == 1) ? 8 : 4;
            _fragmentDuration = input.readBytes(len);
        }

        /**
         * The fragment duration is an integer that declares length of the
         * presentation of the whole movie including fragments (in the timescale
         * indicated in the Movie Header Box). The value of this field corresponds
         * to the duration of the longest track, including movie fragments. If an
         * MP4 file is created in real-time, such as used in live streaming, it is
         * not likely that the fragment duration is known in advance and this box
         * may be omitted.
         * 
         * @return the fragment duration
         */
        public long GetFragmentDuration()
        {
            return _fragmentDuration;
        }
    }
}
