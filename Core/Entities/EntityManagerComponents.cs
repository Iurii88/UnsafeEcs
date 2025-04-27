using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            archetype.componentBits.SetComponent(typeIndex);

            if (typeIndex >= chunks.m_length)
            {
                chunks.Resize(typeIndex + 1);

                var size = UnsafeUtility.SizeOf<T>();
                var stackChunk = new ComponentChunk(size, InitialEntityCapacity);
                var chunk = (ComponentChunk*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ComponentChunk>(), UnsafeUtility.AlignOf<ComponentChunk>(), Allocator.Persistent);
                UnsafeUtility.CopyStructureToPtr(ref stackChunk, chunk);
                chunks.Ptr[typeIndex] = ChunkUnion.FromComponentChunk(chunk);
            }

            var existingChunk = chunks.Ptr[typeIndex].AsComponentChunk();
            existingChunk->Add(entity.id, UnsafeUtility.AddressOf(ref component));

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
            archetype.componentBits.RemoveComponent(typeIndex);

            RemoveComponentInternal(entity, typeIndex);
            IncrementComponentVersion(typeIndex);
        }

        private void RemoveComponentInternal(Entity entity, int typeIndex)
        {
            if (typeIndex >= chunks.m_length)
                return;

            var chunkUnion = chunks.Ptr[typeIndex];
            var chunk = chunkUnion.AsComponentChunk();
            if (chunk == null)
                return;

            chunk->Remove(entity.id);
        }

        public ref T GetComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
#if DEBUG
            if (!IsEntityAlive(entity))
                throw new InvalidOperationException($"Entity {entity} is not alive");
#endif
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (typeIndex >= chunks.Length)
                throw new InvalidOperationException($"Entity does not have component of type {typeof(T).Name}");

            var chunk = chunks.Ptr[typeIndex].AsComponentChunk();
            if (chunk == null)
                throw new InvalidOperationException($"Entity does not have component of type {typeof(T).Name}");

            void* componentPtr = chunk->GetComponentPtr(entity.id);

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

            if (typeIndex < chunks.Length)
            {
                var chunk = chunks.Ptr[typeIndex].AsComponentChunk();
                if (chunk != null && entity.id <= chunk->maxEntityId && chunk->HasComponent(entity.id))
                {
                    return ref UnsafeUtility.AsRef<T>(chunk->GetComponentPtr(entity.id));
                }
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

            if (typeIndex < chunks.Length)
            {
                var chunk = chunks.Ptr[typeIndex].AsComponentChunk();
                if (chunk != null && entity.id <= chunk->maxEntityId && chunk->HasComponent(entity.id))
                {
                    return ref UnsafeUtility.AsRef<T>(chunk->GetComponentPtr(entity.id));
                }
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

            if (typeIndex >= chunks.Length)
                return false;

            var chunk = chunks.Ptr[typeIndex].AsComponentChunk();
            return chunk != null && chunk->HasComponent(entity.id);
        }

        public bool TryGetComponent<T>(Entity entity, out T component) where T : unmanaged, IComponent
        {
            component = default;

#if DEBUG
            if (!IsEntityAlive(entity))
                return false;
#endif
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (typeIndex >= chunks.Length)
                return false;

            var chunk = chunks.Ptr[typeIndex].AsComponentChunk();
            if (chunk == null)
                return false;

            void* componentPtr = chunk->GetComponentPtr(entity.id);
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

            if (typeIndex >= chunks.Length || chunks.Ptr[typeIndex].AsComponentChunk() == null ||
                !chunks.Ptr[typeIndex].AsComponentChunk()->HasComponent(entity.id))
            {
                AddComponent(entity, component);
                return;
            }

            var chunk = chunks.Ptr[typeIndex].AsComponentChunk();
            void* componentPtr = chunk->GetComponentPtr(entity.id);
            UnsafeUtility.CopyStructureToPtr(ref component, componentPtr);
            IncrementComponentVersion(typeIndex);
        }

        public ComponentArray<T> GetComponentArray<T>() where T : unmanaged, IComponent
        {
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (typeIndex < chunks.Length)
            {
                var chunk = chunks.Ptr[typeIndex].AsComponentChunk();
                if (chunk != null)
                {
                    return new ComponentArray<T>
                    {
                        ptr = chunk->ptr,
                        length = chunk->length,
                        componentSize = chunk->componentSize,
                        componentIndices = chunk->componentIndices,
                        maxEntityId = chunk->maxEntityId
                    };
                }
            }

            return default;
        }

        private void DestroyEntityComponents(Entity entity)
        {
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            foreach (var componentIndex in archetype.componentBits)
            {
                if (componentIndex < chunks.Length)
                {
                    var chunk = chunks.Ptr[componentIndex].AsComponentChunk();
                    if (chunk != null)
                    {
                        chunk->Remove(entity.id);
                        IncrementComponentVersion(componentIndex);
                    }
                }
            }

            archetype.componentBits.Clear();
        }
    }
}