using SharpJaad.AAC.Ps;
using SharpJaad.AAC.Syntax;
using System;

namespace SharpJaad.AAC.Sbr
{
    public class SBR
    {
        private bool _downSampledSBR;
        public SampleFrequency _sampleRate;
        public int _maxAACLine;
        public int _rate;
        public bool _justSeeked;
        public int _ret;

        public bool[] _ampRes = new bool[2];

        public int _k0;
        public int _kx;
        public int _M;
        public int _N_master;
        public int _N_high;
        public int _N_low;
        public int _N_Q;
        public int[] _N_L = new int[4];
        public int[] _n = new int[2];

        public int[] _f_master = new int[64];
        public int[,] _f_table_res = new int[2, 64];
        public int[] _f_table_noise = new int[64];
        public int[,] _f_table_lim = new int[4, 64];

        public int[] _table_map_k_to_g = new int[64];

        public int[] _abs_bord_lead = new int[2];
        public int[] _abs_bord_trail = new int[2];
        public int[] _n_rel_lead = new int[2];
        public int[] _n_rel_trail = new int[2];

        public int[] _L_E = new int[2];
        public int[] _L_E_prev = new int[2];
        public int[] _L_Q = new int[2];

        public int[,] _t_E = new int[2, Constants.MAX_L_E + 1];
        public int[,] _t_Q = new int[2, 3];
        public int[,] _f = new int[2, Constants.MAX_L_E + 1];
        public int[] _f_prev = new int[2];

        public float[,,] _G_temp_prev = new float[2, 5, 64];
        public float[,,] _Q_temp_prev = new float[2, 5, 64];
        public int[] _GQ_ringbuf_index = new int[2];

        public int[,,] _E = new int[2, 64, Constants.MAX_L_E];
        public int[,] _E_prev = new int[2, 64];
        public float[,,] _E_orig = new float[2, 64, Constants.MAX_L_E];
        public float[,,] _E_curr = new float[2, 64, Constants.MAX_L_E];
        public int[,,] _Q = new int[2, 64, 2];
        public float[,,] _Q_div = new float[2, 64, 2];
        public float[,,] _Q_div2 = new float[2, 64, 2];
        public int[,] _Q_prev = new int[2, 64];

        public int[] _l_A = new int[2];
        public int[] _l_A_prev = new int[2];

        public int[,] _bs_invf_mode = new int[2, Constants.MAX_L_E];
        public int[,] _bs_invf_mode_prev = new int[2, Constants.MAX_L_E];
        public float[,] _bwArray = new float[2, 64];
        public float[,] _bwArray_prev = new float[2, 64];

        public int _noPatches;
        public int[] _patchNoSubbands = new int[64];
        public int[] _patchStartSubband = new int[64];

        public int[,] _bs_add_harmonic = new int[2, 64];
        public int[,] _bs_add_harmonic_prev = new int[2, 64];

        public int[] _index_noise_prev = new int[2];
        public int[] _psi_is_prev = new int[2];

        public int _bs_start_freq_prev;
        public int _bs_stop_freq_prev;
        public int _bs_xover_band_prev;
        public int _bs_freq_scale_prev;
        public bool _bs_alter_scale_prev;
        public int _bs_noise_bands_prev;

        public int[] _prevEnvIsShort = new int[2];

        public int _kx_prev;
        public int _bsco;
        public int _bsco_prev;
        public int _M_prev;

        public bool _Reset;
        public int _frame;
        public int _header_count;

        public bool _stereo;
        public AnalysisFilterbank[] _qmfa = new AnalysisFilterbank[2];
        public SynthesisFilterbank[] _qmfs = new SynthesisFilterbank[2];

        public float[,,,] _Xsbr = new float[2, Constants.MAX_NTSRHFG, 64, 2];

        public int _numTimeSlotsRate;
        public int _numTimeSlots;
        public int _tHFGen;
        public int _tHFAdj;

        public PS _ps;
        public bool _ps_used;
        public bool _psResetFlag;

        /* to get it compiling */
        /* we'll see during the coding of all the tools, whether
         these are all used or not.
         */
        public bool _bs_header_flag;
        public int _bs_crc_flag;
        public int _bs_sbr_crc_bits;
        public int _bs_protocol_version;
        public bool _bs_amp_res;
        public int _bs_start_freq;
        public int _bs_stop_freq;
        public int _bs_xover_band;
        public int _bs_freq_scale;
        public bool _bs_alter_scale;
        public int _bs_noise_bands;
        public int _bs_limiter_bands;
        public int _bs_limiter_gains;
        public bool _bs_interpol_freq;
        public bool _bs_smoothing_mode;
        public int _bs_samplerate_mode;
        public bool[] _bs_add_harmonic_flag = new bool[2];
        public bool[] _bs_add_harmonic_flag_prev = new bool[2];
        public bool _bs_extended_data;
        public int _bs_extension_id;
        public int _bs_extension_data;
        public bool _bs_coupling;
        public int[] _bs_frame_class = new int[2];
        public int[,] _bs_rel_bord = new int[2, 9];
        public int[,] _bs_rel_bord_0 = new int[2, 9];
        public int[,] _bs_rel_bord_1 = new int[2, 9];
        public int[] _bs_pointer = new int[2];
        public int[] _bs_abs_bord_0 = new int[2];
        public int[] _bs_abs_bord_1 = new int[2];
        public int[] _bs_num_rel_0 = new int[2];
        public int[] _bs_num_rel_1 = new int[2];
        public int[,] _bs_df_env = new int[2, 9];
        public int[,] _bs_df_noise = new int[2, 3];

