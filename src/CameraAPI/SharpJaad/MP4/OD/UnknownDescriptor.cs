namespace SharpJaad.MP4.OD
{
    /**
      * This class is used if any unknown Descriptor is found in a stream. All
      * contents of the Descriptor will be skipped.
      *
      * @author in-somnia
      */
    public class UnknownDescriptor : Descriptor
    {
	    public override void decode(MP4InputStream input)
        {
            //content will be skipped
        }
    }
}
