namespace SharpJaad.MP4.Boxes.Impl.DRM
{
    public class FairPlayDataBox : BoxImpl
    {
        private byte[] data;

        public FairPlayDataBox() : base("iTunes FairPlay Data Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            data = new byte[(int)GetLeft(input)];
            input.readBytes(data);
        }

        public byte[] getData()
        {
            return data;
        }
    }
}
