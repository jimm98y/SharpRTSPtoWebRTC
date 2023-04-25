using System.IO;

namespace SharpJaad.ADTS
{
    public class ADTSDemultiplexer {

		private const int MAXIMUM_FRAME_SIZE = 6144;
		private PushbackInputStream _input;
		private DataInputStream _din;
		private bool _first;
		private ADTSFrame _frame;

		public ADTSDemultiplexer(Stream input) 
		{
			this._input = new PushbackInputStream(input);
			_din = new DataInputStream(this._input);
			_first = true;
			if(!FindNextFrame()) throw new IOException("no ADTS header found");
		}

		public byte[] GetDecoderSpecificInfo() 
		{
			return _frame.CreateDecoderSpecificInfo();
		}

		public byte[] ReadNextFrame()
		{
			if(_first) _first = false;
			else FindNextFrame();

			byte[] b = new byte[_frame.GetFrameLength()];
			_din.ReadFully(b);
			return b;
		}

		private bool FindNextFrame()
		{
            //find next ADTS ID
            bool found = false;
			int left = MAXIMUM_FRAME_SIZE;
			int i;
			while(!found&&left>0) {
				i = _input.read();
				left--;
				if(i==0xFF) {
					i = _input.read();
					if(((i>>4)&0xF)==0xF) found = true;
                    _input.unread(i);
				}
			}

			if(found) _frame = new ADTSFrame(_din);
			return found;
		}

		public int GetSampleFrequency() 
		{
			return _frame.GetSampleFrequency();
		}

		public int GetChannelCount() 
		{
			return _frame.GetChannelCount();
		}
	}
}
