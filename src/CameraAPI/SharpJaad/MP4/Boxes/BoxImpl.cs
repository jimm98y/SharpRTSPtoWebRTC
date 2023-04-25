using System.Collections.Generic;
using System.Linq;

namespace SharpJaad.MP4.Boxes
{
    public class BoxImpl : Box
    {
        private readonly string name;
        protected long size, type, offset;
        protected Box parent;
        protected readonly List<Box> children;

        public BoxImpl(string name)
        {
            this.name = name;

            children = new List<Box>(4);
        }

        public void SetParams(Box parent, long size, long type, long offset)
        {
            this.size = size;
            this.type = type;
            this.parent = parent;
            this.offset = offset;
        }

        protected long GetLeft(MP4InputStream input)
        {
            return (offset + size) - input.getOffset();
        }

        /**
         * Decodes the given input stream by reading this box and all of its
         * children (if any).
         * 
         * @param in an input stream
         * @throws IOException if an error occurs while reading
         */
        public virtual void decode(MP4InputStream input)
        {
        }

        // formerly GetType
        public long GetBoxType()
        {
            return type;
        }

        public long GetSize()
        {
            return size;
        }

        public long GetOffset()
        {
            return offset;
        }

        public Box GetParent()
        {
            return parent;
        }

        public string GetName()
        {
            return name;
        }

        public override string ToString()
        {
            return name + " [" + BoxFactory.TypeToString(type) + "]";
        }

        //container methods
        public bool HasChildren()
        {
            return children.Count > 0;
        }

        public bool HasChild(long type)
        {
            bool b = false;
            foreach (Box box in children)
            {
                if (box.GetBoxType() == type)
                {
                    b = true;
                    break;
                }
            }
            return b;
        }

        public Box GetChild(long type)
        {
            Box box = null, b = null;
            int i = 0;
            while (box == null && i < children.Count)
            {
                b = children[i];
                if (b.GetBoxType() == type) box = b;
                i++;
            }
            return box;
        }

        public List<Box> GetChildren()
        {
            return children.ToList<Box>();
        }

        public List<Box> GetChildren(long type)
        {
            List<Box> l = new List<Box>();
            foreach (Box box in children)
            {
                if (box.GetBoxType() == type) l.Add(box);
            }
            return l;
        }

        public void ReadChildren(MP4InputStream input)
        {
            Box box;
            while (input.getOffset() < (offset + size))
            {
                box = BoxFactory.ParseBox(this, input);
                children.Add(box);
            }
        }

        public void ReadChildren(MP4InputStream input, int len)
        {
            Box box;
            for (int i = 0; i < len; i++)
            {
                box = BoxFactory.ParseBox(this, input);
                children.Add(box);
            }
        }
    }
}