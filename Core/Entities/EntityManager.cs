using System;
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
            m_globalVersions.Value = 1;
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

        public UnsafeList<Entity> CreateEntities(EntityArchetype archetype, int count, Allocator allocator)
        {
            if (count <= 0)
                throw new ArgumentException("Count must be greater than zero", nameof(count));

            // Allocate memory for the new entities
            var result = new UnsafeList<Entity>(count, allocator);
            result.Length = count;

            // Calculate starting entity ID
            int startId = nextId.Value;
            nextId.Value += count;
            int endId = startId + count;

            // Ensure capacity in our entity lists - expand once in advance
            int requiredCapacity = endId;
            if (entities.Length < requiredCapacity)
            {
                entities.Resize(requiredCapacity);
                entityArchetypes.Resize(requiredCapacity);
                deadEntities.Resize(requiredCapacity);
            }

            // Cache manager pointer - call only once
            var managerPtr = GetManagerPtr();

            // Prepare all component chunks in advance
            // First pass to resize all chunks to minimize dictionary accesses
            foreach (var typeIndex in archetype.componentBits)
            {
                if (!TypeManager.IsBufferType(typeIndex))
                {
                    // Handle regular components
                    if (componentChunks.TryGetValue(typeIndex, out var chunk))
                    {
                        int requiredChunkCapacity = chunk.length + count;
                        if (chunk.capacity < requiredChunkCapacity)
                        {
                            chunk.Resize(Math.Max(chunk.capacity * 2, requiredChunkCapacity));
                            componentChunks[typeIndex] = chunk;
                        }

                        // Ensure componentIndices array can accommodate all entities
                        chunk.EnsureEntityCapacity(endId - 1);
                        componentChunks[typeIndex] = chunk;
                    }
                    else
                    {
                        var size = TypeManager.GetTypeSizeByIndex(typeIndex);
                        var initialCapacity = Math.Max(InitialEntityCapacity, count); // Create with adequate capacity
                        chunk = new ComponentChunk(size, initialCapacity);

                        // Ensure componentIndices array can accommodate all entities
                        chunk.EnsureEntityCapacity(endId - 1);
                        componentChunks[typeIndex] = chunk;
                    }
                }
                else
                {
                    // Handle buffer components
                    if (bufferChunks.TryGetValue(typeIndex, out var bufferChunk))
                    {
                        int requiredChunkCapacity = bufferChunk.length + count;
                        if (bufferChunk.capacity < requiredChunkCapacity)
                        {
                            bufferChunk.Resize(Math.Max(bufferChunk.capacity * 2, requiredChunkCapacity));
                            bufferChunks[typeIndex] = bufferChunk;
                        }

                        // Ensure bufferIndices array can accommodate all entities
                        bufferChunk.EnsureEntityCapacity(endId - 1);
                        bufferChunks[typeIndex] = bufferChunk;
                    }
                    else
                    {
                        var elementSize = TypeManager.GetTypeSizeByIndex(typeIndex);
                        var initialCapacity = Math.Max(InitialEntityCapacity, count);
                        bufferChunk = new BufferComponentChunk(elementSize, initialCapacity, endId);
                        bufferChunks[typeIndex] = bufferChunk;
                    }
                }
            }

            // Create all entities in a single pass
            for (int i = 0; i < count; i++)
            {
                int entityId = startId + i;

                var entity = new Entity
                {
                    id = entityId,
                    version = 1,
                    managerPtr = managerPtr
                };

                entities.Ptr[entityId] = entity;
                entityArchetypes.Ptr[entityId] = archetype;
                deadEntities.Ptr[entityId] = false;
                result.Ptr[i] = entity;
            }

            // Process regular component chunks in batch for all entities
            foreach (var typeIndex in archetype.componentBits)
            {
                if (!TypeManager.IsBufferType(typeIndex))
                {
                    var chunk = componentChunks[typeIndex]; // Only retrieve from dictionary once

                    // Create default value for this component type
                    var defaultComponentData = stackalloc byte[chunk.componentSize];
                    UnsafeUtility.MemClear(defaultComponentData, chunk.componentSize);

                    // Batch add all entities
                    for (int i = 0; i < count; i++)
                    {
                        int entityId = startId + i;

                        // Store the entity ID and update the index mapping
                        chunk.entityIds[chunk.length] = entityId;
                        chunk.componentIndices[entityId] = chunk.length;

                        // Copy default component data
                        UnsafeUtility.MemCpy(
                            (byte*)chunk.ptr + chunk.length * chunk.componentSize,
                            defaultComponentData,
                            chunk.componentSize);

                        chunk.length++;
                    }

                    // Write the updated chunk back to the dictionary
                    componentChunks[typeIndex] = chunk;

                    // Increment component version once for the entire batch
                    IncrementComponentVersion(typeIndex);
                }
                else
                {
                    // Process buffer components in batch
                    var bufferChunk = bufferChunks[typeIndex]; // Only retrieve from dictionary once
                    int initialBufferCapacity = 8; // Default initial capacity for each buffer

                    // Batch add all entity buffers
                    for (int i = 0; i < count; i++)
                    {
                        int entityId = startId + i;

                        // Add buffer for entity and initialize it
                        int bufferIndex = bufferChunk.length;

                        // Initialize the buffer at this index
                        bufferChunk.InitializeBuffer(bufferIndex, initialBufferCapacity);

                        // Set up mappings
                        bufferChunk.entityIds[bufferIndex] = entityId;
                        bufferChunk.bufferIndices[entityId] = bufferIndex;

                        bufferChunk.length++;
                    }

                    // Write the updated buffer chunk back to the dictionary
                    bufferChunks[typeIndex] = bufferChunk;

                    // Increment component version once for the entire batch
                    IncrementComponentVersion(typeIndex);
                }
            }

            return result;
        }

        private EntityManager* GetManagerPtr()
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