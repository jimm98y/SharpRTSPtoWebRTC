namespace SharpJaad.MP4.Boxes.Impl.FD
{
    /**
     * The FD item information box is optional, although it is mandatory for files
     * using FD hint tracks. It provides information on the partitioning of source
     * files and how FD hint tracks are combined into FD sessions. Each partition
     * entry provides details on a particular file partitioning, FEC encoding and
     * associated FEC reservoirs. It is possible to provide multiple entries for one
     * source file (identified by its item ID) if alternative FEC encoding schemes
     * or partitionings are used in the file. All partition entries are implicitly
     * numbered and the first entry has number 1.
     *
     * @author in-somnia
     */
    public class FDItemInformationBox : FullBox
    {
        public FDItemInformationBox() : base("FD Item Information Box")
        { }

        public override void Decode(MP4InputStream input) 
        {
            base.Decode(input);

            int entryCount = (int)input.readBytes(2);
            ReadChildren(input, entryCount); //partition entries

            ReadChildren(input); //FDSessionGroupBox and GroupIDToNameBox
        }
    }
}
