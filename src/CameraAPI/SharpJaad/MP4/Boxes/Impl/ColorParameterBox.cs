namespace SharpJaad.MP4.Boxes.Impl
{
    //TODO: check decoding, add get-methods
    public class ColorParameterBox : FullBox
    {
        private long _colorParameterType;
        private int _primariesIndex, _transferFunctionIndex, _matrixIndex;

        public ColorParameterBox() : base("Color Parameter Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _colorParameterType = input.ReadBytes(4);
            _primariesIndex = (int)input.ReadBytes(2);
            _transferFunctionIndex = (int)input.ReadBytes(2);
            _matrixIndex = (int)input.ReadBytes(2);
        }
    }
}
