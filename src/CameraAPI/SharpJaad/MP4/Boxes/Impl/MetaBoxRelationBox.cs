namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * The metabox relation box indicates a relation between two meta boxes at the
     * same level, i.e., the top level of the file, the Movie Box, or Track Box. The
     * relation between two meta boxes is unspecified if there is no metabox
     * relation box for those meta boxes. Meta boxes are referenced by specifying
     * their handler types.
     *
     * @author in-somnia
     */
    public class MetaBoxRelationBox : FullBox
    {
        private long _firstMetaboxHandlerType, _secondMetaboxHandlerType;
        private int _metaboxRelation;

        public MetaBoxRelationBox() : base("Meta Box Relation Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _firstMetaboxHandlerType = input.ReadBytes(4);
            _secondMetaboxHandlerType = input.ReadBytes(4);
            _metaboxRelation = input.Read();
        }

        /**
         * The first meta box to be related.
         */
        public long GetFirstMetaboxHandlerType()
        {
            return _firstMetaboxHandlerType;
        }

        /**
         * The second meta box to be related.
         */
        public long GetSecondMetaboxHandlerType()
        {
            return _secondMetaboxHandlerType;
        }

        /**
         * The metabox relation indicates the relation between the two meta boxes.
         * The following values are defined:
         * <ol start="1">
         * <li>The relationship between the boxes is unknown (which is the default
         * when this box is not present)</li>
         * <li>the two boxes are semantically un-related (e.g., one is presentation,
         * the other annotation)</li>
         * <li>the two boxes are semantically related but complementary (e.g., two
         * disjoint sets of meta-data expressed in two different meta-data systems)
         * </li>
         * <li>the two boxes are semantically related but overlap (e.g., two sets of
         * meta-data neither of which is a subset of the other); neither is
         * 'preferred' to the other</li>
         * <li>the two boxes are semantically related but the second is a proper
         * subset or weaker version of the first; the first is preferred</li>
         * <li>the two boxes are semantically related and equivalent (e.g., two
         * essentially identical sets of meta-data expressed in two different
         * meta-data systems)</li>
         * </ol>
         */
        public int GetMetaboxRelation()
        {
            return _metaboxRelation;
        }
    }
}
