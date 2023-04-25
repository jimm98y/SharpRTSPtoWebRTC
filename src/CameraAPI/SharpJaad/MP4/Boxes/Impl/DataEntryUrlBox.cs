namespace SharpJaad.MP4.Boxes.Impl
{
    public class DataEntryUrlBox : FullBox
    {
        private bool _inFile;
        private string _location;

        public DataEntryUrlBox() : base("Data Entry Url Box")
        {  }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _inFile = (_flags & 1) == 1;
            if (!_inFile) _location = input.ReadUTFString((int)GetLeft(input), MP4InputStream.UTF8);
        }

        public bool IsInFile()
        {
            return _inFile;
        }

        public string GetLocation()
        {
            return _location;
        }
    }
}
