using CameraAPI.AAC.Sbr;

namespace CameraAPI.AAC.Syntax
{
    public class Element : Constants
    {
        private int elementInstanceTag;
        private SBR sbr;

        protected void readElementInstanceTag(BitStream input)
        {
            elementInstanceTag = input.readBits(4);
	    }

        public int getElementInstanceTag()
        {
            return elementInstanceTag;
        }

        public void decodeSBR(BitStream input, SampleFrequency sf, int count, bool stereo, bool crc, bool downSampled, bool smallFrames)
        {
            if (sbr == null) {
                /* implicit SBR signalling, see 4.6.18.2.6 */
                int fq = sf.GetFrequency();
                if (fq < 24000 && !downSampled)
                    sf = SampleFrequencyExtensions.FromFrequency(2 * fq);
                sbr = new SBR(smallFrames, stereo, sf, downSampled);
            }
            sbr.Decode(input, count, crc);
        }

        public bool isSBRPresent()
        {
            return sbr != null;
        }

        public SBR getSBR()
        {
            return sbr;
        }
    }
}
