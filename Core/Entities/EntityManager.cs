using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

        private UnsafeHashMap<ulong, QueryCacheEntry> m_queryCache;
        private UnsafeList<uint> m_componentVersions;

        public EntityManager(int initialCapacity)
        {
            componentChunks = new UnsafeHashMap<int, ComponentChunk>(InitialComponentChunkCapacity, Allocator.Persistent);
            bufferChunks = new UnsafeHashMap<int, BufferComponentChunk>(InitialBufferChunkCapacity, Allocator.Persistent);

            entities = new UnsafeList<Entity>(initialCapacity, Allocator.Persistent);
            entityArchetypes = new UnsafeList<EntityArchetype>(initialCapacity, Allocator.Persistent);
            deadEntities = new UnsafeList<bool>(initialCapacity, Allocator.Persistent);

            freeEntities = new UnsafeList<Entity>(OtherCapacity, Allocator.Persistent);
            nextId = new UnsafeItem<int>(0);

            // Initialize query cache
            m_componentVersions = new UnsafeList<uint>(32, Allocator.Persistent);
            m_componentVersions.Resize(32, NativeArrayOptions.ClearMemory);
            for (int i = 0; i < m_componentVersions.Length; i++)
                m_componentVersions[i] = 1;

            m_queryCache = new UnsafeHashMap<ulong, QueryCacheEntry>(OtherCapacity, Allocator.Persistent);
        }

        public void Dispose()
        {
            foreach (var kv in componentChunks)
                kv.Value.Dispose();
            componentChunks.Dispose();

            foreach (var kv in bufferChunks)
                kv.Value.Dispose();
            bufferChunks.Dispose();

            entities.Dispose();
            entityArchetypes.Dispose();
            deadEntities.Dispose();
            freeEntities.Dispose();
            nextId.Dispose();

            // Dispose query cache
            if (m_queryCache.IsCreated)
            {
                var enumerator = m_queryCache.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    enumerator.Current.Value.Dispose();
                }

                m_queryCache.Dispose();
            }

            if (m_componentVersions.IsCreated)
                m_componentVersions.Dispose();
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

            // Clear query cache
            if (m_queryCache.IsCreated)
            {
                var enumerator = m_queryCache.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    enumerator.Current.Value.Clear();
                }

                m_queryCache.Clear();
            }

            if (m_componentVersions.IsCreated)
            {
                for (int i = 0; i < m_componentVersions.Length; i++)
                    m_componentVersions[i] = 1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEntityAlive(Entity entity)
        {
            if (entity.id < 0 || entity.id >= entities.Length)
                return false;

            return !deadEntities.Ptr[entity.id];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private EntityManager* GetManagerPtr()
        {
            return (EntityManager*)UnsafeUtility.AddressOf(ref this);
        }
    }
}