namespace SharpJaad.MP4.Boxes.Impl.SampleEntries
{
    public class VideoSampleEntry : SampleEntry
    {
        private int _width, _height;
        private double _horizontalResolution, _verticalResolution;
        private int _frameCount, _depth;
        private string _compressorName;

        public VideoSampleEntry(string name) : base(name)
        { }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            input.skipBytes(2); //pre-defined: 0
            input.skipBytes(2); //reserved
                                //3x32 pre_defined
            input.skipBytes(4); //pre-defined: 0
            input.skipBytes(4); //pre-defined: 0
            input.skipBytes(4); //pre-defined: 0

            _width = (int)input.readBytes(2);
            _height = (int)input.readBytes(2);
            _horizontalResolution = input.readFixedPoint(16, 16);
            _verticalResolution = input.readFixedPoint(16, 16);
            input.skipBytes(4); //reserved
            _frameCount = (int)input.readBytes(2);

            int len = input.read();
            _compressorName = input.readString(len);
            input.skipBytes(31 - len);

            _depth = (int)input.readBytes(2);
            input.skipBytes(2); //pre-defined: -1

            ReadChildren(input);
        }

        /**
         * The width is the maximum visual width of the stream described by this
         * sample description, in pixels.
         */
        public int GetWidth()
        {
            return _width;
        }

        /**
         * The height is the maximum visual height of the stream described by this
         * sample description, in pixels.
         */
        public int GetHeight()
        {
            return _height;
        }

        /**
         * The horizontal resolution of the image in pixels-per-inch, as a floating
         * point value.
         */
        public double GetHorizontalResolution()
        {
            return _horizontalResolution;
        }

        /**
         * The vertical resolution of the image in pixels-per-inch, as a floating
         * point value.
         */
        public double GetVerticalResolution()
        {
            return _verticalResolution;
        }

        /**
         * The frame count indicates how many frames of compressed video are stored 
         * in each sample.
         */
        public int GetFrameCount()
        {
            return _frameCount;
        }

        /**
         * The compressor name, for informative purposes.
         */
        public string GetCompressorName()
        {
            return _compressorName;
        }

        /**
         * The depth takes one of the following values
         * DEFAULT_DEPTH (0x18) – images are in colour with no alpha
         */
        public int GetDepth()
        {
            return _depth;
        }
    }
}
