using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace UnsafeEcs.Core.Components
{
    public unsafe partial struct ComponentChunk : IDisposable
    {
        public void* ptr;
        public int length;
        public int capacity;
        public readonly int componentSize;
        public UnsafeHashMap<int, int> entityToIndex;
        public UnsafeHashMap<int, int> indexToEntity;

        public ComponentChunk(int componentSize, int capacity)
        {
            this.componentSize = componentSize;
            this.capacity = capacity;
            length = 0;
            ptr = UnsafeUtility.Malloc(capacity * componentSize, 16, Allocator.Persistent);
            entityToIndex = new UnsafeHashMap<int, int>(capacity, Allocator.Persistent);
            indexToEntity = new UnsafeHashMap<int, int>(capacity, Allocator.Persistent);
        }

        public void Dispose()
        {
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

            var newPtr = UnsafeUtility.Malloc(newCapacity * componentSize, 16, Allocator.Persistent);
            UnsafeUtility.MemCpy(newPtr, ptr, math.min(length, capacity) * componentSize);
            UnsafeUtility.Free(ptr, Allocator.Persistent);

            ptr = newPtr;
            capacity = newCapacity;
        }
    }
}