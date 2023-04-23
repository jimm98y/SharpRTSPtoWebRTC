namespace SharpJaad.AAC
{
    /// <summary>
    /// The SampleBuffer holds the decoded AAC frame. It contains the raw PCM data and its format.
    /// </summary>
    public class SampleBuffer
    {
        public int SampleRate { get; private set; }
        public int Channels { get; private set; }
        public int BitsPerSample { get; private set; }
        public double Length { get; private set; }
        public double Bitrate { get; private set; }
        public double EncodedBitrate { get; private set; }
        public byte[] Data { get; private set; }
        public bool BigEndian { get; private set; }

        public SampleBuffer()
        {
            Data = new byte[0];
            SampleRate = 0;
            Channels = 0;
            BitsPerSample = 0;
            BigEndian = true;
        }

        /// <summary>
        /// Sets the endianness for the data.
        /// </summary>
        /// <param name="bigEndian">if true the data will be in big endian, else in little endian</param>
        public void SetBigEndian(bool bigEndian)
        {
            if (bigEndian != BigEndian)
            {
                byte tmp;
                for (int i = 0; i < Data.Length; i += 2)
                {
                    tmp = Data[i];
                    Data[i] = Data[i + 1];
                    Data[i + 1] = tmp;
                }
                BigEndian = bigEndian;
            }
        }

        public void SetData(byte[] data, int sampleRate, int channels, int bitsPerSample, int bitsRead)
        {
            Data = data;
            SampleRate = sampleRate;
            Channels = channels;
            BitsPerSample = bitsPerSample;

            if (sampleRate == 0)
            {
                Length = 0;
                Bitrate = 0;
                EncodedBitrate = 0;
            }
            else
            {
                int bytesPerSample = bitsPerSample / 8; //usually 2
                int samplesPerChannel = data.Length / (bytesPerSample * channels); //=1024
                Length = samplesPerChannel / (double)sampleRate;
                Bitrate = samplesPerChannel * bitsPerSample * channels / Length;
                EncodedBitrate = bitsRead / Length;
            }
        }
    }
}
