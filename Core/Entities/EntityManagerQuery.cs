using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct EntityManager
    {
        private struct QueryCacheEntry : IDisposable
        {
            public UnsafeList<Entity> entities;
            public UnsafeHashMap<int, uint> componentVersions;

            public void Dispose()
            {
                entities.Dispose();
                componentVersions.Dispose();
            }

            public void Clear()
            {
                entities.Clear();
                componentVersions.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ValidateQueryCache(ref EntityQuery query, out ulong cacheKey, out QueryCacheEntry cacheEntry)
        {
            cacheKey = (ulong)query.GetHashCode();

            if (!m_queryCache.TryGetValue(cacheKey, out cacheEntry))
                return false;

            foreach (var typeIndex in query.componentBits)
            {
                if (chunks.m_length <= typeIndex)
                    break;

                ref var chunk = ref chunks.Ptr[typeIndex];
                var version = chunk.GetVersion();

                if (!cacheEntry.componentVersions.TryGetValue(typeIndex, out var cachedVersion))
                {
                    return false;
                }

                if (version != cachedVersion)
                {
                    return false;
                }
            }

            return true;
        }

        public UnsafeList<Entity> QueryEntities(ref EntityQuery query)
        {
            if (!ValidateQueryCache(ref query, out var cacheKey, out var cacheEntry))
            {
                var job = new QueryJob
                {
                    managerPtr = m_ptr,
                    queryPtr = (EntityQuery*)UnsafeUtility.AddressOf(ref query),
                    cacheKeyValue = cacheKey
                }.Schedule();
                job.Complete();

                if (!m_queryCache.TryGetValue(cacheKey, out cacheEntry))
                {
                    throw new InvalidOperationException("Query cache entry not found after job completion");
                }
            }

            return cacheEntry.entities;
        }

        public UnsafeList<Entity> QueryEntitiesWithoutJob(ref EntityQuery query)
        {
            if (!ValidateQueryCache(ref query, out var cacheKey, out var cacheEntry))
            {
                ExecuteQueryAndUpdateCache(ref query, cacheKey);

                if (!m_queryCache.TryGetValue(cacheKey, out cacheEntry))
                {
                    throw new InvalidOperationException("Query cache entry not found after job completion");
                }
            }

            return cacheEntry.entities;
        }

        public ReadOnlySpan<Entity> QueryEntitiesReadOnly(ref EntityQuery query)
        {
            var queryEntities = QueryEntities(ref query);
            return new ReadOnlySpan<Entity>(queryEntities.Ptr, queryEntities.m_length);
        }

        private void ExecuteQueryAndUpdateCache(ref EntityQuery query, ulong cacheKey)
        {
            UnsafeList<Entity> resultEntities;
            UnsafeHashMap<int, uint> componentVersions;

            var reuseMemory = m_queryCache.TryGetValue(cacheKey, out var existingEntry);
            if (reuseMemory)
            {
                resultEntities = existingEntry.entities;
                componentVersions = existingEntry.componentVersions;
                resultEntities.Clear();
                componentVersions.Clear();
            }
            else
            {
                var initialCapacity = Math.Min(64, entities.m_length);
                resultEntities = new UnsafeList<Entity>(initialCapacity, Allocator.Persistent);
                componentVersions = new UnsafeHashMap<int, uint>(16, Allocator.Persistent);
            }

            for (var i = 0; i < entities.m_length; i++)
            {
                var entity = entities.Ptr[i];
                if (entity.id >= deadEntities.m_length || deadEntities.Ptr[entity.id])
                    continue;

                ref var archetype = ref entityArchetypes.Ptr[entity.id];
                if (query.MatchesQuery(in archetype.componentBits))
                {
                    entity.managerPtr = m_ptr;
                    resultEntities.Add(entity);
                }
            }

            foreach (var typeIndex in query.componentBits)
            {
                if (chunks.m_length > typeIndex)
                {
                    ref var chunk = ref chunks.Ptr[typeIndex];
                    componentVersions[typeIndex] = chunk.GetVersion();
                }
            }

            var cacheEntry = new QueryCacheEntry
            {
                entities = resultEntities,
                componentVersions = componentVersions
            };

            m_queryCache[cacheKey] = cacheEntry;
        }

        public EntityQuery CreateQuery()
        {
            return new EntityQuery(m_ptr);
        }

        [BurstCompile]
        private struct QueryJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public EntityQuery* queryPtr;
            [NativeDisableUnsafePtrRestriction] public EntityManager* managerPtr;
            public ulong cacheKeyValue;

            public void Execute()
            {
                ref var query = ref UnsafeUtility.AsRef<EntityQuery>(queryPtr);
                managerPtr->ExecuteQueryAndUpdateCache(ref query, cacheKeyValue);
            }
        }
    }
}