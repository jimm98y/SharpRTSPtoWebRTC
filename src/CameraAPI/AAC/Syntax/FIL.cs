namespace CameraAPI.AAC.Syntax
{
    public class FIL
    {
        public class DynamicRangeInfo 
		{
			public const int MAX_NBR_BANDS = 7;
            public bool[] _excludeMask;
            public bool[] _additionalExcludedChannels;
            public bool _pceTagPresent;
            public int _pceInstanceTag;
            public int _tagReservedBits;
            public bool _excludedChannelsPresent;
            public bool _bandsPresent;
            public int _bandsIncrement, _interpolationScheme;
            public int[] _bandTop;
            public bool _progRefLevelPresent;
            public int _progRefLevel, _progRefLevelReservedBits;
            public bool[] _dynRngSgn;
            public int[] _dynRngCtl;

			public DynamicRangeInfo() 
			{
				_excludeMask = new bool[MAX_NBR_BANDS];
				_additionalExcludedChannels = new bool[MAX_NBR_BANDS];
			}
		}

		private const int TYPE_FILL = 0;
		private const int TYPE_FILL_DATA = 1;
		private const int TYPE_EXT_DATA_ELEMENT = 2;
		private const int TYPE_DYNAMIC_RANGE = 11;
		private const int TYPE_SBR_DATA = 13;
		private const int TYPE_SBR_DATA_CRC = 14;
		private bool _downSampledSBR;
		private DynamicRangeInfo _dri;

		public FIL(bool downSampledSBR) 
		{
			this._downSampledSBR = downSampledSBR;
		}

		public void Decode(BitStream input, Element prev, SampleFrequency sf, bool sbrEnabled, bool smallFrames)
		{
			int count = input.ReadBits(4);
			if(count==15) count += input.ReadBits(8)-1;
			count *= 8; //convert to bits

			int cpy = count;
			int pos = input.GetPosition();

			while(count>0) 
			{
				count = DecodeExtensionPayload(input, count, prev, sf, sbrEnabled, smallFrames);
			}

			int pos2 = input.GetPosition()-pos;
			int bitsLeft = cpy-pos2;
			if(bitsLeft>0) input.SkipBits(pos2);
			else if(bitsLeft<0) throw new AACException("FIL element overread: "+bitsLeft);
		}

		private int DecodeExtensionPayload(BitStream input, int count, Element prev, SampleFrequency sf, bool sbrEnabled, bool smallFrames) 
		{
			int type = input.ReadBits(4);
			int ret = count - 4;
			switch(type)
			{
				case TYPE_DYNAMIC_RANGE:
					ret = DecodeDynamicRangeInfo(input, ret);
					break;
				case TYPE_SBR_DATA:
				case TYPE_SBR_DATA_CRC:
					if(sbrEnabled)
					{
						if(prev is SCE_LFE||prev is CPE||prev is CCE) 
						{
							prev.DecodeSBR(input, sf, ret, (prev is CPE), (type==TYPE_SBR_DATA_CRC), _downSampledSBR, smallFrames);
							ret = 0;
							break;
						}
						else throw new AACException("SBR applied on unexpected element: "+prev);
					}
					else 
					{
                        input.SkipBits(ret);
						ret = 0;
					}
					break;
				case TYPE_FILL:
				case TYPE_FILL_DATA:
				case TYPE_EXT_DATA_ELEMENT:
				default:
                    input.SkipBits(ret);
					ret = 0;
					break;
			}
			return ret;
		}

		private int DecodeDynamicRangeInfo(BitStream input, int count)
		{
			if(_dri==null) _dri = new DynamicRangeInfo();
			int ret = count;

			int bandCount = 1;

			//pce tag
			if(_dri._pceTagPresent = input.ReadBool()) 
			{
				_dri._pceInstanceTag = input.ReadBits(4);
				_dri._tagReservedBits = input.ReadBits(4);
			}

			//excluded channels
			if(_dri._excludedChannelsPresent = input.ReadBool()) 
			{
				ret -= DecodeExcludedChannels(input);
			}

			//bands
			if(_dri._bandsPresent = input.ReadBool()) 
			{
				_dri._bandsIncrement = input.ReadBits(4);
				_dri._interpolationScheme = input.ReadBits(4);
				ret -= 8;
				bandCount += _dri._bandsIncrement;
				_dri._bandTop = new int[bandCount];
				for(int i = 0; i<bandCount; i++)
				{
					_dri._bandTop[i] = input.ReadBits(8);
					ret -= 8;
				}
			}

			//prog ref level
			if(_dri._progRefLevelPresent = input.ReadBool()) 
			{
				_dri._progRefLevel = input.ReadBits(7);
				_dri._progRefLevelReservedBits = input.ReadBits(1);
				ret -= 8;
			}

			_dri._dynRngSgn = new bool[bandCount];
			_dri._dynRngCtl = new int[bandCount];
			for(int i = 0; i<bandCount; i++) 
			{
				_dri._dynRngSgn[i] = input.ReadBool();
				_dri._dynRngCtl[i] = input.ReadBits(7);
				ret -= 8;
			}
			return ret;
		}

		private int DecodeExcludedChannels(BitStream input) 
		{
			int i;
			int exclChs = 0;

			do 
			{
				for(i = 0; i < 7; i++)
				{
					_dri._excludeMask[exclChs] = input.ReadBool();
					exclChs++;
				}
			}
			while(exclChs < 57 && input.ReadBool());

			return (exclChs / 7) * 8;
		}
    }
}