        public SBR(bool smallFrames, bool stereo, SampleFrequency sample_rate, bool downSampledSBR)
        {
            _downSampledSBR = downSampledSBR;
            _stereo = stereo;
            _sampleRate = sample_rate;

            _bs_freq_scale = 2;
            _bs_alter_scale = true;
            _bs_noise_bands = 2;
            _bs_limiter_bands = 2;
            _bs_limiter_gains = 2;
            _bs_interpol_freq = true;
            _bs_smoothing_mode = true;
            _bs_start_freq = 5;
            _bs_amp_res = true;
            _bs_samplerate_mode = 1;
            _prevEnvIsShort[0] = -1;
            _prevEnvIsShort[1] = -1;
            _header_count = 0;
            _Reset = true;

            _tHFGen = Constants.T_HFGEN;
            _tHFAdj = Constants.T_HFADJ;

            _bsco = 0;
            _bsco_prev = 0;
            _M_prev = 0;

            /* force sbr reset */
            _bs_start_freq_prev = -1;

            if (smallFrames)
            {
                _numTimeSlotsRate = Constants.RATE * Constants.NO_TIME_SLOTS_960;
                _numTimeSlots = Constants.NO_TIME_SLOTS_960;
            }
            else
            {
                _numTimeSlotsRate = Constants.RATE * Constants.NO_TIME_SLOTS;
                _numTimeSlots = Constants.NO_TIME_SLOTS;
            }

            _GQ_ringbuf_index[0] = 0;
            _GQ_ringbuf_index[1] = 0;

            if (stereo)
            {
                /* stereo */
                _qmfa[0] = new AnalysisFilterbank(32);
                _qmfa[1] = new AnalysisFilterbank(32);
                _qmfs[0] = new SynthesisFilterbank(downSampledSBR ? 32 : 64);
                _qmfs[1] = new SynthesisFilterbank(downSampledSBR ? 32 : 64);
            }
            else
            {
                /* mono */
                _qmfa[0] = new AnalysisFilterbank(32);
                _qmfs[0] = new SynthesisFilterbank(downSampledSBR ? 32 : 64);
                _qmfs[1] = null;
            }
        }

        private void SbrReset()
        {
            int j = 5;
            if (_qmfa[0] != null) _qmfa[0].Reset();
            if (_qmfa[1] != null) _qmfa[1].Reset();
            if (_qmfs[0] != null) _qmfs[0].Reset();
            if (_qmfs[1] != null) _qmfs[1].Reset();

#warning Review this
            Array.Clear(_G_temp_prev, 0, _G_temp_prev.Length);
            Array.Clear(_Q_temp_prev, 0, _Q_temp_prev.Length);

            for (int i = 0; i < 40; i++)
            {
                for (int k = 0; k < 64; k++)
                {
                    _Xsbr[0, i, j, 0] = 0;
                    _Xsbr[0, i, j, 1] = 0;
                    _Xsbr[1, i, j, 0] = 0;
                    _Xsbr[1, i, j, 1] = 0;
                }
            }

            _GQ_ringbuf_index[0] = 0;
            _GQ_ringbuf_index[1] = 0;
            _header_count = 0;
            _Reset = true;

            _L_E_prev[0] = 0;
            _L_E_prev[1] = 0;
            _bs_freq_scale = 2;
            _bs_alter_scale = true;
            _bs_noise_bands = 2;
            _bs_limiter_bands = 2;
            _bs_limiter_gains = 2;
            _bs_interpol_freq = true;
            _bs_smoothing_mode = true;
            _bs_start_freq = 5;
            _bs_amp_res = true;
            _bs_samplerate_mode = 1;
            _prevEnvIsShort[0] = -1;
            _prevEnvIsShort[1] = -1;
            _bsco = 0;
            _bsco_prev = 0;
            _M_prev = 0;
            _bs_start_freq_prev = -1;

            _f_prev[0] = 0;
            _f_prev[1] = 0;
            for (j = 0; j < Constants.MAX_M; j++)
            {
                _E_prev[0, j] = 0;
                _Q_prev[0, j] = 0;
                _E_prev[1, j] = 0;
                _Q_prev[1, j] = 0;
                _bs_add_harmonic_prev[0, j] = 0;
                _bs_add_harmonic_prev[1, j] = 0;
            }
            _bs_add_harmonic_flag_prev[0] = false;
            _bs_add_harmonic_flag_prev[1] = false;
        }

        private void SbrResetInternal()
        {

            /* if these are different from the previous frame: Reset = 1 */
            if (_bs_start_freq != _bs_start_freq_prev
                || _bs_stop_freq != _bs_stop_freq_prev
                || _bs_freq_scale != _bs_freq_scale_prev
                || _bs_alter_scale != _bs_alter_scale_prev
                || _bs_xover_band != _bs_xover_band_prev
                || _bs_noise_bands != _bs_noise_bands_prev)
            {
                _Reset = true;
            }
            else
            {
                _Reset = false;
            }

            _bs_start_freq_prev = _bs_start_freq;
            _bs_stop_freq_prev = _bs_stop_freq;
            _bs_freq_scale_prev = _bs_freq_scale;
            _bs_alter_scale_prev = _bs_alter_scale;
            _bs_xover_band_prev = _bs_xover_band;
            _bs_noise_bands_prev = _bs_noise_bands;
        }

