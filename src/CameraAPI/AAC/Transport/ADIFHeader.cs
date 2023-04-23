using CameraAPI.AAC.Syntax;

namespace CameraAPI.AAC.Transport
{
    public sealed class ADIFHeader
    {
        private const long ADIF_ID = 0x41444946; //'ADIF'
		private long _id;
		private bool _copyrightIDPresent;
		private byte[] _copyrightID;
		private bool _originalCopy, _home, _bitstreamType;
		private int _bitrate;
		private int _pceCount;
		private int[] _adifBufferFullness;
		private PCE[] _pces;

		public static bool IsPresent(BitStream input)
		{
			return input.PeekBits(32)==ADIF_ID;
		}

		private ADIFHeader() 
		{
			_copyrightID = new byte[9];
		}

		public static ADIFHeader ReadHeader(BitStream input) 
		{
			ADIFHeader h = new ADIFHeader();
			h.Decode(input);
			return h;
		}

		private void Decode(BitStream input) 
		{
			int i;
			_id = input.ReadBits(32); //'ADIF'
			_copyrightIDPresent = input.ReadBool();
			if(_copyrightIDPresent) 
			{
				for(i = 0; i<9; i++) 
				{
					_copyrightID[i] = (byte)input.ReadBits(8);
				}
			}
			_originalCopy = input.ReadBool();
			_home = input.ReadBool();
			_bitstreamType = input.ReadBool();
			_bitrate = input.ReadBits(23);
			_pceCount = input.ReadBits(4)+1;
			_pces = new PCE[_pceCount];
			_adifBufferFullness = new int[_pceCount];
			for(i = 0; i<_pceCount; i++)
			{
				if(_bitstreamType) _adifBufferFullness[i] = -1;
				else _adifBufferFullness[i] = input.ReadBits(20);
				_pces[i] = new PCE();
				_pces[i].Decode(input);
			}
		}

		public PCE GetFirstPCE()
		{
			return _pces[0];
		}
    }
}
