using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace UnsafeEcs.Core.DynamicBuffers
{
    public unsafe struct BufferChunk : IDisposable
    {
        public byte* ptr; // Buffer headers storage
        public int length; // Number of active buffers
        public int capacity; // Total capacity of the arrays
        public readonly int headerSize; // Size of BufferHeader struct
        public readonly int elementSize; // Size of buffer element type

        // Direct arrays instead of hashmaps
        public int* entityIds; // Maps buffer index -> entity id (length-sized)
        public int* bufferIndices; // Maps entity id -> buffer index (maxEntityId-sized)
        public int maxEntityId; // Current maximum entity ID
        public uint version;

        public BufferChunk(int elementSize, int initialCapacity, int maxInitialEntityId = 1024)
        {
            this.elementSize = elementSize;
            headerSize = UnsafeUtility.SizeOf<BufferHeader>();
            capacity = initialCapacity;
            length = 0;
            maxEntityId = maxInitialEntityId;
            version = 0;

            // Allocate memory for buffer headers
            ptr = (byte*)UnsafeUtility.Malloc(capacity * headerSize, UnsafeUtility.AlignOf<BufferHeader>(), Allocator.Persistent);

            // Allocate direct mapping arrays
            entityIds = (int*)UnsafeUtility.Malloc(capacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Persistent);

            // Initialize entity->buffer index mapping to -1 (no buffer)
            bufferIndices = (int*)UnsafeUtility.Malloc((maxEntityId + 1) * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Persistent);

            // Initialize all indices to -1 (indicating no component)
            for (var i = 0; i <= maxEntityId; i++) bufferIndices[i] = -1;
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

            if (entityIds != null)
            {
                UnsafeUtility.Free(entityIds, Allocator.Persistent);
                entityIds = null;
            }

            if (bufferIndices != null)
            {
                UnsafeUtility.Free(bufferIndices, Allocator.Persistent);
                bufferIndices = null;
            }
        }

        // Ensure the bufferIndices array can handle the given entity ID
        public void EnsureEntityCapacity(int entityId)
        {
            if (entityId <= maxEntityId)
                return;

            var newMaxEntityId = Math.Max(entityId, maxEntityId * 2);
            var newBufferIndices = (int*)UnsafeUtility.Malloc((newMaxEntityId + 1) * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Persistent);

            // Copy existing mappings
            UnsafeUtility.MemCpy(newBufferIndices, bufferIndices, (maxEntityId + 1) * sizeof(int));

            // Initialize new mappings to -1
            for (var i = maxEntityId + 1; i <= newMaxEntityId; i++) newBufferIndices[i] = -1;

            // Free old array and update references
            UnsafeUtility.Free(bufferIndices, Allocator.Persistent);
            bufferIndices = newBufferIndices;
            maxEntityId = newMaxEntityId;
        }

        public void Resize(int newCapacity)
        {
            if (newCapacity <= capacity) return;

            // Resize buffer headers array
            var newPtr = (byte*)UnsafeUtility.Malloc(newCapacity * headerSize, UnsafeUtility.AlignOf<BufferHeader>(), Allocator.Persistent);
            UnsafeUtility.MemCpy(newPtr, ptr, length * headerSize);
            UnsafeUtility.Free(ptr, Allocator.Persistent);
            ptr = newPtr;

            // Resize entityIds array
            var newEntityIds = (int*)UnsafeUtility.Malloc(newCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Persistent);
            UnsafeUtility.MemCpy(newEntityIds, entityIds, length * sizeof(int));
            UnsafeUtility.Free(entityIds, Allocator.Persistent);
            entityIds = newEntityIds;

            capacity = newCapacity;
        }

        // Initialize a new buffer at the specified index with default capacity
        public void InitializeBuffer(int index, int initialBufferCapacity = 8)
        {
            var header = (BufferHeader*)(ptr + index * headerSize);

            header->length = 0;
            header->capacity = initialBufferCapacity;
            header->pointer = (byte*)UnsafeUtility.Malloc(elementSize * initialBufferCapacity, UnsafeUtility.AlignOf<byte>(), Allocator.Persistent);
        }

        // Add a buffer for an entity
        public int Add(int entityId, int initialBufferCapacity = 8)
        {
            // Resize if needed
            if (length >= capacity)
                Resize(math.max(4, capacity * 2));

            // Ensure we have capacity for this entity ID
            EnsureEntityCapacity(entityId);

            // Initialize the buffer
            var bufferIndex = length;
            InitializeBuffer(bufferIndex, initialBufferCapacity);

            // Set up the mappings
            entityIds[bufferIndex] = entityId;
            bufferIndices[entityId] = bufferIndex;

            length++;
            version++;
            return bufferIndex;
        }

        // Remove a buffer for an entity
        public bool RemoveEntityBuffer(int entityId)
        {
            // Check if entity has a buffer
            if (entityId > maxEntityId || bufferIndices[entityId] < 0)
                return false;

            var index = bufferIndices[entityId];

            // Free the buffer memory
            var header = (BufferHeader*)(ptr + index * headerSize);
            if (header->pointer != null)
            {
                UnsafeUtility.Free(header->pointer, Allocator.Persistent);
                header->pointer = null;
            }

            // Handle the swap-remove operation
            var lastIndex = length - 1;

            if (index < lastIndex)
            {
                // Copy the last buffer header to the removed position
                var srcHeader = (BufferHeader*)(ptr + lastIndex * headerSize);
                UnsafeUtility.MemCpy(header, srcHeader, headerSize);

                // Update the mappings for the moved entity
                var lastEntityId = entityIds[lastIndex];
                entityIds[index] = lastEntityId;
                bufferIndices[lastEntityId] = index;
            }

            // Clear the mapping for the removed entity
            bufferIndices[entityId] = -1;
            length--;
            version++;

            return true;
        }

        // Get the buffer index for an entity
        public bool TryGetBufferIndex(int entityId, out int bufferIndex)
        {
            if (entityId <= maxEntityId)
            {
                bufferIndex = bufferIndices[entityId];
                return bufferIndex >= 0;
            }

            bufferIndex = -1;
            return false;
        }

        // Check if an entity has a buffer
        public bool HasBuffer(int entityId)
        {
            return entityId <= maxEntityId && bufferIndices[entityId] >= 0;
        }
    }
}