        private int CalcSbrTables(int start_freq, int stop_freq,
            int samplerate_mode, int freq_scale,
            bool alter_scale, int xover_band)
        {
            int result = 0;
            int k2;

            /* calculate the Master Frequency Table */
            _k0 = FBT.QmfStartChannel(start_freq, samplerate_mode, _sampleRate);
            k2 = FBT.QmfStopChannel(stop_freq, _sampleRate, _k0);

            /* check k0 and k2 */
            if (_sampleRate.GetFrequency() >= 48000)
            {
                if (k2 - _k0 > 32)
                    result += 1;
            }
            else if (_sampleRate.GetFrequency() <= 32000)
            {
                if (k2 - _k0 > 48)
                    result += 1;
            }
            else
            { /* (sbr.sample_rate == 44100) */

                if (k2 - _k0 > 45)
                    result += 1;
            }

            if (freq_scale == 0)
            {
                result += FBT.MasterFrequencyTableFs0(this, _k0, k2, alter_scale);
            }
            else
            {
                result += FBT.MasterFrequencyTable(this, _k0, k2, freq_scale, alter_scale);
            }
            result += FBT.DerivedFrequencyTable(this, xover_band, k2);

            result = result > 0 ? 1 : 0;

            return result;
        }

        /* table 2 */
        public int Decode(BitStream ld, int bits, bool crc)
        {
            int result = 0;
            int num_align_bits = 0;
            long num_sbr_bits1 = ld.GetPosition();
            int num_sbr_bits2;

            int saved_start_freq, saved_samplerate_mode;
            int saved_stop_freq, saved_freq_scale;
            int saved_xover_band;
            bool saved_alter_scale;

            if (crc)
            {
                _bs_sbr_crc_bits = ld.ReadBits(10);
            }

            /* save old header values, in case the new ones are corrupted */
            saved_start_freq = _bs_start_freq;
            saved_samplerate_mode = _bs_samplerate_mode;
            saved_stop_freq = _bs_stop_freq;
            saved_freq_scale = _bs_freq_scale;
            saved_alter_scale = _bs_alter_scale;
            saved_xover_band = _bs_xover_band;

            _bs_header_flag = ld.ReadBool();

            if (_bs_header_flag)
                SbrHeader(ld);

            /* Reset? */
            SbrResetInternal();

            /* first frame should have a header */
            //if (!(sbr.frame == 0 && sbr.bs_header_flag == 0))
            if (_header_count != 0)
            {
                if (_Reset || _bs_header_flag && _justSeeked)
                {
                    int rt = CalcSbrTables(_bs_start_freq, _bs_stop_freq,
                        _bs_samplerate_mode, _bs_freq_scale,
                        _bs_alter_scale, _bs_xover_band);

                    /* if an error occured with the new header values revert to the old ones */
                    if (rt > 0)
                    {
                        CalcSbrTables(saved_start_freq, saved_stop_freq,
                            saved_samplerate_mode, saved_freq_scale,
                            saved_alter_scale, saved_xover_band);
                    }
                }

                if (result == 0)
                {
                    result = SbrData(ld);

                    /* sbr_data() returning an error means that there was an error in
                     envelope_time_border_vector().
                     In this case the old time border vector is saved and all the previous
                     data normally read after sbr_grid() is saved.
                     */
                    /* to be on the safe side, calculate old sbr tables in case of error */
                    if (result > 0
                        && (_Reset || _bs_header_flag && _justSeeked))
                    {
                        CalcSbrTables(saved_start_freq, saved_stop_freq,
                            saved_samplerate_mode, saved_freq_scale,
                            saved_alter_scale, saved_xover_band);
                    }

                    /* we should be able to safely set result to 0 now, */
                    /* but practise indicates this doesn't work well */
                }
            }
            else
            {
                result = 1;
            }

            num_sbr_bits2 = (int)(ld.GetPosition() - num_sbr_bits1);

            /* check if we read more bits then were available for sbr */
            if (bits < num_sbr_bits2)
            {
                throw new AACException("frame overread");
                //faad_resetbits(ld, num_sbr_bits1+8*cnt);
                //num_sbr_bits2 = 8*cnt;

                /* turn off PS for the unfortunate case that we randomly read some
                 * PS data that looks correct */
                //this.ps_used = 0;

                /* Make sure it doesn't decode SBR in this frame, or we'll get glitches */
                //return 1;
            }


            {
                /* -4 does not apply, bs_extension_type is re-read in this function */
                num_align_bits = bits /*- 4*/- num_sbr_bits2;
                ld.SkipBits(num_align_bits);
            }

            return result;
        }

        /* table 3 */
        private void SbrHeader(BitStream ld)
        {
            bool bs_header_extra_1, bs_header_extra_2;

            _header_count++;

            _bs_amp_res = ld.ReadBool();

            /* bs_start_freq and bs_stop_freq must define a fequency band that does
             not exceed 48 channels */
            _bs_start_freq = ld.ReadBits(4);
            _bs_stop_freq = ld.ReadBits(4);
            _bs_xover_band = ld.ReadBits(3);
            ld.ReadBits(2); //reserved
            bs_header_extra_1 = ld.ReadBool();
            bs_header_extra_2 = ld.ReadBool();

            if (bs_header_extra_1)
            {
                _bs_freq_scale = ld.ReadBits(2);
                _bs_alter_scale = ld.ReadBool();
                _bs_noise_bands = ld.ReadBits(2);
            }
            else
            {
                /* Default values */
                _bs_freq_scale = 2;
                _bs_alter_scale = true;
                _bs_noise_bands = 2;
            }

            if (bs_header_extra_2)
            {
                _bs_limiter_bands = ld.ReadBits(2);
                _bs_limiter_gains = ld.ReadBits(2);
                _bs_interpol_freq = ld.ReadBool();
                _bs_smoothing_mode = ld.ReadBool();
            }
            else
            {
                /* Default values */
                _bs_limiter_bands = 2;
                _bs_limiter_gains = 2;
                _bs_interpol_freq = true;
                _bs_smoothing_mode = true;
            }

        }

