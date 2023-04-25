namespace SharpJaad.MP4.OD
{
    /**
     * The <code>DecoderSpecificInfo</code> constitutes an opaque container with
     * information for a specific media decoder. Depending on the required amout of
     * data, two classes with a maximum of 255 and 2<sup>32</sup>-1 bytes of data
     * are provided. The existence and semantics of the
     * <code>DecoderSpecificInfo</code> depends on the stream type and object
     * profile of the parent <code>DecoderConfigDescriptor</code>.
     *
     * @author in-somnia
     */
    public class DecoderSpecificInfo : Descriptor
    {
        private byte[] _data;

        public override void Decode(MP4InputStream input) 
        {
            _data = new byte[_size];
            input.ReadBytes(_data);
        }

        /**
         * A byte array containing the decoder specific information.
         *
         * @return the decoder specific information
         */
        public byte[] GetData()
        {
            return _data;
        }
    }
}