namespace SharpJaad.MP4.Boxes.Impl
{
    public class TrackFragmentRandomAccessBox : FullBox
    {
        private long _trackID;
        private int _entryCount;
        private long[] _times, _moofOffsets, _trafNumbers, _trunNumbers, _sampleNumbers;

        public TrackFragmentRandomAccessBox() : base("Track Fragment Random Access Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _trackID = input.readBytes(4);
            //26 bits reserved, 2 bits trafSizeLen, 2 bits trunSizeLen, 2 bits sampleSizeLen
            long l = input.readBytes(4);
            int trafNumberLen = (int)((l >> 4) & 0x3) + 1;
            int trunNumberLen = (int)((l >> 2) & 0x3) + 1;
            int sampleNumberLen = (int)(l & 0x3) + 1;
            _entryCount = (int)input.readBytes(4);

            int len = (version == 1) ? 8 : 4;

            for (int i = 0; i < _entryCount; i++)
            {
                _times[i] = input.readBytes(len);
                _moofOffsets[i] = input.readBytes(len);
                _trafNumbers[i] = input.readBytes(trafNumberLen);
                _trunNumbers[i] = input.readBytes(trunNumberLen);
                _sampleNumbers[i] = input.readBytes(sampleNumberLen);
            }
        }

        /**
         * The track ID is an integer identifying the associated track.
         *
         * @return the track ID
         */
        public long GetTrackID()
        {
            return _trackID;
        }

        public int GetEntryCount()
        {
            return _entryCount;
        }

        /**
         * The time is an integer that indicates the presentation time of the random
         * access sample in units defined in the 'mdhd' of the associated track.
         *
         * @return the times of all entries
         */
        public long[] GetTimes()
        {
            return _times;
        }

        /**
         * The moof-Offset is an integer that gives the offset of the 'moof' used in
         * the an entry. Offset is the byte-offset between the beginning of the file
         * and the beginning of the 'moof'.
         *
         * @return the offsets for all entries
         */
        public long[] GetMoofOffsets()
        {
            return _moofOffsets;
        }

        /**
         * The 'traf' number that contains the random accessible sample. The number
         * ranges from 1 (the first 'traf' is numbered 1) in each 'moof'.
         *
         * @return the 'traf' numbers for all entries
         */
        public long[] GetTrafNumbers()
        {
            return _trafNumbers;
        }

        /**
         * The 'trun' number that contains the random accessible sample. The number
         * ranges from 1 in each 'traf'.
         *
         * @return the 'trun' numbers for all entries
         */
        public long[] GetTrunNumbers()
        {
            return _trunNumbers;
        }

        /**
         * The sample number that contains the random accessible sample. The number
         * ranges from 1 in each 'trun'.
         *
         * @return the sample numbers for all entries
         */
        public long[] GetSampleNumbers()
        {
            return _sampleNumbers;
        }
    }
}
