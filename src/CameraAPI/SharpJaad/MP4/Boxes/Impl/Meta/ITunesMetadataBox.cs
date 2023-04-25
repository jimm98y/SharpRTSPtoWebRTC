using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    /**
     * This box contains the data for a metadata tag. It is right below an
     * iTunes metadata box (e.g. '@nam') or a custom meta tag box ('----'). A custom
     * meta tag box also contains a 'name'-box declaring the tag's name.
     *
     * @author in-somnia
     */
    /*TODO: use generics here? -> each DataType should return <T> corresponding to
    its class (String/Integer/...)*/
    public class ITunesMetadataBox : FullBox
    {
        private static readonly string[] TIMESTAMPS = { "yyyy", "yyyy-MM", "yyyy-MM-dd" };

        public enum DataType
        {
            IMPLICIT, /*Object.class*/
            UTF8,     /*String.class*/
            UTF16,    /*String.class*/
            HTML,     /*String.class*/
            XML,      /*String.class*/
            UUID,     /*Long.class*/
            ISRC,     /*String.class*/
            MI3P,     /*String.class*/
            GIF,      /*byte[].class*/
            JPEG,     /*byte[].class*/
            PNG,      /*byte[].class*/
            URL,      /*String.class*/
            DURATION, /*Long.class*/
            DATETIME, /*Long.class*/
            GENRE,    /*Integer.class*/
            INTEGER,  /*Long.class*/
            RIAA,     /*Integer.class*/
            UPC,      /*String.class*/
            BMP,      /*byte[].class*/
            UNDEFINED /*byte[].class*/

        }

        private static readonly DataType[] TYPES = {
            DataType.IMPLICIT, DataType.UTF8, DataType.UTF16, DataType.UNDEFINED, DataType.UNDEFINED, DataType.UNDEFINED, DataType.HTML, DataType.XML, DataType.UUID, DataType.ISRC, DataType.MI3P, DataType.UNDEFINED,
            DataType.GIF, DataType.JPEG, DataType.PNG, DataType.URL, DataType.DURATION, DataType.DATETIME, DataType.GENRE, DataType.UNDEFINED, DataType.UNDEFINED, DataType.INTEGER,
            DataType.UNDEFINED, DataType.UNDEFINED, DataType.RIAA, DataType.UPC, DataType.UNDEFINED, DataType.BMP
        };

        public static DataType ForInt(int i)
        {
            DataType type = DataType.UNDEFINED;
            if (i >= 0 && i < TYPES.Length) type = TYPES[i];
            return type;
        }

        private DataType _dataType;
        private byte[] _data;

        public ITunesMetadataBox() : base("iTunes Metadata Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            _dataType = ForInt(flags);

            input.skipBytes(4); //padding?

            _data = new byte[(int)GetLeft(input)];
            input.readBytes(_data);
        }

        public DataType GetDataType()
        {
            return _dataType;
        }

        /**
         * Returns an unmodifiable array with the raw content, that can be present
         * in different formats.
         * 
         * @return the raw metadata
         */
        public byte[] GetData()
        {
            return _data.ToArray();
        }

        /**
         * Returns the content as a text string.
         * @return the metadata as text
         */
        public string GetText()
        {
            //first four bytes are padding (zero)
            return Encoding.UTF8.GetString(_data);
        }

        /**
         * Returns the content as an unsigned 8-bit integer.
         * @return the metadata as an integer
         */
        public long GetNumber()
        {
            //first four bytes are padding (zero)
            long l = 0;
            for (int i = 0; i < _data.Length; i++)
            {
                l <<= 8;
                l |= (_data[i] & 0xFF);
            }
            return l;
        }

        public int GetInteger()
        {
            return (int)GetNumber();
        }

        /**
         * Returns the content as a boolean (flag) value.
         * @return the metadata as a boolean
         */
        public bool GetBoolean()
        {
            return GetNumber() != 0;
        }

        public DateTime GetDate()
        {
            //timestamp lengths: 4,7,9
            int i = (int)Math.Floor((float)(_data.Length / 3)) - 1;
            DateTime date;
            if (i >= 0 && i < TIMESTAMPS.Length)
            {
                date = DateTime.ParseExact(Encoding.ASCII.GetString(_data), TIMESTAMPS[i], CultureInfo.InvariantCulture);
            }
            else date = DateTime.MinValue;
            return date;
        }
    }
}