        /* table 4 */
        private int SbrData(BitStream ld)
        {
            int result;

            _rate = _bs_samplerate_mode != 0 ? 2 : 1;

            if (_stereo)
            {
                if ((result = SbrChannelPairElement(ld)) > 0)
                    return result;
            }
            else
            {
                if ((result = SbrSingleChannelElement(ld)) > 0)
                    return result;
            }

            return 0;
        }

        /* table 5 */
        private int SbrSingleChannelElement(BitStream ld)
        {
            int result;

            if (ld.ReadBool())
            {
                ld.ReadBits(4); //reserved
            }

            if ((result = SbrGrid(ld, 0)) > 0)
                return result;

            SbrDtdf(ld, 0);
            InvfMode(ld, 0);
            SbrEnvelope(ld, 0);
            SbrNoise(ld, 0);

            NoiseEnvelope.DequantChannel(this, 0);

            Array.Clear(_bs_add_harmonic, 0, _bs_add_harmonic.Length);

            _bs_add_harmonic_flag[0] = ld.ReadBool();
            if (_bs_add_harmonic_flag[0])
                SinusoidalCoding(ld, 0);

            _bs_extended_data = ld.ReadBool();

            if (_bs_extended_data)
            {
                int nr_bits_left;
                int ps_ext_read = 0;
                int cnt = ld.ReadBits(4);
                if (cnt == 15)
                {
                    cnt += ld.ReadBits(8);
                }

                nr_bits_left = 8 * cnt;
                while (nr_bits_left > 7)
                {
                    int tmp_nr_bits = 0;

                    _bs_extension_id = ld.ReadBits(2);
                    tmp_nr_bits += 2;

                    /* allow only 1 PS extension element per extension data */
                    if (_bs_extension_id == Constants.EXTENSION_ID_PS)
                    {
                        if (ps_ext_read == 0)
                        {
                            ps_ext_read = 1;
                        }
                        else
                        {
                            /* to be safe make it 3, will switch to "default"
                             * in sbr_extension() */
                            _bs_extension_id = 3;
                        }
                    }

                    tmp_nr_bits += SbrExtension(ld, _bs_extension_id, nr_bits_left);

                    /* check if the data read is bigger than the number of available bits */
                    if (tmp_nr_bits > nr_bits_left)
                        return 1;

                    nr_bits_left -= tmp_nr_bits;
                }

                /* Corrigendum */
                if (nr_bits_left > 0)
                {
                    ld.ReadBits(nr_bits_left);
                }
            }

            return 0;
        }

        /* table 6 */
        private int SbrChannelPairElement(BitStream ld)
        {
            int n, result;

            if (ld.ReadBool())
            {
                //reserved
                ld.ReadBits(4);
                ld.ReadBits(4);
            }

            _bs_coupling = ld.ReadBool();

            if (_bs_coupling)
            {
                if ((result = SbrGrid(ld, 0)) > 0)
                    return result;

                /* need to copy some data from left to right */
                _bs_frame_class[1] = _bs_frame_class[0];
                _L_E[1] = _L_E[0];
                _L_Q[1] = _L_Q[0];
                _bs_pointer[1] = _bs_pointer[0];

                for (n = 0; n <= _L_E[0]; n++)
                {
                    _t_E[1, n] = _t_E[0, n];
                    _f[1, n] = _f[0, n];
                }
                for (n = 0; n <= _L_Q[0]; n++)
                {
                    _t_Q[1, n] = _t_Q[0, n];
                }

                SbrDtdf(ld, 0);
                SbrDtdf(ld, 1);
                InvfMode(ld, 0);

                /* more copying */
                for (n = 0; n < _N_Q; n++)
                {
                    _bs_invf_mode[1, n] = _bs_invf_mode[0, n];
                }

                SbrEnvelope(ld, 0);
                SbrNoise(ld, 0);
                SbrEnvelope(ld, 1);
                SbrNoise(ld, 1);

#warning Review this
                Array.Clear(_bs_add_harmonic, 0, _bs_add_harmonic.Length);

                _bs_add_harmonic_flag[0] = ld.ReadBool();
                if (_bs_add_harmonic_flag[0])
                    SinusoidalCoding(ld, 0);

                _bs_add_harmonic_flag[1] = ld.ReadBool();
                if (_bs_add_harmonic_flag[1])
                    SinusoidalCoding(ld, 1);
            }
            else
            {
                int[] saved_t_E = new int[6], saved_t_Q = new int[3];
                int saved_L_E = _L_E[0];
                int saved_L_Q = _L_Q[0];
                int saved_frame_class = _bs_frame_class[0];

                for (n = 0; n < saved_L_E; n++)
                {
                    saved_t_E[n] = _t_E[0, n];
                }
                for (n = 0; n < saved_L_Q; n++)
                {
                    saved_t_Q[n] = _t_Q[0, n];
                }

                if ((result = SbrGrid(ld, 0)) > 0)
                    return result;
                if ((result = SbrGrid(ld, 1)) > 0)
                {
                    /* restore first channel data as well */
                    _bs_frame_class[0] = saved_frame_class;
                    _L_E[0] = saved_L_E;
                    _L_Q[0] = saved_L_Q;
                    for (n = 0; n < 6; n++)
                    {
                        _t_E[0, n] = saved_t_E[n];
                    }
                    for (n = 0; n < 3; n++)
                    {
                        _t_Q[0, n] = saved_t_Q[n];
                    }

                    return result;
                }
                SbrDtdf(ld, 0);
                SbrDtdf(ld, 1);
                InvfMode(ld, 0);
                InvfMode(ld, 1);
                SbrEnvelope(ld, 0);
                SbrEnvelope(ld, 1);
                SbrNoise(ld, 0);
                SbrNoise(ld, 1);

                Array.Clear(_bs_add_harmonic, 0, _bs_add_harmonic.Length);

                _bs_add_harmonic_flag[0] = ld.ReadBool();
                if (_bs_add_harmonic_flag[0])
                    SinusoidalCoding(ld, 0);

                _bs_add_harmonic_flag[1] = ld.ReadBool();
                if (_bs_add_harmonic_flag[1])
                    SinusoidalCoding(ld, 1);
            }
            NoiseEnvelope.DequantChannel(this, 0);
            NoiseEnvelope.DequantChannel(this, 1);

            if (_bs_coupling)
                NoiseEnvelope.Unmap(this);

            _bs_extended_data = ld.ReadBool();
            if (_bs_extended_data)
            {
                int nr_bits_left;
                int cnt = ld.ReadBits(4);
                if (cnt == 15)
                {
                    cnt += ld.ReadBits(8);
                }

                nr_bits_left = 8 * cnt;
                while (nr_bits_left > 7)
                {
                    int tmp_nr_bits = 0;

                    _bs_extension_id = ld.ReadBits(2);
                    tmp_nr_bits += 2;
                    tmp_nr_bits += SbrExtension(ld, _bs_extension_id, nr_bits_left);

                    /* check if the data read is bigger than the number of available bits */
                    if (tmp_nr_bits > nr_bits_left)
                        return 1;

                    nr_bits_left -= tmp_nr_bits;
                }

                /* Corrigendum */
                if (nr_bits_left > 0)
                {
                    ld.ReadBits(nr_bits_left);
                }
            }

            return 0;
        }

