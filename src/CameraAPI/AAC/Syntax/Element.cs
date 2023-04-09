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
		    if(sbr==null) sbr = new SBR(smallFrames, elementInstanceTag==ELEMENT_CPE, sf, downSampled);
            sbr.Decode(input, count);
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
