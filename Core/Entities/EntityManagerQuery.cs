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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ValidateQueryCache(ref EntityQuery query, out int cacheKey, out ulong versionHash, out QueryCacheEntry cacheEntry)
        {
            cacheKey = query.GetHashCode();
            versionHash = GetQueryVersionHash(ref query);
            return m_queryCache.TryGetValue((ulong)cacheKey, out cacheEntry) &&
                   cacheEntry.componentVersionHash == versionHash;
        }

        public UnsafeList<Entity> QueryEntities(ref EntityQuery query)
        {
            if (!ValidateQueryCache(ref query, out var cacheKey, out var versionHash, out var cacheEntry))
            {
                var job = new QueryJob
                {
                    managerPtr = (EntityManager*)UnsafeUtility.AddressOf(ref this),
                    queryPtr = (EntityQuery*)UnsafeUtility.AddressOf(ref query),
                    cacheKeyValue = cacheKey,
                    versionHash = versionHash
                }.Schedule();
                job.Complete();

                cacheEntry = m_queryCache[(ulong)cacheKey];
            }

            return cacheEntry.entities;
        }

        public ReadOnlySpan<Entity> QueryEntitiesReadOnly(ref EntityQuery query)
        {
            var queryEntities = QueryEntities(ref query);
            return new ReadOnlySpan<Entity>(queryEntities.Ptr, queryEntities.Length);
        }

        private void ExecuteQueryAndUpdateCache(ref EntityQuery query, int cacheKey, ulong versionHash)
        {
            var matchCount = 0;
            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities.Ptr[i];
                ref var archetype = ref entityArchetypes.Ptr[entity.id];
                if (query.MatchesQuery(in archetype.componentBits))
                    matchCount++;
            }

            UnsafeList<Entity> resultEntities;
            if (m_queryCache.TryGetValue((ulong)cacheKey, out var existingCache))
            {
                if (existingCache.entities.Capacity < matchCount)
                    existingCache.entities.Resize(matchCount);

                resultEntities = existingCache.entities;
            }
            else
            {
                resultEntities = new UnsafeList<Entity>(matchCount, Allocator.Persistent);
                m_queryCache.Add((ulong)cacheKey, new QueryCacheEntry
                {
                    entities = resultEntities,
                    componentVersionHash = versionHash
                });
            }

            resultEntities.Length = 0;

            for (var i = 0; i < entities.Length; i++)
            {
                ref var entity = ref entities.Ptr[i];
                if (deadEntities.Ptr[entity.id])
                    continue;

                ref var archetype = ref entityArchetypes.Ptr[entity.id];
                if (query.MatchesQuery(in archetype.componentBits))
                {
                    entity.managerPtr = GetManagerPtr();
                    resultEntities.Add(entity);
                }
            }

            var cache = m_queryCache[(ulong)cacheKey];
            cache.componentVersionHash = versionHash;
            cache.entities = resultEntities;
            m_queryCache[(ulong)cacheKey] = cache;
        }

        public EntityQuery CreateQuery()
        {
            return new EntityQuery(GetManagerPtr());
        }

        [BurstCompile]
        private struct QueryJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public EntityQuery* queryPtr;
            [NativeDisableUnsafePtrRestriction] public EntityManager* managerPtr;
            public int cacheKeyValue;
            public ulong versionHash;

            public void Execute()
            {
                ref var query = ref UnsafeUtility.AsRef<EntityQuery>(queryPtr);
                managerPtr->ExecuteQueryAndUpdateCache(ref query, cacheKeyValue, versionHash);
            }
        }
    }
}