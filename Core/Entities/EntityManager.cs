using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;
using UnsafeEcs.Core.Utils;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct EntityManager
    {
        public const int InitialEntityCapacity = 0;
        private const int InitialComponentChunkCapacity = 0;
        private const int InitialBufferChunkCapacity = 0;
        private const int OtherCapacity = 0;

        public UnsafeHashMap<int, ComponentChunk> componentChunks;
        public UnsafeHashMap<int, BufferComponentChunk> bufferChunks;

        public UnsafeList<Entity> entities;
        public UnsafeList<EntityArchetype> entityArchetypes;
        public UnsafeList<bool> deadEntities;

        public UnsafeItem<int> nextId;
        public UnsafeList<Entity> freeEntities;

        //cache zone
        private UnsafeList<uint> m_componentVersions;
        private UnsafeItem<uint> m_globalVersions;
        private UnsafeHashMap<ulong, QueryCacheEntry> m_queryCache;
        private UnsafeHashMap<ComponentBits, ulong> m_queryVersionHashes;

        private struct QueryCacheEntry
        {
            public UnsafeList<Entity> entities;
            public ulong componentVersionHash;
        }

        public EntityManager(int initialCapacity)
        {
            componentChunks = new UnsafeHashMap<int, ComponentChunk>(InitialComponentChunkCapacity, Allocator.Persistent);
            bufferChunks = new UnsafeHashMap<int, BufferComponentChunk>(InitialBufferChunkCapacity, Allocator.Persistent);

            entities = new UnsafeList<Entity>(initialCapacity, Allocator.Persistent);
            entityArchetypes = new UnsafeList<EntityArchetype>(initialCapacity, Allocator.Persistent);
            deadEntities = new UnsafeList<bool>(initialCapacity, Allocator.Persistent);

            freeEntities = new UnsafeList<Entity>(OtherCapacity, Allocator.Persistent);
            nextId = new UnsafeItem<int>(0);

            //cache
            m_globalVersions = new UnsafeItem<uint>(1);
            m_componentVersions = new UnsafeList<uint>(OtherCapacity, Allocator.Persistent);
            m_componentVersions.Resize(OtherCapacity, NativeArrayOptions.ClearMemory);
            m_queryCache = new UnsafeHashMap<ulong, QueryCacheEntry>(OtherCapacity, Allocator.Persistent);
            m_queryVersionHashes = new UnsafeHashMap<ComponentBits, ulong>(OtherCapacity, Allocator.Persistent);
        }

        public void Dispose()
        {
            foreach (var kv in componentChunks)
                kv.Value.Dispose();
            componentChunks.Dispose();

            foreach (var kv in bufferChunks)
                kv.Value.Dispose();
            bufferChunks.Dispose();

            foreach (var kv in m_queryCache)
                kv.Value.entities.Dispose();
            m_queryCache.Dispose();

            entities.Dispose();
            entityArchetypes.Dispose();
            deadEntities.Dispose();
            freeEntities.Dispose();
            nextId.Dispose();

            m_componentVersions.Dispose();
            m_queryVersionHashes.Dispose();
            m_globalVersions.Dispose();
        }

        public void Clear()
        {
            componentChunks.Clear();
            bufferChunks.Clear();
            entities.Clear();
            entityArchetypes.Clear();
            deadEntities.Clear();
            freeEntities.Clear();
            nextId.Value = 0;
            m_componentVersions.Clear();
            m_globalVersions.Value = 0;
            m_queryCache.Clear();
            m_queryVersionHashes.Clear();
        }

        public Entity CreateEntity()
        {
            int entityId;
            uint version;

            if (freeEntities.Length > 0)
            {
                var recycledEntity = freeEntities[^1];
                entityId = recycledEntity.id;
                // Increment version for recycled ID
                version = recycledEntity.version + 1;
                freeEntities.RemoveAt(freeEntities.Length - 1);
                deadEntities.Ptr[entityId] = false;
                
                var entity = new Entity
                {
                    id = entityId, version = version,
                    managerPtr = GetManagerPtr()
                };
                
                entities.Ptr[entityId] = entity;
                entityArchetypes.Ptr[entityId] = new EntityArchetype();
                deadEntities.Ptr[entityId] = false;

                return entity;
            }
            else
            {
                entityId = nextId.Value++;
                version = 1;

                var entity = new Entity
                {
                    id = entityId, version = version,
                    managerPtr = GetManagerPtr()
                };

                entities.Add(entity);
                entityArchetypes.Add(new EntityArchetype());
                deadEntities.Add(false);

                return entity;
            }
        }

        private unsafe EntityManager* GetManagerPtr()
        {
            fixed (EntityManager* ptr = &this)
                return ptr;
        }
        
        public void DestroyEntity(Entity entity)
        {
            if (!IsEntityAlive(entity)) return;
            
            DestroyEntityComponents(entity);
            DestroyEntityBuffers(entity);
    
            freeEntities.Add(entity);
            deadEntities.Ptr[entity.id] = true;
        }

        // Check if an entity is alive based on its ID and version
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEntityAlive(Entity entity)
        {
            if (entity.id < 0 || entity.id >= entities.Length)
                return false;

            return !deadEntities.Ptr[entity.id];
        }

        private void IncrementComponentVersion(int typeIndex)
        {
            if (typeIndex >= m_componentVersions.Length)
                m_componentVersions.Resize(typeIndex + 1, NativeArrayOptions.ClearMemory);

            m_componentVersions[typeIndex]++;
            m_globalVersions.Value++;

            ComponentBits componentMask = default;
            componentMask.SetComponent(typeIndex);

            foreach (var pair in m_queryVersionHashes)
            {
                if (pair.Key.HasAny(componentMask))
                    m_queryVersionHashes[pair.Key] = pair.Value + 1;
            }
        }

        private ulong GetQueryVersionHash(ref EntityQuery query)
        {
            var key = query.componentBits;
            if (!m_queryVersionHashes.TryGetValue(key, out var hash))
            {
                m_queryVersionHashes.Add(key, m_globalVersions.Value);
                hash = m_globalVersions.Value;
            }

            return hash;
        }
    }
}