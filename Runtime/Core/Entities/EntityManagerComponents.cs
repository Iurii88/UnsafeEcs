using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct EntityManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentChunk* GetComponentChunk(int typeIndex)
        {
            return chunks.Ptr[typeIndex].AsComponentChunk();
        }

        /// <summary>
        /// Gets a ComponentArray without ensuring chunk exists. Burst-compatible, no allocations.
        /// Use in jobs when you know the chunk already exists.
        /// Returns default ComponentArray if chunk doesn't exist.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentArray<T> GetComponentArrayUnsafe<T>(int typeIndex) where T : unmanaged, IComponent
        {
            if (typeIndex >= chunks.m_length)
                return default;

            var chunkUnion = chunks.Ptr + typeIndex;
            if (chunkUnion->chunkPtr == null || chunkUnion->isBuffer)
                return default;

            return new ComponentArray<T>((ComponentChunk*)chunkUnion->chunkPtr);
        }

        public void AddComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
            var component = default(T);
            AddComponent(entity, component);
        }

        public void AddComponent<T>(Entity entity, T component) where T : unmanaged, IComponent
        {
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            // Use the helper method to ensure the component chunk exists
            EnsureComponentChunkExists(typeIndex);

            // Get the chunk and add the component
            var existingChunk = chunks.Ptr[typeIndex].AsComponentChunk();
            existingChunk->Add(entity.id, UnsafeUtility.AddressOf(ref component));
        }

        /// <summary>
        /// Ensures that a component chunk exists for the specified component type.
        /// Call this in OnAwake() to guarantee chunk availability in jobs.
        /// </summary>
        public void EnsureComponentChunkExists<T>() where T : unmanaged, IComponent
        {
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();
            EnsureComponentChunkExists(typeIndex);
        }

        /// <summary>
        /// Ensures that a component chunk exists for the specified type index.
        /// Use this overload in Burst jobs with pre-cached type indices.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureComponentChunkExists(int typeIndex)
        {
            if (typeIndex >= chunks.m_length)
                chunks.Resize(typeIndex + 1, NativeArrayOptions.ClearMemory);

            if (chunks.Ptr[typeIndex].chunkPtr != null)
                return;

            var size = TypeManager.GetTypeSizeByIndex(typeIndex);
            var stackChunk = new ComponentChunk(size, InitialEntityCapacity, typeIndex, m_managerPtr);
            var chunk = (ComponentChunk*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ComponentChunk>(), UnsafeUtility.AlignOf<ComponentChunk>(), Allocator.Persistent);
            UnsafeUtility.CopyStructureToPtr(ref stackChunk, chunk);
            chunks.Ptr[typeIndex] = ChunkUnion.FromComponentChunk(chunk);
        }

        public void RemoveComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
            if (!IsEntityAlive(entity))
                EcsDebug.ThrowEntityNotAlive(entity.id, m_managerPtr);
#endif

            var typeIndex = TypeManager.GetComponentTypeIndex<T>();
            RemoveComponentInternal(entity, typeIndex);
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
#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
            if (!IsEntityAlive(entity))
                EcsDebug.ThrowEntityNotAlive(entity.id, m_managerPtr);
#endif
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
            if (typeIndex >= chunks.Length)
                EcsDebug.ThrowNoComponent<T>(entity.id, typeIndex, m_managerPtr);
#endif

            var chunk = chunks.Ptr[typeIndex].AsComponentChunk();
#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
            if (chunk == null)
                EcsDebug.ThrowNoComponent<T>(entity.id, typeIndex, m_managerPtr);
#endif

            var componentPtr = chunk->GetComponentPtr(entity.id);

#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
            if (componentPtr == null)
                EcsDebug.ThrowNoComponent<T>(entity.id, typeIndex, m_managerPtr);
