using System.Drawing;

namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * The video media header contains general presentation information, independent
     * of the coding, for video media
     * @author in-somnia
     */
    public class VideoMediaHeaderBox : FullBox
    {
        private long _graphicsMode;
        private Color _color;

        public VideoMediaHeaderBox() : base("Video Media Header Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _graphicsMode = input.readBytes(2);
            //6 byte RGB color
            int[]
            c = new int[3];
            for (int i = 0; i < 3; i++)
            {
                c[i] = (input.read() & 0xFF) | ((input.read() << 8) & 0xFF);
            }
            _color = Color.FromArgb(c[0], c[1], c[2]);
        }

        /**
         * The graphics mode specifies a composition mode for this video track.
         * Currently, only one mode is defined:
         * '0': copy over the existing image
         */
        public long GetGraphicsMode()
        {
            return _graphicsMode;
        }

        /**
         * A color available for use by graphics modes.
         */
        public Color GetColor()
        {
            return _color;
        }
    }
}
