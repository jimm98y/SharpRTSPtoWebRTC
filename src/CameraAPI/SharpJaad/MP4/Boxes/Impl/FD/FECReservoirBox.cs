namespace SharpJaad.MP4.Boxes.Impl.FD
{
    /**
 * The FEC reservoir box associates the source file identified in the file
 * partition box with FEC reservoirs stored as additional items. It contains a
 * list that starts with the first FEC reservoir associated with the first
 * source block of the source file and continues sequentially through the source
 * blocks of the source file.
 *
 * @author in-somnia
 */
    public class FECReservoirBox : FullBox
    {
        private int[] _itemIDs;
        private long[] _symbolCounts;

        public FECReservoirBox() : base("FEC Reservoir Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            int entryCount = (int)input.ReadBytes(2);
            _itemIDs = new int[entryCount];
            _symbolCounts = new long[entryCount];
            for (int i = 0; i < entryCount; i++)
            {
                _itemIDs[i] = (int)input.ReadBytes(2);
                _symbolCounts[i] = input.ReadBytes(4);
            }
        }

        /**
         * The item ID indicates the location of the FEC reservoir associated with a
         * source block.
         *
         * @return all item IDs
         */
        public int[] GetItemIDs()
        {
            return _itemIDs;
        }

        /**
         * The symbol count indicates the number of repair symbols contained in the
         * FEC reservoir.
         *
         * @return all symbol counts
         */
        public long[] GetSymbolCounts()
        {
            return _symbolCounts;
        }
    }
}
