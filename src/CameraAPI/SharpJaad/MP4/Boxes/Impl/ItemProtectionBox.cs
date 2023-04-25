namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * The item protection box provides an array of item protection information, for
     * use by the Item Information Box.
     *
     * @author in-somnia
     */
    public class ItemProtectionBox : FullBox
    {
        public ItemProtectionBox() : base("Item Protection Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            int protectionCount = (int)input.readBytes(2);

            ReadChildren(input, protectionCount);
        }
    }
}
