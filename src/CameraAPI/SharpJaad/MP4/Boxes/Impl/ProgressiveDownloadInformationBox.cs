using System.Collections.Generic;

namespace SharpJaad.MP4.Boxes.Impl
{
    /**
     * The Progressive download information box aids the progressive download of an
     * ISO file. The box contains pairs of numbers (to the end of the box)
     * specifying combinations of effective file download bitrate in units of
     * bytes/sec and a suggested initial playback delay in units of milliseconds.
     *
     * The download rate can be estimated from the download rate and obtain an upper
     * estimate for a suitable initial delay by linear interpolation between pairs,
     * or by extrapolation from the first or last entry.
     * @author in-somnia
     */
    public class ProgressiveDownloadInformationBox : FullBox
    {
        private Dictionary<long, long> _pairs;

        public ProgressiveDownloadInformationBox() : base("Progressive Download Information Box")
        {
            _pairs = new Dictionary<long, long>();
        }

        public override void decode(MP4InputStream input)
        {
            base.decode(input);

            long rate, initialDelay;
            while (GetLeft(input) > 0)
            {
                rate = input.readBytes(4);
                initialDelay = input.readBytes(4);
                _pairs.Add(rate, initialDelay);
            }
        }

        /**
         * The map contains pairs of bitrates and playback delay.
         * @return the information pairs
         */
        public Dictionary<long, long> GetInformationPairs()
        {
            return _pairs;
        }
    }
}