        /* integer log[2](x): input range [0,10) */
        private int SbrLog2(int val)
        {
            int[] log2tab = new int[] { 0, 0, 1, 2, 2, 3, 3, 3, 3, 4 };
            if (val < 10 && val >= 0)
                return log2tab[val];
            else
                return 0;
        }


        /* table 7 */
        private int SbrGrid(BitStream ld, int ch)
        {
            int i, env, rel, result;
            int bs_abs_bord, bs_abs_bord_1;
            int bs_num_env = 0;
            int saved_L_E = _L_E[ch];
            int saved_L_Q = _L_Q[ch];
            int saved_frame_class = _bs_frame_class[ch];

            _bs_frame_class[ch] = ld.ReadBits(2);

            switch (_bs_frame_class[ch])
            {
                case Constants.FIXFIX:
                    i = ld.ReadBits(2);

                    bs_num_env = Math.Min(1 << i, 5);

                    i = ld.ReadBit();
                    for (env = 0; env < bs_num_env; env++)
                    {
                        _f[ch, env] = i;
                    }

                    _abs_bord_lead[ch] = 0;
                    _abs_bord_trail[ch] = _numTimeSlots;
                    _n_rel_lead[ch] = bs_num_env - 1;
                    _n_rel_trail[ch] = 0;
                    break;

                case Constants.FIXVAR:
                    bs_abs_bord = ld.ReadBits(2) + _numTimeSlots;
                    bs_num_env = ld.ReadBits(2) + 1;

                    for (rel = 0; rel < bs_num_env - 1; rel++)
                    {
                        _bs_rel_bord[ch, rel] = 2 * ld.ReadBits(2) + 2;
                    }
                    i = SbrLog2(bs_num_env + 1);
                    _bs_pointer[ch] = ld.ReadBits(i);

                    for (env = 0; env < bs_num_env; env++)
                    {
                        _f[ch, bs_num_env - env - 1] = ld.ReadBit();
                    }

                    _abs_bord_lead[ch] = 0;
                    _abs_bord_trail[ch] = bs_abs_bord;
                    _n_rel_lead[ch] = 0;
                    _n_rel_trail[ch] = bs_num_env - 1;
                    break;

                case Constants.VARFIX:
                    bs_abs_bord = ld.ReadBits(2);
                    bs_num_env = ld.ReadBits(2) + 1;

                    for (rel = 0; rel < bs_num_env - 1; rel++)
                    {
                        _bs_rel_bord[ch, rel] = 2 * ld.ReadBits(2) + 2;
                    }
                    i = SbrLog2(bs_num_env + 1);
                    _bs_pointer[ch] = ld.ReadBits(i);

                    for (env = 0; env < bs_num_env; env++)
                    {
                        _f[ch, env] = ld.ReadBit();
                    }

                    _abs_bord_lead[ch] = bs_abs_bord;
                    _abs_bord_trail[ch] = _numTimeSlots;
                    _n_rel_lead[ch] = bs_num_env - 1;
                    _n_rel_trail[ch] = 0;
                    break;

                case Constants.VARVAR:
                    bs_abs_bord = ld.ReadBits(2);
                    bs_abs_bord_1 = ld.ReadBits(2) + _numTimeSlots;
                    _bs_num_rel_0[ch] = ld.ReadBits(2);
                    _bs_num_rel_1[ch] = ld.ReadBits(2);

                    bs_num_env = Math.Min(5, _bs_num_rel_0[ch] + _bs_num_rel_1[ch] + 1);

                    for (rel = 0; rel < _bs_num_rel_0[ch]; rel++)
                    {
                        _bs_rel_bord_0[ch, rel] = 2 * ld.ReadBits(2) + 2;
                    }
                    for (rel = 0; rel < _bs_num_rel_1[ch]; rel++)
                    {
                        _bs_rel_bord_1[ch, rel] = 2 * ld.ReadBits(2) + 2;
                    }
                    i = SbrLog2(_bs_num_rel_0[ch] + _bs_num_rel_1[ch] + 2);
                    _bs_pointer[ch] = ld.ReadBits(i);

                    for (env = 0; env < bs_num_env; env++)
                    {
                        _f[ch, env] = ld.ReadBit();
                    }

                    _abs_bord_lead[ch] = bs_abs_bord;
                    _abs_bord_trail[ch] = bs_abs_bord_1;
                    _n_rel_lead[ch] = _bs_num_rel_0[ch];
                    _n_rel_trail[ch] = _bs_num_rel_1[ch];
                    break;
            }

            if (_bs_frame_class[ch] == Constants.VARVAR)
                _L_E[ch] = Math.Min(bs_num_env, 5);
            else
                _L_E[ch] = Math.Min(bs_num_env, 4);

            if (_L_E[ch] <= 0)
                return 1;

            if (_L_E[ch] > 1)
                _L_Q[ch] = 2;
            else
                _L_Q[ch] = 1;

            /* TODO: this code can probably be integrated into the code above! */
            if ((result = TFGrid.EnvelopeTimeBorderVector(this, ch)) > 0)
            {
                _bs_frame_class[ch] = saved_frame_class;
                _L_E[ch] = saved_L_E;
                _L_Q[ch] = saved_L_Q;
                return result;
            }
            TFGrid.NoiseFloorTimeBorderVector(this, ch);

            return 0;
        }

