namespace CameraAPI.AAC
{
    public enum SampleFrequency : int
    {
        SAMPLE_FREQUENCY_96000 = 0,
        SAMPLE_FREQUENCY_88200 = 1,
        SAMPLE_FREQUENCY_64000 = 2,
        SAMPLE_FREQUENCY_48000 = 3,
        SAMPLE_FREQUENCY_44100 = 4,
        SAMPLE_FREQUENCY_32000 = 5,
        SAMPLE_FREQUENCY_24000 = 6,
        SAMPLE_FREQUENCY_22050 = 7,
        SAMPLE_FREQUENCY_16000 = 8,
        SAMPLE_FREQUENCY_12000 = 9,
        SAMPLE_FREQUENCY_11025 = 10,
        SAMPLE_FREQUENCY_8000 = 11,
        SAMPLE_FREQUENCY_NONE = -1
    }

    public static class SampleFrequencyExtensions
    {
        public static SampleFrequency FromFrequency(int frequency)
        {
            SampleFrequency ret;
            switch (frequency)
            {
                case 96000:
                    ret = SampleFrequency.SAMPLE_FREQUENCY_96000;
                    break;

                case 88200:
                    ret = SampleFrequency.SAMPLE_FREQUENCY_88200;
                    break;

                case 64000:
                    ret = SampleFrequency.SAMPLE_FREQUENCY_64000;
                    break;

                case 48000:
                    ret = SampleFrequency.SAMPLE_FREQUENCY_48000;
                    break;

                case 44100:
                    ret = SampleFrequency.SAMPLE_FREQUENCY_44100;
                    break;

                case 32000:
                    ret = SampleFrequency.SAMPLE_FREQUENCY_32000;
                    break;

                case 24000:
                    ret = SampleFrequency.SAMPLE_FREQUENCY_24000;
                    break;

                case 22050:
                    ret = SampleFrequency.SAMPLE_FREQUENCY_22050;
                    break;

                case 16000:
                    ret = SampleFrequency.SAMPLE_FREQUENCY_16000;
                    break;

                case 12000:
                    ret = SampleFrequency.SAMPLE_FREQUENCY_12000;
                    break;

                case 11025:
                    ret = SampleFrequency.SAMPLE_FREQUENCY_11025;
                    break;

                case 8000:
                    ret = SampleFrequency.SAMPLE_FREQUENCY_8000;
                    break;

                default:
                    ret = SampleFrequency.SAMPLE_FREQUENCY_NONE;
                    break;
            }

            return ret;
        }

        public static int GetFrequency(this SampleFrequency frequency)
        {
            int ret;
            switch (frequency)
            {
                case SampleFrequency.SAMPLE_FREQUENCY_96000:
                    ret = 96000;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_88200:
                    ret = 88200;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_64000:
                    ret = 64000;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_48000:
                    ret = 48000;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_44100:
                    ret = 44100;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_32000:
                    ret = 32000;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_24000:
                    ret = 24000;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_22050:
                    ret = 22050;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_16000:
                    ret = 16000;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_12000:
                    ret = 12000;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_11025:
                    ret = 11025;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_8000 :
                    ret = 8000;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_NONE:
                default:
                    ret = 0;
                    break;
            }

            return ret;
        }

        public static int GetMaximalPredictionSFB(this SampleFrequency frequency)
        {
            int ret;
            switch (frequency)
            {
                case SampleFrequency.SAMPLE_FREQUENCY_96000:
                case SampleFrequency.SAMPLE_FREQUENCY_88200:
                    ret = 33;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_64000:
                    ret = 38;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_48000:
                case SampleFrequency.SAMPLE_FREQUENCY_44100:
                case SampleFrequency.SAMPLE_FREQUENCY_32000:
                    ret = 40;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_24000:
                case SampleFrequency.SAMPLE_FREQUENCY_22050:
                    ret = 41;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_16000:
                case SampleFrequency.SAMPLE_FREQUENCY_12000:
                case SampleFrequency.SAMPLE_FREQUENCY_11025:
                    ret = 37;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_8000:
                    ret = 34;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_NONE:
                default:
                    ret = 0;
                    break;
            }

            return ret;
        }

        public static int GetPredictorCount(this SampleFrequency frequency)
        {
            int ret;
            switch (frequency)
            {
                case SampleFrequency.SAMPLE_FREQUENCY_96000:
                case SampleFrequency.SAMPLE_FREQUENCY_88200:
                    ret = 512;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_64000:
                    ret = 664;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_48000:
                case SampleFrequency.SAMPLE_FREQUENCY_44100:
                case SampleFrequency.SAMPLE_FREQUENCY_32000:
                    ret = 672;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_24000:
                case SampleFrequency.SAMPLE_FREQUENCY_22050:
                    ret = 652;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_16000:
                case SampleFrequency.SAMPLE_FREQUENCY_12000:
                case SampleFrequency.SAMPLE_FREQUENCY_11025:
                case SampleFrequency.SAMPLE_FREQUENCY_8000:
                    ret = 664;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_NONE:
                default:
                    ret = 0;
                    break;
            }

            return ret;
        }

        public static int GetMaximalTNS_SFB(this SampleFrequency frequency, bool shortWindow)
        {
            int ret;
            switch (frequency)
            {
                case SampleFrequency.SAMPLE_FREQUENCY_96000:
                case SampleFrequency.SAMPLE_FREQUENCY_88200:
                    ret = shortWindow ? 9 : 31;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_64000:
                    ret = shortWindow ? 10 : 34;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_48000:
                    ret = shortWindow ? 14 : 40;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_44100:
                    ret = shortWindow ? 14 : 42;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_32000:
                    ret = shortWindow ? 14 : 51;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_24000:
                case SampleFrequency.SAMPLE_FREQUENCY_22050:
                    ret = shortWindow ? 14 : 46;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_16000:
                case SampleFrequency.SAMPLE_FREQUENCY_12000:
                case SampleFrequency.SAMPLE_FREQUENCY_11025:
                    ret = shortWindow ? 14 : 42;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_8000:
                    ret = shortWindow ? 14 : 39;
                    break;

                case SampleFrequency.SAMPLE_FREQUENCY_NONE:
                default:
                    ret = 0;
                    break;
            }

            return ret;
        }
    }
}
