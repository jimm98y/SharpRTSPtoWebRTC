using SharpJaad.AAC.Syntax;
using System;

namespace SharpJaad.AAC.Ps
{
    public class PS
    {
        /* bitstream parameters */
        private bool _enableIid, _enableIcc, _enableExt;
        private int _iidMode;
        private int _iccMode;
        private int _nrIidPar;
        private int _nrIpdopdPar;
        private int _nrIccPar;
        private int _frameClass;
        private int _numEnv;
        private int[] _borderPosition = new int[PSConstants.MAX_PS_ENVELOPES + 1];
        private bool[] _iidDt = new bool[PSConstants.MAX_PS_ENVELOPES];
        private bool[] _iccDt = new bool[PSConstants.MAX_PS_ENVELOPES];
        private bool _enableIpdopd;
        private int _ipdMode;
        private bool[] _ipdDt = new bool[PSConstants.MAX_PS_ENVELOPES];
        private bool[] _opdDt = new bool[PSConstants.MAX_PS_ENVELOPES];

        /* indices */
        private int[] _iidIndexPrev = new int[34];
        private int[] _iccIndexPrev = new int[34];
        private int[] _ipdIndexPrev = new int[17];
        private int[] _opdIndexPrev = new int[17];
        private int[][] _iidIndex = new int[PSConstants.MAX_PS_ENVELOPES][];
        private int[][] _iccIndex = new int[PSConstants.MAX_PS_ENVELOPES][];
        private int[][] _ipdIndex = new int[PSConstants.MAX_PS_ENVELOPES][];
        private int[][] _opdIndex = new int[PSConstants.MAX_PS_ENVELOPES][];

        private int[] _ipdIndex1 = new int[17];
        private int[] _opdIndex1 = new int[17];
        private int[] _ipdIndex2 = new int[17];
        private int[] _opdIndex2 = new int[17];
        /* ps data was correctly read */
        private int _psDataAvailable;
        /* a header has been read */
        public bool _headerRead;
        /* hybrid filterbank parameters */
        private Filterbank _hyb;
        private bool _use34hybridBands;
        private int _numTimeSlotsRate;
        private int _numGroups;
        private int _numHybridGroups;
        private int _nrParBands;
        private int _nrAllpassBands;
        private int _decayCutoff;
        private int[] _groupBorder;
        private int[] _mapGroup2bk;
        /* filter delay handling */
        private int _savedDelay;
        private int[] _delayBufIndexSer = new int[PSConstants.NO_ALLPASS_LINKS];
        private int[] _numSampleDelaySer = new int[PSConstants.NO_ALLPASS_LINKS];
        private int[] _delayD = new int[64];
        private int[] _delayBufIndexDelay = new int[64];
        private float[,,] _delayQmf = new float[14, 64, 2]; /* 14 samples delay max, 64 QMF channels */

        private float[,,] _delaySubQmf = new float[2, 32, 2]; /* 2 samples delay max (SubQmf is always allpass filtered) */

        private float[,,,] _delayQmfSer = new float[PSConstants.NO_ALLPASS_LINKS, 5, 64, 2]; /* 5 samples delay max (table 8.34), 64 QMF channels */

        private float[,,,] _delaySubQmfSer = new float[PSConstants.NO_ALLPASS_LINKS, 5, 32, 2]; /* 5 samples delay max (table 8.34) */
        /* transients */

        private float _alphaDecay;
        private float _alphaSmooth;
        private float[] _P_PeakDecayNrg = new float[34];
        private float[] _P_prev = new float[34];
        private float[] _P_SmoothPeakDecayDiffNrg_prev = new float[34];
        /* mixing and phase */
        private float[,] _h11Prev = new float[50, 2];
        private float[,] _h12Prev = new float[50, 2];
        private float[,] _h21Prev = new float[50, 2];
        private float[,] _h22Prev = new float[50, 2];
        private int _phaseHist;
        private float[,,] _ipdPrev = new float[20, 2, 2];
        private float[,,] _opdPrev = new float[20, 2, 2];

        public PS(SampleFrequency sr, int numTimeSlotsRate)
        {
            int i;
            int short_delay_band;

            for (i = 0; i < PSConstants.MAX_PS_ENVELOPES; i++)
            {
                _iidIndex[i] = new int[34];
                _iccIndex[i] = new int[34];
                _ipdIndex[i] = new int[17];
                _opdIndex[i] = new int[17];
            }

            _hyb = new Filterbank(numTimeSlotsRate);
            _numTimeSlotsRate = numTimeSlotsRate;

            _psDataAvailable = 0;

            /* delay stuff*/
            _savedDelay = 0;

            for (i = 0; i < 64; i++)
            {
                _delayBufIndexDelay[i] = 0;
            }

            for (i = 0; i < PSConstants.NO_ALLPASS_LINKS; i++)
            {
                _delayBufIndexSer[i] = 0;
                /* THESE ARE CONSTANTS NOW */
                _numSampleDelaySer[i] = PSTables.delay_length_d[i];
            }

            /* THESE ARE CONSTANTS NOW */
            short_delay_band = 35;
            _nrAllpassBands = 22;
            _alphaDecay = 0.76592833836465f;
            _alphaSmooth = 0.25f;

            /* THESE ARE CONSTANT NOW IF PS IS INDEPENDANT OF SAMPLERATE */
            for (i = 0; i < short_delay_band; i++)
            {
                _delayD[i] = 14;
            }
            for (i = short_delay_band; i < 64; i++)
            {
                _delayD[i] = 1;
            }

            /* mixing and phase */
            for (i = 0; i < 50; i++)
            {
                _h11Prev[i, 0] = 1;
                _h12Prev[i, 1] = 1;
                _h11Prev[i, 0] = 1;
                _h12Prev[i, 1] = 1;
            }

            _phaseHist = 0;

            for (i = 0; i < 20; i++)
            {
                _ipdPrev[i, 0, 0] = 0;
                _ipdPrev[i, 0, 1] = 0;
                _ipdPrev[i, 1, 0] = 0;
                _ipdPrev[i, 1, 1] = 0;
                _opdPrev[i, 0, 0] = 0;
                _opdPrev[i, 0, 1] = 0;
                _opdPrev[i, 1, 0] = 0;
                _opdPrev[i, 1, 1] = 0;
            }
        }

