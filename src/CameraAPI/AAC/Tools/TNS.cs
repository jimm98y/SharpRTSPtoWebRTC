using CameraAPI.AAC.Syntax;

namespace CameraAPI.AAC.Tools
{
    public class TNS
    {
		private static int TNS_MAX_ORDER = 20;
		private static int[] SHORT_BITS = {1, 4, 3}, LONG_BITS = {2, 6, 5};
		//bitstream
		private int[] _nFilt;
		private int[,] _length, _order;
		private bool[,] _direction;
		private float[,,] _coef;

		public TNS()
		{
			_nFilt = new int[8];
			_length = new int[8,4];
			_direction = new bool[8,4];
			_order = new int[8,4];
			_coef = new float[8,4,TNS_MAX_ORDER];
		}

		public void Decode(BitStream input, ICSInfo info) 
		{
			int windowCount = info.GetWindowCount();
			int[] bits = info.IsEightShortFrame() ? SHORT_BITS : LONG_BITS;

			int w, i, filt, coefLen, coefRes, coefCompress, tmp;
			for(w = 0; w<windowCount; w++) {
				if((_nFilt[w] = input.ReadBits(bits[0]))!=0) {
					coefRes = input.ReadBit();

					for(filt = 0; filt<_nFilt[w]; filt++) {
						_length[w,filt] = input.ReadBits(bits[1]);

						if((_order[w,filt] = input.ReadBits(bits[2]))>20) throw new AACException("TNS filter out of range: "+_order[w,filt]);
						else if(_order[w,filt]!=0) {
							_direction[w,filt] = input.ReadBool();
							coefCompress = input.ReadBit();
							coefLen = coefRes+3-coefCompress;
							tmp = 2*coefCompress+coefRes;

							for(i = 0; i<_order[w,filt]; i++) {
								_coef[w,filt,i] = TNSTables.TNS_TABLES[tmp][input.ReadBits(coefLen)];
							}
						}
					}
				}
			}
		}

		public void Process(ICStream ics, float[] spec, SampleFrequency sf, bool decode) {
			//TODO...
		}
    }
}
