namespace SharpJaad.MP4.Boxes.Impl
{
    public class DataEntryUrlBox : FullBox
    {
        private bool _inFile;
        private string _location;

        public DataEntryUrlBox() : base("Data Entry Url Box")
        {  }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _inFile = (flags & 1) == 1;
            if (!_inFile) _location = input.readUTFString((int)GetLeft(input), MP4InputStream.UTF8);
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
