namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * When the primary data is in XML format and it is desired that the XML be
     * stored directly in the meta-box, either the XMLBox or the BinaryXMLBox is
     * used. The Binary XML Box may only be used when there is a single well-defined
     * binarization of the XML for that defined format as identified by the handler.
     *
     * @see BinaryXMLBox
     * @author in-somnia
     */
    public class XMLBox : FullBox
    {
        private string _content;

        public XMLBox() : base("XML Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _content = input.ReadUTFString((int)GetLeft(input));
        }

        /**
         * The XML content.
         */
        public string GetContent()
        {
            return _content;
        }
    }
}
