namespace UnsafeEcs.Core.DynamicBuffers
{
    public unsafe struct BufferHeader
    {
        public byte* pointer;
        public int length;
        public int capacity;
    }
}