        /* table 8 */
        private void SbrDtdf(BitStream ld, int ch)
        {
            int i;

            for (i = 0; i < _L_E[ch]; i++)
            {
                _bs_df_env[ch, i] = ld.ReadBit();
            }

            for (i = 0; i < _L_Q[ch]; i++)
            {
                _bs_df_noise[ch, i] = ld.ReadBit();
            }
        }

        /* table 9 */
        private void InvfMode(BitStream ld, int ch)
        {
            int n;

            for (n = 0; n < _N_Q; n++)
            {
                _bs_invf_mode[ch, n] = ld.ReadBits(2);
            }
        }

        private int SbrExtension(BitStream ld, int bs_extension_id, int num_bits_left)
        {
            int ret;

            switch (bs_extension_id)
            {
                case Constants.EXTENSION_ID_PS:
                    if (_ps == null)
                    {
                        _ps = new PS(_sampleRate, _numTimeSlotsRate);
                    }
                    if (_psResetFlag)
                    {
                        _ps._headerRead = false;
                    }
                    ret = _ps.Decode(ld);

                    /* enable PS if and only if: a header has been decoded */
                    if (!_ps_used && _ps._headerRead)
                    {
                        _ps_used = true;
                    }

                    if (_ps._headerRead)
                    {
                        _psResetFlag = false;
                    }

                    return ret;
                default:
                    _bs_extension_data = ld.ReadBits(6);
                    return 6;
            }
        }

        /* table 12 */
        private void SinusoidalCoding(BitStream ld, int ch)
        {
            int n;

            for (n = 0; n < _N_high; n++)
            {
                _bs_add_harmonic[ch, n] = ld.ReadBit();
            }
        }
        /* table 10 */

        private void SbrEnvelope(BitStream ld, int ch)
        {
            int env, band;
            int delta = 0;
            int[]
            []
            t_huff, f_huff;

            if (_L_E[ch] == 1 && _bs_frame_class[ch] == Constants.FIXFIX)
                _ampRes[ch] = false;
            else
                _ampRes[ch] = _bs_amp_res;

            if (_bs_coupling && ch == 1)
            {
                delta = 1;
                if (_ampRes[ch])
                {
                    t_huff = HuffmanTables.T_HUFFMAN_ENV_BAL_3_0DB;
                    f_huff = HuffmanTables.F_HUFFMAN_ENV_BAL_3_0DB;
                }
                else
                {
                    t_huff = HuffmanTables.T_HUFFMAN_ENV_BAL_1_5DB;
                    f_huff = HuffmanTables.F_HUFFMAN_ENV_BAL_1_5DB;
                }
            }
            else
            {
                delta = 0;
                if (_ampRes[ch])
                {
                    t_huff = HuffmanTables.T_HUFFMAN_ENV_3_0DB;
                    f_huff = HuffmanTables.F_HUFFMAN_ENV_3_0DB;
                }
                else
                {
                    t_huff = HuffmanTables.T_HUFFMAN_ENV_1_5DB;
                    f_huff = HuffmanTables.F_HUFFMAN_ENV_1_5DB;
                }
            }

            for (env = 0; env < _L_E[ch]; env++)
            {
                if (_bs_df_env[ch, env] == 0)
                {
                    if (_bs_coupling && ch == 1)
                    {
                        if (_ampRes[ch])
                        {
                            _E[ch, 0, env] = ld.ReadBits(5) << delta;
                        }
                        else
                        {
                            _E[ch, 0, env] = ld.ReadBits(6) << delta;
                        }
                    }
                    else
                    {
                        if (_ampRes[ch])
                        {
                            _E[ch, 0, env] = ld.ReadBits(6) << delta;
                        }
                        else
                        {
                            _E[ch, 0, env] = ld.ReadBits(7) << delta;
                        }
                    }

                    for (band = 1; band < _n[_f[ch, env]]; band++)
                    {
                        _E[ch, band, env] = DecodeHuffman(ld, f_huff) << delta;
                    }

                }
                else
                {
                    for (band = 0; band < _n[_f[ch, env]]; band++)
                    {
                        _E[ch, band, env] = DecodeHuffman(ld, t_huff) << delta;
                    }
                }
            }

            NoiseEnvelope.ExtractEnvelopeData(this, ch);
        }

