using System;

namespace CameraAPI.AAC.Sbr
{
    public class HFGeneration
    {
        private static int[] goalSbTab = { 21, 23, 32, 43, 46, 64, 85, 93, 128, 0, 0, 0 };

        private class ACorrCoef
        {
            public float[] _r01 = new float[2];
            public float[] _r02 = new float[2];
            public float[] _r11 = new float[2];
            public float[] _r12 = new float[2];
            public float[] _r22 = new float[2];
            public float _det;
        }

        public static void HfGeneration(SBR sbr, float[,,,] Xlow, float[,,,] Xhigh, int ch)
        {
            int l, i, x;
            float[,] alpha_0 = new float[64,2], alpha_1 = new float[64,2];

            int offset = sbr._tHFAdj;
            int first = sbr._t_E[ch,0];
            int last = sbr._t_E[ch,sbr._L_E[ch]];

            CalcChirpFactors(sbr, ch);

            if ((ch == 0) && (sbr._Reset))
                PatchConstruction(sbr);

            /* calculate the prediction coefficients */

            /* actual HF generation */
            for (i = 0; i < sbr._noPatches; i++)
            {
                for (x = 0; x < sbr._patchNoSubbands[i]; x++)
                {
                    float a0_r, a0_i, a1_r, a1_i;
                    float bw, bw2;
                    int q, p, k, g;

                    /* find the low and high band for patching */
                    k = sbr._kx + x;
                    for (q = 0; q < i; q++)
                    {
                        k += sbr._patchNoSubbands[q];
                    }
                    p = sbr._patchStartSubband[i] + x;

                    g = sbr._table_map_k_to_g[k];

                    bw = sbr._bwArray[ch,g];
                    bw2 = bw * bw;

                    /* do the patching */
                    /* with or without filtering */
                    if (bw2 > 0)
                    {
                        float temp1_r, temp2_r, temp3_r;
                        float temp1_i, temp2_i, temp3_i;
                        CalcPredictionCoef(sbr, Xlow, ch, alpha_0, alpha_1, p);

                        a0_r = (alpha_0[p,0] * bw);
                        a1_r = (alpha_1[p,0] * bw2);
                        a0_i = (alpha_0[p,1] * bw);
                        a1_i = (alpha_1[p,1] * bw2);

                        temp2_r = (Xlow[ch,first - 2 + offset,p,0]);
                        temp3_r = (Xlow[ch, first - 1 + offset,p,0]);
                        temp2_i = (Xlow[ch, first - 2 + offset,p,1]);
                        temp3_i = (Xlow[ch, first - 1 + offset,p,1]);
                        for (l = first; l < last; l++)
                        {
                            temp1_r = temp2_r;
                            temp2_r = temp3_r;
                            temp3_r = (Xlow[ch, l + offset,p,0]);
                            temp1_i = temp2_i;
                            temp2_i = temp3_i;
                            temp3_i = (Xlow[ch, l + offset,p,1]);

                            Xhigh[ch, l + offset,k,0]
                                = temp3_r
                                + ((a0_r * temp2_r)
                                - (a0_i * temp2_i)
                                + (a1_r * temp1_r)
                                - (a1_i * temp1_i));
                            Xhigh[ch, l + offset,k,1]
                                = temp3_i
                                + ((a0_i * temp2_r)
                                + (a0_r * temp2_i)
                                + (a1_i * temp1_r)
                                + (a1_r * temp1_i));
                        }
                    }
                    else
                    {
                        for (l = first; l < last; l++)
                        {
                            Xhigh[ch, l + offset,k,0] = Xlow[ch, l + offset,p,0];
                            Xhigh[ch, l + offset,k,1] = Xlow[ch, l + offset,p,1];
                        }
                    }
                }
            }

            if (sbr._Reset)
            {
                FBT.LimiterFrequencyTable(sbr);
            }
        }

