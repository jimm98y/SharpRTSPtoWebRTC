using CameraAPI.AAC.Sbr;

namespace CameraAPI.AAC.Syntax
{
    public abstract class Element
    {
        private int _elementInstanceTag;
        private SBR _sbr;

        protected void ReadElementInstanceTag(BitStream input)
        {
            _elementInstanceTag = input.ReadBits(4);
	    }

        public int GetElementInstanceTag()
        {
            return _elementInstanceTag;
        }

        public void DecodeSBR(BitStream input, SampleFrequency sf, int count, bool stereo, bool crc, bool downSampled, bool smallFrames)
        {
            if (_sbr == null) 
            {
                /* implicit SBR signalling, see 4.6.18.2.6 */
                int fq = sf.GetFrequency();
                if (fq < 24000 && !downSampled)
                    sf = SampleFrequencyExtensions.FromFrequency(2 * fq);
                _sbr = new SBR(smallFrames, stereo, sf, downSampled);
            }
            _sbr.Decode(input, count, crc);
        }

        public bool IsSBRPresent()
        {
            return _sbr != null;
        }

        public SBR GetSBR()
        {
            return _sbr;
        }
    }
}
