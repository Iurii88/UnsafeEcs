using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace UnsafeEcs.Core.Components
{
    public unsafe struct ComponentChunk : IDisposable
    {
        public void* ptr; // Component data
        public int length; // Current number of components
        public int capacity; // Total allocated capacity
        public readonly int componentSize; // Size of each component in bytes

        // Direct mapping arrays instead of HashMaps
        public int* entityIds; // Stores entity ID at the same index as the component
        public int* componentIndices; // Maps entity ID -> component index (sparse set)
        public int maxEntityId; // Tracks the highest entity ID to resize componentIndices array

        public uint version;

        public ComponentChunk(int componentSize, int capacity)
        {
            this.componentSize = componentSize;
            this.capacity = capacity;
            length = 0;
            maxEntityId = -1;
            version = 0;

            // Allocate component data buffer
            ptr = UnsafeUtility.Malloc(capacity * componentSize, 16, Allocator.Persistent);

            // Allocate entity ID tracking array
            entityIds = (int*)UnsafeUtility.Malloc(capacity * sizeof(int), 16, Allocator.Persistent);

            // Initially allocate a small componentIndices array - will grow as needed
            componentIndices = (int*)UnsafeUtility.Malloc(16 * sizeof(int), 16, Allocator.Persistent);
            // Initialize all indices to -1 (indicating no component for that entity)
            UnsafeUtility.MemSet(componentIndices, 0xFF, 16 * sizeof(int));
        }

        public void Dispose()
        {
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

            if (componentIndices != null)
            {
                UnsafeUtility.Free(componentIndices, Allocator.Persistent);
                componentIndices = null;
            }
        }

        public void Resize(int newCapacity)
        {
            if (newCapacity <= capacity) return;

            // Resize component data
            var newPtr = UnsafeUtility.Malloc(newCapacity * componentSize, 16, Allocator.Persistent);
            UnsafeUtility.MemCpy(newPtr, ptr, length * componentSize);
            UnsafeUtility.Free(ptr, Allocator.Persistent);
            ptr = newPtr;

            // Resize entity ID array
            var newEntityIds = (int*)UnsafeUtility.Malloc(newCapacity * sizeof(int), 16, Allocator.Persistent);
            UnsafeUtility.MemCpy(newEntityIds, entityIds, length * sizeof(int));
            UnsafeUtility.Free(entityIds, Allocator.Persistent);
            entityIds = newEntityIds;

            capacity = newCapacity;
        }

        // Ensure componentIndices array can hold a given entity ID
        public void EnsureEntityCapacity(int entityId)
        {
            if (entityId <= maxEntityId) return;

            var currentSize = maxEntityId + 1;
            var newSize = entityId + 1;

            var newIndices = (int*)UnsafeUtility.Malloc(newSize * sizeof(int), 16, Allocator.Persistent);

            // Copy existing data
            if (currentSize > 0)
                UnsafeUtility.MemCpy(newIndices, componentIndices, currentSize * sizeof(int));

            // Initialize new elements to -1
            UnsafeUtility.MemSet((byte*)newIndices + currentSize * sizeof(int), 0xFF, (newSize - currentSize) * sizeof(int));

            UnsafeUtility.Free(componentIndices, Allocator.Persistent);
            componentIndices = newIndices;
            maxEntityId = newSize - 1;
        }

        public void Add(int entityId, void* componentData)
        {
            // Check if resize is needed
            if (length >= capacity)
                Resize(math.max(capacity * 2, 4)); // Double capacity or use minimum size

            EnsureEntityCapacity(entityId);

            // Store the entity ID and update the index mapping
            entityIds[length] = entityId;
            componentIndices[entityId] = length;

            // Copy component data
            UnsafeUtility.MemCpy((byte*)ptr + length * componentSize, componentData, componentSize);
            length++;
            version++;
        }

        public bool Remove(int entityId)
        {
            if (entityId > maxEntityId || componentIndices[entityId] < 0)
                return false;

            var index = componentIndices[entityId];
            var lastIndex = length - 1;

            // If this isn't the last element, move the last element to fill the gap
            if (index < lastIndex)
            {
                // Copy the last component data to the removed slot
                UnsafeUtility.MemCpy((byte*)ptr + index * componentSize, (byte*)ptr + lastIndex * componentSize, componentSize);

                // Update indices for the moved entity
                var lastEntityId = entityIds[lastIndex];
                entityIds[index] = lastEntityId;
                componentIndices[lastEntityId] = index;
            }

            // Mark this entity as having no component
            componentIndices[entityId] = -1;
            length--;
            version++;
            return true;
        }

        public void* GetComponentPtr(int entityId)
        {
            if (entityId > maxEntityId || componentIndices[entityId] < 0)
                return null;

            return (byte*)ptr + componentIndices[entityId] * componentSize;
        }

        public bool HasComponent(int entityId)
        {
            return entityId <= maxEntityId && componentIndices[entityId] >= 0;
        }
    }
}