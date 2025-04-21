using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct Entity
    {
        public readonly void AddComponent<T>(T component) where T : unmanaged, IComponent
        {
            managerPtr->AddComponent(this, component);
        }

        public readonly void RemoveComponent<T>() where T : unmanaged, IComponent
        {
            managerPtr->RemoveComponent<T>(this);
        }

        public readonly void SetComponent<T>(T component) where T : unmanaged, IComponent
        {
            managerPtr->SetComponent(this, component);
        }

        public readonly ref T GetComponent<T>() where T : unmanaged, IComponent
        {
            return ref managerPtr->GetComponent<T>(this);
        }

        public readonly bool HasComponent<T>() where T : unmanaged, IComponent
        {
            return managerPtr->HasComponent<T>(this);
        }

        public readonly bool TryGetComponent<T>(out T component) where T : unmanaged, IComponent
        {
            return managerPtr->TryGetComponent(this, out component);
        }
        
        public readonly ref T GetOrAddComponent<T>() where T : unmanaged, IComponent
        {
            if (!HasComponent<T>())
                AddComponent(new T());
            return ref GetComponent<T>();
        }
        
        public readonly ref T GetOrAddComponent<T>(T defaultValue) where T : unmanaged, IComponent
        {
            if (!HasComponent<T>())
                AddComponent(defaultValue);
            return ref GetComponent<T>();
        }
    }
}