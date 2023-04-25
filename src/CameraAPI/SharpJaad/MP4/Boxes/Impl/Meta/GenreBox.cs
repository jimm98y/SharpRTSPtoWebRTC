﻿using System.Text;

namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class GenreBox : FullBox
    {
        private string _languageCode, _genre;

        public GenreBox() : base("Genre Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            //3gpp or iTunes
            if (parent.GetBoxType() == BoxTypes.USER_DATA_BOX)
            {
                base.Decode(input);
                _languageCode = Utils.getLanguageCode(input.readBytes(2));
                byte[] b = input.readTerminated((int)GetLeft(input), 0);
                _genre = Encoding.UTF8.GetString(b);
            }
            else ReadChildren(input);
        }

        public string GetLanguageCode()
        {
            return _languageCode;
        }

        public string GetGenre()
        {
            return _genre;
        }
    }
}
