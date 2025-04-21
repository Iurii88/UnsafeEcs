using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace UnsafeEcs.Core.Utils
{
    public unsafe struct UnsafeItem<T> : IDisposable where T : unmanaged
    {
        private UnsafeList<T> m_item;

        public UnsafeItem(T value = default, Allocator allocator = Allocator.Persistent)
        {
            m_item = new UnsafeList<T>(1, allocator);
            m_item.Length = 1;
            Value = value;
        }
        
        public ref T Value => ref m_item.Ptr[0];
        public bool IsCreated => m_item.IsCreated;
        
        public void Dispose()
        {
            m_item.Dispose();
        }
        
        public JobHandle Dispose(JobHandle jobHandle)
        {
            return m_item.Dispose(jobHandle);
        }
    }
}