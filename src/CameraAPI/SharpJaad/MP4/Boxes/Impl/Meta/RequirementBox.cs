namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class RequirementBox : FullBox
    {
        private string _requirement;

        public RequirementBox() : base("Requirement Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _requirement = input.ReadString((int)GetLeft(input));
        }

        public string GetRequirement()
        {
            return _requirement;
        }
    }
}
