namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * This box contains an explicit timeline map. Each entry defines part of the
     * track time-line: by mapping part of the media time-line, or by indicating
     * 'empty' time, or by defining a 'dwell', where a single time-point in the
     * media is held for a period.
     *
     * Starting offsets for tracks (streams) are represented by an initial empty
     * edit. For example, to play a track from its start for 30 seconds, but at 10
     * seconds into the presentation, we have the following edit list:
     *
     * [0]:
     * Segment-duration = 10 seconds
     * Media-Time = -1
     * Media-Rate = 1
     *
     * [1]:
     * Segment-duration = 30 seconds (could be the length of the whole track)
     * Media-Time = 0 seconds
     * Media-Rate = 1
     */
    public class EditListBox : FullBox
    {
        private long[] _segmentDuration, _mediaTime;
        private double[] _mediaRate;

        public EditListBox() : base("Edit List Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            int entryCount = (int)input.ReadBytes(4);
            int len = (_version == 1) ? 8 : 4;

            _segmentDuration = new long[entryCount];
            _mediaTime = new long[entryCount];
            _mediaRate = new double[entryCount];

            for (int i = 0; i < entryCount; i++)
            {
                _segmentDuration[i] = input.ReadBytes(len);
                _mediaTime[i] = input.ReadBytes(len);

                //int(16) mediaRate_integer;
                //int(16) media_rate_fraction = 0;
                _mediaRate[i] = input.ReadFixedPoint(16, 16);
            }
        }

        /**
         * The segment duration is an integer that specifies the duration of this
         * edit segment in units of the timescale in the Movie Header Box.
         */
        public long[] GetSegmentDuration()
        {
            return _segmentDuration;
        }

        /**
         * The media time is an integer containing the starting time within the
         * media of a specific edit segment (in media time scale units, in
         * composition time). If this field is set to –1, it is an empty edit. The
         * last edit in a track shall never be an empty edit. Any difference between
         * the duration in the Movie Header Box, and the track's duration is
         * expressed as an implicit empty edit at the end.
         */
        public long[] GetMediaTime()
        {
            return _mediaTime;
        }

        /**
         * The media rate specifies the relative rate at which to play the media
         * corresponding to a specific edit segment. If this value is 0, then the
         * edit is specifying a ‘dwell’: the media at media-time is presented for the
         * segment-duration. Otherwise this field shall contain the value 1.
         */
        public double[] GetMediaRate()
        {
            return _mediaRate;
        }
    }
}