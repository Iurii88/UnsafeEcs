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
            public ulong versionChecksum;

            public void Dispose()
            {
                entities.Dispose();
            }

            public void Clear()
            {
                entities.Clear();
                versionChecksum = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ValidateQueryCache(ref EntityQuery query, out ulong cacheKey, out QueryCacheEntry cacheEntry)
        {
            cacheKey = (ulong)query.GetHashCode();

            if (!m_queryCache.TryGetValue(cacheKey, out cacheEntry))
                return false;

            var currentChecksum = ComputeVersionChecksum(ref query);
            return currentChecksum == cacheEntry.versionChecksum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong ComputeVersionChecksum(ref EntityQuery query)
        {
            var bits = query.withMask | query.withoutMask | query.withAnyMask;
            ulong checksum = 0;
            var chunksLength = chunks.m_length;
            var chunksPtr = chunks.Ptr;

            // Process part0 (bits 0-63)
            var partBits = bits.part0;
            while (partBits != 0)
            {
                var bitIndex = Unity.Mathematics.math.tzcnt(partBits);
                if (bitIndex < chunksLength)
                {
                    var version = chunksPtr[bitIndex].GetVersion();
                    checksum ^= ((ulong)version << 32) | ((ulong)bitIndex * 2654435761UL);
                }
                partBits &= partBits - 1;
            }

            // Process part1 (bits 64-127)
            partBits = bits.part1;
            while (partBits != 0)
            {
                var bitIndex = Unity.Mathematics.math.tzcnt(partBits) + 64;
                if (bitIndex < chunksLength)
                {
                    var version = chunksPtr[bitIndex].GetVersion();
                    checksum ^= ((ulong)version << 32) | ((ulong)bitIndex * 2654435761UL);
                }
                partBits &= partBits - 1;
            }

            // Process part2 (bits 128-191)
            partBits = bits.part2;
            while (partBits != 0)
            {
                var bitIndex = Unity.Mathematics.math.tzcnt(partBits) + 128;
                if (bitIndex < chunksLength)
                {
                    var version = chunksPtr[bitIndex].GetVersion();
                    checksum ^= ((ulong)version << 32) | ((ulong)bitIndex * 2654435761UL);
                }
                partBits &= partBits - 1;
            }

            // Process part3 (bits 192-255)
            partBits = bits.part3;
            while (partBits != 0)
            {
                var bitIndex = Unity.Mathematics.math.tzcnt(partBits) + 192;
                if (bitIndex < chunksLength)
                {
                    var version = chunksPtr[bitIndex].GetVersion();
                    checksum ^= ((ulong)version << 32) | ((ulong)bitIndex * 2654435761UL);
                }
                partBits &= partBits - 1;
            }

            return checksum;
        }

        public UnsafeList<Entity> QueryEntities(ref EntityQuery query)
        {
            if (!ValidateQueryCache(ref query, out var cacheKey, out var cacheEntry))
            {
                var job = new QueryJob
                {
                    managerPtr = m_managerPtr,
                    queryPtr = (EntityQuery*)UnsafeUtility.AddressOf(ref query),
                    cacheKeyValue = cacheKey
                }.Schedule();
                job.Complete();

                if (!m_queryCache.TryGetValue(cacheKey, out cacheEntry))
                    throw new InvalidOperationException("Query cache entry not found after job completion");
            }

            return cacheEntry.entities;
        }

        public UnsafeList<Entity> QueryEntities<TFilter>(ref EntityQuery query, ref TFilter filter) where TFilter : unmanaged, IQueryFilter
        {
            var cacheKey = (ulong)query.GetHashCode();
            var job = new QueryJob<TFilter>
            {
                managerPtr = m_managerPtr,
                queryPtr = (EntityQuery*)UnsafeUtility.AddressOf(ref query),
                filterPtr = (TFilter*)UnsafeUtility.AddressOf(ref filter),
                cacheKeyValue = cacheKey
            }.Schedule();
            job.Complete();

            if (!m_queryCache.TryGetValue(cacheKey, out var cacheEntry))
                throw new InvalidOperationException("Query cache entry not found after job completion");

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

        public UnsafeList<Entity> QueryEntitiesWithoutJob<TFilter>(ref EntityQuery query, ref TFilter filter) where TFilter : unmanaged, IQueryFilter
        {
            if (!ValidateQueryCache(ref query, out var cacheKey, out var cacheEntry))
            {
                ExecuteQueryAndUpdateCache(ref query, cacheKey, ref filter);

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

            var reuseMemory = m_queryCache.TryGetValue(cacheKey, out var existingEntry);
            if (reuseMemory)
            {
                resultEntities = existingEntry.entities;
                resultEntities.Clear();
            }
            else
            {
                var initialCapacity = Math.Min(16, entities.m_length);
                resultEntities = new UnsafeList<Entity>(initialCapacity, Allocator.Persistent);
            }

            for (var i = 0; i < entities.m_length; i++)
            {
                var entity = entities.Ptr[i];
                if (entity.id >= deadEntities.m_length || deadEntities.Ptr[entity.id])
                    continue;

                ref var archetype = ref entityArchetypes.Ptr[entity.id];
                if (query.MatchesQuery(in archetype.componentBits))
                {
                    entity.managerPtr = m_managerPtr;
                    resultEntities.Add(entity);
                }
            }

            var cacheEntry = new QueryCacheEntry
            {
                entities = resultEntities,
                versionChecksum = ComputeVersionChecksum(ref query)
            };

            m_queryCache[cacheKey] = cacheEntry;
        }

        private void ExecuteQueryAndUpdateCache<TFilter>(ref EntityQuery query, ulong cacheKey, ref TFilter filter) where TFilter : unmanaged, IQueryFilter
        {
            UnsafeList<Entity> resultEntities;

            var reuseMemory = m_queryCache.TryGetValue(cacheKey, out var existingEntry);
            if (reuseMemory)
            {
                resultEntities = existingEntry.entities;
                resultEntities.Clear();
            }
            else
            {
                var initialCapacity = Math.Min(16, entities.m_length);
                resultEntities = new UnsafeList<Entity>(initialCapacity, Allocator.Persistent);
            }

            for (var i = 0; i < entities.m_length; i++)
            {
                var entity = entities.Ptr[i];
                if (entity.id >= deadEntities.m_length || deadEntities.Ptr[entity.id])
                    continue;

                ref var archetype = ref entityArchetypes.Ptr[entity.id];
                if (query.MatchesQuery(in archetype.componentBits) && filter.Validate(entity))
                {
                    entity.managerPtr = m_managerPtr;
                    resultEntities.Add(entity);
                }
            }

            var cacheEntry = new QueryCacheEntry
            {
                entities = resultEntities,
                versionChecksum = ComputeVersionChecksum(ref query)
            };

            m_queryCache[cacheKey] = cacheEntry;
        }

        public EntityQuery CreateQuery()
        {
            return new EntityQuery(m_managerPtr);
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

        [BurstCompile]
        private struct QueryJob<TFilter> : IJob where TFilter : unmanaged, IQueryFilter
        {
            [NativeDisableUnsafePtrRestriction] public EntityQuery* queryPtr;
            [NativeDisableUnsafePtrRestriction] public EntityManager* managerPtr;
            [NativeDisableUnsafePtrRestriction] public TFilter* filterPtr;
            public ulong cacheKeyValue;

            public void Execute()
            {
                ref var query = ref UnsafeUtility.AsRef<EntityQuery>(queryPtr);
                ref var filter = ref UnsafeUtility.AsRef<TFilter>(filterPtr);
                managerPtr->ExecuteQueryAndUpdateCache(ref query, cacheKeyValue, ref filter);
            }
        }

        public JobHandle QueryEntities(ref EntityQuery query, ref UnsafeList<Entity> resultEntities, JobHandle inputDependency = default)
        {
            var job = new QueryToListJob
            {
                managerPtr = m_managerPtr,
                queryPtr = (EntityQuery*)UnsafeUtility.AddressOf(ref query),
                resultEntitiesPtr = (UnsafeList<Entity>*)UnsafeUtility.AddressOf(ref resultEntities)
            };
            return job.Schedule(inputDependency);
        }

        public JobHandle QueryEntities<TFilter>(ref EntityQuery query, ref UnsafeList<Entity> resultEntities, ref TFilter filter, JobHandle inputDependency = default)
            where TFilter : unmanaged, IQueryFilter
        {
            var job = new QueryToListJob<TFilter>
            {
                managerPtr = m_managerPtr,
                queryPtr = (EntityQuery*)UnsafeUtility.AddressOf(ref query),
                resultEntitiesPtr = (UnsafeList<Entity>*)UnsafeUtility.AddressOf(ref resultEntities),
                filterPtr = (TFilter*)UnsafeUtility.AddressOf(ref filter)
            };
            return job.Schedule(inputDependency);
        }

        [BurstCompile]
        private struct QueryToListJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public EntityQuery* queryPtr;
            [NativeDisableUnsafePtrRestriction] public EntityManager* managerPtr;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<Entity>* resultEntitiesPtr;

            public void Execute()
            {
                ref var query = ref UnsafeUtility.AsRef<EntityQuery>(queryPtr);
                ref var resultEntities = ref UnsafeUtility.AsRef<UnsafeList<Entity>>(resultEntitiesPtr);
                ref var manager = ref UnsafeUtility.AsRef<EntityManager>(managerPtr);

                resultEntities.Clear();

                for (var i = 0; i < manager.entities.m_length; i++)
                {
                    var entity = manager.entities.Ptr[i];
                    if (entity.id >= manager.deadEntities.m_length || manager.deadEntities.Ptr[entity.id])
                        continue;

                    ref var archetype = ref manager.entityArchetypes.Ptr[entity.id];
                    if (query.MatchesQuery(in archetype.componentBits))
                    {
                        entity.managerPtr = managerPtr;
                        resultEntities.Add(entity);
                    }
                }
            }
        }

        [BurstCompile]
        private struct QueryToListJob<TFilter> : IJob where TFilter : unmanaged, IQueryFilter
        {
            [NativeDisableUnsafePtrRestriction] public EntityQuery* queryPtr;
            [NativeDisableUnsafePtrRestriction] public EntityManager* managerPtr;
            [NativeDisableUnsafePtrRestriction] public UnsafeList<Entity>* resultEntitiesPtr;
            [NativeDisableUnsafePtrRestriction] public TFilter* filterPtr;

            public void Execute()
            {
                ref var query = ref UnsafeUtility.AsRef<EntityQuery>(queryPtr);
                ref var resultEntities = ref UnsafeUtility.AsRef<UnsafeList<Entity>>(resultEntitiesPtr);
                ref var manager = ref UnsafeUtility.AsRef<EntityManager>(managerPtr);
                ref var filter = ref UnsafeUtility.AsRef<TFilter>(filterPtr);

                resultEntities.Clear();

                for (var i = 0; i < manager.entities.m_length; i++)
                {
                    var entity = manager.entities.Ptr[i];
                    if (entity.id >= manager.deadEntities.m_length || manager.deadEntities.Ptr[entity.id])
                        continue;

                    ref var archetype = ref manager.entityArchetypes.Ptr[entity.id];
                    if (query.MatchesQuery(in archetype.componentBits) && filter.Validate(entity))
                    {
                        entity.managerPtr = managerPtr;
                        resultEntities.Add(entity);
                    }
                }
            }
        }

        public JobHandle QueryEntities(ref EntityQuery query, NativeArray<Entity> resultEntities, NativeReference<int> resultCount, JobHandle inputDependency = default)
        {
            var job = new QueryToArrayJob
            {
                managerPtr = m_managerPtr,
                queryPtr = (EntityQuery*)UnsafeUtility.AddressOf(ref query),
                resultEntities = resultEntities,
                resultCount = resultCount
            };
            return job.Schedule(inputDependency);
        }

        public JobHandle QueryEntities<TFilter>(ref EntityQuery query, NativeArray<Entity> resultEntities, NativeReference<int> resultCount, ref TFilter filter, JobHandle inputDependency = default)
            where TFilter : unmanaged, IQueryFilter
        {
            var job = new QueryToArrayJob<TFilter>
            {
                managerPtr = m_managerPtr,
                queryPtr = (EntityQuery*)UnsafeUtility.AddressOf(ref query),
                resultEntities = resultEntities,
                resultCount = resultCount,
                filterPtr = (TFilter*)UnsafeUtility.AddressOf(ref filter)
            };
            return job.Schedule(inputDependency);
        }

        [BurstCompile]
        private struct QueryToArrayJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public EntityQuery* queryPtr;
            [NativeDisableUnsafePtrRestriction] public EntityManager* managerPtr;
            public NativeArray<Entity> resultEntities;
            public NativeReference<int> resultCount;

            public void Execute()
            {
                ref var query = ref UnsafeUtility.AsRef<EntityQuery>(queryPtr);
                ref var manager = ref UnsafeUtility.AsRef<EntityManager>(managerPtr);

                var count = 0;
                var capacity = resultEntities.Length;

                for (var i = 0; i < manager.entities.m_length && count < capacity; i++)
                {
                    var entity = manager.entities.Ptr[i];
                    if (entity.id >= manager.deadEntities.m_length || manager.deadEntities.Ptr[entity.id])
                        continue;

                    ref var archetype = ref manager.entityArchetypes.Ptr[entity.id];
                    if (query.MatchesQuery(in archetype.componentBits))
                    {
                        entity.managerPtr = managerPtr;
                        resultEntities[count++] = entity;
                    }
                }

                resultCount.Value = count;
            }
        }

        [BurstCompile]
        private struct QueryToArrayJob<TFilter> : IJob where TFilter : unmanaged, IQueryFilter
        {
            [NativeDisableUnsafePtrRestriction] public EntityQuery* queryPtr;
            [NativeDisableUnsafePtrRestriction] public EntityManager* managerPtr;
            public NativeArray<Entity> resultEntities;
            public NativeReference<int> resultCount;
            [NativeDisableUnsafePtrRestriction] public TFilter* filterPtr;

            public void Execute()
            {
                ref var query = ref UnsafeUtility.AsRef<EntityQuery>(queryPtr);
                ref var manager = ref UnsafeUtility.AsRef<EntityManager>(managerPtr);
                ref var filter = ref UnsafeUtility.AsRef<TFilter>(filterPtr);

                var count = 0;
                var capacity = resultEntities.Length;

                for (var i = 0; i < manager.entities.m_length && count < capacity; i++)
                {
                    var entity = manager.entities.Ptr[i];
                    if (entity.id >= manager.deadEntities.m_length || manager.deadEntities.Ptr[entity.id])
                        continue;

                    ref var archetype = ref manager.entityArchetypes.Ptr[entity.id];
                    if (query.MatchesQuery(in archetype.componentBits) && filter.Validate(entity))
                    {
                        entity.managerPtr = managerPtr;
                        resultEntities[count++] = entity;
                    }
                }

                resultCount.Value = count;
            }
        }
    }
}