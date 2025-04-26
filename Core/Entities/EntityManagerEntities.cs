using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct EntityManager
    {
        public Entity CreateEntity()
        {
            int entityId;
            uint version;

            if (freeEntities.Length > 0)
            {
                var recycledEntity = freeEntities[^1];
                entityId = recycledEntity.id;
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
            var result = new UnsafeList<Entity>(count, allocator);
            result.Length = count;

            var startId = nextId.Value;
            nextId.Value += count;
            var endId = startId + count;

            var requiredCapacity = endId;
            if (entities.Length < requiredCapacity)
            {
                entities.Resize(requiredCapacity);
                entityArchetypes.Resize(requiredCapacity);
                deadEntities.Resize(requiredCapacity);
            }

            var managerPtr = GetManagerPtr();

            foreach (var typeIndex in archetype.componentBits)
            {
                if (!TypeManager.IsBufferType(typeIndex))
                {
                    if (componentChunks.TryGetValue(typeIndex, out var chunk))
                    {
                        var requiredChunkCapacity = chunk.length + count;
                        if (chunk.capacity < requiredChunkCapacity)
                        {
                            chunk.Resize(Math.Max(chunk.capacity * 2, requiredChunkCapacity));
                            componentChunks[typeIndex] = chunk;
                        }

                        chunk.EnsureEntityCapacity(endId - 1);
                        componentChunks[typeIndex] = chunk;
                    }
                    else
                    {
                        var size = TypeManager.GetTypeSizeByIndex(typeIndex);
                        var initialCapacity = Math.Max(InitialEntityCapacity, count);
                        chunk = new ComponentChunk(size, initialCapacity);

                        chunk.EnsureEntityCapacity(endId - 1);
                        componentChunks[typeIndex] = chunk;
                    }
                }
                else
                {
                    if (bufferChunks.TryGetValue(typeIndex, out var bufferChunk))
                    {
                        var requiredChunkCapacity = bufferChunk.length + count;
                        if (bufferChunk.capacity < requiredChunkCapacity)
                        {
                            bufferChunk.Resize(Math.Max(bufferChunk.capacity * 2, requiredChunkCapacity));
                            bufferChunks[typeIndex] = bufferChunk;
                        }

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

            for (var i = 0; i < count; i++)
            {
                var entityId = startId + i;

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

            foreach (var typeIndex in archetype.componentBits)
            {
                if (!TypeManager.IsBufferType(typeIndex))
                {
                    var chunk = componentChunks[typeIndex];

                    var defaultComponentData = stackalloc byte[chunk.componentSize];
                    UnsafeUtility.MemClear(defaultComponentData, chunk.componentSize);

                    for (var i = 0; i < count; i++)
                    {
                        var entityId = startId + i;

                        chunk.entityIds[chunk.length] = entityId;
                        chunk.componentIndices[entityId] = chunk.length;

                        UnsafeUtility.MemCpy(
                            (byte*)chunk.ptr + chunk.length * chunk.componentSize,
                            defaultComponentData,
                            chunk.componentSize);

                        chunk.length++;
                    }

                    componentChunks[typeIndex] = chunk;
                    IncrementComponentVersion(typeIndex);
                }
                else
                {
                    var bufferChunk = bufferChunks[typeIndex];
                    var initialBufferCapacity = 8;

                    for (var i = 0; i < count; i++)
                    {
                        var entityId = startId + i;
                        var bufferIndex = bufferChunk.length;

                        bufferChunk.InitializeBuffer(bufferIndex, initialBufferCapacity);

                        bufferChunk.entityIds[bufferIndex] = entityId;
                        bufferChunk.bufferIndices[entityId] = bufferIndex;

                        bufferChunk.length++;
                    }

                    bufferChunks[typeIndex] = bufferChunk;
                    IncrementComponentVersion(typeIndex);
                }
            }

            return result;
        }

        public void DestroyEntity(Entity entity)
        {
            if (!IsEntityAlive(entity)) return;

            DestroyEntityComponents(entity);
            DestroyEntityBuffers(entity);

            freeEntities.Add(entity);
            deadEntities.Ptr[entity.id] = true;
        }
    }
}