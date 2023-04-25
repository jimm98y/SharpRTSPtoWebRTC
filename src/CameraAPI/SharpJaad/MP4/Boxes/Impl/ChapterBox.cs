using System.Collections.Generic;

namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * The chapter box allows to specify individual chapters along the main timeline
     * of a movie. The chapter box occurs within a movie box.
     * Defined in "Adobe Video File Format Specification v10".
     *
     * @author in-somnia
     */
    public class ChapterBox : FullBox
    {
        private readonly Dictionary<long, string> _chapters;

        public ChapterBox() : base("Chapter Box")
        {
            _chapters = new Dictionary<long, string>();
        }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            input.skipBytes(4); //??

            int count = input.read();

            long timestamp;
            int len;
            string name;
            for (int i = 0; i < count; i++)
            {
                timestamp = input.readBytes(8);
                len = input.read();
                name = input.readString(len);
                _chapters.Add(timestamp, name);
            }
        }

        /**
         * Returns a map that maps the timestamp of each chapter to its name.
         *
         * @return the chapters
         */
        public Dictionary<long, string> GetChapters()
        {
            return _chapters;
        }
    }
}