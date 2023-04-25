using System;

namespace SharpJaad.MP4.OD
{
    //ISO 14496-1 - 10.2.3
    //TODO: not working: reads too much! did the specification change?
    public class SLConfigDescriptor : Descriptor
    {
        private bool _useAccessUnitStart, _useAccessUnitEnd, _useRandomAccessPoint,
                _usePadding, _useTimeStamp, _useWallClockTimeStamp, _useIdle, _duration;
        private long _timeStampResolution, _ocrResolution;
        private int _timeStampLength, _ocrLength, _instantBitrateLength,
                _degradationPriorityLength, _seqNumberLength;
        private long _timeScale;
        private int _accessUnitDuration, _compositionUnitDuration;
        private long _wallClockTimeStamp, _startDecodingTimeStamp, _startCompositionTimeStamp;
        private bool _ocrStream;
        private int _ocrES_ID;

        public override void Decode(MP4InputStream input)
        {
            int tmp;

            bool predefined = input.Read() == 1;
            if (!predefined)
            {
                //flags
                tmp = input.Read();
                _useAccessUnitStart = ((tmp >> 7) & 1) == 1;
                _useAccessUnitEnd = ((tmp >> 6) & 1) == 1;
                _useRandomAccessPoint = ((tmp >> 5) & 1) == 1;
                _usePadding = ((tmp >> 4) & 1) == 1;
                _useTimeStamp = ((tmp >> 3) & 1) == 1;
                _useWallClockTimeStamp = ((tmp >> 2) & 1) == 1;
                _useIdle = ((tmp >> 1) & 1) == 1;
                _duration = (tmp & 1) == 1;

                _timeStampResolution = input.ReadBytes(4);
                _ocrResolution = input.ReadBytes(4);
                _timeStampLength = input.Read();
                _ocrLength = input.Read();
                _instantBitrateLength = input.Read();
                tmp = input.Read();
                _degradationPriorityLength = (tmp >> 4) & 15;
                _seqNumberLength = tmp & 15;

                if (_duration)
                {
                    _timeScale = input.ReadBytes(4);
                    _accessUnitDuration = (int)input.ReadBytes(2);
                    _compositionUnitDuration = (int)input.ReadBytes(2);
                }

                if (!_useTimeStamp)
                {
                    if (_useWallClockTimeStamp) _wallClockTimeStamp = input.ReadBytes(4);
                    tmp = (int)Math.Ceiling((double)(2 * _timeStampLength) / 8);
                    long tmp2 = input.ReadBytes(tmp);
                    long mask = ((1 << _timeStampLength) - 1);
                    _startDecodingTimeStamp = (tmp2 >> _timeStampLength) & mask;
                    _startCompositionTimeStamp = tmp2 & mask;
                }
            }

            tmp = input.Read();
            _ocrStream = ((tmp >> 7) & 1) == 1;
            if (_ocrStream) _ocrES_ID = (int)input.ReadBytes(2);
        }
    }
}