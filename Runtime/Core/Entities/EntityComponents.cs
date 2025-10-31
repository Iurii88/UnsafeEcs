using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.Components.Managed;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct Entity
    {
        public readonly void AddComponent<T>(T component) where T : unmanaged, IComponent
        {
            managerPtr->AddComponent(this, component);
        }

        public readonly void AddComponent<T>() where T : unmanaged, IComponent
        {
            managerPtr->AddComponent<T>(this);
        }

        public readonly void RemoveComponent<T>() where T : unmanaged, IComponent
        {
            managerPtr->RemoveComponent<T>(this);
        }

        public readonly void SetComponent<T>(T component) where T : unmanaged, IComponent
        {
            managerPtr->SetComponent(this, component);
        }

        public readonly void SetComponent<T>() where T : unmanaged, IComponent
        {
            managerPtr->SetComponent<T>(this);
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

        public void AddReference<T>(T reference) where T : class
        {
            var refComponent = managerPtr->world.managedStorage.Add(reference);
            AddComponent(refComponent);
        }

        public bool TryGetReference<T>(out T reference) where T : class
        {
            if (!HasComponent<ManagedRef<T>>())
            {
                reference = null;
                return false;
            }

            reference = GetReference<T>();
            return true;
        }

        public T GetReference<T>() where T : class
        {
            ref var managedRef = ref GetComponent<ManagedRef<T>>();
            var reference = managedRef.Get(managerPtr->world);
            return reference;
        }
    }
}