        private static void AutoCorrelation(SBR sbr, ACorrCoef ac, float[,,,] buffer, int ch, int bd, int len)
        {
            float r01r = 0, r01i = 0, r02r = 0, r02i = 0, r11r = 0;
            float temp1_r, temp1_i, temp2_r, temp2_i, temp3_r, temp3_i, temp4_r, temp4_i, temp5_r, temp5_i;
            float rel = 1.0f / (1 + 1e-6f);
            int j;
            int offset = sbr._tHFAdj;

            temp2_r = buffer[ch,offset - 2,bd,0];
            temp2_i = buffer[ch, offset - 2,bd,1];
            temp3_r = buffer[ch, offset - 1,bd,0];
            temp3_i = buffer[ch, offset - 1,bd,1];
            // Save these because they are needed after loop
            temp4_r = temp2_r;
            temp4_i = temp2_i;
            temp5_r = temp3_r;
            temp5_i = temp3_i;

            for (j = offset; j < len + offset; j++)
            {
                temp1_r = temp2_r; // temp1_r = QMF_RE(buffer[j-2][bd];
                temp1_i = temp2_i; // temp1_i = QMF_IM(buffer[j-2][bd];
                temp2_r = temp3_r; // temp2_r = QMF_RE(buffer[j-1][bd];
                temp2_i = temp3_i; // temp2_i = QMF_IM(buffer[j-1][bd];
                temp3_r = buffer[ch, j,bd,0];
                temp3_i = buffer[ch, j,bd,1];
                r01r += temp3_r * temp2_r + temp3_i * temp2_i;
                r01i += temp3_i * temp2_r - temp3_r * temp2_i;
                r02r += temp3_r * temp1_r + temp3_i * temp1_i;
                r02i += temp3_i * temp1_r - temp3_r * temp1_i;
                r11r += temp2_r * temp2_r + temp2_i * temp2_i;
            }

            // These are actual values in temporary variable at this point
            // temp1_r = QMF_RE(buffer[len+offset-1-2][bd];
            // temp1_i = QMF_IM(buffer[len+offset-1-2][bd];
            // temp2_r = QMF_RE(buffer[len+offset-1-1][bd];
            // temp2_i = QMF_IM(buffer[len+offset-1-1][bd];
            // temp3_r = QMF_RE(buffer[len+offset-1][bd]);
            // temp3_i = QMF_IM(buffer[len+offset-1][bd]);
            // temp4_r = QMF_RE(buffer[offset-2][bd]);
            // temp4_i = QMF_IM(buffer[offset-2][bd]);
            // temp5_r = QMF_RE(buffer[offset-1][bd]);
            // temp5_i = QMF_IM(buffer[offset-1][bd]);
            ac._r12[0] = r01r
                - (temp3_r * temp2_r + temp3_i * temp2_i)
                + (temp5_r * temp4_r + temp5_i * temp4_i);
            ac._r12[1] = r01i
                - (temp3_i * temp2_r - temp3_r * temp2_i)
                + (temp5_i * temp4_r - temp5_r * temp4_i);
            ac._r22[0] = r11r
                - (temp2_r * temp2_r + temp2_i * temp2_i)
                + (temp4_r * temp4_r + temp4_i * temp4_i);

            ac._r01[0] = r01r;
            ac._r01[1] = r01i;
            ac._r02[0] = r02r;
            ac._r02[1] = r02i;
            ac._r11[0] = r11r;

            ac._det = (ac._r11[0] * ac._r22[0]) - (rel * ((ac._r12[0] * ac._r12[0]) + (ac._r12[1] * ac._r12[1])));
        }

        /* calculate linear prediction coefficients using the covariance method */
        private static void CalcPredictionCoef(SBR sbr, float[,,,] Xlow, int ch, float[,] alpha_0, float[,] alpha_1, int k)
        {
            float tmp;
            ACorrCoef ac = new ACorrCoef();

            AutoCorrelation(sbr, ac, Xlow, ch, k, sbr._numTimeSlotsRate + 6);

            if (ac._det == 0)
            {
                alpha_1[k,0] = 0;
                alpha_1[k,1] = 0;
            }
            else
            {
                tmp = 1.0f / ac._det;
                alpha_1[k,0] = ((ac._r01[0] * ac._r12[0]) - (ac._r01[1] * ac._r12[1]) - (ac._r02[0] * ac._r11[0])) * tmp;
                alpha_1[k,1] = ((ac._r01[1] * ac._r12[0]) + (ac._r01[0] * ac._r12[1]) - (ac._r02[1] * ac._r11[0])) * tmp;
            }

            if (ac._r11[0] == 0)
            {
                alpha_0[k,0] = 0;
                alpha_0[k,1] = 0;
            }
            else
            {
                tmp = 1.0f / ac._r11[0];
                alpha_0[k,0] = -(ac._r01[0] + (alpha_1[k,0] * ac._r12[0]) + (alpha_1[k,1] * ac._r12[1])) * tmp;
                alpha_0[k,1] = -(ac._r01[1] + (alpha_1[k,1] * ac._r12[0]) - (alpha_1[k,0] * ac._r12[1])) * tmp;
            }

            if (((alpha_0[k,0] * alpha_0[k,0]) + (alpha_0[k,1] * alpha_0[k,1]) >= 16.0f)
                || ((alpha_1[k,0] * alpha_1[k,0]) + (alpha_1[k,1] * alpha_1[k,1]) >= 16.0f))
            {
                alpha_0[k,0] = 0;
                alpha_0[k,1] = 0;
                alpha_1[k,0] = 0;
                alpha_1[k,1] = 0;
            }
        }

