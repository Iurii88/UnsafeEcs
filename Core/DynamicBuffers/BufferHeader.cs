namespace UnsafeEcs.Core.DynamicBuffers
{
    internal unsafe struct BufferHeader
    {
        public byte* pointer;
        public int length;
        public int capacity;
    }
}