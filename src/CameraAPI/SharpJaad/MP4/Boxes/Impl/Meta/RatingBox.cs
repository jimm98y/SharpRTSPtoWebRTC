using System;
using System.Text;

namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class RatingBox : FullBox
    {
        private string _languageCode, _rating;

        public RatingBox() : base("Rating Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            //3gpp or iTunes
            if (_parent.GetBoxType() == BoxTypes.USER_DATA_BOX)
            {
                base.Decode(input);

                //TODO: what to do with both?
                long entity = input.ReadBytes(4);
                long criteria = input.ReadBytes(4);
                _languageCode = Utils.GetLanguageCode(input.ReadBytes(2));
                byte[] b = input.ReadTerminated((int)GetLeft(input), 0);
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
