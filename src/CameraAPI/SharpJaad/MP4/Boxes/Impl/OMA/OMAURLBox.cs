using System.Text;

namespace SharpJaad.MP4.Boxes.Impl.OMA
{
    /**
     * This box is used for several sub-boxes of the user-data box in an OMA DRM 
     * file. These boxes have in common, that they only contain one String.
     * 
     * @author in-somnia
     */
    public class OMAURLBox : FullBox
    {
        private string content;

        public OMAURLBox(string name) : base(name)
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            byte[]
            b = new byte[(int)GetLeft(input)];
            input.ReadBytes(b);
            content = Encoding.UTF8.GetString(b);
        }

        /**
         * Returns the String that this box contains. Its meaning depends on the 
         * type of this box.
         * 
         * @return the content of this box
         */
        public string GetContent()
        {
            return content;
        }
    }
}
