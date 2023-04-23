namespace CameraAPI.AAC.Syntax
{
    public class PCE : Element
    {
		private const int MAX_FRONT_CHANNEL_ELEMENTS = 16;
		private const int MAX_SIDE_CHANNEL_ELEMENTS = 16;
		private const int MAX_BACK_CHANNEL_ELEMENTS = 16;
		private const int MAX_LFE_CHANNEL_ELEMENTS = 4;
		private const int MAX_ASSOC_DATA_ELEMENTS = 8;
		private const int MAX_VALID_CC_ELEMENTS = 16;

		public sealed class TaggedElement 
		{
			public bool _isCPE;
			public int _tag;

			public TaggedElement(bool isCPE, int tag)
			{
				this._isCPE = isCPE;
				this._tag = tag;
			}

			public bool IsIsCPE() 
			{
				return _isCPE;
			}

			public int GetTag() 
			{
				return _tag;
			}
		}

		public sealed class CCE 
		{
			public bool _isIndSW;
			public int _tag;

			public CCE(bool isIndSW, int tag) 
			{
				this._isIndSW = isIndSW;
				this._tag = tag;
			}

			public bool IsIsIndSW() 
			{
				return _isIndSW;
			}

			public int GetTag() 
			{
				return _tag;
			}
		}

		private Profile _profile;
		private SampleFrequency _sampleFrequency;
		private int _frontChannelElementsCount, _sideChannelElementsCount, _backChannelElementsCount;
		private int _lfeChannelElementsCount, _assocDataElementsCount;
		private int _validCCElementsCount;
		private bool _monoMixdown, _stereoMixdown, _matrixMixdownIDXPresent;
		private int _monoMixdownElementNumber, _stereoMixdownElementNumber, _matrixMixdownIDX;
		private bool _pseudoSurround;
		private TaggedElement[] _frontElements, _sideElements, _backElements;
		private int[] _lfeElementTags;
		private int[] _assocDataElementTags;
		private CCE[] _ccElements;
		private byte[] _commentFieldData;

		public PCE() 
		{
			_frontElements = new TaggedElement[MAX_FRONT_CHANNEL_ELEMENTS];
			_sideElements = new TaggedElement[MAX_SIDE_CHANNEL_ELEMENTS];
			_backElements = new TaggedElement[MAX_BACK_CHANNEL_ELEMENTS];
			_lfeElementTags = new int[MAX_LFE_CHANNEL_ELEMENTS];
			_assocDataElementTags = new int[MAX_ASSOC_DATA_ELEMENTS];
			_ccElements = new CCE[MAX_VALID_CC_ELEMENTS];
			_sampleFrequency = SampleFrequency.SAMPLE_FREQUENCY_NONE;
		}

		public void Decode(BitStream input) 
		{
			ReadElementInstanceTag(input);

			_profile = (Profile)(input.ReadBits(2));

			_sampleFrequency = (SampleFrequency)(input.ReadBits(4));

			_frontChannelElementsCount = input.ReadBits(4);
			_sideChannelElementsCount = input.ReadBits(4);
			_backChannelElementsCount = input.ReadBits(4);
			_lfeChannelElementsCount = input.ReadBits(2);
			_assocDataElementsCount = input.ReadBits(3);
			_validCCElementsCount = input.ReadBits(4);

			if(_monoMixdown = input.ReadBool()) 
			{
				//Constants.LOGGER.warning("mono mixdown present, but not yet supported");
				_monoMixdownElementNumber = input.ReadBits(4);
			}
			if(_stereoMixdown = input.ReadBool()) 
			{
				//Constants.LOGGER.warning("stereo mixdown present, but not yet supported");
				_stereoMixdownElementNumber = input.ReadBits(4);
			}
			if(_matrixMixdownIDXPresent = input.ReadBool())
			{
				//Constants.LOGGER.warning("matrix mixdown present, but not yet supported");
				_matrixMixdownIDX = input.ReadBits(2);
				_pseudoSurround = input.ReadBool();
			}

			ReadTaggedElementArray(_frontElements, input, _frontChannelElementsCount);

			ReadTaggedElementArray(_sideElements, input, _sideChannelElementsCount);

			ReadTaggedElementArray(_backElements, input, _backChannelElementsCount);

			int i;
			for(i = 0; i<_lfeChannelElementsCount; ++i) 
			{
				_lfeElementTags[i] = input.ReadBits(4);
			}

			for(i = 0; i<_assocDataElementsCount; ++i) 
			{
				_assocDataElementTags[i] = input.ReadBits(4);
			}

			for(i = 0; i<_validCCElementsCount; ++i) 
			{
				_ccElements[i] = new CCE(input.ReadBool(), input.ReadBits(4));
			}

            input.ByteAlign();

			int commentFieldBytes = input.ReadBits(8);
			_commentFieldData = new byte[commentFieldBytes];
			for(i = 0; i<commentFieldBytes; i++) 
			{
				_commentFieldData[i] = (byte)input.ReadBits(8);
			}
		}

		private void ReadTaggedElementArray(TaggedElement[] te, BitStream input, int len) 
		{
			for(int i = 0; i<len; ++i)
			{
				te[i] = new TaggedElement(input.ReadBool(), input.ReadBits(4));
			}
		}

		public Profile GetProfile() 
		{
			return _profile;
		}

		public SampleFrequency GetSampleFrequency() 
		{
			return _sampleFrequency;
		}

		public int GetChannelCount()
		{
            int count = _lfeChannelElementsCount + _assocDataElementsCount;

            for (int n = 0; n < _frontChannelElementsCount; ++n)
                count += _frontElements[n]._isCPE ? 2 : 1;

            for (int n = 0; n < _sideChannelElementsCount; ++n)
                count += _sideElements[n]._isCPE ? 2 : 1;

            for (int n = 0; n < _backChannelElementsCount; ++n)
                count += _backElements[n]._isCPE ? 2 : 1;

            return count;
        }
    }
}
