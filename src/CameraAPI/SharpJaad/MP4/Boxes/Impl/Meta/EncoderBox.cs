namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class EncoderBox : FullBox
    {
        private string _data;

        public EncoderBox() : base("Encoder Box")
        {  }

        public override void Decode(MP4InputStream input)
        {
            if (_parent.GetBoxType() == BoxTypes.ITUNES_META_LIST_BOX) ReadChildren(input);
            else
            {
                base.Decode(input);
                _data = input.ReadString((int)GetLeft(input));
            }
        }

        public string GetData()
        {
            return _data;
        }
    }
}
