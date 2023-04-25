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

        public override void Decode(MP4InputStream input)
        {
            base.Decode(input);

            input.SkipBytes(2); //pre-defined: 0
            input.SkipBytes(2); //reserved
                                //3x32 pre_defined
            input.SkipBytes(4); //pre-defined: 0
            input.SkipBytes(4); //pre-defined: 0
            input.SkipBytes(4); //pre-defined: 0

            _width = (int)input.ReadBytes(2);
            _height = (int)input.ReadBytes(2);
            _horizontalResolution = input.ReadFixedPoint(16, 16);
            _verticalResolution = input.ReadFixedPoint(16, 16);
            input.SkipBytes(4); //reserved
            _frameCount = (int)input.ReadBytes(2);

            int len = input.Read();
            _compressorName = input.ReadString(len);
            input.SkipBytes(31 - len);

            _depth = (int)input.ReadBytes(2);
            input.SkipBytes(2); //pre-defined: -1

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
