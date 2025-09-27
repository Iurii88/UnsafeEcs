using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct Entity
    {
        public DynamicBuffer<T> AddBuffer<T>() where T : unmanaged, IBufferElement
        {
            return managerPtr->AddBuffer<T>(this);
        }

        public DynamicBuffer<T> AddBuffer<T>(T[] initialData) where T : unmanaged, IBufferElement
        {
            return managerPtr->AddBuffer<T>(this, initialData);
        }

        public DynamicBuffer<T> GetBuffer<T>() where T : unmanaged, IBufferElement
        {
            return managerPtr->GetBuffer<T>(this);
        }

        public void RemoveBuffer<T>() where T : unmanaged, IBufferElement
        {
            managerPtr->RemoveBuffer<T>(this);
        }

        public bool HasBuffer<T>() where T : unmanaged, IBufferElement
        {
            return managerPtr->HasBuffer<T>(this);
        }

        public bool TryGetBuffer<T>(Entity entity, out DynamicBuffer<T> buffer) where T : unmanaged, IBufferElement
        {
            return managerPtr->TryGetBuffer<T>(this, out buffer);
        }

        public DynamicBuffer<T> GetOrCreateBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
            return managerPtr->GetOrCreateBuffer<T>(this);
        }

        public DynamicBuffer<T> SetBuffer<T>(Entity entity, T[] data) where T : unmanaged, IBufferElement
        {
            return managerPtr->SetBuffer<T>(this, data);
        }

        public DynamicBuffer<T> AppendToBuffer<T>(Entity entity, T[] data) where T : unmanaged, IBufferElement
        {
            return managerPtr->AppendToBuffer<T>(this, data);
        }

        public bool ClearBuffer<T>(Entity entity) where T : unmanaged, IBufferElement
        {
            return managerPtr->ClearBuffer<T>(this);
        }

        public ReadOnlyDynamicBuffer<T> GetBufferReadOnly<T>() where T : unmanaged, IBufferElement
        {
            return managerPtr->GetBufferReadOnly<T>(this);
        }
    }
}