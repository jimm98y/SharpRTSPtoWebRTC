namespace CameraAPI.AAC.Syntax
{
    public class SCE_LFE : Element
    {
		private ICStream ics;

		public SCE_LFE(DecoderConfig config) {
			ics = new ICStream(config);
		}

		public void decode(BitStream input, DecoderConfig conf) {
			readElementInstanceTag(input);
			ics.decode(input, false, conf);
		}

		public ICStream getICStream() {
			return ics;
		}
    }
}
