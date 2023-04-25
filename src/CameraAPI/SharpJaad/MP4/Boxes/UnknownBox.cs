namespace SharpJaad.MP4.Boxes
{
    /**
     * Box implementation that is used for unknown types.
     * 
     * @author in-somnia
     */
    public class UnknownBox : BoxImpl
    {
        public UnknownBox() : base("unknown")
        { }

        public override void decode(MP4InputStream input)
        {
            //no need to read, box will be skipped
        }
    }
}
