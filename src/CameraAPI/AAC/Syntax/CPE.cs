using CameraAPI.AAC.Tools;

namespace CameraAPI.AAC.Syntax
{
    public class CPE : Element
    {
        private MSMask _msMask;
		private bool[] _msUsed;
		private bool _commonWindow;
		ICStream _icsL, _icsR;

		public CPE(DecoderConfig config)
		{
			_msUsed = new bool[Constants.MAX_MS_MASK];
			_icsL = new ICStream(config);
			_icsR = new ICStream(config);
		}

		public void Decode(BitStream input, DecoderConfig conf) 
		{
			Profile profile = conf.GetProfile();
			SampleFrequency sf = conf.GetSampleFrequency();
			if(sf.Equals(SampleFrequency.SAMPLE_FREQUENCY_NONE)) throw new AACException("invalid sample frequency");

			ReadElementInstanceTag(input);

			_commonWindow = input.ReadBool();
			ICSInfo info = _icsL.GetInfo();
			if(_commonWindow) {
				info.Decode(input, conf, _commonWindow);
                _icsR.GetInfo().SetData(input, conf, info);

                _msMask = (MSMask)(input.ReadBits(2));
				if(_msMask.Equals(MSMask.TYPE_USED)) {
					int maxSFB = info.GetMaxSFB();
					int windowGroupCount = info.GetWindowGroupCount();

					for(int idx = 0; idx<windowGroupCount*maxSFB; idx++) {
						_msUsed[idx] = input.ReadBool();
					}
				}
				else if(_msMask.Equals(MSMask.TYPE_ALL_1)) Arrays.Fill(_msUsed, true);
				else if(_msMask.Equals(MSMask.TYPE_ALL_0)) Arrays.Fill(_msUsed, false);
				else throw new AACException("reserved MS mask type used");
			}
			else {
				_msMask = MSMask.TYPE_ALL_0;
				Arrays.Fill(_msUsed, false);
			}

            if (profile.IsErrorResilientProfile()) {
                LTPrediction ltp = _icsR.GetInfo().GetLTPrediction();
                if (ltp != null) ltp.Decode(input, info, profile);
            }

			_icsL.Decode(input, _commonWindow, conf);
			_icsR.Decode(input, _commonWindow, conf);
		}

		public ICStream GetLeftChannel()
		{
			return _icsL;
		}

		public ICStream GetRightChannel() 
		{
			return _icsR;
		}

		public MSMask GetMSMask() 
		{
			return _msMask;
		}

		public bool IsMSUsed(int off) 
		{
			return _msUsed[off];
		}

		public bool IsMSMaskPresent()
		{
			return !_msMask.Equals(MSMask.TYPE_ALL_0);
		}

		public bool IsCommonWindow() 
		{
			return _commonWindow;
		}
    }
}
