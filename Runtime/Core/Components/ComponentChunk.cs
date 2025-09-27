using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Core.Components
{
    public unsafe struct ComponentChunk : IDisposable
    {
        [NativeDisableUnsafePtrRestriction] public void* ptr; // Component data
        public int length; // Current number of components
        public int capacity; // Total allocated capacity
        public readonly int componentSize; // Size of each component in bytes

        // Direct mapping arrays instead of HashMaps
        [NativeDisableUnsafePtrRestriction] public int* entityIds; // Stores entity ID at the same index as the component
        [NativeDisableUnsafePtrRestriction] public int* componentIndices; // Maps entity ID -> component index (sparse set)
        public int maxEntityId; // Tracks the highest entity ID to resize componentIndices array
        public uint version;

        public int typeIndex;
        [NativeDisableUnsafePtrRestriction] public readonly EntityManager* managerPtr;

        public ComponentChunk(int componentSize, int capacity, int typeIndex, EntityManager* managerPtr)
        {
            this.componentSize = componentSize;
            this.capacity = capacity;
            this.managerPtr = managerPtr;
            this.typeIndex = typeIndex;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int entityId, bool clearComponent = false)
        {
            if (length >= capacity)
                Resize(math.max(capacity * 2, 4)); // Double capacity or use minimum size

            EnsureEntityCapacity(entityId);

            // Store the entity ID and update the index mapping
            entityIds[length] = entityId;
            componentIndices[entityId] = length;

            if (clearComponent)
            {
                // Allocate memory for the component based on size
                void* componentPtr = (byte*)ptr + length * componentSize;
                // Initialize memory to zero
                UnsafeUtility.MemClear(componentPtr, componentSize);
            }

            managerPtr->entityArchetypes.Ptr[entityId].SetComponent(typeIndex);

            length++;
            version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            managerPtr->entityArchetypes.Ptr[entityId].SetComponent(typeIndex);

            length++;
            version++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            managerPtr->entityArchetypes.Ptr[entityId].RemoveComponent(typeIndex);

            length--;
            version++;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetComponentPtr(int entityId)
        {
            if (entityId > maxEntityId || componentIndices[entityId] < 0)
                return null;

            return (byte*)ptr + componentIndices[entityId] * componentSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(int entityId)
        {
            return entityId <= maxEntityId && componentIndices[entityId] >= 0;
        }
    }
}