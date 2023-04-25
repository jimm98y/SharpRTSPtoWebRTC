namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * When the primary data is in XML format and it is desired that the XML be
     * stored directly in the meta-box, either the XMLBox or the BinaryXMLBox is
     * used. The Binary XML Box may only be used when there is a single well-defined
     * binarization of the XML for that defined format as identified by the handler.
     *
     * @see XMLBox
     * @author in-somnia
     */
    public class BinaryXMLBox : FullBox
    {
        private byte[] _data;

        public BinaryXMLBox() : base("Binary XML Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _data = new byte[(int)GetLeft(input)];
            input.readBytes(_data);
        }

        /**
         * The binary data.
         */
        public byte[] GetData()
        {
            return _data;
        }
    }
}
