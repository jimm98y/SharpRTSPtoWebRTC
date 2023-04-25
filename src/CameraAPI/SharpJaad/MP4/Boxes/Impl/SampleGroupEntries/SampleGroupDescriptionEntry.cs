namespace SharpJaad.MP4.Boxes.Impl.SampleGroupEntries
{
    public abstract class SampleGroupDescriptionEntry : BoxImpl
    {
        protected SampleGroupDescriptionEntry(string name) : base(name)
        { }

        public override abstract void decode(MP4InputStream input);
    }
}