        /* table 11 */
        private void SbrNoise(BitStream ld, int ch)
        {
            int noise, band;
            int delta = 0;
            int[]
            []
            t_huff, f_huff;

            if (_bs_coupling && ch == 1)
            {
                delta = 1;
                t_huff = HuffmanTables.T_HUFFMAN_NOISE_BAL_3_0DB;
                f_huff = HuffmanTables.F_HUFFMAN_ENV_BAL_3_0DB;
            }
            else
            {
                delta = 0;
                t_huff = HuffmanTables.T_HUFFMAN_NOISE_3_0DB;
                f_huff = HuffmanTables.F_HUFFMAN_ENV_3_0DB;
            }

            for (noise = 0; noise < _L_Q[ch]; noise++)
            {
                if (_bs_df_noise[ch, noise] == 0)
                {
                    if (_bs_coupling && ch == 1)
                    {
                        _Q[ch, 0, noise] = ld.ReadBits(5) << delta;
                    }
                    else
                    {
                        _Q[ch, 0, noise] = ld.ReadBits(5) << delta;
                    }
                    for (band = 1; band < _N_Q; band++)
                    {
                        _Q[ch, band, noise] = DecodeHuffman(ld, f_huff) << delta;
                    }
                }
                else
                {
                    for (band = 0; band < _N_Q; band++)
                    {
                        _Q[ch, band, noise] = DecodeHuffman(ld, t_huff) << delta;
                    }
                }
            }

            NoiseEnvelope.ExtractNoiseFloorData(this, ch);
        }

        private int DecodeHuffman(BitStream ld, int[][] t_huff)
        {
            int bit;
            int index = 0;

            while (index >= 0)
            {
                bit = ld.ReadBit();
                index = t_huff[index][bit];
            }

            return index + 64;
        }

        private int SbrSavePrevData(int ch)
        {
            int i;

            /* save data for next frame */
            _kx_prev = _kx;
            _M_prev = _M;
            _bsco_prev = _bsco;

            _L_E_prev[ch] = _L_E[ch];

            /* sbr.L_E[ch] can become 0 on files with bit errors */
            if (_L_E[ch] <= 0)
                return 19;

            _f_prev[ch] = _f[ch, _L_E[ch] - 1];
            for (i = 0; i < Constants.MAX_M; i++)
            {
                _E_prev[ch, i] = _E[ch, i, _L_E[ch] - 1];
                _Q_prev[ch, i] = _Q[ch, i, _L_Q[ch] - 1];
            }

            for (i = 0; i < Constants.MAX_M; i++)
            {
                _bs_add_harmonic_prev[ch, i] = _bs_add_harmonic[ch, i];
            }
            _bs_add_harmonic_flag_prev[ch] = _bs_add_harmonic_flag[ch];

            if (_l_A[ch] == _L_E[ch])
                _prevEnvIsShort[ch] = 0;
            else
                _prevEnvIsShort[ch] = -1;

            return 0;
        }

