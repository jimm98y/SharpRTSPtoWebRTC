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
		private bool predictorReset;
		private int predictorResetGroup;
		private bool[] predictionUsed;
		private PredictorState[] states;

		private sealed class PredictorState {

			public float cor0 = 0.0f;
            public float cor1 = 0.0f;
            public float var0 = 0.0f;
            public float var1 = 0.0f;
            public float r0 = 1.0f;
            public float r1 = 1.0f;
		}

		public ICPrediction() {
			states = new PredictorState[MAX_PREDICTORS];
			resetAllPredictors();
		}

		public void decode(BitStream input, int maxSFB, SampleFrequency sf) {
			int predictorCount = sf.GetPredictorCount();

			if(predictorReset = input.readBool()) predictorResetGroup = input.readBits(5);

			int maxPredSFB = sf.GetMaximalPredictionSFB();
			int length = Math.Min(maxSFB, maxPredSFB);
			predictionUsed = new bool[length];
			for(int sfb = 0; sfb<length; sfb++) {
				predictionUsed[sfb] = input.readBool();
			}
			//Constants.LOGGER.log(Level.WARNING, "ICPrediction: maxSFB={0}, maxPredSFB={1}", new int[]{maxSFB, maxPredSFB});
			/*//if maxSFB<maxPredSFB set remaining to false
			for(int sfb = length; sfb<maxPredSFB; sfb++) {
			predictionUsed[sfb] = false;
			}*/
		}

		public void setPredictionUnused(int sfb) {
			predictionUsed[sfb] = false;
		}

		public void process(ICStream ics, float[] data, SampleFrequency sf) {
			ICSInfo info = ics.getInfo();

			if(info.isEightShortFrame()) resetAllPredictors();
			else {
				int len = Math.Min(sf.GetMaximalPredictionSFB(), info.getMaxSFB());
				int[] swbOffsets = info.getSWBOffsets();
				int k;
				for(int sfb = 0; sfb<len; sfb++) {
					for(k = swbOffsets[sfb]; k<swbOffsets[sfb+1]; k++) {
						predict(data, k, predictionUsed[sfb]);
					}
				}
				if(predictorReset) resetPredictorGroup(predictorResetGroup);
			}
		}

		private void resetPredictState(int index) {
			if(states[index]==null) states[index] = new PredictorState();
			states[index].r0 = 0;
			states[index].r1 = 0;
			states[index].cor0 = 0;
			states[index].cor1 = 0;
			states[index].var0 = 0x3F80;
			states[index].var1 = 0x3F80;
		}

		private void resetAllPredictors() {
			int i;
			for(i = 0; i<states.Length; i++) {
				resetPredictState(i);
			}
		}

		private void resetPredictorGroup(int group) {
			int i;
			for(i = group-1; i<states.Length; i += 30) {
				resetPredictState(i);
			}
		}

		private void predict(float[] data, int off, bool output) {
			if(states[off]==null) states[off] = new PredictorState();
			PredictorState state = states[off];
			float r0 = state.r0, r1 = state.r1;
			float cor0 = state.cor0, cor1 = state.cor1;
			float var0 = state.var0, var1 = state.var1;

			float k1 = var0>1 ? cor0*even(A/var0) : 0;
			float k2 = var1>1 ? cor1*even(A/var1) : 0;

			float pv = round(k1*r0+k2*r1);
			if(output) data[off] += pv*SF_SCALE;

			float e0 = (data[off]*INV_SF_SCALE);
			float e1 = e0-k1*r0;

			state.cor1 = trunc(ALPHA*cor1+r1*e1);
			state.var1 = trunc(ALPHA*var1+0.5f*(r1*r1+e1*e1));
			state.cor0 = trunc(ALPHA*cor0+r0*e0);
			state.var0 = trunc(ALPHA*var0+0.5f*(r0*r0+e0*e0));

			state.r1 = trunc(A*(r0-k1*e0));
			state.r0 = trunc(A*e0);
		}

#warning Review this
		private float round(float pf) {
			return intBitsToFloat((int)((floatToIntBits(pf) + 0x00008000) & 0xFFFF0000));
		}

		private float even(float pf) {
			int i = floatToIntBits(pf);
			i = (int)((i + 0x00007FFF + (i & 0x00010000 >> 16)) & 0xFFFF0000);
			return intBitsToFloat(i);
		}

		private float trunc(float pf) {
			return intBitsToFloat((int)(floatToIntBits(pf) & 0xFFFF0000));
		}

		private static int floatToIntBits(float f)
		{
			return BitConverter.ToInt32(BitConverter.GetBytes(f), 0);
        }

        private static float intBitsToFloat(int i)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(i), 0);
        }
    }
}
