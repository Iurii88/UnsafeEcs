using System.Runtime.CompilerServices;
using Unity.Collections;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Core.DynamicBuffers
{
    public readonly unsafe struct ReadOnlyDynamicBuffer<T> where T : unmanaged, IBufferElement
    {
        private readonly DynamicBuffer<T> m_buffer;

        internal ReadOnlyDynamicBuffer(DynamicBuffer<T> buffer)
        {
            m_buffer = buffer;
        }

        // Length property
        public int Length => m_buffer.Length;

        // Capacity property
        public int Capacity => m_buffer.Capacity;

        // Readonly indexer
        public ref readonly T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_buffer[index];
        }

        // Converts to NativeArray (creates a copy)
        public NativeArray<T> ToNativeArray(Allocator allocator)
        {
            return m_buffer.ToNativeArray(allocator);
        }

        // Get an unsafe readonly pointer
        public T* GetUnsafeReadOnlyPtr()
        {
            return m_buffer.GetUnsafePtr();
        }
    }
}