namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * The movie header box defines overall information which is media-independent,
     * and relevant to the entire presentation considered as a whole.
     * @author in-somnia
     */
    public class MovieHeaderBox : FullBox
    {
        private long _creationTime, _modificationTime, _timeScale, _duration;
        private double _rate, _volume;
        private double[] _matrix;
        private long _nextTrackID;

        public MovieHeaderBox() : base("Movie Header Box")
        {
            _matrix = new double[9];
        }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);
            int len = (_version == 1) ? 8 : 4;
            _creationTime = input.ReadBytes(len);
            _modificationTime = input.ReadBytes(len);
            _timeScale = input.ReadBytes(4);
            _duration = Utils.DetectUndetermined(input.ReadBytes(len));

            _rate = input.ReadFixedPoint(16, 16);
            _volume = input.ReadFixedPoint(8, 8);

            input.SkipBytes(10); //reserved

            for (int i = 0; i < 9; i++)
            {
                if (i < 6) _matrix[i] = input.ReadFixedPoint(16, 16);

                else _matrix[i] = input.ReadFixedPoint(2, 30);
            }

            input.SkipBytes(24); //reserved

            _nextTrackID = input.ReadBytes(4);
        }

        /**
         * The creation time is an integer that declares the creation time of the
         * presentation in seconds since midnight, Jan. 1, 1904, in UTC time.
         * @return the creation time
         */
        public long GetCreationTime()
        {
            return _creationTime;
        }

        /**
         * The modification time is an integer that declares the most recent time
         * the presentation was modified in seconds since midnight, Jan. 1, 1904,
         * in UTC time.
         */
        public long GetModificationTime()
        {
            return _modificationTime;
        }

        /**
         * The time-scale is an integer that specifies the time-scale for the entire
         * presentation; this is the number of time units that pass in one second.
         * For example, a time coordinate system that measures time in sixtieths of
         * a second has a time scale of 60.
         * @return the time-scale
         */
        public long GetTimeScale()
        {
            return _timeScale;
        }

        /**
         * The duration is an integer that declares length of the presentation (in
         * the indicated timescale). This property is derived from the
         * presentation's tracks: the value of this field corresponds to the
         * duration of the longest track in the presentation. If the duration cannot
         * be determined then duration is set to -1.
         * @return the duration of the longest track
         */
        public long GetDuration()
        {
            return _duration;
        }

        /**
         * The rate is a floting point number that indicates the preferred rate
         * to play the presentation; 1.0 is normal forward playback
         * @return the playback rate
         */
        public double GetRate()
        {
            return _rate;
        }

        /**
         * The volume is a floating point number that indicates the preferred
         * playback volume: 0.0 is mute, 1.0 is normal volume.
         * @return the volume
         */
        public double GetVolume()
        {
            return _volume;
        }

        /**
         * Provides a transformation matrix for the video:
         * [A,B,U,C,D,V,X,Y,W]
         * A: width scale
         * B: width rotate
         * U: width angle
         * C: height rotate
         * D: height scale
         * V: height angle
         * X: position from left
         * Y: position from top
         * W: divider scale (restricted to 1.0)
         *
         * The normal values for scale are 1.0 and for rotate 0.0.
         * The angles are restricted to 0.0.
         *
         * @return the transformation matrix for the video
         */
        public double[] GetTransformationMatrix()
        {
            return _matrix;
        }

        /**
         * The next-track-ID is a non-zero integer that indicates a value to use
         * for the track ID of the next track to be added to this presentation. Zero
         * is not a valid track ID value. The value shall be larger than the largest
         * track-ID in use. If this value is equal to all 1s (32-bit), and a new
         * media track is to be added, then a search must be made in the file for an
         * unused track identifier.
         * @return the ID for the next track
         */
        public long GetNextTrackID()
        {
            return _nextTrackID;
        }
    }
}