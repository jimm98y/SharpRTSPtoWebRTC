namespace SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec
{
    /**
     * The <code>CodecSpecificBox</code> can be used instead of an <code>ESDBox</code>
     * in a sample entry. It contains <code>DecoderSpecificInfo</code>s.
     *
     * @author in-somnia
     */
    public abstract class CodecSpecificBox : BoxImpl
    {
        private long _vendor;
        private int _decoderVersion;

        public CodecSpecificBox(string name) : base(name)
        { }

        protected void DecodeCommon(MP4InputStream input)
        {
            _vendor = input.readBytes(4);
            _decoderVersion = input.read();
        }

        public long GetVendor()
        {
            return _vendor;
        }

        public int GetDecoderVersion()
        {
            return _decoderVersion;
        }
    }
}
