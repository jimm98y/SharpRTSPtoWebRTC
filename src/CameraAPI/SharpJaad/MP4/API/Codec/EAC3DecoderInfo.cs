using SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec;
using System;
using System.Collections.Generic;

namespace SharpJaad.MP4.API.Codec
{
    public class EAC3DecoderInfo : DecoderInfo
    {
        private EAC3SpecificBox box;
        private IndependentSubstream[] _independentSubstream;

	    public EAC3DecoderInfo(CodecSpecificBox box)
        {
            this.box = (EAC3SpecificBox)box;

            _independentSubstream = new IndependentSubstream[this.box.GetIndependentSubstreamCount()];
            for (int i = 0; i < _independentSubstream.Length; i++)
            {
                _independentSubstream[i] = new IndependentSubstream(i, this.box);
            }
        }

        public int GetDataRate()
        {
            return box.GetDataRate();
        }

        public IndependentSubstream[] GetIndependentSubstreams()
        {
            return _independentSubstream;
        }

        public class IndependentSubstream
        {
            private EAC3SpecificBox box;
            private readonly int index;
            private readonly DependentSubstream[] dependentSubstreams;

		    public IndependentSubstream(int index, EAC3SpecificBox box)
            {
                this.box = box;
                this.index = index;

                int loc = box.GetDependentSubstreamLocation()[index];
                List<DependentSubstream> list = new List<DependentSubstream>();
                for (int i = 0; i < 9; i++)
                {
                    if (((loc >> (8 - i)) & 1) == 1) list.Add((DependentSubstream)Enum.GetValues(typeof(DependentSubstream)).GetValue(i));
                }
                dependentSubstreams = list.ToArray();
            }

            public int GetFscod()
            {
                return box.GetFscods()[index];
            }

            public int GetBsid()
            {
                return box.GetBsids()[index];
            }

            public int GetBsmod()
            {
                return box.GetBsmods()[index];
            }

            public int GetAcmod()
            {
                return box.GetAcmods()[index];
            }

            public bool IsLfeon()
            {
                return box.GetLfeons()[index];
            }

            public DependentSubstream[] GetDependentSubstreams()
            {
                return dependentSubstreams;
            }
        }

        public enum DependentSubstream
        {
            LC_RC_PAIR,
            LRS_RRS_PAIR,
            CS,
            TS,
            LSD_RSD_PAIR,
            LW_RW_PAIR,
            LVH_RVH_PAIR,
            CVH,
            LFE2
        }
    }
}
