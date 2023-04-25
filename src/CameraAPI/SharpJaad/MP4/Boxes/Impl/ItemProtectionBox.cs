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

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            int protectionCount = (int)input.ReadBytes(2);

            ReadChildren(input, protectionCount);
        }
    }
}
