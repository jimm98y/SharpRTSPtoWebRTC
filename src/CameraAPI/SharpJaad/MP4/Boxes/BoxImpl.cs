using System.Collections.Generic;
using System.Linq;

namespace SharpJaad.MP4.Boxes
{
    public abstract class BoxImpl : Box
    {
        private readonly string _name;
        protected long _size, _type, _offset;
        protected Box _parent;
        protected readonly List<Box> _children;

        public BoxImpl(string name)
        {
            this._name = name;

            _children = new List<Box>(4);
        }

        public void SetParams(Box parent, long size, long type, long offset)
        {
            this._size = size;
            this._type = type;
            this._parent = parent;
            this._offset = offset;
        }

        protected long GetLeft(MP4InputStream input)
        {
            return (_offset + _size) - input.GetOffset();
        }

        /**
         * Decodes the given input stream by reading this box and all of its
         * children (if any).
         * 
         * @param in an input stream
         * @throws IOException if an error occurs while reading
         */
        public abstract void Decode(MP4InputStream input);

        // formerly GetType
        public long GetBoxType()
        {
            return _type;
        }

        public long GetSize()
        {
            return _size;
        }

        public long GetOffset()
        {
            return _offset;
        }

        public Box GetParent()
        {
            return _parent;
        }

        public string GetName()
        {
            return _name;
        }

        public override string ToString()
        {
            return _name + " [" + BoxFactory.TypeToString(_type) + "]";
        }

        //container methods
        public bool HasChildren()
        {
            return _children.Count > 0;
        }

        public bool HasChild(long type)
        {
            bool b = false;
            foreach (Box box in _children)
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
            while (box == null && i < _children.Count)
            {
                b = _children[i];
                if (b.GetBoxType() == type) box = b;
                i++;
            }
            return box;
        }

        public List<Box> GetChildren()
        {
            return _children.ToList<Box>();
        }

        public List<Box> GetChildren(long type)
        {
            List<Box> l = new List<Box>();
            foreach (Box box in _children)
            {
                if (box.GetBoxType() == type) l.Add(box);
            }
            return l;
        }

        public void ReadChildren(MP4InputStream input)
        {
            Box box;
            while (input.GetOffset() < (_offset + _size))
            {
                box = BoxFactory.ParseBox(this, input);
                _children.Add(box);
            }
        }

        public void ReadChildren(MP4InputStream input, int len)
        {
            Box box;
            for (int i = 0; i < len; i++)
            {
                box = BoxFactory.ParseBox(this, input);
                _children.Add(box);
            }
        }
    }
}