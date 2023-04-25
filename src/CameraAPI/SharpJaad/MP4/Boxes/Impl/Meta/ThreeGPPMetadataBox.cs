﻿namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class ThreeGPPMetadataBox : FullBox
    {
        private string _languageCode, _data;

        public ThreeGPPMetadataBox(string name) : base(name)
        { }

        public override void decode(MP4InputStream input)
        {
            DecodeCommon(input);

            _data = input.readUTFString((int)GetLeft(input));
        }

        //called directly by subboxes that don't contain the 'data' string
        protected void DecodeCommon(MP4InputStream input)
        {
            base.decode(input);
            _languageCode = Utils.getLanguageCode(input.readBytes(2));
        }

        /**
         * The language code for the following text. See ISO 639-2/T for the set of
         * three character codes.
         */
        public string GetLanguageCode()
        {
            return _languageCode;
        }

        public string GetData()
        {
            return _data;
        }
    }
}
