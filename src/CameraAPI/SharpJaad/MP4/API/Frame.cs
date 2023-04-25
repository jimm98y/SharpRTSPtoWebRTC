using System;

namespace SharpJaad.MP4.API
{
    public class Frame : IComparable<Frame>
    {
        private readonly Type _type;
	    private readonly long _offset, _size;
        private readonly double _time;
        private byte[] _data;

        public Frame(Type type, long offset, long size, double time)
        {
            this._type = type;
            this._offset = offset;
            this._size = size;
            this._time = time;
        }

        public Type GetFrameType()
        {
            return _type;
        }

        public long GetOffset()
        {
            return _offset;
        }

        public long GetSize()
        {
            return _size;
        }

        public double GetTime()
        {
            return _time;
        }

        public void SetData(byte[] data)
        {
            this._data = data;
        }

        public byte[] GetData()
        {
            return _data;
        }

        public int CompareTo(Frame f)
        {
            double d = _time - f._time;
            //0 should not happen, since frames don't have the same timestamps
            return (d < 0) ? -1 : ((d > 0) ? 1 : 0);
        }
    }
}
