using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct EntityManager
    {
        public DynamicBuffer<T> AddBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            var typeIndex = ComponentTypeManager.GetTypeIndex<T>();
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            archetype.componentBits.SetComponent(typeIndex);

            if (!bufferChunks.TryGetValue(typeIndex, out var chunk))
            {
                var elementSize = UnsafeUtility.SizeOf<T>();
                chunk = new BufferComponentChunk(elementSize, InitialEntityCapacity);
            }

            if (chunk.length >= chunk.capacity)
                chunk.Resize(math.max(4, chunk.capacity * 2));

            // Initialize the buffer
            chunk.InitializeBuffer(chunk.length);

            chunk.entityToIndex.Add(entity.id, chunk.length);
            chunk.indexToEntity.Add(chunk.length, entity.id);

            // Create and return the DynamicBuffer
            var header = (BufferHeader*)(chunk.ptr + chunk.length * chunk.headerSize);
            var buffer = new DynamicBuffer<T>(header);

            chunk.length++;
            bufferChunks[typeIndex] = chunk;
            IncrementComponentVersion(typeIndex);

            return buffer;
        }

        public DynamicBuffer<T> AddBuffer<T>(Entity entity, T[] initialData) where T : unmanaged, IBufferElement
        {
            var buffer = AddBuffer<T>(entity);
            buffer.CopyFrom(initialData);
            return buffer;
        }

        public DynamicBuffer<T> GetBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            var typeIndex = ComponentTypeManager.GetTypeIndex<T>();

            if (!bufferChunks.TryGetValue(typeIndex, out var chunk))
                throw new InvalidOperationException($"Entity does not have buffer component of type {typeof(T).Name}");

            if (!chunk.entityToIndex.TryGetValue(entity.id, out var index))
                throw new InvalidOperationException($"Entity does not have buffer component of type {typeof(T).Name}");

            var header = (BufferHeader*)(chunk.ptr + index * chunk.headerSize);
            return new DynamicBuffer<T>(header);
        }

        public void RemoveBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            var typeIndex = ComponentTypeManager.GetTypeIndex<T>();

            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            archetype.RemoveComponent(typeIndex);

            if (!bufferChunks.TryGetValue(typeIndex, out var chunk))
                return;

            if (!chunk.entityToIndex.TryGetValue(entity.id, out var index))
                return;

            // Free the buffer memory
            var header = (BufferHeader*)(chunk.ptr + index * chunk.headerSize);
            if (header->pointer != null)
            {
                UnsafeUtility.Free(header->pointer, Allocator.Persistent);
                header->pointer = null;
            }

            // Handle the swap-remove operation
            var last = chunk.length - 1;
            if (index < last)
            {
                // Copy the last buffer header to the removed position
                var srcHeader = (BufferHeader*)(chunk.ptr + last * chunk.headerSize);
                UnsafeUtility.MemCpy(header, srcHeader, chunk.headerSize);

                var lastEntity = chunk.indexToEntity[last];
                chunk.entityToIndex[lastEntity] = index;
                chunk.indexToEntity.Remove(last);
                chunk.indexToEntity[index] = lastEntity;
            }
            else
            {
                chunk.indexToEntity.Remove(last);
            }

            chunk.entityToIndex.Remove(entity.id);
            chunk.length--;

            bufferChunks[typeIndex] = chunk;
            IncrementComponentVersion(typeIndex);
        }

        public bool HasBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
            if (!IsEntityAlive(entity))
                return false;

            var typeIndex = ComponentTypeManager.GetTypeIndex<T>();
            return bufferChunks.TryGetValue(typeIndex, out var chunk) &&
                   chunk.ptr != null &&
                   chunk.entityToIndex.ContainsKey(entity.id);
        }

        private void DestroyEntityBuffers(Entity entity)
        {
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            foreach (var componentIndex in archetype.componentBits)
            {
                if (bufferChunks.TryGetValue(componentIndex, out var bufferChunk) &&
                    bufferChunk.entityToIndex.TryGetValue(entity.id, out var index))
                {
                    // Free the buffer memory
                    var header = (BufferHeader*)(bufferChunk.ptr + index * bufferChunk.headerSize);
                    if (header->pointer != null)
                    {
                        UnsafeUtility.Free(header->pointer, Allocator.Persistent);
                        header->pointer = null;
                    }
                }
            }
        }

        public bool TryGetBuffer<T>(Entity entity, out DynamicBuffer<T> buffer) where T : unmanaged, IBufferElement
        {
            buffer = default;

            if (!IsEntityAlive(entity))
                return false;

            var typeIndex = ComponentTypeManager.GetTypeIndex<T>();

            if (!bufferChunks.TryGetValue(typeIndex, out var chunk) ||
                !chunk.entityToIndex.TryGetValue(entity.id, out var index))
                return false;

            var header = (BufferHeader*)(chunk.ptr + index * chunk.headerSize);
            buffer = new DynamicBuffer<T>(header);
            return true;
        }

        // Get or create a buffer component on an entity
        public DynamicBuffer<T> GetOrCreateBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            if (TryGetBuffer<T>(entity, out var buffer))
                return buffer;

            return AddBuffer<T>(entity);
        }

        // Set the contents of a buffer from an array (will create if it doesn't exist)
        public DynamicBuffer<T> SetBuffer<T>(Entity entity, T[] data) where T : unmanaged, IBufferElement
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            var buffer = GetOrCreateBuffer<T>(entity);
            buffer.Clear();
            buffer.CopyFrom(data);
            IncrementComponentVersion(ComponentTypeManager.GetTypeIndex<T>());
            return buffer;
        }

        // Append elements to an existing buffer or create a new one
        public DynamicBuffer<T> AppendToBuffer<T>(Entity entity, T[] data) where T : unmanaged, IBufferElement
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            var buffer = GetOrCreateBuffer<T>(entity);

            if (data == null || data.Length == 0)
                return buffer;

            fixed (T* ptr = data)
            {
                buffer.AddRange(ptr, data.Length);
            }

            IncrementComponentVersion(ComponentTypeManager.GetTypeIndex<T>());
            return buffer;
        }

        // Clear a buffer (removes all elements but keeps the buffer component)
        public bool ClearBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
            if (!IsEntityAlive(entity))
                return false;

            if (!TryGetBuffer<T>(entity, out var buffer))
                return false;

            buffer.Clear();
            IncrementComponentVersion(ComponentTypeManager.GetTypeIndex<T>());
            return true;
        }

        // Get a read-only version of a buffer (useful for systems that shouldn't modify data)
        public ReadOnlyDynamicBuffer<T> GetBufferReadOnly<T>(Entity entity) where T : unmanaged, IBufferElement
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            var buffer = GetBuffer<T>(entity);
            return new ReadOnlyDynamicBuffer<T>(buffer);
        }

        public BufferArray<T> GetBufferArray<T>() where T : unmanaged, IBufferElement
        {
            var typeIndex = ComponentTypeManager.GetTypeIndex<T>();

            if (bufferChunks.TryGetValue(typeIndex, out var chunk) && chunk.ptr != null)
            {
                return new BufferArray<T>
                {
                    ptr = chunk.ptr,
                    length = chunk.length,
                    headerSize = chunk.headerSize,
                    entityToIndex = chunk.entityToIndex
                };
            }

            return default;
        }
    }
}