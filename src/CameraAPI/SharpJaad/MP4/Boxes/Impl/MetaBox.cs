namespace SharpJaad.MP4.Boxes.Impl
{
    //needs to be defined, because readChildren() is not called by factory
    /* TODO: this class shouldn't be needed. at least here, things become too
    complicated. change this!!! */
    public class MetaBox : FullBox
    {
        public MetaBox() : base("Meta Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);
            ReadChildren(input);
        }
    }
}
