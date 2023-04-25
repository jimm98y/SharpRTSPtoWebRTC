namespace SharpJaad.MP4.Boxes.Impl.OMA
{
    public class OMAContentObjectBox : FullBox
    {
        private byte[] _data;

        public OMAContentObjectBox() : base("OMA Content Object Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            int len = (int)input.ReadBytes(4);
            _data = new byte[len];
            input.ReadBytes(_data);
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
