#if !NET35
namespace System
{
    internal struct Span<T>
    {
        private byte[] array;
        private int start;
        private int length;

        public Span(byte[] bytes, int index, int length)
        {
            this.array = bytes;
            this.start = index;
            this.length = length;
        }

        public int Length => length;



        public byte this[int index]
        {
            get { return array[start + index]; }
            set { array[start + index] = value; }
        }

        internal uint ReadUInt16()
        {
            return BitConverter.ToUInt16(array, start);
        }

        public unsafe void Write(uint v)
        {
            fixed (byte* ptr = array)
            {
                *(uint*)ptr = v;
            }
        }
        public unsafe void Write(ushort v)
        {
            fixed (byte* ptr = array)
            {
                *(ushort*)ptr = v;
            }
        }

        public Span<byte> Slice(int _index)
        {
            return new Span<byte>(array, this.start + _index, this.length - _index);
        }

        public uint ReadUInt32()
        {
            return BitConverter.ToUInt32(array, start);
        }

        public Span<byte> Slice(int index, int length)
        {
            return new Span<byte>(array, this.start + index, length);
        }
    }
}
#endif