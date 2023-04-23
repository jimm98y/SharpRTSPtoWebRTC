using CameraAPI.AAC.Tools;
using System.Linq;

namespace CameraAPI.AAC.Syntax
{
    public class ICSInfo
    {
        public const int WINDOW_SHAPE_SINE = 0;
		public const int WINDOW_SHAPE_KAISER = 1;
		public const int PREVIOUS = 0;
		public const int CURRENT = 1;

		public enum WindowSequence 
		{
			ONLY_LONG_SEQUENCE = 0,
			LONG_START_SEQUENCE = 1,
			EIGHT_SHORT_SEQUENCE = 2,
			LONG_STOP_SEQUENCE = 3
		}

		private int _frameLength;
		private WindowSequence _windowSequence;
		private int[] _windowShape;
		private int _maxSFB;
		//prediction
		private bool _predictionDataPresent;
		private ICPrediction _icPredict;
		private LTPrediction _ltPredict;
		//windows/sfbs
		private int _windowCount;
		private int _windowGroupCount;
		private int[] _windowGroupLength;
		private int _swbCount;
		private int[] _swbOffsets;

		public ICSInfo(DecoderConfig config)
		{
			this._frameLength = config.GetFrameLength();
			_windowShape = new int[2];
			_windowSequence = WindowSequence.ONLY_LONG_SEQUENCE;
			_windowGroupLength = new int[Constants.MAX_WINDOW_GROUP_COUNT];

            if (LTPrediction.IsLTPProfile(config.GetProfile()))
                _ltPredict = new LTPrediction(_frameLength);
            else
                _ltPredict = null;
        }

		/* ========== decoding ========== */
		public void Decode(BitStream input, DecoderConfig conf, bool commonWindow) {
			SampleFrequency sf = conf.GetSampleFrequency();
			if(sf.Equals(SampleFrequency.SAMPLE_FREQUENCY_NONE)) throw new AACException("invalid sample frequency");

			input.SkipBit(); //reserved
			_windowSequence = (WindowSequence)(input.ReadBits(2));
			_windowShape[PREVIOUS] = _windowShape[CURRENT];
			_windowShape[CURRENT] = input.ReadBit();

			_windowGroupCount = 1;
			_windowGroupLength[0] = 1;

			if(_windowSequence.Equals(WindowSequence.EIGHT_SHORT_SEQUENCE)) {
				_maxSFB = input.ReadBits(4);
				int i;
				for(i = 0; i<7; i++) {
					if(input.ReadBool()) _windowGroupLength[_windowGroupCount-1]++;
					else {
						_windowGroupCount++;
						_windowGroupLength[_windowGroupCount-1] = 1;
					}
				}
				_windowCount = 8;
				_swbOffsets = ScaleFactorBands.SWB_OFFSET_SHORT_WINDOW[(int)sf];
				_swbCount = ScaleFactorBands.SWB_SHORT_WINDOW_COUNT[(int)sf];
			}
			else {
				_maxSFB = input.ReadBits(6);
				_windowCount = 1;
				_swbOffsets = ScaleFactorBands.SWB_OFFSET_LONG_WINDOW[(int)sf];
				_swbCount = ScaleFactorBands.SWB_LONG_WINDOW_COUNT[(int)sf];
				_predictionDataPresent = input.ReadBool();
				if(_predictionDataPresent) ReadPredictionData(input, conf.GetProfile(), sf, commonWindow);
			}
		}

		private void ReadPredictionData(BitStream input, Profile profile, SampleFrequency sf, bool commonWindow) {
			switch(profile) {
				case Profile.AAC_MAIN:
					if(_icPredict==null) _icPredict = new ICPrediction();
					_icPredict.Decode(input, _maxSFB, sf);
					break;
				case Profile.AAC_LTP:
                    _ltPredict.Decode(input, this, profile);
                    break;
				case Profile.ER_AAC_LTP:
					if(!commonWindow) {
                        _ltPredict.Decode(input, this, profile);
                    }
					break;
				default:
					throw new AACException("unexpected profile for LTP: "+profile);
			}
		}

		/* =========== gets ============ */
		public int GetMaxSFB() {
			return _maxSFB;
		}

		public int GetSWBCount() {
			return _swbCount;
		}

		public int[] GetSWBOffsets() {
			return _swbOffsets;
		}

		public int GetSWBOffsetMax() {
			return _swbOffsets[_swbCount];
		}

		public int GetWindowCount() {
			return _windowCount;
		}

		public int GetWindowGroupCount() {
			return _windowGroupCount;
		}

		public int GetWindowGroupLength(int g) {
			return _windowGroupLength[g];
		}

		public WindowSequence GetWindowSequence() {
			return _windowSequence;
		}

		public bool IsEightShortFrame() {
			return _windowSequence.Equals(WindowSequence.EIGHT_SHORT_SEQUENCE);
		}

		public int GetWindowShape(int index) {
			return _windowShape[index];
		}

		public bool IsICPredictionPresent() {
			return _predictionDataPresent;
		}

		public ICPrediction GetICPrediction() {
			return _icPredict;
		}

        public LTPrediction GetLTPrediction() {
            return _ltPredict;
        }

		public void UnsetPredictionSFB(int sfb) {
			if(_predictionDataPresent) _icPredict.SetPredictionUnused(sfb);
            if (_ltPredict != null) _ltPredict.SetPredictionUnused(sfb);
        }

		public void SetData(BitStream input, DecoderConfig conf, ICSInfo info) {
			_windowSequence = info._windowSequence;
			_windowShape[PREVIOUS] = _windowShape[CURRENT];
			_windowShape[CURRENT] = info._windowShape[CURRENT];
			_maxSFB = info._maxSFB;
			_predictionDataPresent = info._predictionDataPresent;
			if(_predictionDataPresent) _icPredict = info._icPredict;
			
			_windowCount = info._windowCount;
			_windowGroupCount = info._windowGroupCount;
			_windowGroupLength = info._windowGroupLength.ToArray();
			_swbCount = info._swbCount;
			_swbOffsets = info._swbOffsets.ToArray();

            if (_predictionDataPresent) {
                _ltPredict.Decode(input, this, conf.GetProfile());
            }
        }
    }
}
