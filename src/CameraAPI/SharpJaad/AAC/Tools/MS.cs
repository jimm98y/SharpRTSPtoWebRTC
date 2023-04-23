using SharpJaad.AAC.Huffman;
using SharpJaad.AAC.Syntax;

namespace SharpJaad.AAC.Tools
{
    public class MS
    {
        public static void Process(CPE cpe, float[] specL, float[] specR)
        {
            ICStream ics = cpe.GetLeftChannel();
            ICSInfo info = ics.GetInfo();
            int[] offsets = info.GetSWBOffsets();
            int windowGroups = info.GetWindowGroupCount();
            int maxSFB = info.GetMaxSFB();
            int[] sfbCBl = ics.getSfbCB();
            int[] sfbCBr = cpe.GetRightChannel().getSfbCB();
            int groupOff = 0;
            int g, i, w, j, idx = 0;

            for (g = 0; g < windowGroups; g++)
            {
                for (i = 0; i < maxSFB; i++, idx++)
                {
                    if (cpe.IsMSUsed(idx) && sfbCBl[idx] < HCB.NOISE_HCB && sfbCBr[idx] < HCB.NOISE_HCB)
                    {
                        for (w = 0; w < info.GetWindowGroupLength(g); w++)
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
                groupOff += info.GetWindowGroupLength(g) * 128;
            }
        }
    }
}