        public int Decode(BitStream ld)
        {
            int tmp, n;
            long bits = ld.GetPosition();

            /* check for new PS header */
            if (ld.ReadBool())
            {
                _headerRead = true;

                _use34hybridBands = false;

                /* Inter-channel Intensity Difference (IID) parameters enabled */
                _enableIid = ld.ReadBool();

                if (_enableIid)
                {
                    _iidMode = ld.ReadBits(3);

                    _nrIidPar = PSTables.nr_iid_par_tab[_iidMode];
                    _nrIpdopdPar = PSTables.nr_ipdopd_par_tab[_iidMode];

                    if (_iidMode == 2 || _iidMode == 5)
                        _use34hybridBands = true;

                    /* IPD freq res equal to IID freq res */
                    _ipdMode = _iidMode;
                }

                /* Inter-channel Coherence (ICC) parameters enabled */
                _enableIcc = ld.ReadBool();

                if (_enableIcc)
                {
                    _iccMode = ld.ReadBits(3);

                    _nrIccPar = PSTables.nr_icc_par_tab[_iccMode];

                    if (_iccMode == 2 || _iccMode == 5)
                        _use34hybridBands = true;
                }

                /* PS extension layer enabled */
                _enableExt = ld.ReadBool();
            }

            /* we are here, but no header has been read yet */
            if (_headerRead == false)
            {
                _psDataAvailable = 0;
                return 1;
            }

            _frameClass = ld.ReadBit();
            tmp = ld.ReadBits(2);

            _numEnv = PSTables.num_env_tab[_frameClass][tmp];

            if (_frameClass != 0)
            {
                for (n = 1; n < _numEnv + 1; n++)
                {
                    _borderPosition[n] = ld.ReadBits(5) + 1;
                }
            }

            if (_enableIid)
            {
                for (n = 0; n < _numEnv; n++)
                {
                    _iidDt[n] = ld.ReadBool();

                    /* iid_data */
                    if (_iidMode < 3)
                    {
                        HuffData(ld, _iidDt[n], _nrIidPar, HuffmanTables.t_huff_iid_def,
                            HuffmanTables.f_huff_iid_def, _iidIndex[n]);
                    }
                    else
                    {
                        HuffData(ld, _iidDt[n], _nrIidPar, HuffmanTables.t_huff_iid_fine,
                            HuffmanTables.f_huff_iid_fine, _iidIndex[n]);
                    }
                }
            }

            if (_enableIcc)
            {
                for (n = 0; n < _numEnv; n++)
                {
                    _iccDt[n] = ld.ReadBool();

                    /* icc_data */
                    HuffData(ld, _iccDt[n], _nrIccPar, HuffmanTables.t_huff_icc,
                        HuffmanTables.f_huff_icc, _iccIndex[n]);
                }
            }

            if (_enableExt)
            {
                int num_bits_left;
                int cnt = ld.ReadBits(4);
                if (cnt == 15)
                {
                    cnt += ld.ReadBits(8);
                }

                num_bits_left = 8 * cnt;
                while (num_bits_left > 7)
                {
                    int ps_extension_id = ld.ReadBits(2);

                    num_bits_left -= 2;
                    num_bits_left -= PsExtension(ld, ps_extension_id, num_bits_left);
                }

                ld.SkipBits(num_bits_left);
            }

            int bits2 = (int)(ld.GetPosition() - bits);

            _psDataAvailable = 1;

            return bits2;
        }

        private int PsExtension(BitStream ld, int ps_extension_id, int num_bits_left)
        {
            int n;
            long bits = ld.GetPosition();

            if (ps_extension_id == 0)
            {
                _enableIpdopd = ld.ReadBool();

                if (_enableIpdopd)
                {
                    for (n = 0; n < _numEnv; n++)
                    {
                        _ipdDt[n] = ld.ReadBool();

                        /* ipd_data */
                        HuffData(ld, _ipdDt[n], _nrIpdopdPar, HuffmanTables.t_huff_ipd,
                            HuffmanTables.f_huff_ipd, _ipdIndex[n]);

                        _opdDt[n] = ld.ReadBool();

                        /* opd_data */
                        HuffData(ld, _opdDt[n], _nrIpdopdPar, HuffmanTables.t_huff_opd,
                            HuffmanTables.f_huff_opd, _opdIndex[n]);
                    }
                }
                ld.ReadBit(); //reserved
            }

            /* return number of bits read */
            int bits2 = (int)(ld.GetPosition() - bits);

            return bits2;
        }

        /* read huffman data coded in either the frequency or the time direction */
        private void HuffData(BitStream ld, bool dt, int nr_par, int[][] t_huff, int[][] f_huff, int[] par)
        {
            int n;

            if (dt)
            {
                /* coded in time direction */
                for (n = 0; n < nr_par; n++)
                {
                    par[n] = PsHuffDec(ld, t_huff);
                }
            }
            else
            {
                /* coded in frequency direction */
                par[0] = PsHuffDec(ld, f_huff);

                for (n = 1; n < nr_par; n++)
                {
                    par[n] = PsHuffDec(ld, f_huff);
                }
            }
        }

        /* binary search huffman decoding */
        private int PsHuffDec(BitStream ld, int[][] t_huff)
        {
            int bit;
            int index = 0;

            while (index >= 0)
            {
                bit = ld.ReadBit();
                index = t_huff[index][bit];
            }

            return index + 31;
        }

        /* limits the value i to the range [min,max] */
        private int DeltaClip(int i, int min, int max)
        {
            if (i < min) return min;
            else if (i > max) return max;
            else return i;
        }


        /* delta decode array */
        private void DeltaDecode(bool enable, int[] index, int[] index_prev, bool dt_flag, int nr_par, int stride, int min_index, int max_index)
        {
            int i;

            if (enable)
            {
                if (!dt_flag)
                {
                    /* delta coded in frequency direction */
                    index[0] = 0 + index[0];
                    index[0] = DeltaClip(index[0], min_index, max_index);

                    for (i = 1; i < nr_par; i++)
                    {
                        index[i] = index[i - 1] + index[i];
                        index[i] = DeltaClip(index[i], min_index, max_index);
                    }
                }
                else
                {
                    /* delta coded in time direction */
                    for (i = 0; i < nr_par; i++)
                    {
                        //int8_t tmp2;
                        //int8_t tmp = index[i];

                        //printf("%d %d\n", index_prev[i*stride], index[i]);
                        //printf("%d\n", index[i]);
                        index[i] = index_prev[i * stride] + index[i];
                        //tmp2 = index[i];
                        index[i] = DeltaClip(index[i], min_index, max_index);

                        //if (iid)
                        //{
                        //    if (index[i] == 7)
                        //    {
                        //        printf("%d %d %d\n", index_prev[i*stride], tmp, tmp2);
                        //    }
                        //}
                    }
                }
            }
            else
            {
                /* set indices to zero */
                for (i = 0; i < nr_par; i++)
                {
                    index[i] = 0;
                }
            }

            /* coarse */
            if (stride == 2)
            {
                for (i = (nr_par << 1) - 1; i > 0; i--)
                {
                    index[i] = index[i >> 1];
                }
            }
        }

