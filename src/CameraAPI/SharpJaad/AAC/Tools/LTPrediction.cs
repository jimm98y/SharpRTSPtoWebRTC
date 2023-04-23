using SharpJaad.AAC.Filterbank;
using SharpJaad.AAC.Syntax;
using System;
using System.Linq;

namespace SharpJaad.AAC.Tools
{
    public class LTPrediction
    {
        private static readonly float[] CODEBOOK =
        {
            0.570829f,
            0.696616f,
            0.813004f,
            0.911304f,
            0.984900f,
            1.067894f,
            1.194601f,
            1.369533f
        };

        private bool _isPresent = false;

        private int _frameLength;
        private int[] _states;
        private int _coef, _lag, _lastBand;
        private bool _lagUpdate;
        private bool[] _shortUsed, _shortLagPresent, _longUsed;
        private int[] _shortLag;

        public LTPrediction(int frameLength)
        {
            _frameLength = frameLength;
            _states = new int[4 * frameLength];
        }

        public bool IsPresent()
        {
            return _isPresent;
        }

        public void Decode(BitStream input, ICSInfo info, Profile profile)
        {
            _lag = 0;

            _isPresent = input.ReadBool();
            if (!_isPresent)
            {
                return;
            }

            if (profile.Equals(Profile.AAC_LD))
            {
                _lagUpdate = input.ReadBool();
                if (_lagUpdate) _lag = input.ReadBits(10);
            }
            else _lag = input.ReadBits(11);
            if (_lag > _frameLength << 1) throw new AACException("LTP lag too large: " + _lag);
            _coef = input.ReadBits(3);

            int windowCount = info.GetWindowCount();

            if (info.IsEightShortFrame())
            {
                _shortUsed = new bool[windowCount];
                _shortLagPresent = new bool[windowCount];
                _shortLag = new int[windowCount];
                for (int w = 0; w < windowCount; w++)
                {
                    if (_shortUsed[w] = input.ReadBool())
                    {
                        _shortLagPresent[w] = input.ReadBool();
                        if (_shortLagPresent[w]) _shortLag[w] = input.ReadBits(4);
                    }
                }
            }
            else
            {
                _lastBand = Math.Min(info.GetMaxSFB(), Constants.MAX_LTP_SFB);
                _longUsed = new bool[_lastBand];

                for (int i = 0; i < _lastBand; i++)
                {
                    _longUsed[i] = input.ReadBool();
                }
            }
        }

        public void SetPredictionUnused(int sfb)
        {
            if (_longUsed != null) _longUsed[sfb] = false;
        }

        public void Process(ICStream ics, float[] data, FilterBank filterBank, SampleFrequency sf)
        {
            if (!_isPresent)
                return;

            ICSInfo info = ics.GetInfo();

            if (!info.IsEightShortFrame())
            {
                int samples = _frameLength << 1;
                float[] input = new float[2048];
                float[] output = new float[2048];

                for (int i = 0; i < samples; i++)
                {
                    input[i] = _states[samples + i - _lag] * CODEBOOK[_coef];
                }

                filterBank.ProcessLTP(info.GetWindowSequence(), info.GetWindowShape(ICSInfo.CURRENT),
                        info.GetWindowShape(ICSInfo.PREVIOUS), input, output);

                if (ics.IsTNSDataPresent()) ics.GetTNS().Process(ics, output, sf, true);

                int[] swbOffsets = info.GetSWBOffsets();
                int swbOffsetMax = info.GetSWBOffsetMax();
                int low, high, bin;
                for (int sfb = 0; sfb < _lastBand; sfb++)
                {
                    if (_longUsed[sfb])
                    {
                        low = swbOffsets[sfb];
                        high = Math.Min(swbOffsets[sfb + 1], swbOffsetMax);

                        for (bin = low; bin < high; bin++)
                        {
                            data[bin] += output[bin];
                        }
                    }
                }
            }
        }

        public void UpdateState(float[] time, float[] overlap, Profile profile)
        {
            int i;
            if (profile.Equals(Profile.AAC_LD))
            {
                for (i = 0; i < _frameLength; i++)
                {
                    _states[i] = _states[i + _frameLength];
                    _states[_frameLength + i] = _states[i + _frameLength * 2];
                    _states[_frameLength * 2 + i] = (int)Math.Round(time[i]);
                    _states[_frameLength * 3 + i] = (int)Math.Round(overlap[i]);
                }
            }
            else
            {
                for (i = 0; i < _frameLength; i++)
                {
                    _states[i] = _states[i + _frameLength];
                    _states[_frameLength + i] = (int)Math.Round(time[i]);
                    _states[_frameLength * 2 + i] = (int)Math.Round(overlap[i]);
                }
            }
            _isPresent = false;
        }

        public static bool IsLTPProfile(Profile profile)
        {
            return profile.Equals(Profile.AAC_LTP) || profile.Equals(Profile.ER_AAC_LTP) || profile.Equals(Profile.AAC_LD);
        }

        public void Copy(LTPrediction ltp)
        {
            Array.Copy(ltp._states, 0, _states, 0, _states.Length);
            _coef = ltp._coef;
            _lag = ltp._lag;
            _lastBand = ltp._lastBand;
            _lagUpdate = ltp._lagUpdate;
            _shortUsed = ltp._shortUsed.ToArray();
            _shortLagPresent = ltp._shortLagPresent.ToArray();
            _shortLag = ltp._shortLag.ToArray();
            _longUsed = ltp._longUsed.ToArray();
        }
    }
}
