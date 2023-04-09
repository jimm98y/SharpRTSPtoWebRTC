using CameraAPI.AAC.Huffman;
using CameraAPI.AAC.Syntax;

namespace CameraAPI.AAC.Tools
{
    public class IS : Constants
    {
        public IS()
        {
        }

        public static void process(CPE cpe, float[] specL, float[] specR)
        {
            ICStream ics = cpe.getRightChannel();
            ICSInfo info = ics.getInfo();
            int[] offsets = info.getSWBOffsets();
            int windowGroups = info.getWindowGroupCount();
            int maxSFB = info.getMaxSFB();
            int[] sfbCB = ics.getSfbCB();
            int[] sectEnd = ics.getSectEnd();
            float[] scaleFactors = ics.getScaleFactors();

            int w, i, j, c, end, off;
            int idx = 0, groupOff = 0;
            float scale;
            for (int g = 0; g < windowGroups; g++)
            {
                for (i = 0; i < maxSFB;)
                {
                    if (sfbCB[idx] == HCB.INTENSITY_HCB || sfbCB[idx] == HCB.INTENSITY_HCB2)
                    {
                        end = sectEnd[idx];
                        for (; i < end; i++, idx++)
                        {
                            c = sfbCB[idx] == HCB.INTENSITY_HCB ? 1 : -1;
                            if (cpe.isMSMaskPresent())
                                c *= cpe.isMSUsed(idx) ? -1 : 1;
                            scale = c * scaleFactors[idx];
                            for (w = 0; w < info.getWindowGroupLength(g); w++)
                            {
                                off = groupOff + w * 128 + offsets[i];
                                for (j = 0; j < offsets[i + 1] - offsets[i]; j++)
                                {
                                    specR[off + j] = specL[off + j] * scale;
                                }
                            }
                        }
                    }
                    else
                    {
                        end = sectEnd[idx];
                        idx += end - i;
                        i = end;
                    }
                }
                groupOff += info.getWindowGroupLength(g) * 128;
            }
        }
    }
}
