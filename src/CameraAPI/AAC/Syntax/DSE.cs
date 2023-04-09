namespace CameraAPI.AAC.Syntax
{
    public class DSE
    {
        private byte[] dataStreamBytes;

		public DSE() {
			
		}

		public void decode(BitStream input) {
			bool byteAlign = input.readBool();
			int count = input.readBits(8);
			if(count==255) count += input.readBits(8);

			if(byteAlign) input.byteAlign();

			dataStreamBytes = new byte[count];
			for(int i = 0; i<count; i++) {
				dataStreamBytes[i] = (byte)input.readBits(8);
			}
		}
    }
}
