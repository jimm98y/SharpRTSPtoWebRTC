namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * For a given handler, the primary data may be one of the referenced items when
     * it is desired that it be stored elsewhere, or divided into extents; or the
     * primary metadata may be contained in the meta-box (e.g. in an XML box).
     *
     * Either this box must occur, or there must be a box within the meta-box (e.g.
     * an XML box) containing the primary information in the format required by the
     * identified handler.
     *
     * @author in-somnia
     */
    public class PrimaryItemBox : FullBox
    {
        private int itemID;

        public PrimaryItemBox() : base("Primary Item Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            itemID = (int)input.ReadBytes(2);
        }

        /**
         * The item ID is the identifier of the primary item.
         *
         * @return the item ID
         */
        public int GetItemID()
        {
            return itemID;
        }
    }
}
