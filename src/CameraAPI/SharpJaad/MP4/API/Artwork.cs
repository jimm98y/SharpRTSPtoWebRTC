using static SharpJaad.MP4.Boxes.Impl.Meta.ITunesMetadataBox;

namespace SharpJaad.MP4.API
{
    public class Artwork
    {
        //TODO: need this enum? it just copies the DataType
        public enum ArtworkType
        {
            GIF, JPEG, PNG, BMP, Unknown
        }

        public static ArtworkType ForDataType(DataType dataType)
        {
            ArtworkType type;
            switch (dataType)
            {
                case DataType.GIF:
                    type = ArtworkType.GIF;
                    break;
                case DataType.JPEG:
                    type = ArtworkType.JPEG;
                    break;
                case DataType.PNG:
                    type = ArtworkType.PNG;
                    break;
                case DataType.BMP:
                    type = ArtworkType.BMP;
                    break;
                default:
                    type = ArtworkType.Unknown;
                    break;
            }
            return type;
        }

        private ArtworkType _type;
        private byte[] _data;

        public Artwork(ArtworkType type, byte[] data)
        {
            this._type = type;
            this._data = data;
        }

        /**
	     * Returns the type of data in this artwork.
	     *
	     * @see Type
	     * @return the data's type
	     */
        public ArtworkType GetArtworkType()
        {
            return _type;
        }

        /**
	     * Returns the encoded data of this artwork.
	     *
	     * @return the encoded data
	     */
        public byte[] GetData()
        {
            return _data;
        }

        // TODO: This would require ImageSharp dependency
        /**
	     * Returns the decoded image, that can be painted.
	     *
	     * @return the decoded image
	     * @throws IOException if decoding fails
	     */
      //  public Image getImage() throws IOException
      //  {
		    //try {
      //          if (image == null) image = ImageIO.read(new ByteArrayInputStream(data));
      //          return image;
      //      }
		    //catch(IOException e) {
      //          Logger.getLogger("MP4 API").log(Level.SEVERE, "Artwork.getImage failed: {0}", e.toString());
      //          throw e;
      //      }
      //  }
    }
}
