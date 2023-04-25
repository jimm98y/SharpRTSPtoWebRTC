using SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec;

namespace SharpJaad.MP4.API.Codec
{
    public class AC3DecoderInfo : DecoderInfo
	{
		private AC3SpecificBox box;

		public AC3DecoderInfo(CodecSpecificBox box) 
		{
			this.box = (AC3SpecificBox) box;
		}

		public bool IsLfeon() 
		{
			return box.IsLfeon();
		}

		public int GetFscod() 
		{
			return box.GetFscod();
		}

		public int GetBsmod() 
		{
			return box.GetBsmod();
		}

		public int GetBsid() 
		{
			return box.GetBsid();
		}

		public int GetBitRateCode()
		{
			return box.GetBitRateCode();
		}

		public int GetAcmod()
		{
			return box.GetAcmod();
		}
	}
}
