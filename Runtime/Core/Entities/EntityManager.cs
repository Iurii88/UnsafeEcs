using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Utils;

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

        public EntityManager(int initialCapacity)
        {
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
                chunks.Ptr[i].Dispose();
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
    }
}