using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct EntityManager
    {
        // Add component without providing a value (default constructed)
        public void AddComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
            // Create default instance of T
            var component = default(T);
            AddComponent(entity, component);
        }

        // Add component with specific value
        public void AddComponent<T>(Entity entity, T component) where T : unmanaged, IComponent
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            var typeIndex = TypeManager.GetComponentTypeIndex<T>();
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            archetype.componentBits.SetComponent(typeIndex);

            if (!componentChunks.TryGetValue(typeIndex, out var chunk))
            {
                var size = UnsafeUtility.SizeOf<T>();
                chunk = new ComponentChunk(size, InitialEntityCapacity);
            }

            // Ensure we have enough capacity
            if (chunk.length >= chunk.capacity)
                chunk.Resize(math.max(4, chunk.capacity * 2));

            // Ensure we can store this entity ID
            chunk.EnsureEntityCapacity(entity.id);

            // Add component using our direct indexing method
            chunk.Add(entity.id, UnsafeUtility.AddressOf(ref component));

            componentChunks[typeIndex] = chunk;
            IncrementComponentVersion(typeIndex);
        }

        // Remove component of type T from entity
        public void RemoveComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            var typeIndex = TypeManager.GetComponentTypeIndex<T>();
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            archetype.RemoveComponent(typeIndex);

            RemoveComponentInternal(entity, typeIndex);
            IncrementComponentVersion(typeIndex);
        }

        // Helper method to remove a component by type index
        private void RemoveComponentInternal(Entity entity, int typeIndex)
        {
            if (!componentChunks.TryGetValue(typeIndex, out var chunk))
                return;

            // Use our direct indexing method to remove component
            if (chunk.Remove(entity.id))
            {
                componentChunks[typeIndex] = chunk;
            }
        }

        // GetComponent implementation for the new ComponentChunk design
        public ref T GetComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (!componentChunks.TryGetValue(typeIndex, out var chunk))
                throw new InvalidOperationException($"Entity does not have component of type {typeof(T).Name}");

            void* componentPtr = chunk.GetComponentPtr(entity.id);
            if (componentPtr == null)
                throw new InvalidOperationException($"Entity does not have component of type {typeof(T).Name}");

            return ref UnsafeUtility.AsRef<T>(componentPtr);
        }

        // Get or add a component with default initialization
        public ref T GetOrAddComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            // Check if entity already has this component
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (componentChunks.TryGetValue(typeIndex, out var chunk) &&
                entity.id <= chunk.maxEntityId &&
                chunk.componentIndices[entity.id] >= 0)
            {
                // Component exists, return reference
                int index = chunk.componentIndices[entity.id];
                var ptr = (byte*)chunk.ptr + index * chunk.componentSize;
                return ref UnsafeUtility.AsRef<T>(ptr);
            }

            // Component doesn't exist, add it with default value
            var component = default(T);
            AddComponent(entity, component);

            // Get reference to the newly added component
            return ref GetComponent<T>(entity);
        }

        // Get or add a component with custom initialization
        public ref T GetOrAddComponent<T>(Entity entity, T defaultValue) where T : unmanaged, IComponent
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            // Check if entity already has this component
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (componentChunks.TryGetValue(typeIndex, out var chunk) &&
                entity.id <= chunk.maxEntityId &&
                chunk.componentIndices[entity.id] >= 0)
            {
                // Component exists, return reference
                int index = chunk.componentIndices[entity.id];
                var ptr = (byte*)chunk.ptr + index * chunk.componentSize;
                return ref UnsafeUtility.AsRef<T>(ptr);
            }

            // Component doesn't exist, add it with provided value
            AddComponent(entity, defaultValue);

            // Get reference to the newly added component
            return ref GetComponent<T>(entity);
        }

        // HasComponent implementation for the new ComponentChunk design
        public bool HasComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
            if (!IsEntityAlive(entity))
                return false;

            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            return componentChunks.TryGetValue(typeIndex, out var chunk) &&
                   chunk.ptr != null &&
                   chunk.HasComponent(entity.id);
        }

        // TryGetComponent implementation for the new ComponentChunk design
        public bool TryGetComponent<T>(Entity entity, out T component) where T : unmanaged, IComponent
        {
            component = default;

            if (!IsEntityAlive(entity))
                return false;

            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (!componentChunks.TryGetValue(typeIndex, out var chunk))
                return false;

            void* componentPtr = chunk.GetComponentPtr(entity.id);
            if (componentPtr == null)
                return false;

            component = UnsafeUtility.AsRef<T>(componentPtr);
            return true;
        }

        // SetComponent implementation for the new ComponentChunk design
        public void SetComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
            SetComponent<T>(entity, default);
        }

        public void SetComponent<T>(Entity entity, T component) where T : unmanaged, IComponent
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (!componentChunks.TryGetValue(typeIndex, out var chunk) ||
                !chunk.HasComponent(entity.id))
            {
                AddComponent(entity, component);
                return;
            }

            void* componentPtr = chunk.GetComponentPtr(entity.id);
            UnsafeUtility.CopyStructureToPtr(ref component, componentPtr);
            IncrementComponentVersion(typeIndex);
        }

        // ComponentArray implementation needs updating too
        public ComponentArray<T> GetComponentArray<T>() where T : unmanaged, IComponent
        {
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (componentChunks.TryGetValue(typeIndex, out var chunk) && chunk.ptr != null)
            {
                return new ComponentArray<T>
                {
                    ptr = chunk.ptr,
                    length = chunk.length,
                    componentIndices = chunk.componentIndices, // Add the componentIndices mapping
                    maxEntityId = chunk.maxEntityId // Add the maxEntityId value
                };
            }

            return default;
        }

        private void DestroyEntityComponents(Entity entity)
        {
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            foreach (var componentIndex in archetype.componentBits)
            {
                if (componentChunks.TryGetValue(componentIndex, out var chunk))
                {
                    // Use our new Remove method to remove the component
                    chunk.Remove(entity.id);
                    componentChunks[componentIndex] = chunk;
                    IncrementComponentVersion(componentIndex);
                }
            }

            archetype.componentBits.Clear();
        }
    }
}