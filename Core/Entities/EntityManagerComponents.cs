using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct EntityManager
    {
        public void AddComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
            var component = default(T);
            AddComponent(entity, component);
        }

        public void AddComponent<T>(Entity entity, T component) where T : unmanaged, IComponent
        {
#if DEBUG
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");
#endif
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            archetype.componentBits.SetComponent(typeIndex);

            if (!componentChunks.TryGetValue(typeIndex, out var chunk))
            {
                var size = UnsafeUtility.SizeOf<T>();
                chunk = new ComponentChunk(size, InitialEntityCapacity);
            }

            if (chunk.length >= chunk.capacity)
                chunk.Resize(math.max(4, chunk.capacity * 2));

            chunk.EnsureEntityCapacity(entity.id);
            chunk.Add(entity.id, UnsafeUtility.AddressOf(ref component));

            componentChunks[typeIndex] = chunk;
            IncrementComponentVersion(typeIndex);
        }

        public void RemoveComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
#if DEBUG
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");
#endif

            var typeIndex = TypeManager.GetComponentTypeIndex<T>();
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            archetype.RemoveComponent(typeIndex);

            RemoveComponentInternal(entity, typeIndex);
            IncrementComponentVersion(typeIndex);
        }

        private void RemoveComponentInternal(Entity entity, int typeIndex)
        {
            if (!componentChunks.TryGetValue(typeIndex, out var chunk))
                return;

            if (chunk.Remove(entity.id))
            {
                componentChunks[typeIndex] = chunk;
            }
        }

        public ref T GetComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
#if DEBUG
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");
#endif
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();
            
            if (!componentChunks.TryGetValue(typeIndex, out var chunk))
                throw new InvalidOperationException($"Entity does not have component of type {typeof(T).Name}");
            
            void* componentPtr = chunk.GetComponentPtr(entity.id);
            
            if (componentPtr == null)
                throw new InvalidOperationException($"Entity does not have component of type {typeof(T).Name}");
            
            return ref UnsafeUtility.AsRef<T>(componentPtr);
        }

        public ref T GetOrAddComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
#if DEBUG
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");
#endif
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (componentChunks.TryGetValue(typeIndex, out var chunk) &&
                entity.id <= chunk.maxEntityId &&
                chunk.componentIndices[entity.id] >= 0)
            {
                int index = chunk.componentIndices[entity.id];
                var ptr = (byte*)chunk.ptr + index * chunk.componentSize;
                return ref UnsafeUtility.AsRef<T>(ptr);
            }

            var component = default(T);
            AddComponent(entity, component);
            return ref GetComponent<T>(entity);
        }

        public ref T GetOrAddComponent<T>(Entity entity, T defaultValue) where T : unmanaged, IComponent
        {
#if DEBUG
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");
#endif
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (componentChunks.TryGetValue(typeIndex, out var chunk) &&
                entity.id <= chunk.maxEntityId &&
                chunk.componentIndices[entity.id] >= 0)
            {
                int index = chunk.componentIndices[entity.id];
                var ptr = (byte*)chunk.ptr + index * chunk.componentSize;
                return ref UnsafeUtility.AsRef<T>(ptr);
            }

            AddComponent(entity, defaultValue);
            return ref GetComponent<T>(entity);
        }

        public bool HasComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
#if DEBUG
            if (!IsEntityAlive(entity))
                return false;
#endif
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            return componentChunks.TryGetValue(typeIndex, out var chunk) &&
                   chunk.ptr != null &&
                   chunk.HasComponent(entity.id);
        }

        public bool TryGetComponent<T>(Entity entity, out T component) where T : unmanaged, IComponent
        {
            component = default;

#if DEBUG
            if (!IsEntityAlive(entity))
                return false;
#endif
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (!componentChunks.TryGetValue(typeIndex, out var chunk))
                return false;

            void* componentPtr = chunk.GetComponentPtr(entity.id);
            if (componentPtr == null)
                return false;

            component = UnsafeUtility.AsRef<T>(componentPtr);
            return true;
        }

        public void SetComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
            SetComponent<T>(entity, default);
        }

        public void SetComponent<T>(Entity entity, T component) where T : unmanaged, IComponent
        {
#if DEBUG
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");
#endif
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

        public ComponentArray<T> GetComponentArray<T>() where T : unmanaged, IComponent
        {
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (componentChunks.TryGetValue(typeIndex, out var chunk) && chunk.ptr != null)
            {
                return new ComponentArray<T>
                {
                    ptr = chunk.ptr,
                    length = chunk.length,
                    componentSize = chunk.componentSize,
                    componentIndices = chunk.componentIndices,
                    maxEntityId = chunk.maxEntityId
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
                    chunk.Remove(entity.id);
                    componentChunks[componentIndex] = chunk;
                    IncrementComponentVersion(componentIndex);
                }
            }

            archetype.componentBits.Clear();
        }
    }
}