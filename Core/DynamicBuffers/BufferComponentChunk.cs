using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnsafeEcs.Core.DynamicBuffers
{
    public unsafe struct BufferComponentChunk : IDisposable
    {
        public byte* ptr;
        public int length;
        public int capacity;
        public readonly int headerSize;
        public readonly int elementSize;
        public UnsafeHashMap<int, int> entityToIndex;
        public UnsafeHashMap<int, int> indexToEntity;

        public BufferComponentChunk(int elementSize, int initialCapacity)
        {
            this.elementSize = elementSize;
            headerSize = UnsafeUtility.SizeOf<BufferHeader>();
            capacity = initialCapacity;
            length = 0;

            // Allocate memory for buffer headers
            ptr = (byte*)UnsafeUtility.Malloc(
                capacity * headerSize,
                UnsafeUtility.AlignOf<BufferHeader>(),
                Allocator.Persistent);

            entityToIndex = new UnsafeHashMap<int, int>(initialCapacity, Allocator.Persistent);
            indexToEntity = new UnsafeHashMap<int, int>(initialCapacity, Allocator.Persistent);
        }

        public void Dispose()
        {
            // Free all buffer memory
            for (var i = 0; i < length; i++)
            {
                var header = (BufferHeader*)(ptr + i * headerSize);
                if (header->pointer != null)
                {
                    UnsafeUtility.Free(header->pointer, Allocator.Persistent);
                    header->pointer = null;
                }
            }

            if (ptr != null)
            {
                UnsafeUtility.Free(ptr, Allocator.Persistent);
                ptr = null;
            }

            entityToIndex.Dispose();
            indexToEntity.Dispose();
        }


        public void Resize(int newCapacity)
        {
            if (newCapacity <= capacity) return;

            var newPtr = (byte*)UnsafeUtility.Malloc(newCapacity * headerSize, UnsafeUtility.AlignOf<BufferHeader>(), Allocator.Persistent);
            UnsafeUtility.MemCpy(newPtr, ptr, length * headerSize);
            UnsafeUtility.Free(ptr, Allocator.Persistent);

            ptr = newPtr;
            capacity = newCapacity;
        }

        // Initialize a new buffer at the specified index with default capacity
        public void InitializeBuffer(int index, int initialBufferCapacity = 8)
        {
            var header = (BufferHeader*)(ptr + index * headerSize);

            header->length = 0;
            header->capacity = initialBufferCapacity;
            header->pointer = (byte*)UnsafeUtility.Malloc(
                elementSize * initialBufferCapacity,
                UnsafeUtility.AlignOf<byte>(),
                Allocator.Persistent);
        }
    }
}