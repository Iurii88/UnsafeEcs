using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.Utils;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct EntityManager
    {
        public const int InitialEntityCapacity = 0;
        private const int InitialChunkCapacity = 0;
        private const int OtherCapacity = 0;

        public UnsafeList<ChunkUnion> chunks;

        public UnsafeList<Entity> entities;
        public UnsafeList<EntityArchetype> entityArchetypes;
        public UnsafeList<bool> deadEntities;

        public UnsafeItem<int> nextId;
        public UnsafeList<Entity> freeEntities;

        private UnsafeHashMap<ulong, QueryCacheEntry> m_queryCache;

        [NativeDisableUnsafePtrRestriction]
        private EntityManager* m_managerPtr;

        private readonly IntPtr m_worldHandle;
        public World world => (World)GCHandle.FromIntPtr(m_worldHandle).Target;

        public EntityManager(World world, int initialCapacity)
        {
            m_worldHandle = GCHandle.ToIntPtr(GCHandle.Alloc(world));
            chunks = new UnsafeList<ChunkUnion>(InitialChunkCapacity, Allocator.Persistent);

            entities = new UnsafeList<Entity>(initialCapacity, Allocator.Persistent);
            entityArchetypes = new UnsafeList<EntityArchetype>(initialCapacity, Allocator.Persistent);
            deadEntities = new UnsafeList<bool>(initialCapacity, Allocator.Persistent);

            freeEntities = new UnsafeList<Entity>(OtherCapacity, Allocator.Persistent);
            nextId = new UnsafeItem<int>(0);

            m_queryCache = new UnsafeHashMap<ulong, QueryCacheEntry>(OtherCapacity, Allocator.Persistent);
            m_managerPtr = null;
        }

        public void Initialize()
        {
            m_managerPtr = (EntityManager*)UnsafeUtility.AddressOf(ref this);
        }

        public void Dispose()
        {
            for (var i = 0; i < chunks.Length; i++)
            {
                if (!chunks.Ptr[i].IsValid)
                    continue;

                chunks.Ptr[i].Dispose();
            }
            chunks.Dispose();

            entities.Dispose();
            entityArchetypes.Dispose();
            deadEntities.Dispose();
            freeEntities.Dispose();
            nextId.Dispose();

            // Dispose query cache
            foreach (var kv in m_queryCache)
                kv.Value.Dispose();
            m_queryCache.Dispose();
        }

        public void Clear()
        {
            GCHandle.FromIntPtr(m_worldHandle).Free();

            foreach (var chunk in chunks)
                chunk.Dispose();
            chunks.Clear();

            entities.Clear();
            entityArchetypes.Clear();
            deadEntities.Clear();
            freeEntities.Clear();
            nextId.Value = 0;

            // Clear query cache
            foreach (var kv in m_queryCache)
                kv.Value.Clear();
            m_queryCache.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEntityAlive(Entity entity)
        {
            if (entity.id < 0 || entity.id >= entities.m_length)
                return false;

            return !deadEntities.Ptr[entity.id];
        }

#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
        /// <summary>
        /// Gets a debug string for an entity, including EntityName and World name if available.
        /// Format: "[WorldName] EntityName (id:version)" or "[WorldName] Entity (id:version)" if no entity name.
        /// </summary>
        public string GetEntityDebugString(Entity entity)
        {
            return GetEntityDebugString(entity.id);
        }

        /// <summary>
        /// Gets a debug string for an entity by id, including EntityName and World name if available.
        /// Format: "[WorldName] EntityName (id:version)" or "[WorldName] Entity (id:version)" if no entity name.
        /// </summary>
        public string GetEntityDebugString(int entityId)
        {
            // Get world name
            var worldName = world?.Name ?? "Unknown";

            // Get entity version
            uint entityVersion = 0;
            if (entityId >= 0 && entityId < entities.Length)
                entityVersion = entities.Ptr[entityId].version;

            // Try to get EntityName component
            var entityNameTypeIndex = TypeManager.GetComponentTypeIndex<EntityName>();
            if (entityNameTypeIndex < chunks.Length)
            {
                var entityNameChunk = chunks.Ptr[entityNameTypeIndex].AsComponentChunk();
                if (entityNameChunk != null && entityNameChunk->HasComponent(entityId))
                {
                    var namePtr = entityNameChunk->GetComponentPtr(entityId);
                    if (namePtr != null)
                    {
                        var entityName = UnsafeUtility.AsRef<EntityName>(namePtr);
                        if (entityName.Value.Length > 0)
                            return $"[{worldName}] {entityName.Value} ({entityId}:{entityVersion})";
                    }
                }
            }

            return $"[{worldName}] Entity ({entityId}:{entityVersion})";
        }
#endif
    }
}