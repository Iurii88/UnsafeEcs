using System;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct EntityManager
    {
        public DynamicBuffer<T> AddBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
#if DEBUG
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");
#endif
            var typeIndex = TypeManager.GetBufferTypeIndex<T>();
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            archetype.componentBits.SetComponent(typeIndex);

            if (!bufferChunks.TryGetValue(typeIndex, out var chunk))
            {
                var elementSize = UnsafeUtility.SizeOf<T>();
                chunk = new BufferComponentChunk(elementSize, InitialEntityCapacity);
            }

            int bufferIndex = chunk.AddEntityBuffer(entity.id);
            var header = (BufferHeader*)(chunk.ptr + bufferIndex * chunk.headerSize);
            var buffer = new DynamicBuffer<T>(header);

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
#if DEBUG
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");
#endif
            var typeIndex = TypeManager.GetBufferTypeIndex<T>();

            if (!bufferChunks.TryGetValue(typeIndex, out var chunk))
                throw new InvalidOperationException($"Entity does not have buffer component of type {typeof(T).Name}");

            if (!chunk.TryGetBufferIndex(entity.id, out var index))
                throw new InvalidOperationException($"Entity does not have buffer component of type {typeof(T).Name}");

            var header = (BufferHeader*)(chunk.ptr + index * chunk.headerSize);
            return new DynamicBuffer<T>(header);
        }

        public void RemoveBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
#if DEBUG
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");
#endif
            var typeIndex = TypeManager.GetBufferTypeIndex<T>();

            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            archetype.RemoveComponent(typeIndex);

            if (!bufferChunks.TryGetValue(typeIndex, out var chunk))
                return;

            chunk.RemoveEntityBuffer(entity.id);

            bufferChunks[typeIndex] = chunk;
            IncrementComponentVersion(typeIndex);
        }

        public bool HasBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
            if (!IsEntityAlive(entity))
                return false;

            var typeIndex = TypeManager.GetBufferTypeIndex<T>();
            return bufferChunks.TryGetValue(typeIndex, out var chunk) &&
                   chunk.ptr != null &&
                   chunk.HasBuffer(entity.id);
        }

        private void DestroyEntityBuffers(Entity entity)
        {
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            foreach (var componentIndex in archetype.componentBits)
            {
                if (bufferChunks.TryGetValue(componentIndex, out var bufferChunk) &&
                    bufferChunk.HasBuffer(entity.id))
                {
                    bufferChunk.RemoveEntityBuffer(entity.id);
                }
            }
        }

        public bool TryGetBuffer<T>(Entity entity, out DynamicBuffer<T> buffer) where T : unmanaged, IBufferElement
        {
            buffer = default;

            if (!IsEntityAlive(entity))
                return false;

            var typeIndex = TypeManager.GetBufferTypeIndex<T>();

            if (!bufferChunks.TryGetValue(typeIndex, out var chunk) ||
                !chunk.TryGetBufferIndex(entity.id, out var index))
                return false;

            var header = (BufferHeader*)(chunk.ptr + index * chunk.headerSize);
            buffer = new DynamicBuffer<T>(header);
            return true;
        }

        public DynamicBuffer<T> GetOrCreateBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
#if DEBUG
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");
#endif
            if (TryGetBuffer<T>(entity, out var buffer))
                return buffer;

            return AddBuffer<T>(entity);
        }

        public DynamicBuffer<T> SetBuffer<T>(Entity entity, T[] data) where T : unmanaged, IBufferElement
        {
#if DEBUG
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");
#endif
            var buffer = GetOrCreateBuffer<T>(entity);
            buffer.Clear();
            buffer.CopyFrom(data);
            IncrementComponentVersion(TypeManager.GetBufferTypeIndex<T>());
            return buffer;
        }

        public DynamicBuffer<T> AppendToBuffer<T>(Entity entity, T[] data) where T : unmanaged, IBufferElement
        {
#if DEBUG
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");
#endif
            var buffer = GetOrCreateBuffer<T>(entity);

            if (data == null || data.Length == 0)
                return buffer;

            fixed (T* ptr = data)
            {
                buffer.AddRange(ptr, data.Length);
            }

            IncrementComponentVersion(TypeManager.GetBufferTypeIndex<T>());
            return buffer;
        }

        public bool ClearBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
            if (!IsEntityAlive(entity))
                return false;

            if (!TryGetBuffer<T>(entity, out var buffer))
                return false;

            buffer.Clear();
            IncrementComponentVersion(TypeManager.GetBufferTypeIndex<T>());
            return true;
        }

        public ReadOnlyDynamicBuffer<T> GetBufferReadOnly<T>(Entity entity) where T : unmanaged, IBufferElement
        {
#if DEBUG
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");
#endif
            var buffer = GetBuffer<T>(entity);
            return new ReadOnlyDynamicBuffer<T>(buffer);
        }

        public BufferArray<T> GetBufferArray<T>() where T : unmanaged, IBufferElement
        {
            var typeIndex = TypeManager.GetBufferTypeIndex<T>();

            if (bufferChunks.TryGetValue(typeIndex, out var chunk) && chunk.ptr != null)
            {
                return new BufferArray<T>
                {
                    ptr = chunk.ptr,
                    length = chunk.length,
                    headerSize = chunk.headerSize,
                    bufferIndices = chunk.bufferIndices,
                    maxEntityId = chunk.maxEntityId
                };
            }

            return default;
        }
    }
}