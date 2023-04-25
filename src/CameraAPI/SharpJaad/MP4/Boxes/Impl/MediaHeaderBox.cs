namespace SharpJaad.MP4.Boxes.Impl
{
    /**
      * The media header declares overall information that is media-independent, and relevant to characteristics of
      * the media in a track.
      */
    public class MediaHeaderBox : FullBox
    {
        private long _creationTime, _modificationTime, _timeScale, _duration;
        private string _language;

        public MediaHeaderBox() : base("Media Header Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            int len = (_version == 1) ? 8 : 4;
            _creationTime = input.ReadBytes(len);
            _modificationTime = input.ReadBytes(len);
            _timeScale = input.ReadBytes(4);
            _duration = Utils.DetectUndetermined(input.ReadBytes(len));

            _language = Utils.GetLanguageCode(input.ReadBytes(2));

            input.SkipBytes(2); //pre-defined: 0
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
         * The time-scale is an integer that specifies the time-scale for this
         * media; this is the number of time units that pass in one second. For
         * example, a time coordinate system that measures time in sixtieths of a
         * second has a time scale of 60.
         * @return the time-scale
         */
        public long GetTimeScale()
        {
            return _timeScale;
        }

        /**
         * The duration is an integer that declares the duration of this media (in 
         * the scale of the timescale). If the duration cannot be determined then 
         * duration is set to -1.
         * @return the duration of this media
         */
        public long GetDuration()
        {
            return _duration;
        }

        /**
         * Language code for this media as defined in ISO 639-2/T.
         * @return the language code
         */
        public string GetLanguage()
        {
            return _language;
        }
    }
}
