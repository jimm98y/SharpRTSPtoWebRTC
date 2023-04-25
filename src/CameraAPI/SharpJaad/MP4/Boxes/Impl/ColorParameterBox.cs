namespace SharpJaad.MP4.Boxes.Impl
{
    //TODO: check decoding, add get-methods
    public class ColorParameterBox : FullBox
    {
        private long _colorParameterType;
        private int _primariesIndex, _transferFunctionIndex, _matrixIndex;

        public ColorParameterBox() : base("Color Parameter Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _colorParameterType = input.readBytes(4);
            _primariesIndex = (int)input.readBytes(2);
            _transferFunctionIndex = (int)input.readBytes(2);
            _matrixIndex = (int)input.readBytes(2);
        }
    }
}
