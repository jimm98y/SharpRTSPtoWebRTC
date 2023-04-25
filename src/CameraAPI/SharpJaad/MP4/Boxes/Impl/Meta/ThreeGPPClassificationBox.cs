namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class ThreeGPPClassificationBox : ThreeGPPMetadataBox
    {
        private long _entity;
        private int _table;

        public ThreeGPPClassificationBox() : base("3GPP Classification Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            DecodeCommon(input);

            _entity = input.ReadBytes(4);
            _table = (int)input.ReadBytes(2);
        }

        public long GetEntity()
        {
            return _entity;
        }

        public int GetTable()
        {
            return _table;
        }
    }
}
