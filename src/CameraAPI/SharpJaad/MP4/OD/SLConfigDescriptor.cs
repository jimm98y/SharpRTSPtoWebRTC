using System;

namespace SharpJaad.MP4.OD
{
    //ISO 14496-1 - 10.2.3
    //TODO: not working: reads too much! did the specification change?
    public class SLConfigDescriptor : Descriptor
    {
        private bool useAccessUnitStart, useAccessUnitEnd, useRandomAccessPoint,
                usePadding, useTimeStamp, useWallClockTimeStamp, useIdle, duration;
        private long timeStampResolution, ocrResolution;
        private int timeStampLength, ocrLength, instantBitrateLength,
                degradationPriorityLength, seqNumberLength;
        private long timeScale;
        private int accessUnitDuration, compositionUnitDuration;
        private long wallClockTimeStamp, startDecodingTimeStamp, startCompositionTimeStamp;
        private bool ocrStream;
        private int ocrES_ID;

        public override void decode(MP4InputStream input)
        {
            int tmp;

            bool predefined = input.read() == 1;
            if (!predefined)
            {
                //flags
                tmp = input.read();
                useAccessUnitStart = ((tmp >> 7) & 1) == 1;
                useAccessUnitEnd = ((tmp >> 6) & 1) == 1;
                useRandomAccessPoint = ((tmp >> 5) & 1) == 1;
                usePadding = ((tmp >> 4) & 1) == 1;
                useTimeStamp = ((tmp >> 3) & 1) == 1;
                useWallClockTimeStamp = ((tmp >> 2) & 1) == 1;
                useIdle = ((tmp >> 1) & 1) == 1;
                duration = (tmp & 1) == 1;

                timeStampResolution = input.readBytes(4);
                ocrResolution = input.readBytes(4);
                timeStampLength = input.read();
                ocrLength = input.read();
                instantBitrateLength = input.read();
                tmp = input.read();
                degradationPriorityLength = (tmp >> 4) & 15;
                seqNumberLength = tmp & 15;

                if (duration)
                {
                    timeScale = input.readBytes(4);
                    accessUnitDuration = (int)input.readBytes(2);
                    compositionUnitDuration = (int)input.readBytes(2);
                }

                if (!useTimeStamp)
                {
                    if (useWallClockTimeStamp) wallClockTimeStamp = input.readBytes(4);
                    tmp = (int)Math.Ceiling((double)(2 * timeStampLength) / 8);
                    long tmp2 = input.readBytes(tmp);
                    long mask = ((1 << timeStampLength) - 1);
                    startDecodingTimeStamp = (tmp2 >> timeStampLength) & mask;
                    startCompositionTimeStamp = tmp2 & mask;
                }
            }

            tmp = input.read();
            ocrStream = ((tmp >> 7) & 1) == 1;
            if (ocrStream) ocrES_ID = (int)input.readBytes(2);
        }
    }
}