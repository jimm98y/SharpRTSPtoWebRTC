namespace SharpJaad.MP4.Boxes.Impl
{
    public class CleanApertureBox : BoxImpl
    {
        private long _cleanApertureWidthN;
        private long _cleanApertureWidthD;
        private long _cleanApertureHeightN;
        private long _cleanApertureHeightD;
        private long _horizOffN;
        private long _horizOffD;
        private long _vertOffN;
        private long _vertOffD;

        public CleanApertureBox() : base("Clean Aperture Box")
        { }

        public override void Decode(MP4InputStream input)
        {
            _cleanApertureWidthN = input.ReadBytes(4);
            _cleanApertureWidthD = input.ReadBytes(4);
            _cleanApertureHeightN = input.ReadBytes(4);
            _cleanApertureHeightD = input.ReadBytes(4);
            _horizOffN = input.ReadBytes(4);
            _horizOffD = input.ReadBytes(4);
            _vertOffN = input.ReadBytes(4);
            _vertOffD = input.ReadBytes(4);
        }

        public long GetCleanApertureWidthN()
        {
            return _cleanApertureWidthN;
        }

        public long GetCleanApertureWidthD()
        {
            return _cleanApertureWidthD;
        }

        public long GetCleanApertureHeightN()
        {
            return _cleanApertureHeightN;
        }

        public long GetCleanApertureHeightD()
        {
            return _cleanApertureHeightD;
        }

        public long GetHorizOffN()
        {
            return _horizOffN;
        }

        public long GetHorizOffD()
        {
            return _horizOffD;
        }

        public long GetVertOffN()
        {
            return _vertOffN;
        }

        public long GetVertOffD()
        {
            return _vertOffD;
        }
    }
}