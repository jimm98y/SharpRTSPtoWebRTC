using SharpJaad.MP4.OD;

namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * The IPMP Control Box may contain IPMP descriptors which may be referenced by
     * any stream in the file.
     *
     * The IPMP ToolListDescriptor is defined in ISO/IEC 14496-1, which conveys the
     * list of IPMP tools required to access the media streams in an ISO Base Media
     * File or meta-box, and may include a list of alternate IPMP tools or
     * parametric descriptions of tools required to access the content.
     * 
     * The presence of IPMP Descriptor in this IPMPControlBox indicates that media
     * streams within the file or meta-box are protected by the IPMP Tool described
     * in the IPMP Descriptor. More than one IPMP Descriptors can be carried here,
     * if there are more than one IPMP Tools providing the global governance.
     *
     * @author in-somnia
     */
    public class IPMPControlBox : FullBox
    {
        private /*IPMPToolList*/Descriptor _toolList;
        private /*IPMP*/Descriptor[] _ipmpDescriptors;

        public IPMPControlBox() : base("IPMP Control Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            _toolList = /*(IPMPToolListDescriptor)*/ Descriptor.CreateDescriptor(input);

            int count = input.read();

            _ipmpDescriptors = new Descriptor[count];
            for (int i = 0; i < count; i++)
            {
                _ipmpDescriptors[i] = /*(IPMPDescriptor)*/ Descriptor.CreateDescriptor(input);
            }
        }

        /**
         * The toollist is an IPMP ToolListDescriptor as defined in ISO/IEC 14496-1.
         *
         * @return the toollist
         */
        public Descriptor GetToolList()
        {
            return _toolList;
        }

        /**
         * The list of contained IPMP Descriptors.
         *
         * @return the IPMP descriptors
         */
        public Descriptor[] GetIPMPDescriptors()
        {
            return _ipmpDescriptors;
        }
    }
}