namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * The item location box provides a directory of resources in this or other
     * files, by locating their containing file, their offset within that file, and
     * their length. Placing this in binary format enables common handling of this
     * data, even by systems which do not understand the particular metadata system
     * (handler) used. For example, a system might integrate all the externally
     * referenced metadata resources into one file, re-adjusting file offsets and
     * file references accordingly.
     *
     * Items may be stored fragmented into extents, e.g. to enable interleaving. An
     * extent is a contiguous subset of the bytes of the resource; the resource is
     * formed by concatenating the extents. If only one extent is used then either
     * or both of the offset and length may be implied:
     * <ul>
     * <li>If the offset is not identified (the field has a length of zero), then
     * the beginning of the file (offset 0) is implied.</li>
     * <li>If the length is not specified, or specified as zero, then the entire
     * file length is implied. References into the same file as this metadata, or
     * items divided into more than one extent, should have an explicit offset and
     * length, or use a MIME type requiring a different interpretation of the file,
     * to avoid infinite recursion.</li>
     * 
     * The size of the item is the sum of the extent lengths.
     *
     * The data-reference index may take the value 0, indicating a reference into
     * the same file as this metadata, or an index into the data-reference table.
     *
     * Some referenced data may itself use offset/length techniques to address
     * resources within it (e.g. an MP4 file might be 'included' in this way).
     * Normally such offsets are relative to the beginning of the containing file.
     * The field 'base offset' provides an additional offset for offset calculations
     * within that contained data. For example, if an MP4 file is included within a
     * file formatted to this specification, then normally data-offsets within that
     * MP4 section are relative to the beginning of file; the base offset adds to
     * those offsets.
     *
     * @author in-somnia
     */
    public class ItemLocationBox : FullBox
    {
        private int[] _itemID, _dataReferenceIndex;
        private long[] _baseOffset;
        private long[][] _extentOffset, _extentLength;

        public ItemLocationBox() : base("Item Location Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            /*4 bits offsetSize
            4 bits lengthSize
            4 bits baseOffsetSize
            4 bits reserved
             */
            long l = input.readBytes(2);
            int offsetSize = (int)(l >> 12) & 0xF;
            int lengthSize = (int)(l >> 8) & 0xF;
            int baseOffsetSize = (int)(l >> 4) & 0xF;

            int itemCount = (int)input.readBytes(2);
            _dataReferenceIndex = new int[itemCount];
            _baseOffset = new long[itemCount];
            _extentOffset = new long[itemCount][];
            _extentLength = new long[itemCount][];

            int j, extentCount;
            for (int i = 0; i < itemCount; i++)
            {
                _itemID[i] = (int)input.readBytes(2);
                _dataReferenceIndex[i] = (int)input.readBytes(2);
                _baseOffset[i] = input.readBytes(baseOffsetSize);

                extentCount = (int)input.readBytes(2);
                _extentOffset[i] = new long[extentCount];
                _extentLength[i] = new long[extentCount];

                for (j = 0; j < extentCount; j++)
                {
                    _extentOffset[i][j] = input.readBytes(offsetSize);
                    _extentLength[i][j] = input.readBytes(lengthSize);
                }
            }
        }

        /**
         * The item ID is an arbitrary integer 'name' for this resource which can be
         * used to refer to it (e.g. in a URL).
         *
         * @return the item ID
         */
        public int[] GetItemID()
        {
            return _itemID;
        }

        /**
         * The data reference index is either zero ('this file') or a 1-based index
         * into the data references in the data information box.
         *
         * @return the data reference index
         */
        public int[] GetDataReferenceIndex()
        {
            return _dataReferenceIndex;
        }

        /**
         * The base offset provides a base value for offset calculations within the 
         * referenced data.
         * 
         * @return the base offsets for all items
         */
        public long[] GetBaseOffset()
        {
            return _baseOffset;
        }

        /**
         * The extent offset provides the absolute offset in bytes from the
         * beginning of the containing file, of this item.
         *
         * @return the offsets for all extents in all items
         */
        public long[][] GetExtentOffset()
        {
            return _extentOffset;
        }

        /**
         * The extends length provides the absolute length in bytes of this metadata
         * item. If the value is 0, then length of the item is the length of the
         * entire referenced file.
         *
         * @return the lengths for all extends in all items
         */
        public long[][] GetExtentLength()
        {
            return _extentLength;
        }
    }
}
