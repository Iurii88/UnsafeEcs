using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Core.Components
{
    public unsafe struct ComponentArray<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction] private readonly ComponentChunk* m_chunkPtr;

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var itemPtr = (byte*)m_chunkPtr->ptr + index * m_chunkPtr->componentSize;
                return ref ((T*)itemPtr)[0];
            }
        }

        public ComponentArray(ComponentChunk* chunk)
        {
            m_chunkPtr = chunk;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(Entity entity)
        {
#if DEBUG
            // Check bounds first to avoid memory access violation
            if (entity.id > m_chunkPtr->maxEntityId)
                throw new InvalidOperationException($"Entity {entity.id} does not have this component");
#endif

            var index = m_chunkPtr->componentIndices[entity.id];
#if DEBUG
            if (index < 0)
                throw new InvalidOperationException($"Entity {entity.id} does not have this component");
#endif

            return ref this[index];
        }

        public bool Remove(Entity entity)
        {
            return m_chunkPtr->Remove(entity.id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Entity entity, bool cleanComponent = false)
        {
            m_chunkPtr->Add(entity.id, cleanComponent);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Entity entity, ref T component)
        {
            m_chunkPtr->Add(entity.id, UnsafeUtility.AddressOf(ref component));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(Entity entity)
        {
            return m_chunkPtr->HasComponent(entity.id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(Entity entity, out T component)
        {
            component = default;

            if (entity.id > m_chunkPtr->maxEntityId)
                return false;

            var index = m_chunkPtr->componentIndices[entity.id];
            if (index < 0)
                return false;

            component = this[index];
            return true;
        }

        // Forwarding properties from the chunk for convenience
        public int Length => m_chunkPtr->length;
        public int Capacity => m_chunkPtr->capacity;
        public int ComponentSize => m_chunkPtr->componentSize;
    }
}