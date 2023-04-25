namespace SharpJaad.MP4.OD
{
    /**
     * The <code>ObjectDescriptor</code> consists of three different parts:
     *
     * The first part uniquely labels the <code>ObjectDescriptor</code> within its
     * name scope by means of an ID. Media objects in the scene description use this
     * ID to refer to their object descriptor. An optional URL String indicates that
     * the actual object descriptor resides at a remote location.
     *
     * The second part is a set of optional descriptors that support the inclusion
     * if future extensions as well as the transport of private data in a backward
     * compatible way.
     *
     * The third part consists of a list of <code>ESDescriptors</code>, each
     * providing parameters for a single elementary stream that relates to the media
     * object as well as an optional set of object content information descriptors.
     *
     * @author in-somnia
     */
    public class ObjectDescriptor : Descriptor
    {
        private int _objectDescriptorID;
        private bool _urlPresent;
        private string _url;

        public override void decode(MP4InputStream input)
        {
            //10 bits objectDescriptorID, 1 bit url flag, 5 bits reserved
            int x = (int)input.readBytes(2);
            _objectDescriptorID = (x >> 6) & 0x3FF;
            _urlPresent = ((x >> 5) & 1) == 1;

            if (_urlPresent) _url = input.readString(_size - 2);

            ReadChildren(input);
        }

        /**
         * The ID uniquely identifies this ObjectDescriptor within its name scope.
         * It should be within 0 and 1023 exclusively. The value 0 is forbidden and
         * the value 1023 is reserved.
         *
         * @return this ObjectDescriptor's ID
         */
        public int GetObjectDescriptorID()
        {
            return _objectDescriptorID;
        }

        /**
         * A flag that indicates the presence of a URL. If set, no profiles are
         * present.
         *
         * @return true if a URL is present
         */
        public bool IsURLPresent()
        {
            return _urlPresent;
        }

        /**
         * A URL String that shall point to another InitialObjectDescriptor. If no
         * URL is present (if <code>isURLPresent()</code> returns false) this method
         * returns null.
         *
         * @return a URL String or null if none is present
         */
        public string GetURL()
        {
            return _url;
        }
    }
}