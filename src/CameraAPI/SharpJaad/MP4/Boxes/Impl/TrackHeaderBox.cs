namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * This box specifies the characteristics of a single track. Exactly one Track
     * Header Box is contained in a track. In the absence of an edit list, the
     * presentation of a track starts at the beginning of the overall presentation.
     * An empty edit is used to offset the start time of a track.
     * If in a presentation all tracks have neither trackInMovie nor trackInPreview
     * set, then all tracks shall be treated as if both flags were set on all
     * tracks. Hint tracks should not have the track header flags set, so that they
     * are ignored for local playback and preview.
     * The width and height in the track header are measured on a notional 'square'
     * (uniform) grid. Track video data is normalized to these dimensions
     * (logically) before any transformation or placement caused by a layup or
     * composition system. Track (and movie) matrices, if used, also operate in this
     * uniformly-scaled space.
     * @author in-somnia
     */
    public class TrackHeaderBox : FullBox
    {
        private bool _enabled, _inMovie, _inPreview;
        private long _creationTime, _modificationTime, _duration;
        private int _trackID, _layer, _alternateGroup;
        private double _volume, _width, _height;
        private double[] _matrix;

        public TrackHeaderBox() : base("Track Header Box")
        {
            _matrix = new double[9];
        }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _enabled = (_flags & 1) == 1;
            _inMovie = (_flags & 2) == 2;
            _inPreview = (_flags & 4) == 4;

            int len = (_version == 1) ? 8 : 4;
            _creationTime = input.ReadBytes(len);
            _modificationTime = input.ReadBytes(len);
            _trackID = (int)input.ReadBytes(4);
            input.SkipBytes(4); //reserved
            _duration = Utils.DetectUndetermined(input.ReadBytes(len));

            input.SkipBytes(8); //reserved

            _layer = (int)input.ReadBytes(2);
            _alternateGroup = (int)input.ReadBytes(2);
            _volume = input.ReadFixedPoint(8, 8);

            input.SkipBytes(2); //reserved

            for (int i = 0; i < 9; i++)
            {
                if (i < 6) _matrix[i] = input.ReadFixedPoint(16, 16);

                else _matrix[i] = input.ReadFixedPoint(2, 30);
            }

            _width = input.ReadFixedPoint(16, 16);
            _height = input.ReadFixedPoint(16, 16);
        }

        /**
         * A flag indicating that the track is enabled. A disabled track is treated
         * as if it were not present.
         * @return true if the track is enabled
         */
        public bool IsTrackEnabled()
        {
            return _enabled;
        }

        /**
         * A flag indicating that the track is used in the presentation.
         * @return true if the track is used
         */
        public bool IsTrackInMovie()
        {
            return _inMovie;
        }

        /**
         * A flag indicating that the track is used when previewing the
         * presentation.
         * @return true if the track is used in previews
         */
        public bool IsTrackInPreview()
        {
            return _inPreview;
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
         * The track ID is an integer that uniquely identifies this track over the
         * entire life-time of this presentation. Track IDs are never re-used and
         * cannot be zero.
         * @return the track's ID
         */
        public int GetTrackID()
        {
            return _trackID;
        }

        /**
         * The duration is an integer that indicates the duration of this track (in 
         * the timescale indicated in the Movie Header Box). The value of this field
         * is equal to the sum of the durations of all of the track's edits. If 
         * there is no edit list, then the duration is the sum of the sample 
         * durations, converted into the timescale in the Movie Header Box. If the 
         * duration of this track cannot be determined then this value is -1.
         * @return the duration this track
         */
        public long GetDuration()
        {
            return _duration;
        }

        /**
         * The layer specifies the front-to-back ordering of video tracks; tracks
         * with lower numbers are closer to the viewer. 0 is the normal value, and
         * -1 would be in front of track 0, and so on.
         * @return the layer
         */
        public int GetLayer()
        {
            return _layer;
        }

        /**
         * The alternate group is an integer that specifies a group or collection
         * of tracks. If this field is 0 there is no information on possible
         * relations to other tracks. If this field is not 0, it should be the same
         * for tracks that contain alternate data for one another and different for
         * tracks belonging to different such groups. Only one track within an
         * alternate group should be played or streamed at any one time, and must be
         * distinguishable from other tracks in the group via attributes such as
         * bitrate, codec, language, packet size etc. A group may have only one
         * member.
         * @return the alternate group
         */
        public int GetAlternateGroup()
        {
            return _alternateGroup;
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
         * The width specifies the track's visual presentation width as a floating
         * point values. This needs not be the same as the pixel width of the
         * images, which is documented in the sample description(s); all images in
         * the sequence are scaled to this width, before any overall transformation
         * of the track represented by the matrix. The pixel dimensions of the
         * images are the default values. 
         * @return the image width
         */
        public double GetWidth()
        {
            return _width;
        }

        /**
         * The height specifies the track's visual presentation height as a floating
         * point value. This needs not be the same as the pixel height of the
         * images, which is documented in the sample description(s); all images in
         * the sequence are scaled to this height, before any overall transformation
         * of the track represented by the matrix. The pixel dimensions of the
         * images are the default values.
         * @return the image height
         */
        public double GetHeight()
        {
            return _height;
        }

        public double[] GetMatrix()
        {
            return _matrix;
        }
    }
}
