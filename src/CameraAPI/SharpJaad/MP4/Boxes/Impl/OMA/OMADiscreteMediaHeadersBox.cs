namespace SharpJaad.MP4.Boxes.Impl.OMA
{
    /**
      * The Discrete Media headers box includes fields specific to the DCF format and
      * the Common Headers box, followed by an optional user-data box. There must be 
      * exactly one OMADiscreteHeaders box in a single OMA DRM Container box, as the 
      * first box in the container.
      * 
      * @author in-somnia
      */
    public class OMADiscreteMediaHeadersBox : FullBox
    {
        private string contentType;

        public OMADiscreteMediaHeadersBox() : base("OMA DRM Discrete Media Headers Box")
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            int len = input.read();
            contentType = input.readString(len);

            ReadChildren(input);
        }

        /**
         * The content type indicates the original MIME media type of the Content 
         * Object i.e. what content type the result of a successful extraction of 
         * the OMAContentBox represents.
         * 
         * @return the content type
         */
        public string GetContentType()
        {
            return contentType;
        }
    }
}
