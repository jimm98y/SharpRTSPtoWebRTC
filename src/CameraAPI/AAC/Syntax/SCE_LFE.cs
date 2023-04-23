namespace CameraAPI.AAC.Syntax
{
    public class SCE_LFE : Element
    {
		private ICStream _ics;

		public SCE_LFE(DecoderConfig config)
		{
			_ics = new ICStream(config);
		}

		public void Decode(BitStream input, DecoderConfig conf)
		{
			ReadElementInstanceTag(input);
			_ics.Decode(input, false, conf);
		}

		public ICStream GetICStream() 
		{
			return _ics;
		}
    }
}
