namespace SharpJaad.MP4.Boxes.Impl
{
    public class PixelAspectRatioBox : BoxImpl
    {
        private long _hSpacing;
        private long _vSpacing;

        public PixelAspectRatioBox() : base("Pixel Aspect Ratio Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            _hSpacing = input.ReadBytes(4);
            _vSpacing = input.ReadBytes(4);
        }

        public long GetHorizontalSpacing()
        {
            return _hSpacing;
        }

        public long GetVerticalSpacing()
        {
            return _vSpacing;
        }
    }
}
