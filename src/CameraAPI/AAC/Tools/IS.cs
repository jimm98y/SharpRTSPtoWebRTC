using CameraAPI.AAC.Huffman;
using CameraAPI.AAC.Syntax;

namespace CameraAPI.AAC.Tools
{
    public class IS
    {
        public static void Process(CPE cpe, float[] specL, float[] specR)
        {
            ICStream ics = cpe.GetRightChannel();
            ICSInfo info = ics.GetInfo();
            int[] offsets = info.GetSWBOffsets();
            int windowGroups = info.GetWindowGroupCount();
            int maxSFB = info.GetMaxSFB();
            int[] sfbCB = ics.getSfbCB();
            int[] sectEnd = ics.GetSectEnd();
            float[] scaleFactors = ics.GetScaleFactors();

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
                            if (cpe.IsMSMaskPresent())
                                c *= cpe.IsMSUsed(idx) ? -1 : 1;
                            scale = c * scaleFactors[idx];
                            for (w = 0; w < info.GetWindowGroupLength(g); w++)
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
                groupOff += info.GetWindowGroupLength(g) * 128;
            }
        }
    }
}
