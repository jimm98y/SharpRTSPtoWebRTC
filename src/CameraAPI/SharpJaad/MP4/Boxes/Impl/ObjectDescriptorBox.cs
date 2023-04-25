using SharpJaad.MP4.OD;

namespace SharpJaad.MP4.Boxes.Impl
{
    public class ObjectDescriptorBox : FullBox
    {
        private Descriptor _objectDescriptor;

        public ObjectDescriptorBox() : base("Object Descriptor Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);
            _objectDescriptor = Descriptor.CreateDescriptor(input);
        }

        public Descriptor GetObjectDescriptor()
        {
            return _objectDescriptor;
        }
    }
}