using System;
using System.IO;

namespace SharpJaad.WAV
{
    public class WaveFileWriter
    {
        private const int HEADER_LENGTH = 44;
        private const int RIFF = 1380533830; //'RIFF'
        private const long WAVE_FMT = 6287401410857104416; //'WAVEfmt '
        private const int DATA = 1684108385; //'data'
        private const int BYTE_MASK = 0xFF;
        private readonly int _sampleRate;
        private readonly int _channels;
        private readonly int _bitsPerSample;
        private int _bytesWritten;
        private Stream _output;

        public WaveFileWriter(Stream output, int sampleRate, int channels, int bitsPerSample)
        {
            this._sampleRate = sampleRate;
            this._channels = channels;
            this._bitsPerSample = bitsPerSample;
            this._bytesWritten = 0;

            this._output = output ?? throw new ArgumentNullException(nameof(output));
            WriteHeaderPlaceholder();
        }

        private void WriteHeaderPlaceholder()
        {
            this._output.Write(new byte[HEADER_LENGTH], 0, HEADER_LENGTH); // space for the header
        }

        public void Write(byte[] data)
        {
            Write(data, 0, data.Length);
        }

        public void Write(byte[] data, int off, int len)
        {
            //convert to little endian
            byte tmp;
            for (int i = off; i < off + data.Length; i += 2)
            {
                tmp = data[i + 1];
                data[i + 1] = data[i];
                data[i] = tmp;
            }
            _output.Write(data, off, len);
            _bytesWritten += data.Length;
        }

        public void Write(short[] data)
        {
            Write(data, 0, data.Length);
        }

        public void Write(short[] data, int off, int len)
        {
            for (int i = off; i < off + data.Length; i++)
            {
                _output.WriteByte((byte)(data[i] & BYTE_MASK));
                _output.WriteByte((byte)((data[i] >> 8) & BYTE_MASK));
                _bytesWritten += 2;
            }
        }

        public void Close()
        {
            WriteWaveHeader();
            _output.Close();
        }

        private void WriteWaveHeader()
        {
            _output.Seek(0, SeekOrigin.Begin);
            int bytesPerSec = (_bitsPerSample + 7) / 8;

            using (BinaryWriter bw = new BinaryWriter(_output))
            {
                bw.Write(RIFF); //wave label
                bw.Write(IntegerReverseBytes(_bytesWritten + 36)); //length in bytes without header
                bw.Write(WAVE_FMT);
                bw.Write(IntegerReverseBytes(16)); //length of pcm format declaration area
                bw.Write(ShortReverseBytes(1)); //is PCM
                bw.Write(ShortReverseBytes((short)_channels)); //number of channels
                bw.Write(IntegerReverseBytes(_sampleRate)); //sample rate
                bw.Write(IntegerReverseBytes(_sampleRate * _channels * bytesPerSec)); //bytes per second
                bw.Write(ShortReverseBytes((short)(_channels * bytesPerSec))); //bytes per sample time
                bw.Write(ShortReverseBytes((short)_bitsPerSample)); //bits per sample
                bw.Write(DATA); //data section label
                bw.Write(IntegerReverseBytes(_bytesWritten)); //length of raw pcm data in bytes
            }
        }

        // TODO: fix inefficient bitconverter
        private static int IntegerReverseBytes(int i)
        {
            byte[] buf = BitConverter.GetBytes(i);
            return (buf[0] << 24) | (buf[1] << 16) | (buf[2] << 8) | buf[3];
        }

        private static short ShortReverseBytes(short i)
        {
            byte[] buf = BitConverter.GetBytes(i);
            return (short)((buf[0] << 8) | buf[1]);
        }
    }
}