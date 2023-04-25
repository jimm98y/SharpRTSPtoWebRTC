namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * This class is used for all boxes, that are known but don't contain necessary 
     * data and can be skipped. This is mainly used for 'skip', 'free' and 'wide'.
     * 
     * @author in-somnia
     */
    public class FreeSpaceBox : BoxImpl
    {
        public FreeSpaceBox() : base("Free Space Box")
        {  }

        public override void Decode(MP4InputStream input)
        {
            //no need to read, box will be skipped
        }
    }
}
