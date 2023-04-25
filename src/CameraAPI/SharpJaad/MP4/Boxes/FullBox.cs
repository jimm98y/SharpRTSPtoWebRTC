namespace SharpJaad.MP4.Boxes
{
    public class FullBox : BoxImpl
    {
        protected int version, flags;

        public FullBox(string name) : base(name)
        { }

        public override void Decode(MP4InputStream input)
        {
            version = input.read();
            flags = (int)input.readBytes(3);
        }
    }
}