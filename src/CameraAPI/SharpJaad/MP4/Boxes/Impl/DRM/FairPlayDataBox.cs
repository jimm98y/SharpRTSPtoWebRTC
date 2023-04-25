namespace SharpJaad.MP4.Boxes.Impl.DRM
{
    public class FairPlayDataBox : BoxImpl
    {
        private byte[] _data;

        public FairPlayDataBox() : base("iTunes FairPlay Data Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            //base.Decode(input);

            _data = new byte[(int)GetLeft(input)];
            input.ReadBytes(_data);
        }

        public byte[] GetData()
        {
            return _data;
        }
    }
}
