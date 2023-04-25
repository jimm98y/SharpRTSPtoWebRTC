namespace SharpJaad.MP4.Boxes.Impl.SampleEntries
{
    public class FDHintSampleEntry : SampleEntry
    {
        private int _hintTrackVersion, _highestCompatibleVersion, _partitionEntryID;
        private double _fecOverhead;

        public FDHintSampleEntry() : base("FD Hint Sample Entry")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _hintTrackVersion = (int)input.ReadBytes(2);
            _highestCompatibleVersion = (int)input.ReadBytes(2);
            _partitionEntryID = (int)input.ReadBytes(2);
            _fecOverhead = input.ReadFixedPoint(8, 8);

            ReadChildren(input);
        }

        /**
         * The partition entry ID indicates the partition entry in the FD item
         * information box. A zero value indicates that no partition entry is
         * associated with this sample entry, e.g., for FDT. If the corresponding FD
         * hint track contains only overhead data this value should indicate the 
         * partition entry whose overhead data is in question. 
         *
         * @return the partition entry ID
         */
        public int GetPartitionEntryID()
        {
            return _partitionEntryID;
        }

        /**
         * The FEC overhead is a floating point value indicating the percentage
         * protection overhead used by the hint sample(s). The intention of
         * providing this value is to provide characteristics to help a server
         * select a session group (and corresponding FD hint tracks). If the 
         * corresponding FD hint track contains only overhead data this value should
         * indicate the protection overhead achieved by using all FD hint tracks in 
         * a session group up to the FD hint track in question.
         *
         * @return the FEC overhead
         */
        public double GetFECOverhead()
        {
            return _fecOverhead;
        }
    }
}
