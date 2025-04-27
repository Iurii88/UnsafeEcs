using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct EntityManager
    {
        public DynamicBuffer<T> AddBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
            var typeIndex = TypeManager.GetBufferTypeIndex<T>();
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            archetype.componentBits.SetComponent(typeIndex);

            // Ensure the buffer chunk exists
            EnsureBufferChunkExists(typeIndex, entity.id + 1);

            var existingBufferChunk = chunks.Ptr[typeIndex].AsBufferChunk();
            if (existingBufferChunk == null)
                throw new InvalidOperationException($"Component type {typeIndex} is registered as a regular component but trying to add as a buffer.");

            // Use the Add method from BufferChunk directly
            var initialBufferCapacity = 8;
            var bufferIndex = existingBufferChunk->Add(entity.id, initialBufferCapacity);

            // Create and return the DynamicBuffer wrapper
            var header = (BufferHeader*)(existingBufferChunk->ptr + bufferIndex * existingBufferChunk->headerSize);
            var buffer = new DynamicBuffer<T>(header);

            return buffer;
        }

        public DynamicBuffer<T> AddBuffer<T>(Entity entity, T[] initialData) where T : unmanaged, IBufferElement
        {
            var buffer = AddBuffer<T>(entity);
            buffer.CopyFrom(initialData);
            return buffer;
        }

        private void EnsureBufferChunkExists(int typeIndex, int maxEntityId)
        {
            if (typeIndex >= chunks.Length)
            {
                chunks.Resize(typeIndex + 1);

                var elementSize = TypeManager.GetTypeSizeByIndex(typeIndex);
                var bufferChunk = (BufferChunk*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BufferChunk>(), UnsafeUtility.AlignOf<BufferChunk>(), Allocator.Persistent);
                *bufferChunk = new BufferChunk(elementSize, InitialEntityCapacity, maxEntityId, typeIndex, m_managerPtr);
                chunks.Ptr[typeIndex] = ChunkUnion.FromBufferChunk(bufferChunk);
            }
        }

        public DynamicBuffer<T> GetBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
#if DEBUG
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");
#endif
            var typeIndex = TypeManager.GetBufferTypeIndex<T>();

            if (typeIndex >= chunks.Length)
                throw new InvalidOperationException($"Entity does not have buffer component of type {typeof(T).Name}");

            var chunk = chunks.Ptr[typeIndex].AsBufferChunk();
            if (chunk == null)
                throw new InvalidOperationException($"Entity does not have buffer component of type {typeof(T).Name}");

            if (!chunk->TryGetBufferIndex(entity.id, out var index))
                throw new InvalidOperationException($"Entity does not have buffer component of type {typeof(T).Name}");

            var header = (BufferHeader*)(chunk->ptr + index * chunk->headerSize);
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
            archetype.componentBits.RemoveComponent(typeIndex);

            if (typeIndex >= chunks.Length)
                return;

            var chunk = chunks.Ptr[typeIndex].AsBufferChunk();
            if (chunk == null)
                return;

            chunk->RemoveEntityBuffer(entity.id);
        }

        public bool HasBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
            if (!IsEntityAlive(entity))
                return false;

            var typeIndex = TypeManager.GetBufferTypeIndex<T>();

            if (typeIndex >= chunks.Length)
                return false;

            var chunk = chunks.Ptr[typeIndex].AsBufferChunk();
            return chunk != null && chunk->HasBuffer(entity.id);
        }

        private void DestroyEntityBuffers(Entity entity)
        {
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            foreach (var componentIndex in archetype.componentBits)
                if (componentIndex < chunks.Length)
                {
                    var bufferChunk = chunks.Ptr[componentIndex].AsBufferChunk();
                    if (bufferChunk != null && bufferChunk->HasBuffer(entity.id))
                        bufferChunk->RemoveEntityBuffer(entity.id);
                }
        }

        public bool TryGetBuffer<T>(Entity entity, out DynamicBuffer<T> buffer) where T : unmanaged, IBufferElement
        {
            buffer = default;

            if (!IsEntityAlive(entity))
                return false;

            var typeIndex = TypeManager.GetBufferTypeIndex<T>();

            if (typeIndex >= chunks.Length)
                return false;

            var chunk = chunks.Ptr[typeIndex].AsBufferChunk();
            if (chunk == null || !chunk->TryGetBufferIndex(entity.id, out var index))
                return false;

            var header = (BufferHeader*)(chunk->ptr + index * chunk->headerSize);
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

            return buffer;
        }

        public bool ClearBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
            if (!IsEntityAlive(entity))
                return false;

            if (!TryGetBuffer<T>(entity, out var buffer))
                return false;

            buffer.Clear();
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
            EnsureBufferChunkExists(typeIndex, 0);
            if (typeIndex < chunks.Length)
            {
                var chunk = chunks.Ptr[typeIndex].AsBufferChunk();
                if (chunk != null)
                    return new BufferArray<T>(chunk);
            }

            return default;
        }
    }
}