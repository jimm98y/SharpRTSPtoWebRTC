using System;
using SharpJaad.AAC;

namespace SharpJaad.AAC.Syntax
{
    public class BitStream
    {
        private const int WORD_BITS = 32;
        private const int WORD_BYTES = 4;
        private const int BYTE_MASK = 0xff;
        private byte[] _buffer;
        private int _pos; //offset in the buffer array
        private int _cache; //current 4 bytes, that are read from the buffer
        protected int _bitsCached; //remaining bits in current cache
        protected int _position; //number of total bits read

        public BitStream()
        { }

        public BitStream(byte[] data)
        {
            SetData(data);
        }

        public void Destroy()
        {
            Reset();
            _buffer = null;
        }

        public void SetData(byte[] data)
        {
            Reset();

            int size = data.Length;

            // reduce the buffer size to an integer number of words
            int shift = size % WORD_BYTES;

            // push leading bytes to cache
            _bitsCached = 8 * shift;

            for (int i = 0; i < shift; ++i)
            {
                byte c = data[i];
                _cache <<= 8;
                _cache |= 0xff & c;
            }

            size -= shift;

            //only reallocate if needed
            if (_buffer == null || _buffer.Length != size)
                _buffer = new byte[size];

            Buffer.BlockCopy(data, shift, _buffer, 0, _buffer.Length);
        }

        public void ByteAlign()
        {
            int toFlush = _bitsCached & 7;
            if (toFlush > 0) SkipBits(toFlush);
        }

        public void Reset()
        {
            _pos = 0;
            _bitsCached = 0;
            _cache = 0;
            _position = 0;
        }

        public int GetPosition()
        {
            return _position;
        }

        public int GetBitsLeft()
        {
            return 8 * (_buffer.Length - _pos) + _bitsCached;
        }

        /**
		 * Reads the next four bytes.
		 * @param peek if true, the stream pointer will not be increased
		 */
        protected int ReadCache(bool peek)
        {
            int i;
            if (_pos > _buffer.Length - WORD_BYTES) throw new AACException("end of stream", true);
            else i = (_buffer[_pos] & BYTE_MASK) << 24
                        | (_buffer[_pos + 1] & BYTE_MASK) << 16
                        | (_buffer[_pos + 2] & BYTE_MASK) << 8
                        | _buffer[_pos + 3] & BYTE_MASK;
            if (!peek) _pos += WORD_BYTES;
            return i;
        }

        public int ReadBits(int n)
        {
            int result;
            if (_bitsCached >= n)
            {
                _bitsCached -= n;
                result = _cache >> _bitsCached & MaskBits(n);
                _position += n;
            }
            else
            {
                _position += n;
                int c = _cache & MaskBits(_bitsCached);
                int left = n - _bitsCached;
                _cache = ReadCache(false);
                _bitsCached = WORD_BITS - left;
                result = _cache >> _bitsCached & MaskBits(left) | c << left;
            }
            return result;
        }

        public int ReadBit()
        {
            int i;
            if (_bitsCached > 0)
            {
                _bitsCached--;
                i = _cache >> _bitsCached & 1;
                _position++;
            }
            else
            {
                _cache = ReadCache(false);
                _bitsCached = WORD_BITS - 1;
                _position++;
                i = _cache >> _bitsCached & 1;
            }
            return i;
        }

        public bool ReadBool()
        {
            return (ReadBit() & 0x1) != 0;
        }

        public int PeekBits(int n)
        {
            int ret;
            if (_bitsCached >= n)
            {
                ret = _cache >> _bitsCached - n & MaskBits(n);
            }
            else
            {
                //old cache
                int c = _cache & MaskBits(_bitsCached);
                n -= _bitsCached;
                //read next & combine
                ret = ReadCache(true) >> WORD_BITS - n & MaskBits(n) | c << n;
            }
            return ret;
        }

        public int PeekBit()
        {
            int ret;
            if (_bitsCached > 0)
            {
                ret = _cache >> _bitsCached - 1 & 1;
            }
            else
            {
                int word = ReadCache(true);
                ret = word >> WORD_BITS - 1 & 1;
            }
            return ret;
        }

        public void SkipBits(int n)
        {
            _position += n;
            if (n <= _bitsCached)
            {
                _bitsCached -= n;
            }
            else
            {
                n -= _bitsCached;
                while (n >= WORD_BITS)
                {
                    n -= WORD_BITS;
                    ReadCache(false);
                }
                if (n > 0)
                {
                    _cache = ReadCache(false);
                    _bitsCached = WORD_BITS - n;
                }
                else
                {
                    _cache = 0;
                    _bitsCached = 0;
                }
            }
        }

        public void SkipBit()
        {
            _position++;
            if (_bitsCached > 0)
            {
                _bitsCached--;
            }
            else
            {
                _cache = ReadCache(false);
                _bitsCached = WORD_BITS - 1;
            }
        }

        public int MaskBits(int n)
        {
            int i;
            if (n == 32) i = -1;
            else i = (1 << n) - 1;
            return i;
        }
    }
}
