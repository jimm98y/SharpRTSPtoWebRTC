namespace SharpJaad.MP4.Boxes.Impl.OMA
{
    /**
     * The ContentID box contains the unique identifier for the Content Object the 
     * metadata are associated with. The value of the content-ID must be the value 
     * of the content-ID stored in the Common Headers for this Content Object. There
     * must be exactly one ContentID sub-box per User-Data box, as the first sub-box
     * in the container.
     * 
     * @author in-somnia
     */
    public class OMAContentIDBox : FullBox
    {
        private string _contentID;

        public OMAContentIDBox() : base("OMA DRM Content ID Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            int len = (int)input.readBytes(2);
            _contentID = input.readString(len);
        }

        /**
         * Returns the content-ID string.
         * 
         * @return the content-ID
         */
        public string getContentID()
        {
            return _contentID;
        }
    }
}
