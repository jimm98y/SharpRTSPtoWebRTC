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

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            int entryCount = (int)input.ReadBytes(4);

            ReadChildren(input, entryCount);
        }
    }
}