        /* delta modulo decode array */
        /* in: log2 value of the modulo value to allow using AND instead of MOD */
        private void DeltaModuloDecode(bool enable, int[] index, int[] index_prev, bool dt_flag, int nr_par, int stride, int and_modulo)
        {
            int i;

            if (enable)
            {
                if (!dt_flag)
                {
                    /* delta coded in frequency direction */
                    index[0] = 0 + index[0];
                    index[0] &= and_modulo;

                    for (i = 1; i < nr_par; i++)
                    {
                        index[i] = index[i - 1] + index[i];
                        index[i] &= and_modulo;
                    }
                }
                else
                {
                    /* delta coded in time direction */
                    for (i = 0; i < nr_par; i++)
                    {
                        index[i] = index_prev[i * stride] + index[i];
                        index[i] &= and_modulo;
                    }
                }
            }
            else
            {
                /* set indices to zero */
                for (i = 0; i < nr_par; i++)
                {
                    index[i] = 0;
                }
            }

            /* coarse */
            if (stride == 2)
            {
                index[0] = 0;
                for (i = (nr_par << 1) - 1; i > 0; i--)
                {
                    index[i] = index[i >> 1];
                }
            }
        }

        private void Map20IndexTo34(int[] index, int bins)
        {
            //index[0] = index[0];
            index[1] = (index[0] + index[1]) / 2;
            index[2] = index[1];
            index[3] = index[2];
            index[4] = (index[2] + index[3]) / 2;
            index[5] = index[3];
            index[6] = index[4];
            index[7] = index[4];
            index[8] = index[5];
            index[9] = index[5];
            index[10] = index[6];
            index[11] = index[7];
            index[12] = index[8];
            index[13] = index[8];
            index[14] = index[9];
            index[15] = index[9];
            index[16] = index[10];

            if (bins == 34)
            {
                index[17] = index[11];
                index[18] = index[12];
                index[19] = index[13];
                index[20] = index[14];
                index[21] = index[14];
                index[22] = index[15];
                index[23] = index[15];
                index[24] = index[16];
                index[25] = index[16];
                index[26] = index[17];
                index[27] = index[17];
                index[28] = index[18];
                index[29] = index[18];
                index[30] = index[18];
                index[31] = index[18];
                index[32] = index[19];
                index[33] = index[19];
            }
        }

        /* parse the bitstream data decoded in ps_data() */
        private void PsDataDecode()
        {
            int env, bin;

            /* ps data not available, use data from previous frame */
            if (_psDataAvailable == 0)
            {
                _numEnv = 0;
            }

            for (env = 0; env < _numEnv; env++)
            {
                int[] iid_index_prev;
                int[] icc_index_prev;
                int[] ipd_index_prev;
                int[] opd_index_prev;

                int num_iid_steps = _iidMode < 3 ? 7 : 15 /*fine quant*/;

                if (env == 0)
                {
                    /* take last envelope from previous frame */
                    iid_index_prev = _iidIndexPrev;
                    icc_index_prev = _iccIndexPrev;
                    ipd_index_prev = _ipdIndexPrev;
                    opd_index_prev = _opdIndexPrev;
                }
                else
                {
                    /* take index values from previous envelope */
                    iid_index_prev = _iidIndex[env - 1];
                    icc_index_prev = _iccIndex[env - 1];
                    ipd_index_prev = _ipdIndex[env - 1];
                    opd_index_prev = _opdIndex[env - 1];
                }

                //        iid = 1;
                /* delta decode iid parameters */
                DeltaDecode(_enableIid, _iidIndex[env], iid_index_prev,
                    _iidDt[env], _nrIidPar,
                    _iidMode == 0 || _iidMode == 3 ? 2 : 1,
                    -num_iid_steps, num_iid_steps);
                //        iid = 0;

                /* delta decode icc parameters */
                DeltaDecode(_enableIcc, _iccIndex[env], icc_index_prev,
                    _iccDt[env], _nrIccPar,
                    _iccMode == 0 || _iccMode == 3 ? 2 : 1,
                    0, 7);

                /* delta modulo decode ipd parameters */
                DeltaModuloDecode(_enableIpdopd, _ipdIndex[env], ipd_index_prev,
                    _ipdDt[env], _nrIpdopdPar, 1, 7);

                /* delta modulo decode opd parameters */
                DeltaModuloDecode(_enableIpdopd, _opdIndex[env], opd_index_prev,
                    _opdDt[env], _nrIpdopdPar, 1, 7);
            }

            /* handle error case */
            if (_numEnv == 0)
            {
                /* force to 1 */
                _numEnv = 1;

                if (_enableIid)
                {
                    for (bin = 0; bin < 34; bin++)
                    {
                        _iidIndex[0][bin] = _iidIndexPrev[bin];
                    }
                }
                else
                {
                    for (bin = 0; bin < 34; bin++)
                    {
                        _iidIndex[0][bin] = 0;
                    }
                }

                if (_enableIcc)
                {
                    for (bin = 0; bin < 34; bin++)
                    {
                        _iccIndex[0][bin] = _iccIndexPrev[bin];
                    }
                }
                else
                {
                    for (bin = 0; bin < 34; bin++)
                    {
                        _iccIndex[0][bin] = 0;
                    }
                }

                if (_enableIpdopd)
                {
                    for (bin = 0; bin < 17; bin++)
                    {
                        _ipdIndex[0][bin] = _ipdIndexPrev[bin];
                        _opdIndex[0][bin] = _opdIndexPrev[bin];
                    }
                }
                else
                {
                    for (bin = 0; bin < 17; bin++)
                    {
                        _ipdIndex[0][bin] = 0;
                        _opdIndex[0][bin] = 0;
                    }
                }
            }

            /* update previous indices */
            for (bin = 0; bin < 34; bin++)
            {
                _iidIndexPrev[bin] = _iidIndex[_numEnv - 1][bin];
            }
            for (bin = 0; bin < 34; bin++)
            {
                _iccIndexPrev[bin] = _iccIndex[_numEnv - 1][bin];
            }
            for (bin = 0; bin < 17; bin++)
            {
                _ipdIndexPrev[bin] = _ipdIndex[_numEnv - 1][bin];
                _opdIndexPrev[bin] = _opdIndex[_numEnv - 1][bin];
            }

            _psDataAvailable = 0;

            if (_frameClass == 0)
            {
                _borderPosition[0] = 0;
                for (env = 1; env < _numEnv; env++)
                {
                    _borderPosition[env] = env * _numTimeSlotsRate / _numEnv;
                }
                _borderPosition[_numEnv] = _numTimeSlotsRate;
            }
            else
            {
                _borderPosition[0] = 0;

                if (_borderPosition[_numEnv] < _numTimeSlotsRate)
                {
                    for (bin = 0; bin < 34; bin++)
                    {
                        _iidIndex[_numEnv][bin] = _iidIndex[_numEnv - 1][bin];
                        _iccIndex[_numEnv][bin] = _iccIndex[_numEnv - 1][bin];
                    }
                    for (bin = 0; bin < 17; bin++)
                    {
                        _ipdIndex[_numEnv][bin] = _ipdIndex[_numEnv - 1][bin];
                        _opdIndex[_numEnv][bin] = _opdIndex[_numEnv - 1][bin];
                    }
                    _numEnv++;
                    _borderPosition[_numEnv] = _numTimeSlotsRate;
                }

                for (env = 1; env < _numEnv; env++)
                {
                    int thr = _numTimeSlotsRate - (_numEnv - env);

                    if (_borderPosition[env] > thr)
                    {
                        _borderPosition[env] = thr;
                    }
                    else
                    {
                        thr = _borderPosition[env - 1] + 1;
                        if (_borderPosition[env] < thr)
                        {
                            _borderPosition[env] = thr;
                        }
                    }
                }
            }

            /* make sure that the indices of all parameters can be mapped
			 * to the same hybrid synthesis filterbank
			 */
            if (_use34hybridBands)
            {
                for (env = 0; env < _numEnv; env++)
                {
                    if (_iidMode != 2 && _iidMode != 5)
                        Map20IndexTo34(_iidIndex[env], 34);
                    if (_iccMode != 2 && _iccMode != 5)
                        Map20IndexTo34(_iccIndex[env], 34);
                    if (_ipdMode != 2 && _ipdMode != 5)
                    {
                        Map20IndexTo34(_ipdIndex[env], 17);
                        Map20IndexTo34(_opdIndex[env], 17);
                    }
                }
            }
        }

