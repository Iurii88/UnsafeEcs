using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
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
                    if (typeIndex >= chunks.Length)
                    {
                        chunks.Resize(typeIndex + 1);

                        var size = TypeManager.GetTypeSizeByIndex(typeIndex);
                        var stackChunk = new ComponentChunk(size, InitialEntityCapacity);
                        var chunk = (ComponentChunk*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ComponentChunk>(), UnsafeUtility.AlignOf<ComponentChunk>(), Allocator.Persistent);
                        UnsafeUtility.CopyStructureToPtr(ref stackChunk, chunk);
                        chunks.Ptr[typeIndex] = ChunkUnion.FromComponentChunk(chunk);
                    }

                    var existingChunk = chunks.Ptr[typeIndex].AsComponentChunk();
                    var requiredChunkCapacity = existingChunk->length + count;
                    existingChunk->Resize(requiredChunkCapacity);
                    existingChunk->EnsureEntityCapacity(endId - 1);

                    var defaultComponentData = (byte*)UnsafeUtility.Malloc(existingChunk->componentSize, UnsafeUtility.AlignOf<byte>(), Allocator.Temp);
                    UnsafeUtility.MemClear(defaultComponentData, existingChunk->componentSize);

                    for (var i = 0; i < count; i++)
                    {
                        var entityId = startId + i;
                        existingChunk->Add(entityId, defaultComponentData);
                    }

                    UnsafeUtility.Free(defaultComponentData, Allocator.Temp);
                }
                else
                {
                    if (typeIndex >= chunks.Length)
                    {
                        chunks.Resize(typeIndex + 1);

                        var elementSize = TypeManager.GetTypeSizeByIndex(typeIndex);
                        var bufferChunk = (BufferChunk*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BufferChunk>(), UnsafeUtility.AlignOf<BufferChunk>(), Allocator.Persistent);
                        *bufferChunk = new BufferChunk(elementSize, count, endId);
                        chunks.Ptr[typeIndex] = ChunkUnion.FromBufferChunk(bufferChunk);
                    }

                    var existingBufferChunk = chunks.Ptr[typeIndex].AsBufferChunk();
                    if (existingBufferChunk == null)
                    {
                        throw new InvalidOperationException($"Component type {typeIndex} is registered as a regular component but trying to add as a buffer.");
                    }

                    var requiredChunkCapacity = existingBufferChunk->length + count;
                    if (existingBufferChunk->capacity < requiredChunkCapacity)
                    {
                        existingBufferChunk->Resize(math.max(existingBufferChunk->capacity * 2, requiredChunkCapacity));
                    }

                    existingBufferChunk->EnsureEntityCapacity(endId - 1);

                    var initialBufferCapacity = 8;
                    for (var i = 0; i < count; i++)
                    {
                        var entityId = startId + i;
                        var bufferIndex = existingBufferChunk->length;

                        existingBufferChunk->InitializeBuffer(bufferIndex, initialBufferCapacity);
                        existingBufferChunk->entityIds[bufferIndex] = entityId;
                        existingBufferChunk->bufferIndices[entityId] = bufferIndex;
                        existingBufferChunk->length++;
                    }
                }

                IncrementComponentVersion(typeIndex);
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