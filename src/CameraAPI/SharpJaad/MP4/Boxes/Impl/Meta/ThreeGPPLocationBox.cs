namespace SharpJaad.MP4.Boxes.Impl.Meta
{
    /**
     * This box contains meta information about a location.
     * 
     * If the location information refers to a time-variant location, the name 
     * should express a high-level location, such as "Finland" for several places in
     * Finland or "Finland-Sweden" for several places in Finland and Sweden. Further
     * details on time-variant locations can be provided as additional notes.
     * 
     * The values of longitude, latitude and altitude provide cursory Global 
     * Positioning System (GPS) information of the media content.
     * 
     * A value of longitude (latitude) that is less than –180 (-90) or greater than 
     * 180 (90) indicates that the GPS coordinates (longitude, latitude, altitude) 
     * are unspecified, i.e. none of the given values for longitude, latitude or 
     * altitude are valid.
     * 
     * @author in-somnia
     */
    public class ThreeGPPLocationBox : ThreeGPPMetadataBox
    {
        private int _role;
        private double _longitude, _latitude, _altitude;
        private string _placeName, _astronomicalBody, _additionalNotes;

        public ThreeGPPLocationBox() : base("3GPP Location Information Box")
        { }

        public override void decode(MP4InputStream input)
        {
            DecodeCommon(input);

            _placeName = input.readUTFString((int)GetLeft(input));
            _role = input.read();
            _longitude = input.readFixedPoint(16, 16);
            _latitude = input.readFixedPoint(16, 16);
            _altitude = input.readFixedPoint(16, 16);

            _astronomicalBody = input.readUTFString((int)GetLeft(input));
            _additionalNotes = input.readUTFString((int)GetLeft(input));
        }

        /**
         * A string indicating the name of the place.
         * 
         * @return the place's name
         */
        public string GetPlaceName()
        {
            return _placeName;
        }

        /**
         * The role of the place:<br />
         * <ol start="0">
         * <li>"shooting location"</li>
         * <li>"real location"</li>
         * <li>"fictional location"</li>
         * </ol><br />
         * Other values are reserved. 
         * 
         * @return the role of the place
         */
        public int GetRole()
        {
            return _role;
        }

        /**
         * A floating point number indicating the longitude in degrees. Negative 
         * values represent western longitude.
         * 
         * @return the longitude
         */
        public double GetLongitude()
        {
            return _longitude;
        }

        /**
         * A floating point number indicating the latitude in degrees. Negative 
         * values represent southern latitude.
         * 
         * @return the latitude
         */
        public double GetLatitude()
        {
            return _latitude;
        }

        /**
         * A floating point number indicating the altitude in meters. The reference 
         * altitude, indicated by zero, is set to the sea level.
         * 
         * @return the altitude
         */
        public double GetAltitude()
        {
            return _altitude;
        }

        /**
         * A string indicating the astronomical body on which the location exists, 
         * e.g. "earth".
         * 
         * @return the astronomical body
         */
        public string GetAstronomicalBody()
        {
            return _astronomicalBody;
        }

        /**
         * A string containing any additional location-related information.
         * 
         * @return the additional notes
         */
        public string GetAdditionalNotes()
        {
            return _additionalNotes;
        }
    }
}
