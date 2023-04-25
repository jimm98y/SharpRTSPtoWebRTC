namespace SharpJaad.MP4.Boxes
{
    public class FullBox : BoxImpl
    {
        protected int _version, _flags;

        public FullBox(string name) : base(name)
        { }

        public override void Decode(MP4InputStream input)
        {
            _version = input.Read();
            _flags = (int)input.ReadBytes(3);
        }
    }
}