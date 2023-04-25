using SharpJaad.AAC;

namespace SharpJaad.ADTS
{
    public class ADTSFrame
	{ 
		//fixed
		private bool _id, _protectionAbsent, _privateBit, _copy, _home;
		private int _layer, _profile, _sampleFrequency, _channelConfiguration;
		//variable
		private bool _copyrightIDBit, _copyrightIDStart;
		private int _frameLength, _adtsBufferFullness, _rawDataBlockCount;
		//error check
		private int[] _rawDataBlockPosition;
		private int _crcCheck;
		//decoder specific info
		private byte[] _info;

		public ADTSFrame(DataInputStream input)
		{
			ReadHeader(input);

			if(!_protectionAbsent) _crcCheck = input.ReadUnsignedShort();
			if(_rawDataBlockCount==0) 
			{
				//raw_data_block();
			}
			else 
			{
				int i;
				//header error check
				if(!_protectionAbsent) 
				{
					_rawDataBlockPosition = new int[_rawDataBlockCount];
					for(i = 0; i<_rawDataBlockCount; i++)
					{
						_rawDataBlockPosition[i] = input.ReadUnsignedShort();
					}
					_crcCheck = input.ReadUnsignedShort();
				}
				//raw data blocks
				for(i = 0; i<_rawDataBlockCount; i++) 
				{
					//raw_data_block();
					if(!_protectionAbsent) _crcCheck = input.ReadUnsignedShort();
				}
			}
		}

		private void ReadHeader(DataInputStream input) 
		{
			//fixed header:
			//1 bit ID, 2 bits layer, 1 bit protection absent
			int i = input.ReadByte();
			_id = ((i>>3)&0x1)==1;
			_layer = (i>>1)&0x3;
			_protectionAbsent = (i&0x1)==1;

			//2 bits profile, 4 bits sample frequency, 1 bit private bit
			i = input.ReadByte();
			_profile = ((i>>6)&0x3)+1;
			_sampleFrequency = (i>>2)&0xF;
			_privateBit = ((i>>1)&0x1)==1;

			//3 bits channel configuration, 1 bit copy, 1 bit home
			i = (i<<8)| input.ReadByte();
			_channelConfiguration = ((i>>6)&0x7);
			_copy = ((i>>5)&0x1)==1;
			_home = ((i>>4)&0x1)==1;
			//int emphasis = in.readBits(2);

			//variable header:
			//1 bit copyrightIDBit, 1 bit copyrightIDStart, 13 bits frame length,
			//11 bits adtsBufferFullness, 2 bits rawDataBlockCount
			_copyrightIDBit = ((i>>3)&0x1)==1;
			_copyrightIDStart = ((i>>2)&0x1)==1;
			i = (i<<16)| input.ReadUnsignedShort();
			_frameLength = (i>>5)&0x1FFF;
			i = (i<<8)| input.ReadByte();
			_adtsBufferFullness = (i>>2)&0x7FF;
			_rawDataBlockCount = i&0x3;
		}

		public int GetFrameLength() 
		{
			return _frameLength-(_protectionAbsent ? 7 : 9);
		}

		public byte[] CreateDecoderSpecificInfo() 
		{
			if(_info==null)
			{
				//5 bits profile, 4 bits sample frequency, 4 bits channel configuration
				_info = new byte[2];
				_info[0] = (byte) (_profile<<3);
				_info[0] |= (byte)((_sampleFrequency>>1)&0x7);
				_info[1] = (byte) ((_sampleFrequency&0x1)<<7);
				_info[1] |= (byte)(_channelConfiguration<<3);
				/*1 bit frame length flag, 1 bit depends on core coder,
				1 bit extension flag (all three currently 0)*/
			}

			return _info;
		}

		public int GetSampleFrequency() 
		{
			return SampleFrequencyExtensions.GetFrequency((SampleFrequency)_sampleFrequency);
		}

		public int GetChannelCount()
		{
			return _channelConfiguration;
		}
	}
}