        private void SbrSaveMatrix(int ch)
        {
            int i;

            for (i = 0; i < _tHFGen; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    _Xsbr[ch, i, j, 0] = _Xsbr[ch, i + _numTimeSlotsRate, j, 0];
                    _Xsbr[ch, i, j, 1] = _Xsbr[ch, i + _numTimeSlotsRate, j, 1];
                }
            }
            for (i = _tHFGen; i < Constants.MAX_NTSRHFG; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    _Xsbr[ch, i, j, 0] = 0;
                    _Xsbr[ch, i, j, 1] = 0;
                }
            }
        }

        private int SbrProcessChannel(float[] channel_buf, float[,,] X,
            int ch, bool dont_process)
        {
            int k, l;
            int ret = 0;

            _bsco = 0;

            /* subband analysis */
            if (dont_process)
                _qmfa[ch].SbrQmfAnalysis32(this, channel_buf, _Xsbr, ch, _tHFGen, 32);
            else
                _qmfa[ch].SbrQmfAnalysis32(this, channel_buf, _Xsbr, ch, _tHFGen, _kx);

            if (!dont_process)
            {
                /* insert high frequencies here */
                /* hf generation using patching */
                HFGeneration.HfGeneration(this, _Xsbr, _Xsbr, ch);


                /* hf adjustment */
                ret = HFAdjustment.HfAdjustment(this, _Xsbr, ch);
                if (ret > 0)
                {
                    dont_process = true;
                }
            }

            if (_justSeeked || dont_process)
            {
                for (l = 0; l < _numTimeSlotsRate; l++)
                {
                    for (k = 0; k < 32; k++)
                    {
                        X[l, k, 0] = _Xsbr[ch, l + _tHFAdj, k, 0];
                        X[l, k, 1] = _Xsbr[ch, l + _tHFAdj, k, 1];
                    }
                    for (k = 32; k < 64; k++)
                    {
                        X[l, k, 0] = 0;
                        X[l, k, 1] = 0;
                    }
                }
            }
            else
            {
                for (l = 0; l < _numTimeSlotsRate; l++)
                {
                    int kx_band, M_band, bsco_band;

                    if (l < _t_E[ch, 0])
                    {
                        kx_band = _kx_prev;
                        M_band = _M_prev;
                        bsco_band = _bsco_prev;
                    }
                    else
                    {
                        kx_band = _kx;
                        M_band = _M;
                        bsco_band = _bsco;
                    }

                    for (k = 0; k < kx_band + bsco_band; k++)
                    {
                        X[l, k, 0] = _Xsbr[ch, l + _tHFAdj, k, 0];
                        X[l, k, 1] = _Xsbr[ch, l + _tHFAdj, k, 1];
                    }
                    for (k = kx_band + bsco_band; k < kx_band + M_band; k++)
                    {
                        X[l, k, 0] = _Xsbr[ch, l + _tHFAdj, k, 0];
                        X[l, k, 1] = _Xsbr[ch, l + _tHFAdj, k, 1];
                    }
                    for (k = Math.Max(kx_band + bsco_band, kx_band + M_band); k < 64; k++)
                    {
                        X[l, k, 0] = 0;
                        X[l, k, 1] = 0;
                    }
                }
            }
            return ret;
        }

        public int Process(float[] left_chan, float[] right_chan,
            bool just_seeked)
        {
            bool dont_process = false;
            int ret = 0;
            float[,,] X = new float[Constants.MAX_NTSR, 64, 2];

            /* case can occur due to bit errors */
            if (!_stereo) return 21;

            if (_ret != 0 || _header_count == 0)
            {
                /* don't process just upsample */
                dont_process = true;

                /* Re-activate reset for next frame */
                if (_ret != 0 && _Reset)
                    _bs_start_freq_prev = -1;
            }

            if (just_seeked)
            {
                _justSeeked = true;
            }
            else
            {
                _justSeeked = false;
            }

            _ret += SbrProcessChannel(left_chan, X, 0, dont_process);
            /* subband synthesis */
            if (_downSampledSBR)
            {
                _qmfs[0].SbrQmfSynthesis32(this, X, left_chan);
            }
            else
            {
                _qmfs[0].SbrQmfSynthesis64(this, X, left_chan);
            }

            _ret += SbrProcessChannel(right_chan, X, 1, dont_process);
            /* subband synthesis */
            if (_downSampledSBR)
            {
                _qmfs[1].SbrQmfSynthesis32(this, X, right_chan);
            }
            else
            {
                _qmfs[1].SbrQmfSynthesis64(this, X, right_chan);
            }

            if (_bs_header_flag)
                _justSeeked = false;

            if (_header_count != 0 && _ret == 0)
            {
                ret = SbrSavePrevData(0);
                if (ret != 0) return ret;
                ret = SbrSavePrevData(1);
                if (ret != 0) return ret;
            }

            SbrSaveMatrix(0);
            SbrSaveMatrix(1);
            _frame++;

            return 0;
        }

        public int Process(float[] channel,
            bool just_seeked)
        {
            bool dont_process = false;
            int ret = 0;
            float[,,] X = new float[Constants.MAX_NTSR, 64, 2];

            /* case can occur due to bit errors */
            if (_stereo) return 21;

            if (_ret != 0 || _header_count == 0)
            {
                /* don't process just upsample */
                dont_process = true;

                /* Re-activate reset for next frame */
                if (_ret != 0 && _Reset)
                    _bs_start_freq_prev = -1;
            }

            if (just_seeked)
            {
                _justSeeked = true;
            }
            else
            {
                _justSeeked = false;
            }

            _ret += SbrProcessChannel(channel, X, 0, dont_process);
            /* subband synthesis */
            if (_downSampledSBR)
            {
                _qmfs[0].SbrQmfSynthesis32(this, X, channel);
            }
            else
            {
                _qmfs[0].SbrQmfSynthesis64(this, X, channel);
            }

            if (_bs_header_flag)
                _justSeeked = false;

            if (_header_count != 0 && _ret == 0)
            {
                ret = SbrSavePrevData(0);
                if (ret != 0) return ret;
            }

            SbrSaveMatrix(0);

            _frame++;

            return 0;
        }

        public int ProcessPS(float[] left_channel, float[] right_channel,
            bool just_seeked)
        {
            int l, k;
            bool dont_process = false;
            int ret = 0;
            float[,,] X_left = new float[38, 64, 2];
            float[,,] X_right = new float[38, 64, 2];

            /* case can occur due to bit errors */
            if (_stereo) return 21;

            if (_ret != 0 || _header_count == 0)
            {
                /* don't process just upsample */
                dont_process = true;

                /* Re-activate reset for next frame */
                if (_ret != 0 && _Reset)
                    _bs_start_freq_prev = -1;
            }

            if (just_seeked)
            {
                _justSeeked = true;
            }
            else
            {
                _justSeeked = false;
            }

            if (_qmfs[1] == null)
            {
                _qmfs[1] = new SynthesisFilterbank(_downSampledSBR ? 32 : 64);
            }

            _ret += SbrProcessChannel(left_channel, X_left, 0, dont_process);

            /* copy some extra data for PS */
            for (l = _numTimeSlotsRate; l < _numTimeSlotsRate + 6; l++)
            {
                for (k = 0; k < 5; k++)
                {
                    X_left[l, k, 0] = _Xsbr[0, _tHFAdj + l, k, 0];
                    X_left[l, k, 1] = _Xsbr[0, _tHFAdj + l, k, 1];
                }
            }

            /* perform parametric stereo */
            _ps.Process(X_left, X_right);

            /* subband synthesis */
            if (_downSampledSBR)
            {
                _qmfs[0].SbrQmfSynthesis32(this, X_left, left_channel);
                _qmfs[1].SbrQmfSynthesis32(this, X_right, right_channel);
            }
            else
            {
                _qmfs[0].SbrQmfSynthesis64(this, X_left, left_channel);
                _qmfs[1].SbrQmfSynthesis64(this, X_right, right_channel);
            }

            if (_bs_header_flag)
                _justSeeked = false;

            if (_header_count != 0 && _ret == 0)
            {
                ret = SbrSavePrevData(0);
                if (ret != 0) return ret;
            }
            SbrSaveMatrix(0);

            _frame++;

            return 0;
        }

        public bool IsPSUsed()
        {
            return _ps_used;
        }
    }
}
