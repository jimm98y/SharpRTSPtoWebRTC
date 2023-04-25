namespace SharpJaad.MP4.Boxes.Impl.SampleEntries
{
    /**
     * The MPEG sample entry is used in MP4 streams other than video, audio and
     * hint. It contains only one <code>ESDBox</code>.
     * 
     * @author in-somnia
     */
    public class MPEGSampleEntry : SampleEntry
    {
        public MPEGSampleEntry() : base("MPEG Sample Entry")
        {  }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            ReadChildren(input);
        }
    }
}