        /* decorrelate the mono signal using an allpass filter */
        private void PsDecorrelate(float[,,] X_left, float[,,] X_right, float[,,] X_hybrid_left, float[,,] X_hybrid_right)
        {
            int gr, n, m, bk;
            int temp_delay = 0;
            int sb, maxsb;
            int[] temp_delay_ser = new int[PSConstants.NO_ALLPASS_LINKS];
            float P_SmoothPeakDecayDiffNrg, nrg;
            float[,] P = new float[32, 34];
            float[,] G_TransientRatio = new float[32, 34];
            float[] inputLeft = new float[2];


            /* chose hybrid filterbank: 20 or 34 band case */
            float[][] Phi_Fract_SubQmf;
            if (_use34hybridBands)
            {
                Phi_Fract_SubQmf = PSTables.Phi_Fract_SubQmf34;
            }
            else
            {
                Phi_Fract_SubQmf = PSTables.Phi_Fract_SubQmf20;
            }

            /* clear the energy values */
            for (n = 0; n < 32; n++)
            {
                for (bk = 0; bk < 34; bk++)
                {
                    P[n, bk] = 0;
                }
            }

            /* calculate the energy in each parameter band b(k) */
            for (gr = 0; gr < _numGroups; gr++)
            {
                /* select the parameter index b(k) to which this group belongs */
                bk = ~PSConstants.NEGATE_IPD_MASK & _mapGroup2bk[gr];

                /* select the upper subband border for this group */
                maxsb = gr < _numHybridGroups ? _groupBorder[gr] + 1 : _groupBorder[gr + 1];

                for (sb = _groupBorder[gr]; sb < maxsb; sb++)
                {
                    for (n = _borderPosition[0]; n < _borderPosition[_numEnv]; n++)
                    {

                        /* input from hybrid subbands or QMF subbands */
                        if (gr < _numHybridGroups)
                        {
                            inputLeft[0] = X_hybrid_left[n, sb, 0];
                            inputLeft[1] = X_hybrid_left[n, sb, 1];
                        }
                        else
                        {
                            inputLeft[0] = X_left[n, sb, 0];
                            inputLeft[1] = X_left[n, sb, 1];
                        }

                        /* accumulate energy */
                        P[n, bk] += inputLeft[0] * inputLeft[0] + inputLeft[1] * inputLeft[1];
                    }
                }
            }

            /* calculate transient reduction ratio for each parameter band b(k) */
            for (bk = 0; bk < _nrParBands; bk++)
            {
                for (n = _borderPosition[0]; n < _borderPosition[_numEnv]; n++)
                {
                    float gamma = 1.5f;

                    _P_PeakDecayNrg[bk] = _P_PeakDecayNrg[bk] * _alphaDecay;
                    if (_P_PeakDecayNrg[bk] < P[n, bk])
                        _P_PeakDecayNrg[bk] = P[n, bk];

                    /* apply smoothing filter to peak decay energy */
                    P_SmoothPeakDecayDiffNrg = _P_SmoothPeakDecayDiffNrg_prev[bk];
                    P_SmoothPeakDecayDiffNrg += (_P_PeakDecayNrg[bk] - P[n, bk] - _P_SmoothPeakDecayDiffNrg_prev[bk]) * _alphaSmooth;
                    _P_SmoothPeakDecayDiffNrg_prev[bk] = P_SmoothPeakDecayDiffNrg;

                    /* apply smoothing filter to energy */
                    nrg = _P_prev[bk];
                    nrg += (P[n, bk] - _P_prev[bk]) * _alphaSmooth;
                    _P_prev[bk] = nrg;

                    /* calculate transient ratio */
                    if (P_SmoothPeakDecayDiffNrg * gamma <= nrg)
                    {
                        G_TransientRatio[n, bk] = 1.0f;
                    }
                    else
                    {
                        G_TransientRatio[n, bk] = nrg / (P_SmoothPeakDecayDiffNrg * gamma);
                    }
                }
            }

            /* apply stereo decorrelation filter to the signal */
            for (gr = 0; gr < _numGroups; gr++)
            {
                if (gr < _numHybridGroups)
                    maxsb = _groupBorder[gr] + 1;
                else
                    maxsb = _groupBorder[gr + 1];

                /* QMF channel */
                for (sb = _groupBorder[gr]; sb < maxsb; sb++)
                {
                    float g_DecaySlope;
                    float[] g_DecaySlope_filt = new float[PSConstants.NO_ALLPASS_LINKS];

                    /* g_DecaySlope: [0..1] */
                    if (gr < _numHybridGroups || sb <= _decayCutoff)
                    {
                        g_DecaySlope = 1.0f;
                    }
                    else
                    {
                        int decay = _decayCutoff - sb;
                        if (decay <= -20 /* -1/DECAY_SLOPE */)
                        {
                            g_DecaySlope = 0;
                        }
                        else
                        {
                            /* decay(int)*decay_slope(frac) = g_DecaySlope(frac) */
                            g_DecaySlope = 1.0f + PSConstants.DECAY_SLOPE * decay;
                        }
                    }

                    /* calculate g_DecaySlope_filt for every m multiplied by filter_a[m] */
                    for (m = 0; m < PSConstants.NO_ALLPASS_LINKS; m++)
                    {
                        g_DecaySlope_filt[m] = g_DecaySlope * PSTables.filter_a[m];
                    }


                    /* set delay indices */
                    temp_delay = _savedDelay;
                    for (n = 0; n < PSConstants.NO_ALLPASS_LINKS; n++)
                    {
                        temp_delay_ser[n] = _delayBufIndexSer[n];
                    }

                    for (n = _borderPosition[0]; n < _borderPosition[_numEnv]; n++)
                    {
                        float[] tmp = new float[2], tmp0 = new float[2], R0 = new float[2];

                        if (gr < _numHybridGroups)
                        {
                            /* hybrid filterbank input */
                            inputLeft[0] = X_hybrid_left[n, sb, 0];
                            inputLeft[1] = X_hybrid_left[n, sb, 1];
                        }
                        else
                        {
                            /* QMF filterbank input */
                            inputLeft[0] = X_left[n, sb, 0];
                            inputLeft[1] = X_left[n, sb, 1];
                        }

                        if (sb > _nrAllpassBands && gr >= _numHybridGroups)
                        {
                            /* delay */

                            /* never hybrid subbands here, always QMF subbands */
                            tmp[0] = _delayQmf[_delayBufIndexDelay[sb], sb, 0];
                            tmp[1] = _delayQmf[_delayBufIndexDelay[sb], sb, 1];
                            R0[0] = tmp[0];
                            R0[1] = tmp[1];
                            _delayQmf[_delayBufIndexDelay[sb], sb, 0] = inputLeft[0];
                            _delayQmf[_delayBufIndexDelay[sb], sb, 1] = inputLeft[1];
                        }
                        else
                        {
                            /* allpass filter */
                            //int m;
                            float[] Phi_Fract = new float[2];

                            /* fetch parameters */
                            if (gr < _numHybridGroups)
                            {
                                /* select data from the hybrid subbands */
                                tmp0[0] = _delaySubQmf[temp_delay, sb, 0];
                                tmp0[1] = _delaySubQmf[temp_delay, sb, 1];

                                _delaySubQmf[temp_delay, sb, 0] = inputLeft[0];
                                _delaySubQmf[temp_delay, sb, 1] = inputLeft[1];

                                Phi_Fract[0] = Phi_Fract_SubQmf[sb][0];
                                Phi_Fract[1] = Phi_Fract_SubQmf[sb][1];
                            }
                            else
                            {
                                /* select data from the QMF subbands */
                                tmp0[0] = _delayQmf[temp_delay, sb, 0];
                                tmp0[1] = _delayQmf[temp_delay, sb, 1];

                                _delayQmf[temp_delay, sb, 0] = inputLeft[0];
                                _delayQmf[temp_delay, sb, 1] = inputLeft[1];

                                Phi_Fract[0] = PSTables.Phi_Fract_Qmf[sb][0];
                                Phi_Fract[1] = PSTables.Phi_Fract_Qmf[sb][1];
                            }

                            /* z^(-2) * Phi_Fract[k] */
                            tmp[0] = tmp[0] * Phi_Fract[0] + tmp0[1] * Phi_Fract[1];
                            tmp[1] = tmp0[1] * Phi_Fract[0] - tmp0[0] * Phi_Fract[1];

                            R0[0] = tmp[0];
                            R0[1] = tmp[1];
                            for (m = 0; m < PSConstants.NO_ALLPASS_LINKS; m++)
                            {
                                float[] Q_Fract_allpass = new float[2], tmp2 = new float[2];

                                /* fetch parameters */
                                if (gr < _numHybridGroups)
                                {
                                    /* select data from the hybrid subbands */
                                    tmp0[0] = _delaySubQmfSer[m, temp_delay_ser[m], sb, 0];
                                    tmp0[1] = _delaySubQmfSer[m, temp_delay_ser[m], sb, 1];

                                    if (_use34hybridBands)
                                    {
                                        Q_Fract_allpass[0] = PSTables.Q_Fract_allpass_SubQmf34[sb][m][0];
                                        Q_Fract_allpass[1] = PSTables.Q_Fract_allpass_SubQmf34[sb][m][1];
                                    }
                                    else
                                    {
                                        Q_Fract_allpass[0] = PSTables.Q_Fract_allpass_SubQmf20[sb][m][0];
                                        Q_Fract_allpass[1] = PSTables.Q_Fract_allpass_SubQmf20[sb][m][1];
                                    }
                                }
                                else
                                {
                                    /* select data from the QMF subbands */
                                    tmp0[0] = _delayQmfSer[m, temp_delay_ser[m], sb, 0];
                                    tmp0[1] = _delayQmfSer[m, temp_delay_ser[m], sb, 1];

                                    Q_Fract_allpass[0] = PSTables.Q_Fract_allpass_Qmf[sb][m][0];
                                    Q_Fract_allpass[1] = PSTables.Q_Fract_allpass_Qmf[sb][m][1];
                                }

                                /* delay by a fraction */
                                /* z^(-d(m)) * Q_Fract_allpass[k,m] */
                                tmp[0] = tmp0[0] * Q_Fract_allpass[0] + tmp0[1] * Q_Fract_allpass[1];
                                tmp[1] = tmp0[1] * Q_Fract_allpass[0] - tmp0[0] * Q_Fract_allpass[1];

                                /* -a(m) * g_DecaySlope[k] */
                                tmp[0] += -(g_DecaySlope_filt[m] * R0[0]);
                                tmp[1] += -(g_DecaySlope_filt[m] * R0[1]);

                                /* -a(m) * g_DecaySlope[k] * Q_Fract_allpass[k,m] * z^(-d(m)) */
                                tmp2[0] = R0[0] + g_DecaySlope_filt[m] * tmp[0];
                                tmp2[1] = R0[1] + g_DecaySlope_filt[m] * tmp[1];

                                /* store sample */
                                if (gr < _numHybridGroups)
                                {
                                    _delaySubQmfSer[m, temp_delay_ser[m], sb, 0] = tmp2[0];
                                    _delaySubQmfSer[m, temp_delay_ser[m], sb, 1] = tmp2[1];
                                }
                                else
                                {
                                    _delayQmfSer[m, temp_delay_ser[m], sb, 0] = tmp2[0];
                                    _delayQmfSer[m, temp_delay_ser[m], sb, 1] = tmp2[1];
                                }

                                /* store for next iteration (or as output value if last iteration) */
                                R0[0] = tmp[0];
                                R0[1] = tmp[1];
                            }
                        }

                        /* select b(k) for reading the transient ratio */
                        bk = ~PSConstants.NEGATE_IPD_MASK & _mapGroup2bk[gr];

                        /* duck if a past transient is found */
                        R0[0] = G_TransientRatio[n, bk] * R0[0];
                        R0[1] = G_TransientRatio[n, bk] * R0[1];

                        if (gr < _numHybridGroups)
                        {
                            /* hybrid */
                            X_hybrid_right[n, sb, 0] = R0[0];
                            X_hybrid_right[n, sb, 1] = R0[1];
                        }
                        else
                        {
                            /* QMF */
                            X_right[n, sb, 0] = R0[0];
                            X_right[n, sb, 1] = R0[1];
                        }

                        /* Update delay buffer index */
                        if (++temp_delay >= 2)
                        {
                            temp_delay = 0;
                        }

                        /* update delay indices */
                        if (sb > _nrAllpassBands && gr >= _numHybridGroups)
                        {
                            /* delay_D depends on the samplerate, it can hold the values 14 and 1 */
                            if (++_delayBufIndexDelay[sb] >= _delayD[sb])
                            {
                                _delayBufIndexDelay[sb] = 0;
                            }
                        }

                        for (m = 0; m < PSConstants.NO_ALLPASS_LINKS; m++)
                        {
                            if (++temp_delay_ser[m] >= _numSampleDelaySer[m])
                            {
                                temp_delay_ser[m] = 0;
                            }
                        }
                    }
                }
            }

            /* update delay indices */
            _savedDelay = temp_delay;
            for (m = 0; m < PSConstants.NO_ALLPASS_LINKS; m++)
            {
                _delayBufIndexSer[m] = temp_delay_ser[m];
            }
        }

