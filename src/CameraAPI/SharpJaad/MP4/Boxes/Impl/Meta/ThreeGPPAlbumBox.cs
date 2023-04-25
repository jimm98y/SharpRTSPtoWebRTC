namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    public class ThreeGPPAlbumBox : ThreeGPPMetadataBox
    {
        private int _trackNumber;

        public ThreeGPPAlbumBox() : base("3GPP Album Box")
        {  }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _trackNumber = (GetLeft(input) > 0) ? input.Read() : -1;
        }

        /**
         * The track number (order number) of the media on this album. This is an 
         * optional field. If the field is not present, -1 is returned.
         * 
         * @return the track number
         */
        public int GetTrackNumber()
        {
            return _trackNumber;
        }
    }
}
