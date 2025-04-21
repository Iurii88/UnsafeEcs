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
            AddComponent(entity, new T());
        }

        public void AddComponent<T>(Entity entity, T component) where T : unmanaged, IComponent
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            var typeIndex = ComponentTypeManager.GetTypeIndex<T>();
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            archetype.componentBits.SetComponent(typeIndex);

            if (!componentChunks.TryGetValue(typeIndex, out var chunk))
            {
                var size = UnsafeUtility.SizeOf<T>();
                chunk = new ComponentChunk(size, InitialEntityCapacity);
            }

            if (chunk.length >= chunk.capacity)
                chunk.Resize( math.max(4, chunk.capacity * 2));

            var dstPtr = (byte*)chunk.ptr + chunk.length * chunk.componentSize;
            UnsafeUtility.CopyStructureToPtr(ref component, dstPtr);
            chunk.entityToIndex.Add(entity.id, chunk.length);
            chunk.indexToEntity.Add(chunk.length, entity.id);

            chunk.length++;

            componentChunks[typeIndex] = chunk;
            IncrementComponentVersion(typeIndex);
        }

        public void RemoveComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            var typeIndex = ComponentTypeManager.GetTypeIndex<T>();
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            archetype.RemoveComponent(typeIndex);

            RemoveComponentInternal(entity, typeIndex);
            IncrementComponentVersion(typeIndex);
        }
        
        public void SetComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
            SetComponent(entity, new T());
        }

        public void SetComponent<T>(Entity entity, T component) where T : unmanaged, IComponent
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            var typeIndex = ComponentTypeManager.GetTypeIndex<T>();

            if (!componentChunks.TryGetValue(typeIndex, out var chunk) ||
                !chunk.entityToIndex.TryGetValue(entity.id, out var index))
            {
                AddComponent(entity, component);
                return;
            }

            var ptr = (byte*)chunk.ptr + index * chunk.componentSize;
            UnsafeUtility.CopyStructureToPtr(ref component, ptr);
            IncrementComponentVersion(typeIndex);
        }

        public ref T GetComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            var typeIndex = ComponentTypeManager.GetTypeIndex<T>();

            if (!componentChunks.TryGetValue(typeIndex, out var chunk) ||
                !chunk.entityToIndex.TryGetValue(entity.id, out var index))
                throw new InvalidOperationException($"Entity does not have component of type {typeof(T).Name}");

            var ptr = (byte*)chunk.ptr + index * chunk.componentSize;
            return ref UnsafeUtility.AsRef<T>(ptr);
        }

        public bool HasComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
            if (!IsEntityAlive(entity))
                return false;

            var typeIndex = ComponentTypeManager.GetTypeIndex<T>();
            return componentChunks.TryGetValue(typeIndex, out var chunk) &&
                   chunk.ptr != null &&
                   chunk.entityToIndex.ContainsKey(entity.id);
        }

        //makes a copy!
        public bool TryGetComponent<T>(Entity entity, out T component) where T : unmanaged, IComponent
        {
            component = default;

            if (!IsEntityAlive(entity))
                return false;

            var typeIndex = ComponentTypeManager.GetTypeIndex<T>();

            if (!componentChunks.TryGetValue(typeIndex, out var chunk) ||
                !chunk.entityToIndex.TryGetValue(entity.id, out var index))
                return false;

            var ptr = (byte*)chunk.ptr + index * chunk.componentSize;
            component = UnsafeUtility.AsRef<T>(ptr);
            return true;
        }

        public ref T GetOrAddComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            if (!HasComponent<T>(entity))
                AddComponent(entity, new T());
            return ref GetComponent<T>(entity);
        }

        public ref T GetOrAddComponent<T>(Entity entity, T defaultValue) where T : unmanaged, IComponent
        {
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");

            if (!HasComponent<T>(entity))
                AddComponent(entity, defaultValue);
            return ref GetComponent<T>(entity);
        }

        private void RemoveComponentInternal(Entity entity, int typeIndex)
        {
            if (!componentChunks.TryGetValue(typeIndex, out var chunk)) return;
            if (!chunk.entityToIndex.TryGetValue(entity.id, out var index)) return;

            var last = chunk.length - 1;
            if (index < last)
            {
                var dst = (byte*)chunk.ptr + index * chunk.componentSize;
                var src = (byte*)chunk.ptr + last * chunk.componentSize;
                UnsafeUtility.MemCpy(dst, src, chunk.componentSize);

                var lastEntity = chunk.indexToEntity[last];
                chunk.entityToIndex[lastEntity] = index;
                chunk.indexToEntity.Remove(last);
                chunk.indexToEntity[index] = lastEntity;
            }
            else
            {
                chunk.indexToEntity.Remove(last);
            }

            chunk.entityToIndex.Remove(entity.id);
            chunk.length--;

            componentChunks[typeIndex] = chunk;
        }

        public ComponentArray<T> GetComponentArray<T>() where T : unmanaged, IComponent
        {
            var typeIndex = ComponentTypeManager.GetTypeIndex<T>();

            if (componentChunks.TryGetValue(typeIndex, out var chunk) && chunk.ptr != null)
            {
                return new ComponentArray<T>
                {
                    ptr = chunk.ptr,
                    length = chunk.length,
                    entityToIndex = chunk.entityToIndex
                };
            }

            return default;
        }

        private void DestroyEntityComponents(Entity entity)
        {
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            foreach (var componentIndex in archetype.componentBits)
            {
                RemoveComponentInternal(entity, componentIndex);
                IncrementComponentVersion(componentIndex);
            }
            archetype.componentBits.Clear();
        }
    }
}