using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Core.DynamicBuffers
{
    public unsafe struct BufferArray<T> where T : unmanaged, IBufferElement
    {
        [NativeDisableUnsafePtrRestriction] private readonly BufferChunk* m_chunk; // Pointer to the underlying BufferChunk

        public BufferArray(BufferChunk* bufferChunk)
        {
            m_chunk = bufferChunk;
        }

        public DynamicBuffer<T> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (m_chunk == null)
                    throw new InvalidOperationException("BufferArray has not been initialized");

                var headerPtr = m_chunk->ptr + index * m_chunk->headerSize;
                return new DynamicBuffer<T>((BufferHeader*)headerPtr);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DynamicBuffer<T> Get(Entity entity)
        {
            if (m_chunk == null)
                throw new InvalidOperationException("BufferArray has not been initialized");

            if (entity.id > m_chunk->maxEntityId)
                throw new InvalidOperationException($"Entity {entity.id} does not have this buffer component");

            var index = m_chunk->bufferIndices[entity.id];
            if (index < 0)
                throw new InvalidOperationException($"Entity {entity.id} does not have this buffer component");

            return this[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(Entity entity)
        {
            if (m_chunk == null) return false;
            return m_chunk->HasBuffer(entity.id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(Entity entity, out DynamicBuffer<T> buffer)
        {
            buffer = default;

            if (m_chunk == null) return false;

            if (entity.id > m_chunk->maxEntityId)
                return false;

            var index = m_chunk->bufferIndices[entity.id];
            if (index < 0)
                return false;

            buffer = this[index];
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DynamicBuffer<T> Add(Entity entity, int initialBufferCapacity = 8)
        {
            if (m_chunk == null)
                throw new InvalidOperationException("BufferArray has not been initialized");

            // Use the underlying chunk to add a buffer
            var bufferIndex = m_chunk->Add(entity.id, initialBufferCapacity);
            return this[bufferIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(Entity entity)
        {
            if (m_chunk == null) return false;

            // Use the underlying chunk to remove the buffer
            return m_chunk->RemoveEntityBuffer(entity.id);
        }

        // Property accessors to provide convenient access to underlying chunk properties
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_chunk != null ? m_chunk->length : 0;
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_chunk != null ? m_chunk->capacity : 0;
        }

        public uint Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_chunk != null ? m_chunk->version : 0;
        }
    }
}