        private float MagnitudeC(float[] c)
        {
            return (float)Math.Sqrt(c[0] * c[0] + c[1] * c[1]);
        }

        private void PsMixPhase(float[,,] X_left, float[,,] X_right, float[,,] X_hybrid_left, float[,,] X_hybrid_right)
        {
            int n;
            int gr;
            int bk = 0;
            int sb, maxsb;
            int env;
            int nr_ipdopd_par;
            float[] h11 = new float[2], h12 = new float[2], h21 = new float[2], h22 = new float[2];
            float[] H11 = new float[2], H12 = new float[2], H21 = new float[2], H22 = new float[2];
            float[] deltaH11 = new float[2], deltaH12 = new float[2], deltaH21 = new float[2], deltaH22 = new float[2];
            float[] tempLeft = new float[2];
            float[] tempRight = new float[2];
            float[] phaseLeft = new float[2];
            float[] phaseRight = new float[2];
            float L;
            float[] sf_iid;
            int no_iid_steps;

            if (_iidMode >= 3)
            {
                no_iid_steps = 15;
                sf_iid = PSTables.sf_iid_fine;
            }
            else
            {
                no_iid_steps = 7;
                sf_iid = PSTables.sf_iid_normal;
            }

            if (_ipdMode == 0 || _ipdMode == 3)
            {
                nr_ipdopd_par = 11; /* resolution */

            }
            else
            {
                nr_ipdopd_par = _nrIpdopdPar;
            }

            for (gr = 0; gr < _numGroups; gr++)
            {
                bk = ~PSConstants.NEGATE_IPD_MASK & _mapGroup2bk[gr];

                /* use one channel per group in the subqmf domain */
                maxsb = gr < _numHybridGroups ? _groupBorder[gr] + 1 : _groupBorder[gr + 1];

                for (env = 0; env < _numEnv; env++)
                {
                    if (_iccMode < 3)
                    {
                        /* type 'A' mixing as described in 8.6.4.6.2.1 */
                        float c_1, c_2;
                        float cosa, sina;
                        float cosb, sinb;
                        float ab1, ab2;
                        float ab3, ab4;

                        /*
						 c_1 = sqrt(2.0 / (1.0 + pow(10.0, quant_iid[no_iid_steps + iid_index] / 10.0)));
						 c_2 = sqrt(2.0 / (1.0 + pow(10.0, quant_iid[no_iid_steps - iid_index] / 10.0)));
						 alpha = 0.5 * acos(quant_rho[icc_index]);
						 beta = alpha * ( c_1 - c_2 ) / sqrt(2.0);
						 */
                        //printf("%d\n", ps.iid_index[env][bk]);

                        /* calculate the scalefactors c_1 and c_2 from the intensity differences */
                        c_1 = sf_iid[no_iid_steps + _iidIndex[env][bk]];
                        c_2 = sf_iid[no_iid_steps - _iidIndex[env][bk]];

                        /* calculate alpha and beta using the ICC parameters */
                        cosa = PSTables.cos_alphas[_iccIndex[env][bk]];
                        sina = PSTables.sin_alphas[_iccIndex[env][bk]];

                        if (_iidMode >= 3)
                        {
                            if (_iidIndex[env][bk] < 0)
                            {
                                cosb = PSTables.cos_betas_fine[-_iidIndex[env][bk]][_iccIndex[env][bk]];
                                sinb = -PSTables.sin_betas_fine[-_iidIndex[env][bk]][_iccIndex[env][bk]];
                            }
                            else
                            {
                                cosb = PSTables.cos_betas_fine[_iidIndex[env][bk]][_iccIndex[env][bk]];
                                sinb = PSTables.sin_betas_fine[_iidIndex[env][bk]][_iccIndex[env][bk]];
                            }
                        }
                        else
                        {
                            if (_iidIndex[env][bk] < 0)
                            {
                                cosb = PSTables.cos_betas_normal[-_iidIndex[env][bk]][_iccIndex[env][bk]];
                                sinb = -PSTables.sin_betas_normal[-_iidIndex[env][bk]][_iccIndex[env][bk]];
                            }
                            else
                            {
                                cosb = PSTables.cos_betas_normal[_iidIndex[env][bk]][_iccIndex[env][bk]];
                                sinb = PSTables.sin_betas_normal[_iidIndex[env][bk]][_iccIndex[env][bk]];
                            }
                        }

                        ab1 = cosb * cosa;
                        ab2 = sinb * sina;
                        ab3 = sinb * cosa;
                        ab4 = cosb * sina;

                        /* h_xy: COEF */
                        h11[0] = c_2 * (ab1 - ab2);
                        h12[0] = c_1 * (ab1 + ab2);
                        h21[0] = c_2 * (ab3 + ab4);
                        h22[0] = c_1 * (ab3 - ab4);
                    }
                    else
                    {
                        /* type 'B' mixing as described in 8.6.4.6.2.2 */
                        float sina, cosa;
                        float cosg, sing;

                        if (_iidMode >= 3)
                        {
                            int abs_iid = Math.Abs(_iidIndex[env][bk]);

                            cosa = PSTables.sincos_alphas_B_fine[no_iid_steps + _iidIndex[env][bk]][_iccIndex[env][bk]];
                            sina = PSTables.sincos_alphas_B_fine[30 - (no_iid_steps + _iidIndex[env][bk])][_iccIndex[env][bk]];
                            cosg = PSTables.cos_gammas_fine[abs_iid][_iccIndex[env][bk]];
                            sing = PSTables.sin_gammas_fine[abs_iid][_iccIndex[env][bk]];
                        }
                        else
                        {
                            int abs_iid = Math.Abs(_iidIndex[env][bk]);

                            cosa = PSTables.sincos_alphas_B_normal[no_iid_steps + _iidIndex[env][bk]][_iccIndex[env][bk]];
                            sina = PSTables.sincos_alphas_B_normal[14 - (no_iid_steps + _iidIndex[env][bk])][_iccIndex[env][bk]];
                            cosg = PSTables.cos_gammas_normal[abs_iid][_iccIndex[env][bk]];
                            sing = PSTables.sin_gammas_normal[abs_iid][_iccIndex[env][bk]];
                        }

                        h11[0] = PSConstants.COEF_SQRT2 * (cosa * cosg);
                        h12[0] = PSConstants.COEF_SQRT2 * (sina * cosg);
                        h21[0] = PSConstants.COEF_SQRT2 * (-cosa * sing);
                        h22[0] = PSConstants.COEF_SQRT2 * (sina * sing);
                    }

                    /* calculate phase rotation parameters H_xy */
                    /* note that the imaginary part of these parameters are only calculated when
					 IPD and OPD are enabled
					 */
                    if (_enableIpdopd && bk < nr_ipdopd_par)
                    {
                        float xy, pq, xypq;

                        /* ringbuffer index */
                        int i = _phaseHist;

                        /* previous value */
                        tempLeft[0] = _ipdPrev[bk, i, 0] * 0.25f;
                        tempLeft[1] = _ipdPrev[bk, i, 1] * 0.25f;
                        tempRight[0] = _opdPrev[bk, i, 0] * 0.25f;
                        tempRight[1] = _opdPrev[bk, i, 1] * 0.25f;

                        /* save current value */
                        _ipdPrev[bk, i, 0] = PSTables.ipdopd_cos_tab[Math.Abs(_ipdIndex[env][bk])];
                        _ipdPrev[bk, i, 1] = PSTables.ipdopd_sin_tab[Math.Abs(_ipdIndex[env][bk])];
                        _opdPrev[bk, i, 0] = PSTables.ipdopd_cos_tab[Math.Abs(_opdIndex[env][bk])];
                        _opdPrev[bk, i, 1] = PSTables.ipdopd_sin_tab[Math.Abs(_opdIndex[env][bk])];

                        /* add current value */
                        tempLeft[0] += _ipdPrev[bk, i, 0];
                        tempLeft[1] += _ipdPrev[bk, i, 1];
                        tempRight[0] += _opdPrev[bk, i, 0];
                        tempRight[1] += _opdPrev[bk, i, 1];

                        /* ringbuffer index */
                        if (i == 0)
                        {
                            i = 2;
                        }
                        i--;

                        /* get value before previous */
                        tempLeft[0] += _ipdPrev[bk, i, 0] * 0.5f;
                        tempLeft[1] += _ipdPrev[bk, i, 1] * 0.5f;
                        tempRight[0] += _opdPrev[bk, i, 0] * 0.5f;
                        tempRight[1] += _opdPrev[bk, i, 1] * 0.5f;

                        xy = MagnitudeC(tempRight);
                        pq = MagnitudeC(tempLeft);

                        if (xy != 0)
                        {
                            phaseLeft[0] = tempRight[0] / xy;
                            phaseLeft[1] = tempRight[1] / xy;
                        }
                        else
                        {
                            phaseLeft[0] = 0;
                            phaseLeft[1] = 0;
                        }

                        xypq = xy * pq;

                        if (xypq != 0)
                        {
                            float tmp1 = tempRight[0] * tempLeft[0] + tempRight[1] * tempLeft[1];
                            float tmp2 = tempRight[1] * tempLeft[0] - tempRight[0] * tempLeft[1];

                            phaseRight[0] = tmp1 / xypq;
                            phaseRight[1] = tmp2 / xypq;
                        }
                        else
                        {
                            phaseRight[0] = 0;
                            phaseRight[1] = 0;
                        }

                        /* MUL_F(COEF, REAL) = COEF */
                        h11[1] = h11[0] * phaseLeft[1];
                        h12[1] = h12[0] * phaseRight[1];
                        h21[1] = h21[0] * phaseLeft[1];
                        h22[1] = h22[0] * phaseRight[1];

                        h11[0] = h11[0] * phaseLeft[0];
                        h12[0] = h12[0] * phaseRight[0];
                        h21[0] = h21[0] * phaseLeft[0];
                        h22[0] = h22[0] * phaseRight[0];
                    }

                    /* length of the envelope n_e+1 - n_e (in time samples) */
                    /* 0 < L <= 32: integer */
                    L = _borderPosition[env + 1] - _borderPosition[env];

                    /* obtain final H_xy by means of linear interpolation */
                    deltaH11[0] = (h11[0] - _h11Prev[gr, 0]) / L;
                    deltaH12[0] = (h12[0] - _h12Prev[gr, 0]) / L;
                    deltaH21[0] = (h21[0] - _h21Prev[gr, 0]) / L;
                    deltaH22[0] = (h22[0] - _h22Prev[gr, 0]) / L;

                    H11[0] = _h11Prev[gr, 0];
                    H12[0] = _h12Prev[gr, 0];
                    H21[0] = _h21Prev[gr, 0];
                    H22[0] = _h22Prev[gr, 0];

                    _h11Prev[gr, 0] = h11[0];
                    _h12Prev[gr, 0] = h12[0];
                    _h21Prev[gr, 0] = h21[0];
                    _h22Prev[gr, 0] = h22[0];

                    /* only calculate imaginary part when needed */
                    if (_enableIpdopd && bk < nr_ipdopd_par)
                    {
                        /* obtain final H_xy by means of linear interpolation */
                        deltaH11[1] = (h11[1] - _h11Prev[gr, 1]) / L;
                        deltaH12[1] = (h12[1] - _h12Prev[gr, 1]) / L;
                        deltaH21[1] = (h21[1] - _h21Prev[gr, 1]) / L;
                        deltaH22[1] = (h22[1] - _h22Prev[gr, 1]) / L;

                        H11[1] = _h11Prev[gr, 1];
                        H12[1] = _h12Prev[gr, 1];
                        H21[1] = _h21Prev[gr, 1];
                        H22[1] = _h22Prev[gr, 1];

                        if ((PSConstants.NEGATE_IPD_MASK & _mapGroup2bk[gr]) != 0)
                        {
                            deltaH11[1] = -deltaH11[1];
                            deltaH12[1] = -deltaH12[1];
                            deltaH21[1] = -deltaH21[1];
                            deltaH22[1] = -deltaH22[1];

                            H11[1] = -H11[1];
                            H12[1] = -H12[1];
                            H21[1] = -H21[1];
                            H22[1] = -H22[1];
                        }

                        _h11Prev[gr, 1] = h11[1];
                        _h12Prev[gr, 1] = h12[1];
                        _h21Prev[gr, 1] = h21[1];
                        _h22Prev[gr, 1] = h22[1];
                    }

                    /* apply H_xy to the current envelope band of the decorrelated subband */
                    for (n = _borderPosition[env]; n < _borderPosition[env + 1]; n++)
                    {
                        /* addition finalises the interpolation over every n */
                        H11[0] += deltaH11[0];
                        H12[0] += deltaH12[0];
                        H21[0] += deltaH21[0];
                        H22[0] += deltaH22[0];
                        if (_enableIpdopd && bk < nr_ipdopd_par)
                        {
                            H11[1] += deltaH11[1];
                            H12[1] += deltaH12[1];
                            H21[1] += deltaH21[1];
                            H22[1] += deltaH22[1];
                        }

                        /* channel is an alias to the subband */
                        for (sb = _groupBorder[gr]; sb < maxsb; sb++)
                        {
                            float[] inLeft = new float[2], inRight = new float[2];

                            /* load decorrelated samples */
                            if (gr < _numHybridGroups)
                            {
                                inLeft[0] = X_hybrid_left[n, sb, 0];
                                inLeft[1] = X_hybrid_left[n, sb, 1];
                                inRight[0] = X_hybrid_right[n, sb, 0];
                                inRight[1] = X_hybrid_right[n, sb, 1];
                            }
                            else
                            {
                                inLeft[0] = X_left[n, sb, 0];
                                inLeft[1] = X_left[n, sb, 1];
                                inRight[0] = X_right[n, sb, 0];
                                inRight[1] = X_right[n, sb, 1];
                            }

                            /* apply mixing */
                            tempLeft[0] = H11[0] * inLeft[0] + H21[0] * inRight[0];
                            tempLeft[1] = H11[0] * inLeft[1] + H21[0] * inRight[1];
                            tempRight[0] = H12[0] * inLeft[0] + H22[0] * inRight[0];
                            tempRight[1] = H12[0] * inLeft[1] + H22[0] * inRight[1];

                            /* only perform imaginary operations when needed */
                            if (_enableIpdopd && bk < nr_ipdopd_par)
                            {
                                /* apply rotation */
                                tempLeft[0] -= H11[1] * inLeft[1] + H21[1] * inRight[1];
                                tempLeft[1] += H11[1] * inLeft[0] + H21[1] * inRight[0];
                                tempRight[0] -= H12[1] * inLeft[1] + H22[1] * inRight[1];
                                tempRight[1] += H12[1] * inLeft[0] + H22[1] * inRight[0];
                            }

                            /* store final samples */
                            if (gr < _numHybridGroups)
                            {
                                X_hybrid_left[n, sb, 0] = tempLeft[0];
                                X_hybrid_left[n, sb, 1] = tempLeft[1];
                                X_hybrid_right[n, sb, 0] = tempRight[0];
                                X_hybrid_right[n, sb, 1] = tempRight[1];
                            }
                            else
                            {
                                X_left[n, sb, 0] = tempLeft[0];
                                X_left[n, sb, 1] = tempLeft[1];
                                X_right[n, sb, 0] = tempRight[0];
                                X_right[n, sb, 1] = tempRight[1];
                            }
                        }
                    }

                    /* shift phase smoother's circular buffer index */
                    _phaseHist++;
                    if (_phaseHist == 2)
                    {
                        _phaseHist = 0;
                    }
                }
            }
        }

