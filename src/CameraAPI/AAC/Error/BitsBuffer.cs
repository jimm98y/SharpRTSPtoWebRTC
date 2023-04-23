using CameraAPI.AAC.Syntax;

namespace CameraAPI.AAC.Error
{
	public class BitsBuffer
	{
		public int _bufa;

		public int _bufb;

		public int _len;

		public BitsBuffer()
		{
			_len = 0;
		}

		public int GetLength()
		{
			return _len;
		}

		public int ShowBits(int bits)
		{
			if (bits == 0) return 0;
			if (_len <= 32)
			{
				//huffman_spectral_data_2 needs to read more than may be available,
				//bits maybe > len, deliver 0 than
				if (_len >= bits) return (int)((_bufa >> (_len - bits)) & (0xFFFFFFFF >> (32 - bits)));
				else return (int)((_bufa << (bits - _len)) & (0xFFFFFFFF >> (32 - bits)));
			}
			else
			{
				if ((_len - bits) < 32) return (int)((_bufb & (0xFFFFFFFF >> (64 - _len))) << (bits - _len + 32)) | (_bufa >> (_len - bits));
				else return (int)((_bufb >> (_len - bits - 32)) & (0xFFFFFFFF >> (32 - bits)));
			}
		}

		public bool FlushBits(int bits)
		{
			_len -= bits;

			bool b;
			if (_len < 0)
			{
				_len = 0;
				b = false;
			}
			else b = true;
			return b;
		}

		public int GetBits(int n)
		{
			int i = ShowBits(n);
			if (!FlushBits(n)) i = -1;
			return i;
		}

		public int GetBit()
		{
			int i = ShowBits(1);
			if (!FlushBits(1)) i = -1;
			return i;
		}

		public void RewindReverse()
		{
			if (_len == 0) return;
			int[] i = HCR.RewindReverse64(_bufb, _bufa, _len);
			_bufb = i[0];
			_bufa = i[1];
		}

		//merge bits of a to b
		public void ConcatBits(BitsBuffer a)
		{
			if (a._len == 0) return;
			int al = a._bufa;
			int ah = a._bufb;

			int bl, bh;
			if (_len > 32)
			{
				//mask off superfluous high b bits
				bl = _bufa;
				bh = _bufb & ((1 << (_len - 32)) - 1);
				//left shift a len bits
				ah = al << (_len - 32);
				al = 0;
			}
			else
			{
				bl = _bufa & ((1 << (_len)) - 1);
				bh = 0;
				ah = (ah << (_len)) | (al >> (32 - _len));
				al = al << _len;
			}

			//merge
			_bufa = bl | al;
			_bufb = bh | ah;

			_len += a._len;
		}

		public void ReadSegment(int segwidth, BitStream input)
		{
			_len = segwidth;

			if (segwidth > 32)
			{
				_bufb = input.ReadBits(segwidth - 32);
				_bufa = input.ReadBits(32);
			}
			else
			{
				_bufa = input.ReadBits(segwidth);
				_bufb = 0;
			}
		}
	}
}
