namespace SharpJaad.MP4.Boxes.Impl.OMA
{
    /**
     * The rights object box may be used to insert a Protected Rights Object, 
     * defined in 'OMA DRM v2.1' section 5.3.9, into a DCF or PDCF. A Mutable DRM 
     * Information box may include zero or more Rights Object boxes.
     * 
     * @author in-somnia
     */
    public class OMARightsObjectBox : FullBox
    {
        private byte[] data;

        public OMARightsObjectBox() : base("OMA DRM Rights Object Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);
            data = new byte[(int)GetLeft(input)];
            input.readBytes(data);
        }

        /**
         * Returns an array containing the rights object.
         * 
         * @return a rights object
         */
        public byte[] GetData()
        {
            return data;
        }
    }
}
