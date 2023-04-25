namespace SharpJaad.MP4.Boxes.Impl.OMA
{
    public class OMAContentObjectBox : FullBox
    {
        private byte[] _data;

        public OMAContentObjectBox() : base("OMA Content Object Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            int len = (int)input.readBytes(4);
            _data = new byte[len];
            input.readBytes(_data);
        }

        /**
         * Returns the data of this content object.
         * 
         * @return the data
         */
        public byte[] GetData()
        {
            return _data;
        }
    }
}
