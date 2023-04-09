using CameraAPI.AAC.Syntax;

namespace CameraAPI.AAC.Tools
{
    public class TNS : Constants
    {
		private static int TNS_MAX_ORDER = 20;
		private static int[] SHORT_BITS = {1, 4, 3}, LONG_BITS = {2, 6, 5};
		//bitstream
		private int[] nFilt;
		private int[,] length, order;
		private bool[,] direction;
		private float[,,] coef;

		public TNS() {
			nFilt = new int[8];
			length = new int[8,4];
			direction = new bool[8,4];
			order = new int[8,4];
			coef = new float[8,4,TNS_MAX_ORDER];
		}

		public void decode(BitStream input, ICSInfo info) {
			int windowCount = info.getWindowCount();
			int[] bits = info.isEightShortFrame() ? SHORT_BITS : LONG_BITS;

			int w, i, filt, coefLen, coefRes, coefCompress, tmp;
			for(w = 0; w<windowCount; w++) {
				if((nFilt[w] = input.readBits(bits[0]))!=0) {
					coefRes = input.readBit();

					for(filt = 0; filt<nFilt[w]; filt++) {
						length[w,filt] = input.readBits(bits[1]);

						if((order[w,filt] = input.readBits(bits[2]))>20) throw new AACException("TNS filter out of range: "+order[w,filt]);
						else if(order[w,filt]!=0) {
							direction[w,filt] = input.readBool();
							coefCompress = input.readBit();
							coefLen = coefRes+3-coefCompress;
							tmp = 2*coefCompress+coefRes;

							for(i = 0; i<order[w,filt]; i++) {
								coef[w,filt,i] = TNSTables.TNS_TABLES[tmp][input.readBits(coefLen)];
							}
						}
					}
				}
			}
		}

		public void process(ICStream ics, float[] spec, SampleFrequency sf, bool decode) {
			//TODO...
		}
    }
}
