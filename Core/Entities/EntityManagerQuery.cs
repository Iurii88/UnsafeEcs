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
        private struct QueryCacheEntry
        {
            public UnsafeList<Entity> entities;
            public UnsafeHashMap<int, uint> componentVersions;
            public bool isValid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ValidateQueryCache(ref EntityQuery query, out ulong cacheKey, out QueryCacheEntry cacheEntry)
        {
            cacheKey = (ulong)query.GetHashCode();

            if (!m_queryCache.TryGetValue(cacheKey, out cacheEntry))
                return false;

            if (!cacheEntry.isValid)
                return false;

            foreach (var typeIndex in query.componentBits)
            {
                if (typeIndex >= m_componentVersions.Length ||
                    !cacheEntry.componentVersions.TryGetValue(typeIndex, out var cachedVersion))
                {
                    var entry = cacheEntry;
                    entry.isValid = false;
                    m_queryCache[cacheKey] = entry;
                    return false;
                }

                if (m_componentVersions[typeIndex] != cachedVersion)
                {
                    var entry = cacheEntry;
                    entry.isValid = false;
                    m_queryCache[cacheKey] = entry;
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
                    managerPtr = (EntityManager*)UnsafeUtility.AddressOf(ref this),
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

        public ReadOnlySpan<Entity> QueryEntitiesReadOnly(ref EntityQuery query)
        {
            var queryEntities = QueryEntities(ref query);
            return new ReadOnlySpan<Entity>(queryEntities.Ptr, queryEntities.Length);
        }

        private void ExecuteQueryAndUpdateCache(ref EntityQuery query, ulong cacheKey)
        {
            UnsafeList<Entity> resultEntities;
            UnsafeHashMap<int, uint> componentVersions;

            var reuseMemory = m_queryCache.TryGetValue(cacheKey, out var existingEntry);

            var matchCount = 0;
            for (var i = 0; i < entities.Length; i++)
            {
                if (i >= deadEntities.Length || deadEntities.Ptr[i])
                    continue;

                ref var archetype = ref entityArchetypes.Ptr[i];
                if (query.MatchesQuery(in archetype.componentBits))
                    matchCount++;
            }

            if (reuseMemory)
            {
                resultEntities = existingEntry.entities;
                componentVersions = existingEntry.componentVersions;

                resultEntities.Clear();
                if (resultEntities.Capacity < matchCount)
                    resultEntities.Capacity = matchCount;

                componentVersions.Clear();
            }
            else
            {
                resultEntities = new UnsafeList<Entity>(matchCount, Allocator.Persistent);
                componentVersions = new UnsafeHashMap<int, uint>(16, Allocator.Persistent);
            }

            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities.Ptr[i];
                if (entity.id >= deadEntities.Length || deadEntities.Ptr[entity.id])
                    continue;

                ref var archetype = ref entityArchetypes.Ptr[entity.id];
                if (query.MatchesQuery(in archetype.componentBits))
                {
                    entity.managerPtr = GetManagerPtr();
                    resultEntities.Add(entity);
                }
            }

            foreach (var typeIndex in query.componentBits)
            {
                if (typeIndex >= m_componentVersions.Length)
                {
                    var oldLength = m_componentVersions.Length;
                    m_componentVersions.Resize(typeIndex + 1, NativeArrayOptions.ClearMemory);

                    for (var i = oldLength; i <= typeIndex; i++)
                        m_componentVersions[i] = 1;
                }

                componentVersions[typeIndex] = m_componentVersions[typeIndex];
            }

            var cacheEntry = new QueryCacheEntry
            {
                entities = resultEntities,
                componentVersions = componentVersions,
                isValid = true
            };

            m_queryCache[cacheKey] = cacheEntry;
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
            public ulong cacheKeyValue;

            public void Execute()
            {
                ref var query = ref UnsafeUtility.AsRef<EntityQuery>(queryPtr);
                managerPtr->ExecuteQueryAndUpdateCache(ref query, cacheKeyValue);
            }
        }
    }
}