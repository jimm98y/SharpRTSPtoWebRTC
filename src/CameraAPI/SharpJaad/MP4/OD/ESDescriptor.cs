namespace SharpJaad.MP4.OD
{
    /**
     * The ESDescriptor conveys all information related to a particular elementary
     * stream and has three major parts:
     *
     * The first part consists of the ES_ID which is a unique reference to the
     * elementary stream within its name scope, a mechanism to group elementary
     * streams within this Descriptor and an optional URL String.
     *
     * The second part is a set of optional extension descriptors that support the
     * inclusion of future extensions as well as the transport of private data in a
     * backward compatible way.
     *
     * The third part consists of the DecoderConfigDescriptor, SLConfigDescriptor,
     * IPIDescriptor and QoSDescriptor which convey the parameters and requirements
     * of the elementary stream.
     *
     * @author in-somnia
     */
    public class ESDescriptor : Descriptor
    {
        private int _esID, _streamPriority, _dependingOnES_ID;
        private bool _streamDependency, _urlPresent /*, _ocrPresent */;
        private string _url;

        public override void Decode(MP4InputStream input)
        {
            _esID = (int)input.ReadBytes(2);

            //1 bit stream dependence flag, 1 it url flag, 1 reserved, 5 bits stream priority
            int flags = input.Read();
            _streamDependency = ((flags >> 7) & 1) == 1;
            _urlPresent = ((flags >> 6) & 1) == 1;
            _streamPriority = flags & 31;

            if (_streamDependency) _dependingOnES_ID = (int)input.ReadBytes(2);
            else _dependingOnES_ID = -1;

            if (_urlPresent)
            {
                int len = input.Read();
                _url = input.ReadString(len);
            }

            ReadChildren(input);
        }

        /**
         * The ES_ID provides a unique label for each elementary stream within its
         * name scope. The value should be within 0 and 65535 exclusively. The
         * values 0 and 65535 are reserved.
         *
         * @return the elementary stream's ID
         */
        public int GetES_ID()
        {
            return _esID;
        }

        /**
         * Indicates if an ID of another stream is present, on which this stream
         * depends.
         *
         * @return true if the dependingOnES_ID is present
         */
        public bool HasStreamDependency()
        {
            return _streamDependency;
        }

        /**
         * The <code>dependingOnES_ID</code> is the ES_ID of another elementary
         * stream on which this elementary stream depends. The stream with the
         * <code>dependingOnES_ID</code> shall also be associated to this
         * Descriptor. If no value is present (if <code>hasStreamDependency()</code>
         * returns false) this method returns -1.
         * 
         * @return the dependingOnES_ID value, or -1 if none is present
         */
        public int GetDependingOnES_ID()
        {
            return _dependingOnES_ID;
        }

        /**
         * A flag that indicates the presence of a URL.
         *
         * @return true if a URL is present
         */
        public bool IsURLPresent()
        {
            return _urlPresent;
        }

        /**
         * A URL String that shall point to the location of an SL-packetized stream
         * by name. The parameters of the SL-packetized stream that is retrieved
         * from the URL are fully specified in this ESDescriptor. 
         * If no URL is present (if <code>isURLPresent()</code> returns false) this
         * method returns null.
         *
         * @return a URL String or null if none is present
         */
        public string GetURL()
        {
            return _url;
        }

        /**
         * The stream priority indicates a relative measure for the priority of this
         * elementary stream. An elementary stream with a higher priority is more
         * important than one with a lower priority. The absolute values are not
         * normatively defined.
         *
         * @return the stream priority
         */
        public int GetStreamPriority()
        {
            return _streamPriority;
        }
    }
}