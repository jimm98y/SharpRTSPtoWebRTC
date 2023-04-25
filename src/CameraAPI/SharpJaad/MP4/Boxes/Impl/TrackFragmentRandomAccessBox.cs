namespace SharpJaad.MP4.Boxes.Impl
{
    public class TrackFragmentRandomAccessBox : FullBox
    {
        private long _trackID;
        private int _entryCount;
        private long[] _times, _moofOffsets, _trafNumbers, _trunNumbers, _sampleNumbers;

        public TrackFragmentRandomAccessBox() : base("Track Fragment Random Access Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _trackID = input.ReadBytes(4);
            //26 bits reserved, 2 bits trafSizeLen, 2 bits trunSizeLen, 2 bits sampleSizeLen
            long l = input.ReadBytes(4);
            int trafNumberLen = (int)((l >> 4) & 0x3) + 1;
            int trunNumberLen = (int)((l >> 2) & 0x3) + 1;
            int sampleNumberLen = (int)(l & 0x3) + 1;
            _entryCount = (int)input.ReadBytes(4);

            int len = (_version == 1) ? 8 : 4;

            _times = new long[_entryCount];
            _moofOffsets = new long[_entryCount];
            _trafNumbers = new long[_entryCount];
            _trunNumbers = new long[_entryCount];
            _sampleNumbers = new long[_entryCount];

            for (int i = 0; i < _entryCount; i++)
            {
                _times[i] = input.ReadBytes(len);
                _moofOffsets[i] = input.ReadBytes(len);
                _trafNumbers[i] = input.ReadBytes(trafNumberLen);
                _trunNumbers[i] = input.ReadBytes(trunNumberLen);
                _sampleNumbers[i] = input.ReadBytes(sampleNumberLen);
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