        /* main Parametric Stereo decoding function */
        public int Process(float[,,] X_left, float[,,] X_right)
        {
            float[,,] X_hybrid_left = new float[32, 32, 2];
            float[,,] X_hybrid_right = new float[32, 32, 2];

            /* delta decoding of the bitstream data */
            PsDataDecode();

            /* set up some parameters depending on filterbank type */
            if (_use34hybridBands)
            {
                _groupBorder = PSTables.group_border34;
                _mapGroup2bk = PSTables.map_group2bk34;
                _numGroups = 32 + 18;
                _numHybridGroups = 32;
                _nrParBands = 34;
                _decayCutoff = 5;
            }
            else
            {
                _groupBorder = PSTables.group_border20;
                _mapGroup2bk = PSTables.map_group2bk20;
                _numGroups = 10 + 12;
                _numHybridGroups = 10;
                _nrParBands = 20;
                _decayCutoff = 3;
            }

            /* Perform further analysis on the lowest subbands to get a higher
			 * frequency resolution
			 */
            _hyb.HybridAnalysis(X_left, X_hybrid_left,
                _use34hybridBands, _numTimeSlotsRate);

            /* decorrelate mono signal */
            PsDecorrelate(X_left, X_right, X_hybrid_left, X_hybrid_right);

            /* apply mixing and phase parameters */
            PsMixPhase(X_left, X_right, X_hybrid_left, X_hybrid_right);

            /* hybrid synthesis, to rebuild the SBR QMF matrices */
            _hyb.HybridSynthesis(X_left, X_hybrid_left,
                _use34hybridBands, _numTimeSlotsRate);

            _hyb.HybridSynthesis(X_right, X_hybrid_right,
                _use34hybridBands, _numTimeSlotsRate);

            return 0;
        }
    }
}
