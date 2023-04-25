using SharpJaad.MP4.OD;

namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * The entry sample descriptor (ESD) box is a container for entry descriptors.
     * If used, it is located in a sample entry. Instead of an <code>ESDBox</code> a
     * <code>CodecSpecificBox</code> may be present.
     * 
     * @author in-somnia
     */
    public class ESDBox : FullBox
    {
        private ESDescriptor _esd;

        public ESDBox() : base("ESD Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _esd = (ESDescriptor)ObjectDescriptor.CreateDescriptor(input);
        }

        public ESDescriptor GetEntryDescriptor()
        {
            return _esd;
        }
    }
}