#endif

            return ref UnsafeUtility.AsRef<T>(componentPtr);
        }

        public ref T GetOrAddComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
            if (!IsEntityAlive(entity))
                EcsDebug.ThrowEntityNotAlive(entity.id, m_managerPtr);
#endif
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (typeIndex < chunks.Length)
            {
                var chunk = chunks.Ptr[typeIndex].AsComponentChunk();
                if (chunk != null && entity.id <= chunk->maxEntityId && chunk->HasComponent(entity.id))
                    return ref UnsafeUtility.AsRef<T>(chunk->GetComponentPtr(entity.id));
            }

            var component = default(T);
            AddComponent(entity, component);
            return ref GetComponent<T>(entity);
        }

        public ref T GetOrAddComponent<T>(Entity entity, T defaultValue) where T : unmanaged, IComponent
        {
#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
            if (!IsEntityAlive(entity))
                EcsDebug.ThrowEntityNotAlive(entity.id, m_managerPtr);
#endif
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (typeIndex < chunks.Length)
            {
                var chunk = chunks.Ptr[typeIndex].AsComponentChunk();
                if (chunk != null && entity.id <= chunk->maxEntityId && chunk->HasComponent(entity.id))
                    return ref UnsafeUtility.AsRef<T>(chunk->GetComponentPtr(entity.id));
            }

            AddComponent(entity, defaultValue);
            return ref GetComponent<T>(entity);
        }

        public bool HasComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
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

#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
            if (!IsEntityAlive(entity))
                return false;
#endif
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (typeIndex >= chunks.Length)
                return false;

            var chunk = chunks.Ptr[typeIndex].AsComponentChunk();
            if (chunk == null)
                return false;

            var componentPtr = chunk->GetComponentPtr(entity.id);
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
#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
            if (!IsEntityAlive(entity))
                EcsDebug.ThrowEntityNotAlive(entity.id, m_managerPtr);
#endif
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();

            if (typeIndex >= chunks.Length || chunks.Ptr[typeIndex].AsComponentChunk() == null ||
                !chunks.Ptr[typeIndex].AsComponentChunk()->HasComponent(entity.id))
            {
                AddComponent(entity, component);
                return;
            }

            var chunk = chunks.Ptr[typeIndex].AsComponentChunk();
            var componentPtr = chunk->GetComponentPtr(entity.id);
            UnsafeUtility.CopyStructureToPtr(ref component, componentPtr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentArray<T> GetComponentArray<T>() where T : unmanaged, IComponent
        {
            var typeIndex = TypeManager.GetComponentTypeIndex<T>();
            return GetComponentArray<T>(typeIndex);
        }

        /// <summary>
        /// Gets a ComponentArray using a pre-cached type index for maximum performance.
        /// Use this when calling GetComponentArray repeatedly with the same type.
        /// Cache the typeIndex once using TypeManager.GetComponentTypeIndex().
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentArray<T> GetComponentArray<T>(int typeIndex) where T : unmanaged, IComponent
        {
            // Fast path: chunk already exists (common case in hot loops)
            if (typeIndex < chunks.m_length)
            {
                var chunkUnion = chunks.Ptr + typeIndex;
                if (chunkUnion->chunkPtr != null && !chunkUnion->isBuffer)
                    return new ComponentArray<T>((ComponentChunk*)chunkUnion->chunkPtr);
            }

            // Slow path: ensure chunk exists and return
            EnsureComponentChunkExists(typeIndex);
            return new ComponentArray<T>((ComponentChunk*)chunks.Ptr[typeIndex].chunkPtr);
        }

        private void DestroyEntityComponents(Entity entity)
        {
            ref var archetype = ref entityArchetypes.Ptr[entity.id];
            foreach (var componentIndex in archetype.componentBits)
                if (componentIndex < chunks.Length)
                {
                    var chunk = chunks.Ptr[componentIndex].AsComponentChunk();
                    if (chunk != null)
                        chunk->Remove(entity.id);
                }

            archetype.componentBits.Clear();
        }
    }
}