namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class ThreeGPPClassificationBox : ThreeGPPMetadataBox
    {
        private long entity;
        private int table;

        public ThreeGPPClassificationBox() : base("3GPP Classification Box")
        { }

        public override void decode(MP4InputStream input)
        {
            DecodeCommon(input);

            entity = input.readBytes(4);
            table = (int)input.readBytes(2);
        }

        public long GetEntity()
        {
            return entity;
        }

        public int GetTable()
        {
            return table;
        }
    }
}
