using System.Collections.Generic;
using System.Linq;

namespace SharpJaad.MP4.OD
{
    /**
     * The abstract base class and factory for all descriptors (defined in ISO
     * 14496-1 as 'ObjectDescriptors').
     *
     * @author in-somnia
     */
    public abstract class Descriptor
    {
        public const int TYPE_OBJECT_DESCRIPTOR = 1;
        public const int TYPE_INITIAL_OBJECT_DESCRIPTOR = 2;
        public const int TYPE_ES_DESCRIPTOR = 3;
        public const int TYPE_DECODER_CONFIG_DESCRIPTOR = 4;
        public const int TYPE_DECODER_SPECIFIC_INFO = 5;
        public const int TYPE_SL_CONFIG_DESCRIPTOR = 6;
        public const int TYPE_ES_ID_INC = 14;
        public const int TYPE_MP4_INITIAL_OBJECT_DESCRIPTOR = 16;

        public static Descriptor CreateDescriptor(MP4InputStream input)
        {
            //read tag and size
            int type = input.Read();
            int read = 1;
            int size = 0;
            int b = 0;
            do
            {
                b = input.Read();
                size <<= 7;
                size |= b & 0x7f;
                read++;
            }
            while ((b & 0x80) == 0x80);

            //create descriptor
            Descriptor desc = ForTag(type);
            desc._type = type;
            desc._size = size;
            desc._start = input.GetOffset();

            //decode
            desc.Decode(input);
            //skip remaining bytes
            long remaining = size - (input.GetOffset() - desc._start);
            if (remaining > 0)
            {
                //Logger.getLogger("MP4 Boxes").log(Level.INFO, "Descriptor: bytes left: {0}, offset: {1}", new long[]{remaining, input.getOffset()});
                input.SkipBytes(remaining);
            }
            desc._size += read; //include type and size fields

            return desc;
        }

        private static Descriptor ForTag(int tag)
        {
            Descriptor desc;
            switch (tag)
            {
                case TYPE_OBJECT_DESCRIPTOR:
                    desc = new ObjectDescriptor();
                    break;
                case TYPE_INITIAL_OBJECT_DESCRIPTOR:
                case TYPE_MP4_INITIAL_OBJECT_DESCRIPTOR:
                    desc = new InitialObjectDescriptor();
                    break;
                case TYPE_ES_DESCRIPTOR:
                    desc = new ESDescriptor();
                    break;
                case TYPE_DECODER_CONFIG_DESCRIPTOR:
                    desc = new DecoderConfigDescriptor();
                    break;
                case TYPE_DECODER_SPECIFIC_INFO:
                    desc = new DecoderSpecificInfo();
                    break;
                case TYPE_SL_CONFIG_DESCRIPTOR:
                //desc = new SLConfigDescriptor();
                //break;
                default:
                    //Logger.getLogger("MP4 Boxes").log(Level.INFO, "Unknown descriptor type: {0}", tag);
                    desc = new UnknownDescriptor();
                    break;
            }
            return desc;
        }
        protected int _type, _size;
        protected long _start;
        private List<Descriptor> _children;

        protected Descriptor()
        {
            _children = new List<Descriptor>();
        }

        public abstract void Decode(MP4InputStream input);

        //children
        protected void ReadChildren(MP4InputStream input)
        {
            Descriptor desc;
            while ((_size - (input.GetOffset() - _start)) > 0)
            {
                desc = CreateDescriptor(input);
                _children.Add(desc);
            }
        }

        public List<Descriptor> GetChildren()
        {
            return _children.ToList();
        }

        //getter
        public int GetDescriptorType()
        {
            return _type;
        }

        public int GetSize()
        {
            return _size;
        }
    }
}
