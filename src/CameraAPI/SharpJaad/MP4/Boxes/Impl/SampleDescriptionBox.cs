namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * The sample description table gives detailed information about the coding type
     * used, and any initialization information needed for that coding.
     * @author in-somnia
     */
    public class SampleDescriptionBox : FullBox
    {
        public SampleDescriptionBox() : base("Sample Description Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            int entryCount = (int)input.readBytes(4);

            ReadChildren(input, entryCount);
        }
    }
}
