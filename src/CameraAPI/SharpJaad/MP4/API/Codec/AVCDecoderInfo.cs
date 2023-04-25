using SharpJaad.MP4.Boxes.Impl.SampleEntries.Codec;

namespace SharpJaad.MP4.API.Codec
{
    public class AVCDecoderInfo : DecoderInfo 
	{
		private AVCSpecificBox box;

		public AVCDecoderInfo(CodecSpecificBox box) 
		{
			this.box = (AVCSpecificBox) box;
		}

		public int GetConfigurationVersion() 
		{
			return box.GetConfigurationVersion();
		}

		public int GetProfile() 
		{
			return box.GetProfile();
		}

		public byte GetProfileCompatibility() 
		{
			return box.GetProfileCompatibility();
		}

		public int GetLevel() 
		{
			return box.GetLevel();
		}

		public int GetLengthSize() 
		{
			return box.GetLengthSize();
		}

		public byte[][] GetSequenceParameterSetNALUnits()
		{
			return box.GetSequenceParameterSetNALUnits();
		}

		public byte[][] GetPictureParameterSetNALUnits()
		{
			return box.GetPictureParameterSetNALUnits();
		}
	}
}
