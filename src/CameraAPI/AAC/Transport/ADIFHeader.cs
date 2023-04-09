using CameraAPI.AAC.Syntax;

namespace CameraAPI.AAC.Transport
{
    public sealed class ADIFHeader
    {
        private const long ADIF_ID = 0x41444946; //'ADIF'
		private long id;
		private bool copyrightIDPresent;
		private byte[] copyrightID;
		private bool originalCopy, home, bitstreamType;
		private int bitrate;
		private int pceCount;
		private int[] adifBufferFullness;
		private PCE[] pces;

		public static bool isPresent(BitStream input) {
			return input.peekBits(32)==ADIF_ID;
		}

		private ADIFHeader() {
			copyrightID = new byte[9];
		}

		public static ADIFHeader readHeader(BitStream input) {
			ADIFHeader h = new ADIFHeader();
			h.decode(input);
			return h;
		}

		private void decode(BitStream input) {
			int i;
			id = input.readBits(32); //'ADIF'
			copyrightIDPresent = input.readBool();
			if(copyrightIDPresent) {
				for(i = 0; i<9; i++) {
					copyrightID[i] = (byte)input.readBits(8);
				}
			}
			originalCopy = input.readBool();
			home = input.readBool();
			bitstreamType = input.readBool();
			bitrate = input.readBits(23);
			pceCount = input.readBits(4)+1;
			pces = new PCE[pceCount];
			adifBufferFullness = new int[pceCount];
			for(i = 0; i<pceCount; i++) {
				if(bitstreamType) adifBufferFullness[i] = -1;
				else adifBufferFullness[i] = input.readBits(20);
				pces[i] = new PCE();
				pces[i].decode(input);
			}
		}

		public PCE getFirstPCE() {
			return pces[0];
		}
    }
}
