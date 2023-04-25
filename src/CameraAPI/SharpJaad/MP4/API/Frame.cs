using System;

namespace SharpJaad.MP4.API
{
    public class Frame : IComparable<Frame>
    {
        private readonly Type type;
	    private readonly long offset, size;
        private readonly double time;
        private byte[] data;

        public Frame(Type type, long offset, long size, double time)
        {
            this.type = type;
            this.offset = offset;
            this.size = size;
            this.time = time;
        }

        public Type GetType()
        {
            return type;
        }

        public long GetOffset()
        {
            return offset;
        }

        public long GetSize()
        {
            return size;
        }

        public double GetTime()
        {
            return time;
        }

        public void SetData(byte[] data)
        {
            this.data = data;
        }

        public byte[] GetData()
        {
            return data;
        }

        public int CompareTo(Frame f)
        {
            double d = time - f.time;
            //0 should not happen, since frames don't have the same timestamps
            return (d < 0) ? -1 : ((d > 0) ? 1 : 0);
        }
    }
}
