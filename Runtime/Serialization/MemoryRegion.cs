using Unity.Collections.LowLevel.Unsafe;

namespace UnsafeEcs.Serialization
{
    /// <summary>
    ///     Provides a unified memory management system for serialization and deserialization operations.
    /// </summary>
    public unsafe struct MemoryRegion
    {
        // Pointer to the memory region
        [NativeDisableUnsafePtrRestriction] public readonly byte* ptr;

        // Total allocated size
        public int length;

        public MemoryRegion(void* dataPtr, int length)
        {
            ptr = (byte*)dataPtr;
            this.length = length;
        }
    }
}