namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class RequirementBox : FullBox
    {
        private string _requirement;

        public RequirementBox() : base("Requirement Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _requirement = input.readString((int)GetLeft(input));
        }

        public string GetRequirement()
        {
            return _requirement;
        }
    }
}
