using System;

namespace SharpJaad.AAC.Sbr
{
    public static class FBT
    {
        /* calculate the start QMF channel for the master frequency band table */
        /* parameter is also called k0 */
        public static int QmfStartChannel(int bs_start_freq, int bs_samplerate_mode,
            SampleFrequency sample_rate)
        {
            int startMin = Constants.startMinTable[(int)sample_rate];
            int offsetIndex = Constants.offsetIndexTable[(int)sample_rate];

            if (bs_samplerate_mode != 0)
            {
                return startMin + Constants.OFFSET[offsetIndex][bs_start_freq];
            }
            else
            {
                return startMin + Constants.OFFSET[6][bs_start_freq];
            }
        }

        private static int[] stopMinTable = {13, 15, 20, 21, 23,
            32, 32, 35, 48, 64, 70, 96};

        private static int[][] STOP_OFFSET_TABLE = {
            new int[] {0, 2, 4, 6, 8, 11, 14, 18, 22, 26, 31, 37, 44, 51},
            new int[] {0, 2, 4, 6, 8, 11, 14, 18, 22, 26, 31, 36, 42, 49},
            new int[] {0, 2, 4, 6, 8, 11, 14, 17, 21, 25, 29, 34, 39, 44},
            new int[] {0, 2, 4, 6, 8, 11, 14, 17, 20, 24, 28, 33, 38, 43},
            new int[] {0, 2, 4, 6, 8, 11, 14, 17, 20, 24, 28, 32, 36, 41},
            new int[] {0, 2, 4, 6, 8, 10, 12, 14, 17, 20, 23, 26, 29, 32},
            new int[] {0, 2, 4, 6, 8, 10, 12, 14, 17, 20, 23, 26, 29, 32},
            new int[] {0, 1, 3, 5, 7, 9, 11, 13, 15, 17, 20, 23, 26, 29},
            new int[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 14, 16},
            new int[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            new int[] {0, -1, -2, -3, -4, -5, -6, -6, -6, -6, -6, -6, -6, -6},
            new int[] {0, -3, -6, -9, -12, -15, -18, -20, -22, -24, -26, -28, -30, -32}
        };

        /* calculate the stop QMF channel for the master frequency band table */
        /* parameter is also called k2 */
        public static int QmfStopChannel(int bs_stop_freq, SampleFrequency sample_rate,
            int k0)
        {
            if (bs_stop_freq == 15)
            {
                return Math.Min(64, k0 * 3);
            }
            else if (bs_stop_freq == 14)
            {
                return Math.Min(64, k0 * 2);
            }
            else
            {

                int stopMin = stopMinTable[(int)sample_rate];

                /* bs_stop_freq <= 13 */
                return Math.Min(64, stopMin + STOP_OFFSET_TABLE[(int)sample_rate][Math.Min(bs_stop_freq, 13)]);
            }
        }

        /* calculate the master frequency table from k0, k2, bs_freq_scale
         and bs_alter_scale

         version for bs_freq_scale = 0
         */
        public static int MasterFrequencyTableFs0(SBR sbr, int k0, int k2, bool bs_alter_scale)
        {
            int incr;
            int k;
            int dk;
            int nrBands, k2Achieved;
            int k2Diff;
            int[] vDk = new int[64];

            /* mft only defined for k2 > k0 */
            if (k2 <= k0)
            {
                sbr._N_master = 0;
                return 1;
            }

            dk = bs_alter_scale ? 2 : 1;

            if (bs_alter_scale)
            {
                nrBands = k2 - k0 + 2 >> 2 << 1;
            }
            else
            {
                nrBands = k2 - k0 >> 1 << 1;
            }
            nrBands = Math.Min(nrBands, 63);
            if (nrBands <= 0)
                return 1;

            k2Achieved = k0 + nrBands * dk;
            k2Diff = k2 - k2Achieved;
            for (k = 0; k < nrBands; k++)
            {
                vDk[k] = dk;
            }

            if (k2Diff != 0)
            {
                incr = k2Diff > 0 ? -1 : 1;
                k = k2Diff > 0 ? nrBands - 1 : 0;

                while (k2Diff != 0)
                {
                    vDk[k] -= incr;
                    k += incr;
                    k2Diff += incr;
                }
            }

            sbr._f_master[0] = k0;
            for (k = 1; k <= nrBands; k++)
            {
                sbr._f_master[k] = sbr._f_master[k - 1] + vDk[k - 1];
            }

            sbr._N_master = nrBands;
            sbr._N_master = Math.Min(sbr._N_master, 64);

            return 0;
        }

        /*
         This function finds the number of bands using this formula:
         bands * log(a1/a0)/log(2.0) + 0.5
         */
        public static int FindBands(int warp, int bands, int a0, int a1)
        {
            float div = (float)Math.Log(2.0);
            if (warp != 0) div *= 1.3f;

            return (int)(bands * Math.Log(a1 / (float)a0) / div + 0.5);
        }

        public static float FindInitialPower(int bands, int a0, int a1)
        {
            return (float)Math.Pow(a1 / (float)a0, 1.0f / bands);
        }

        /*
         version for bs_freq_scale > 0
         */
        public static int MasterFrequencyTable(SBR sbr, int k0, int k2, int bs_freq_scale, bool bs_alter_scale)
        {
            int k, bands;
            bool twoRegions;
            int k1;
            int nrBand0, nrBand1;
            int[] vDk0 = new int[64], vDk1 = new int[64];
            int[] vk0 = new int[64], vk1 = new int[64];
            int[] temp1 = { 6, 5, 4 };
            float q, qk;
            int A_1;

            /* mft only defined for k2 > k0 */
            if (k2 <= k0)
            {
                sbr._N_master = 0;
                return 1;
            }

            bands = temp1[bs_freq_scale - 1];

            if (k2 / (float)k0 > 2.2449)
            {
                twoRegions = true;
                k1 = k0 << 1;
            }
            else
            {
                twoRegions = false;
                k1 = k2;
            }

            nrBand0 = 2 * FindBands(0, bands, k0, k1);
            nrBand0 = Math.Min(nrBand0, 63);
            if (nrBand0 <= 0)
                return 1;

            q = FindInitialPower(nrBand0, k0, k1);
            qk = k0;
            A_1 = (int)(qk + 0.5f);
            for (k = 0; k <= nrBand0; k++)
            {
                int A_0 = A_1;
                qk *= q;
                A_1 = (int)(qk + 0.5f);
                vDk0[k] = A_1 - A_0;
            }

            /* needed? */
            //qsort(vDk0, nrBand0, sizeof(vDk0[0]), longcmp);
            Array.Sort(vDk0, 0, nrBand0);

            vk0[0] = k0;
            for (k = 1; k <= nrBand0; k++)
            {
                vk0[k] = vk0[k - 1] + vDk0[k - 1];
                if (vDk0[k - 1] == 0)
                    return 1;
            }

            if (!twoRegions)
            {
                for (k = 0; k <= nrBand0; k++)
                {
                    sbr._f_master[k] = vk0[k];
                }

                sbr._N_master = nrBand0;
                sbr._N_master = Math.Min(sbr._N_master, 64);
                return 0;
            }

            nrBand1 = 2 * FindBands(1 /* warped */, bands, k1, k2);
            nrBand1 = Math.Min(nrBand1, 63);

            q = FindInitialPower(nrBand1, k1, k2);
            qk = k1;
            A_1 = (int)(qk + 0.5f);
            for (k = 0; k <= nrBand1 - 1; k++)
            {
                int A_0 = A_1;
                qk *= q;
                A_1 = (int)(qk + 0.5f);
                vDk1[k] = A_1 - A_0;
            }

            if (vDk1[0] < vDk0[nrBand0 - 1])
            {
                int change;

                /* needed? */
                //qsort(vDk1, nrBand1+1, sizeof(vDk1[0]), longcmp);
                Array.Sort(vDk1, 0, nrBand1 + 1);
                change = vDk0[nrBand0 - 1] - vDk1[0];
                vDk1[0] = vDk0[nrBand0 - 1];
                vDk1[nrBand1 - 1] = vDk1[nrBand1 - 1] - change;
            }

            /* needed? */
            //qsort(vDk1, nrBand1, sizeof(vDk1[0]), longcmp);
            Array.Sort(vDk1, 0, nrBand1);
            vk1[0] = k1;
            for (k = 1; k <= nrBand1; k++)
            {
                vk1[k] = vk1[k - 1] + vDk1[k - 1];
                if (vDk1[k - 1] == 0)
                    return 1;
            }

            sbr._N_master = nrBand0 + nrBand1;
            sbr._N_master = Math.Min(sbr._N_master, 64);
            for (k = 0; k <= nrBand0; k++)
            {
                sbr._f_master[k] = vk0[k];
            }
            for (k = nrBand0 + 1; k <= sbr._N_master; k++)
            {
                sbr._f_master[k] = vk1[k - nrBand0];
            }

            return 0;
        }

        /* calculate the derived frequency border tables from f_master */
        public static int DerivedFrequencyTable(SBR sbr, int bs_xover_band,
            int k2)
        {
            int k, i = 0;
            int minus;

            /* The following relation shall be satisfied: bs_xover_band < N_Master */
            if (sbr._N_master <= bs_xover_band)
                return 1;

            sbr._N_high = sbr._N_master - bs_xover_band;
            sbr._N_low = (sbr._N_high >> 1) + (sbr._N_high - (sbr._N_high >> 1 << 1));

            sbr._n[0] = sbr._N_low;
            sbr._n[1] = sbr._N_high;

            for (k = 0; k <= sbr._N_high; k++)
            {
                sbr._f_table_res[Constants.HI_RES, k] = sbr._f_master[k + bs_xover_band];
            }

            sbr._M = sbr._f_table_res[Constants.HI_RES, sbr._N_high] - sbr._f_table_res[Constants.HI_RES, 0];
            sbr._kx = sbr._f_table_res[Constants.HI_RES, 0];
            if (sbr._kx > 32)
                return 1;
            if (sbr._kx + sbr._M > 64)
                return 1;

            minus = (sbr._N_high & 1) != 0 ? 1 : 0;

            for (k = 0; k <= sbr._N_low; k++)
            {
                if (k == 0)
                    i = 0;
                else
                    i = 2 * k - minus;
                sbr._f_table_res[Constants.LO_RES, k] = sbr._f_table_res[Constants.HI_RES, i];
            }

            sbr._N_Q = 0;
            if (sbr._bs_noise_bands == 0)
            {
                sbr._N_Q = 1;
            }
            else
            {
                sbr._N_Q = Math.Max(1, FindBands(0, sbr._bs_noise_bands, sbr._kx, k2));
                sbr._N_Q = Math.Min(5, sbr._N_Q);
            }

            for (k = 0; k <= sbr._N_Q; k++)
            {
                if (k == 0)
                {
                    i = 0;
                }
                else
                {
                    /* i = i + (int32_t)((sbr.N_low - i)/(sbr.N_Q + 1 - k)); */
                    i += (sbr._N_low - i) / (sbr._N_Q + 1 - k);
                }
                sbr._f_table_noise[k] = sbr._f_table_res[Constants.LO_RES, i];
            }

            /* build table for mapping k to g in hf patching */
            for (k = 0; k < 64; k++)
            {
                int g;
                for (g = 0; g < sbr._N_Q; g++)
                {
                    if (sbr._f_table_noise[g] <= k
                        && k < sbr._f_table_noise[g + 1])
                    {
                        sbr._table_map_k_to_g[k] = g;
                        break;
                    }
                }
            }
            return 0;
        }

        /* TODO: blegh, ugly */
        /* Modified to calculate for all possible bs_limiter_bands always
         * This reduces the number calls to this functions needed (now only on
         * header reset)
         */
        private static float[] limiterBandsCompare = {1.327152f,
        1.185093f, 1.119872f};

        public static void LimiterFrequencyTable(SBR sbr)
        {
            int k, s;
            int nrLim;

            sbr._f_table_lim[0, 0] = sbr._f_table_res[Constants.LO_RES, 0] - sbr._kx;
            sbr._f_table_lim[0, 1] = sbr._f_table_res[Constants.LO_RES, sbr._N_low] - sbr._kx;
            sbr._N_L[0] = 1;

            for (s = 1; s < 4; s++)
            {
                int[] limTable = new int[100 /*TODO*/];
                int[] patchBorders = new int[64/*??*/];

                patchBorders[0] = sbr._kx;
                for (k = 1; k <= sbr._noPatches; k++)
                {
                    patchBorders[k] = patchBorders[k - 1] + sbr._patchNoSubbands[k - 1];
                }

                for (k = 0; k <= sbr._N_low; k++)
                {
                    limTable[k] = sbr._f_table_res[Constants.LO_RES, k];
                }
                for (k = 1; k < sbr._noPatches; k++)
                {
                    limTable[k + sbr._N_low] = patchBorders[k];
                }

                /* needed */
                //qsort(limTable, sbr.noPatches+sbr.N_low, sizeof(limTable[0]), longcmp);
                Array.Sort(limTable, 0, sbr._noPatches + sbr._N_low);
                k = 1;
                nrLim = sbr._noPatches + sbr._N_low - 1;

                if (nrLim < 0) // TODO: BIG FAT PROBLEM
                    return;

                //restart:
                while (k <= nrLim)
                {
                    float nOctaves;

                    if (limTable[k - 1] != 0)
                        nOctaves = limTable[k] / (float)limTable[k - 1];
                    else
                        nOctaves = 0;

                    if (nOctaves < limiterBandsCompare[s - 1])
                    {
                        int i;
                        if (limTable[k] != limTable[k - 1])
                        {
                            bool found = false, found2 = false;
                            for (i = 0; i <= sbr._noPatches; i++)
                            {
                                if (limTable[k] == patchBorders[i])
                                    found = true;
                            }
                            if (found)
                            {
                                found2 = false;
                                for (i = 0; i <= sbr._noPatches; i++)
                                {
                                    if (limTable[k - 1] == patchBorders[i])
                                        found2 = true;
                                }
                                if (found2)
                                {
                                    k++;
                                    continue;
                                }
                                else
                                {
                                    /* remove (k-1)th element */
                                    limTable[k - 1] = sbr._f_table_res[Constants.LO_RES, sbr._N_low];
                                    //qsort(limTable, sbr.noPatches+sbr.N_low, sizeof(limTable[0]), longcmp);
                                    Array.Sort(limTable, 0, sbr._noPatches + sbr._N_low);
                                    nrLim--;
                                    continue;
                                }
                            }
                        }
                        /* remove kth element */
                        limTable[k] = sbr._f_table_res[Constants.LO_RES, sbr._N_low];
                        //qsort(limTable, nrLim, sizeof(limTable[0]), longcmp);
                        Array.Sort(limTable, 0, nrLim);
                        nrLim--;
                        //continue;
                    }
                    else
                    {
                        k++;
                        //continue;
                    }
                }

                sbr._N_L[s] = nrLim;
                for (k = 0; k <= nrLim; k++)
                {
                    sbr._f_table_lim[s, k] = limTable[k] - sbr._kx;
                }

            }
        }
    }
}
