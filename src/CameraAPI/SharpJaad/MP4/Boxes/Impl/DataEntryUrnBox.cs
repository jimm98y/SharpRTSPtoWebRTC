namespace SharpJaad.MP4.Boxes.Impl
{
    public class DataEntryUrnBox : FullBox
    {
        private bool _inFile;
        private string _referenceName, _location;

        public DataEntryUrnBox() : base("Data Entry Urn Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _inFile = (flags & 1) == 1;
            if (!_inFile)
            {
                _referenceName = input.readUTFString((int)GetLeft(input), MP4InputStream.UTF8);
                if (GetLeft(input) > 0) _location = input.readUTFString((int)GetLeft(input), MP4InputStream.UTF8);
            }
        }

        public bool IsInFile()
        {
            return _inFile;
        }

        public string GetReferenceName()
        {
            return _referenceName;
        }

        public string GetLocation()
        {
            return _location;
        }
    }
}