using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Core.Components
{
    public unsafe partial struct ComponentArray<T> : IEnumerable<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction] public void* ptr;
        public int length;
        public int componentSize;
        [NativeDisableUnsafePtrRestriction] public int* componentIndices; // Maps entity ID to component index
        public int maxEntityId; // Highest entity ID in the mapping

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var itemPtr = (byte*)ptr + index * componentSize;
                return ref ((T*)itemPtr)[0];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(Entity entity)
        {
#if DEBUG
            // Check bounds first to avoid memory access violation
            if (entity.id > maxEntityId)
                throw new InvalidOperationException($"Entity {entity.id} does not have this component");
#endif

            int index = componentIndices[entity.id];
#if DEBUG
            if (index < 0)
                throw new InvalidOperationException($"Entity {entity.id} does not have this component");
#endif

            return ref this[index];
        }

        public bool Has(Entity entity)
        {
            return entity.id <= maxEntityId && componentIndices[entity.id] >= 0;
        }

        public bool TryGet(Entity entity, out T component)
        {
            component = default;

            if (entity.id > maxEntityId)
                return false;

            int index = componentIndices[entity.id];
            if (index < 0)
                return false;

            component = this[index];
            return true;
        }

        // IEnumerable implementation
        public IEnumerator<T> GetEnumerator()
        {
            return new ComponentArrayEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct ComponentArrayEnumerator : IEnumerator<T>
        {
            private readonly ComponentArray<T> m_array;
            private int m_index;

            public ComponentArrayEnumerator(ComponentArray<T> array)
            {
                m_array = array;
                m_index = -1;
            }

            public bool MoveNext()
            {
                m_index++;
                return m_index < m_array.length;
            }

            public void Reset()
            {
                m_index = -1;
            }

            public T Current
            {
                get
                {
                    if (m_index < 0 || m_index >= m_array.length)
                        throw new InvalidOperationException("Enumerator is not in a valid position");

                    return m_array[m_index];
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}