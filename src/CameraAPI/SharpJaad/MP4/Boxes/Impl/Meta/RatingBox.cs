using System;
using System.Text;

namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class RatingBox : FullBox
    {
        private string _languageCode, _rating;

        public RatingBox() : base("Rating Box")
        { }

        public override void decode(MP4InputStream input)
        {
            //3gpp or iTunes
            if (parent.GetBoxType() == BoxTypes.USER_DATA_BOX)
            {
                base.decode(input);

                //TODO: what to do with both?
                long entity = input.readBytes(4);
                long criteria = input.readBytes(4);
                _languageCode = Utils.getLanguageCode(input.readBytes(2));
                byte[] b = input.readTerminated((int)GetLeft(input), 0);
                _rating = Encoding.UTF8.GetString(b);
            }
            else ReadChildren(input);
        }

        public string GetLanguageCode()
        {
            return _languageCode;
        }

        public string GetRating()
        {
            return _rating;
        }
    }
}
