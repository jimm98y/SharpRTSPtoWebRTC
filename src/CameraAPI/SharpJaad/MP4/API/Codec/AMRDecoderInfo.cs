using SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec;

namespace SharpJaad.MP4.API.Codec
{
    public class AMRDecoderInfo : DecoderInfo 
	{
		private AMRSpecificBox box;

		public AMRDecoderInfo(CodecSpecificBox box)
		{
			this.box = (AMRSpecificBox) box;
		}

		public int GetDecoderVersion() 
		{
			return box.GetDecoderVersion();
		}

		public long GetVendor() 
		{
			return box.GetVendor();
		}

		public int GetModeSet() 
		{
			return box.GetModeSet();
		}

		public int GetModeChangePeriod() 
		{
			return box.GetModeChangePeriod();
		}

		public int GetFramesPerSample() 
		{
			return box.GetFramesPerSample();
		}
	}
}
