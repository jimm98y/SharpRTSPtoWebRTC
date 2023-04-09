using CameraAPI.AAC.Huffman;
using CameraAPI.AAC.Syntax;

namespace CameraAPI.AAC.Tools
{
    public class MS : Constants
    {
        public MS()
        {
        }

        public static void process(CPE cpe, float[] specL, float[] specR)
        {
            ICStream ics = cpe.getLeftChannel();
            ICSInfo info = ics.getInfo();
            int[] offsets = info.getSWBOffsets();
            int windowGroups = info.getWindowGroupCount();
            int maxSFB = info.getMaxSFB();
            int[] sfbCBl = ics.getSfbCB();
            int[] sfbCBr = cpe.getRightChannel().getSfbCB();
            int groupOff = 0;
            int g, i, w, j, idx = 0;

            for (g = 0; g < windowGroups; g++)
            {
                for (i = 0; i < maxSFB; i++, idx++)
                {
                    if (cpe.isMSUsed(idx) && sfbCBl[idx] < HCB.NOISE_HCB && sfbCBr[idx] < HCB.NOISE_HCB)
                    {
                        for (w = 0; w < info.getWindowGroupLength(g); w++)
                        {
                            int off = groupOff + w * 128 + offsets[i];
                            for (j = 0; j < offsets[i + 1] - offsets[i]; j++)
                            {
                                float t = specL[off + j] - specR[off + j];
                                specL[off + j] += specR[off + j];
                                specR[off + j] = t;
                            }
                        }
                    }
                }
                groupOff += info.getWindowGroupLength(g) * 128;
            }
        }
    }
}
