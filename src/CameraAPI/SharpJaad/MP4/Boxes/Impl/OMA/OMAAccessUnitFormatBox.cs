namespace SharpJaad.MP4.Boxes.Impl.OMA
{
    public class OMAAccessUnitFormatBox : FullBox
    {
        private bool _selectiveEncrypted;
        private int _keyIndicatorLength, _initialVectorLength;

        public OMAAccessUnitFormatBox() : base("OMA DRM Access Unit Format Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            //1 bit selective encryption, 7 bits reserved
            _selectiveEncrypted = ((input.read() >> 7) & 1) == 1;
            _keyIndicatorLength = input.read(); //always zero?
            _initialVectorLength = input.read();
        }

        public bool IsSelectiveEncrypted()
        {
            return _selectiveEncrypted;
        }

        public int GetKeyIndicatorLength()
        {
            return _keyIndicatorLength;
        }

        public int GetInitialVectorLength()
        {
            return _initialVectorLength;
        }
    }
}
