using CameraAPI.AAC.Syntax;
using System;

namespace CameraAPI.AAC.Tools
{
    public class ICPrediction
    {
        private const float SF_SCALE = 1.0f/-1024.0f;
		private const float INV_SF_SCALE = 1.0f/SF_SCALE;
		private const int MAX_PREDICTORS = 672;
		private const float A = 0.953125f; //61.0 / 64
		private const float ALPHA = 0.90625f;  //29.0 / 32
		private bool _predictorReset;
		private int _predictorResetGroup;
		private bool[] _predictionUsed;
		private PredictorState[] _states;

		private sealed class PredictorState 
		{
			public float _cor0 = 0.0f;
            public float _cor1 = 0.0f;
            public float _var0 = 0.0f;
            public float _var1 = 0.0f;
            public float _r0 = 1.0f;
            public float _r1 = 1.0f;
		}

		public ICPrediction() 
		{
			_states = new PredictorState[MAX_PREDICTORS];
			ResetAllPredictors();
		}

		public void Decode(BitStream input, int maxSFB, SampleFrequency sf) 
		{
			int predictorCount = sf.GetPredictorCount();

			if(_predictorReset = input.ReadBool()) _predictorResetGroup = input.ReadBits(5);

			int maxPredSFB = sf.GetMaximalPredictionSFB();
			int length = Math.Min(maxSFB, maxPredSFB);
			_predictionUsed = new bool[length];
			for(int sfb = 0; sfb<length; sfb++) 
			{
				_predictionUsed[sfb] = input.ReadBool();
			}
			//Constants.LOGGER.log(Level.WARNING, "ICPrediction: maxSFB={0}, maxPredSFB={1}", new int[]{maxSFB, maxPredSFB});
			/*//if maxSFB<maxPredSFB set remaining to false
			for(int sfb = length; sfb<maxPredSFB; sfb++) {
			predictionUsed[sfb] = false;
			}*/
		}

		public void SetPredictionUnused(int sfb) 
		{
			_predictionUsed[sfb] = false;
		}

		public void Process(ICStream ics, float[] data, SampleFrequency sf)
		{
			ICSInfo info = ics.GetInfo();

			if(info.IsEightShortFrame()) ResetAllPredictors();
			else 
			{
				int len = Math.Min(sf.GetMaximalPredictionSFB(), info.GetMaxSFB());
				int[] swbOffsets = info.GetSWBOffsets();
				int k;
				for(int sfb = 0; sfb<len; sfb++) 
				{
					for(k = swbOffsets[sfb]; k<swbOffsets[sfb+1]; k++) 
					{
						Predict(data, k, _predictionUsed[sfb]);
					}
				}
				if(_predictorReset) ResetPredictorGroup(_predictorResetGroup);
			}
		}

		private void ResetPredictState(int index) 
		{
			if(_states[index]==null) _states[index] = new PredictorState();
			_states[index]._r0 = 0;
			_states[index]._r1 = 0;
			_states[index]._cor0 = 0;
			_states[index]._cor1 = 0;
			_states[index]._var0 = 0x3F80;
			_states[index]._var1 = 0x3F80;
		}

		private void ResetAllPredictors()
		{
			int i;
			for(i = 0; i<_states.Length; i++)
			{
				ResetPredictState(i);
			}
		}

		private void ResetPredictorGroup(int group) 
		{
			int i;
			for(i = group-1; i<_states.Length; i += 30)
			{
				ResetPredictState(i);
			}
		}

		private void Predict(float[] data, int off, bool output) 
		{
			if(_states[off]==null) _states[off] = new PredictorState();
			PredictorState state = _states[off];
			float r0 = state._r0, r1 = state._r1;
			float cor0 = state._cor0, cor1 = state._cor1;
			float var0 = state._var0, var1 = state._var1;

			float k1 = var0>1 ? cor0*Even(A/var0) : 0;
			float k2 = var1>1 ? cor1*Even(A/var1) : 0;

			float pv = Round(k1*r0+k2*r1);
			if(output) data[off] += pv*SF_SCALE;

			float e0 = (data[off]*INV_SF_SCALE);
			float e1 = e0-k1*r0;

			state._cor1 = Trunc(ALPHA*cor1+r1*e1);
			state._var1 = Trunc(ALPHA*var1+0.5f*(r1*r1+e1*e1));
			state._cor0 = Trunc(ALPHA*cor0+r0*e0);
			state._var0 = Trunc(ALPHA*var0+0.5f*(r0*r0+e0*e0));

			state._r1 = Trunc(A*(r0-k1*e0));
			state._r0 = Trunc(A*e0);
		}

#warning Review this
		private float Round(float pf) 
		{
			return IntBitsToFloat((int)((FloatToIntBits(pf) + 0x00008000) & 0xFFFF0000));
		}

		private float Even(float pf)
		{
			int i = FloatToIntBits(pf);
			i = (int)((i + 0x00007FFF + (i & 0x00010000 >> 16)) & 0xFFFF0000);
			return IntBitsToFloat(i);
		}

		private float Trunc(float pf) 
		{
			return IntBitsToFloat((int)(FloatToIntBits(pf) & 0xFFFF0000));
		}

		private static int FloatToIntBits(float f)
		{
			return BitConverter.ToInt32(BitConverter.GetBytes(f), 0);
        }

        private static float IntBitsToFloat(int i)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(i), 0);
        }
    }
}
