using System.Collections.Generic;
using System.Linq;

namespace SharpJaad.MP4.API
{
    public class ID3Tag
    {
        private const int ID3_TAG = 4801587; //'ID3'
        private const int SUPPORTED_VERSION = 4; //id3v2.4
        private readonly List<ID3Frame> _frames;
        private readonly int _tag, _flags, _len;

        public ID3Tag(DataInputStream input)
        {
            _frames = new List<ID3Frame>();

		    //id3v2 header
		    _tag = (input.ReadByte()<<16)|(input.ReadByte()<<8)| input.ReadByte(); //'ID3'
            int majorVersion = input.ReadByte();
            input.ReadByte(); //revision
            _flags = input.ReadByte();
            _len = ReadSynch(input);

		    if(_tag==ID3_TAG&&majorVersion<=SUPPORTED_VERSION) 
            {
			    if((_flags&0x40)==0x40) 
                {
				    //extended header; TODO: parse
				    int extSize = ReadSynch(input);

                    //input.skipBytes(extSize-6);
                    for (int i = 0; i < extSize-6; i++)
                    {
                        input.ReadByte();
                    }
                }

                //read all id3 frames
                int left = _len;
                ID3Frame frame;
			    while(left>0) 
                {
				    frame = new ID3Frame(input);
                    _frames.Add(frame);
				    left -= (int)frame.GetSize();
			    }
		    }
	    }

	    public List<ID3Frame> GetFrames()
        {
            return _frames.ToList();
        }

        public static int ReadSynch(DataInputStream input)
        {
		    int x = 0;
		    for(int i = 0; i<4; i++) 
            {
                x |= (input.ReadByte() & 0x7F);
            }
		    return x;
        }
    }
}
