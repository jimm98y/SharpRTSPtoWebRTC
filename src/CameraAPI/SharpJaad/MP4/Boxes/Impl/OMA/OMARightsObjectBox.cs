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
        private byte[] _data;

        public OMARightsObjectBox() : base("OMA DRM Rights Object Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);
            _data = new byte[(int)GetLeft(input)];
            input.ReadBytes(_data);
        }

        /**
         * Returns an array containing the rights object.
         * 
         * @return a rights object
         */
        public byte[] GetData()
        {
            return _data;
        }
    }
}