        /* FIXED POINT: bwArray = COEF */
        private static float MapNewBw(int invf_mode, int invf_mode_prev)
        {
            switch (invf_mode)
            {
                case 1: /* LOW */

                    if (invf_mode_prev == 0) /* NONE */
                        return 0.6f;
                    else
                        return 0.75f;

                case 2: /* MID */

                    return 0.9f;

                case 3: /* HIGH */

                    return 0.98f;

                default: /* NONE */

                    if (invf_mode_prev == 1) /* LOW */
                        return 0.6f;
                    else
                        return 0.0f;
            }
        }

        /* FIXED POINT: bwArray = COEF */
        private static void CalcChirpFactors(SBR sbr, int ch)
        {
            int i;

            for (i = 0; i < sbr._N_Q; i++)
            {
                sbr._bwArray[ch,i] = MapNewBw(sbr._bs_invf_mode[ch,i], sbr._bs_invf_mode_prev[ch,i]);

                if (sbr._bwArray[ch,i] < sbr._bwArray_prev[ch,i])
                    sbr._bwArray[ch,i] = (sbr._bwArray[ch,i] * 0.75f) + (sbr._bwArray_prev[ch,i] * 0.25f);
                else
                    sbr._bwArray[ch,i] = (sbr._bwArray[ch,i] * 0.90625f) + (sbr._bwArray_prev[ch,i] * 0.09375f);

                if (sbr._bwArray[ch,i] < 0.015625f)
                    sbr._bwArray[ch,i] = 0.0f;

                if (sbr._bwArray[ch,i] >= 0.99609375f)
                    sbr._bwArray[ch,i] = 0.99609375f;

                sbr._bwArray_prev[ch,i] = sbr._bwArray[ch,i];
                sbr._bs_invf_mode_prev[ch,i] = sbr._bs_invf_mode[ch,i];
            }
        }

        private static void PatchConstruction(SBR sbr)
        {
            int i, k;
            int odd, sb;
            int msb = sbr._k0;
            int usb = sbr._kx;
            /* (uint8_t)(2.048e6/sbr.sample_rate + 0.5); */
            int goalSb = goalSbTab[(int)sbr._sampleRate];

            sbr._noPatches = 0;

            if (goalSb < (sbr._kx + sbr._M))
            {
                for (i = 0, k = 0; sbr._f_master[i] < goalSb; i++)
                {
                    k = i + 1;
                }
            }
            else
            {
                k = sbr._N_master;
            }

            if (sbr._N_master == 0)
            {
                sbr._noPatches = 0;
                sbr._patchNoSubbands[0] = 0;
                sbr._patchStartSubband[0] = 0;

                return;
            }

            do
            {
                int j = k + 1;

                do
                {
                    j--;

                    sb = sbr._f_master[j];
                    odd = (sb - 2 + sbr._k0) % 2;
                }
                while (sb > (sbr._k0 - 1 + msb - odd));

                sbr._patchNoSubbands[sbr._noPatches] = Math.Max(sb - usb, 0);
                sbr._patchStartSubband[sbr._noPatches] = sbr._k0 - odd
                    - sbr._patchNoSubbands[sbr._noPatches];

                if (sbr._patchNoSubbands[sbr._noPatches] > 0)
                {
                    usb = sb;
                    msb = sb;
                    sbr._noPatches++;
                }
                else
                {
                    msb = sbr._kx;
                }

                if (sbr._f_master[k] - sb < 3)
                    k = sbr._N_master;
            }
            while (sb != (sbr._kx + sbr._M));

            if ((sbr._patchNoSubbands[sbr._noPatches - 1] < 3) && (sbr._noPatches > 1))
            {
                sbr._noPatches--;
            }

            sbr._noPatches = Math.Min(sbr._noPatches, 5);
        }
    }
}
