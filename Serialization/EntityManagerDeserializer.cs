using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Serialization
{
    public unsafe struct EntityManagerDeserializer
    {
        // Magic number for format verification
        private const int SerializationMagic = 0xEC51;

        public static void Deserialize(MemoryRegion memoryRegion, ref EntityManager entityManager)
        {
            fixed (EntityManager* localPtr = &entityManager)
            {
                var deserializeJob = new DeserializeJob
                {
                    manager = localPtr,
                    ptr = memoryRegion.ptr
                };
                deserializeJob.Schedule().Complete();
            }
        }

        [BurstCompile]
        private struct DeserializeJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public EntityManager* manager;
            [NativeDisableUnsafePtrRestriction] public byte* ptr;

            public void Execute()
            {
                int position = 0;

                // Read header - one field at a time
                // Magic number (4 bytes)
                int magic = *(int*)(ptr + position);
                if (magic != SerializationMagic) throw new ArgumentException("Invalid data format");
                position += 4;

                // Type hash (8 bytes)
                long typeInfoHash = *(long*)(ptr + position);
                position += 8;

                // Verify type info matches
                long currentHash = 0;
                foreach (var kv in TypeManager.TypeToIndex.Data)
                {
                    currentHash = currentHash * 31 + kv.Key;
                }

                if (typeInfoHash != currentHash)
                {
                    throw new InvalidOperationException("Type information mismatch between serialized data and current runtime");
                }

                // nextId (4 bytes)
                int nextId = *(int*)(ptr + position);
                position += 4;
                manager->nextId.Value = nextId;

                // Free entities count (4 bytes)
                int freeIdsCount = *(int*)(ptr + position);
                position += 4;

                // Read archetype count (4 bytes)
                int entityArchetypesCount = *(int*)(ptr + position);
                position += 4;

                // Read entity count (4 bytes)
                int entityCount = *(int*)(ptr + position);
                position += 4;

                // Read dead entities count (4 bytes)
                int deadEntitiesCount = *(int*)(ptr + position);
                position += 4;

                // Read component count (4 bytes)
                int componentTypeCount = *(int*)(ptr + position);
                position += 4;

                // Read buffer count (4 bytes)
                int bufferTypeCount = *(int*)(ptr + position);
                position += 4;

                // Read free entities
                manager->freeEntities.Clear();
                for (int i = 0; i < freeIdsCount; i++)
                {
                    int freeId = *(int*)(ptr + position);
                    position += 4;
                    uint freeVersion = *(uint*)(ptr + position);
                    position += 4;
                    manager->freeEntities.Add(new Entity { id = freeId, version = freeVersion });
                }

                // Read archetypes
                manager->entityArchetypes.Clear();
                for (int i = 0; i < entityArchetypesCount; i++)
                {
                    // Read ComponentBits
                    EntityArchetype archetype;
                    archetype.componentBits = new ComponentBits();

                    archetype.componentBits.part0 = *(ulong*)(ptr + position);
                    position += 8;
                    archetype.componentBits.part1 = *(ulong*)(ptr + position);
                    position += 8;
                    archetype.componentBits.part2 = *(ulong*)(ptr + position);
                    position += 8;
                    archetype.componentBits.part3 = *(ulong*)(ptr + position);
                    position += 8;

                    manager->entityArchetypes.Add(archetype);
                }

                // Read entities
                manager->entities.Clear();
                for (int i = 0; i < entityCount; i++)
                {
                    int id = *(int*)(ptr + position);
                    position += 4;
                    uint version = *(uint*)(ptr + position);
                    position += 4;

                    var entity = new Entity { id = id, version = version };
                    manager->entities.Add(entity);
                }

                // Read dead entities (1 byte per entity)
                manager->deadEntities.Clear();
                for (int i = 0; i < deadEntitiesCount; i++)
                {
                    bool isDead = *(bool*)(ptr + position);
                    position += 1;
                    manager->deadEntities.Add(isDead);
                }

                // Read components
                manager->componentChunks.Clear();
                for (int typeIdx = 0; typeIdx < componentTypeCount; typeIdx++)
                {
                    int componentTypeIndex = *(int*)(ptr + position);
                    position += 4;

                    int componentCount = *(int*)(ptr + position);
                    position += 4;

                    int componentCapacity = *(int*)(ptr + position);
                    position += 4;

                    int componentSize = *(int*)(ptr + position);
                    position += 4;

                    int maxEntityId = *(int*)(ptr + position);
                    position += 4;

                    // Create component chunk
                    var chunk = new ComponentChunk(componentSize, componentCapacity);
                    chunk.length = componentCount;
                    chunk.maxEntityId = maxEntityId;

                    // Allocate or resize arrays if needed to match maxEntityId
                    if (chunk.entityIds == null || chunk.capacity < componentCount)
                    {
                        // Free existing array if needed
                        if (chunk.entityIds != null)
                        {
                            UnsafeUtility.Free(chunk.entityIds, Allocator.Persistent);
                        }

                        // Allocate new array
                        chunk.entityIds = (int*)UnsafeUtility.Malloc(
                            sizeof(int) * componentCapacity,
                            UnsafeUtility.AlignOf<int>(),
                            Allocator.Persistent);
                    }

                    if (chunk.componentIndices == null || maxEntityId >= chunk.maxEntityId)
                    {
                        // Free existing array if needed
                        if (chunk.componentIndices != null)
                        {
                            UnsafeUtility.Free(chunk.componentIndices, Allocator.Persistent);
                        }

                        // Allocate new array with sufficient size
                        int newSize = maxEntityId + 1;
                        chunk.componentIndices = (int*)UnsafeUtility.Malloc(
                            sizeof(int) * newSize,
                            UnsafeUtility.AlignOf<int>(),
                            Allocator.Persistent);

                        // Initialize all indices to -1 (no component)
                        UnsafeUtility.MemSet(chunk.componentIndices, 0xFF, sizeof(int) * newSize); // 0xFF gives -1 for int
                    }

                    // Read entityIds array (length-sized)
                    for (int i = 0; i < componentCount; i++)
                    {
                        chunk.entityIds[i] = *(int*)(ptr + position);
                        position += 4;
                    }

                    // Read componentIndices array (maxEntityId+1-sized)
                    for (int i = 0; i <= maxEntityId; i++)
                    {
                        chunk.componentIndices[i] = *(int*)(ptr + position);
                        position += 4;
                    }

                    // Read component data
                    for (int i = 0; i < componentCount; i++)
                    {
                        // Copy component data
                        UnsafeUtility.MemCpy(
                            (byte*)chunk.ptr + i * componentSize,
                            ptr + position,
                            componentSize);
                        position += componentSize;
                    }

                    manager->componentChunks.Add(componentTypeIndex, chunk);
                }

                // Read buffers
                manager->bufferChunks.Clear();
                for (int typeIdx = 0; typeIdx < bufferTypeCount; typeIdx++)
                {
                    int bufferTypeIndex = *(int*)(ptr + position);
                    position += 4;

                    int bufferCount = *(int*)(ptr + position);
                    position += 4;

                    int chunkCapacity = *(int*)(ptr + position);
                    position += 4;

                    int elementSize = *(int*)(ptr + position);
                    position += 4;

                    int maxEntityId = *(int*)(ptr + position);
                    position += 4;

                    var chunk = new BufferComponentChunk(elementSize, chunkCapacity);
                    chunk.length = bufferCount;
                    chunk.maxEntityId = maxEntityId;

                    // Allocate or resize arrays if needed to match maxEntityId
                    if (chunk.entityIds == null || chunk.capacity < bufferCount)
                    {
                        // Free existing array if needed
                        if (chunk.entityIds != null)
                        {
                            UnsafeUtility.Free(chunk.entityIds, Allocator.Persistent);
                        }

                        // Allocate new array
                        chunk.entityIds = (int*)UnsafeUtility.Malloc(
                            sizeof(int) * chunkCapacity,
                            UnsafeUtility.AlignOf<int>(),
                            Allocator.Persistent);
                    }

                    if (chunk.bufferIndices == null || maxEntityId >= chunk.maxEntityId)
                    {
                        // Free existing array if needed
                        if (chunk.bufferIndices != null)
                        {
                            UnsafeUtility.Free(chunk.bufferIndices, Allocator.Persistent);
                        }

                        // Allocate new array with sufficient size
                        int newSize = maxEntityId + 1;
                        chunk.bufferIndices = (int*)UnsafeUtility.Malloc(
                            sizeof(int) * newSize,
                            UnsafeUtility.AlignOf<int>(),
                            Allocator.Persistent);

                        // Initialize all indices to -1 (no buffer)
                        UnsafeUtility.MemSet(chunk.bufferIndices, 0xFF, sizeof(int) * newSize); // 0xFF gives -1 for int
                    }

                    // Read entityIds array (length-sized)
                    for (int i = 0; i < bufferCount; i++)
                    {
                        chunk.entityIds[i] = *(int*)(ptr + position);
                        position += 4;
                    }

                    // Read bufferIndices array (maxEntityId+1-sized)
                    for (int i = 0; i <= maxEntityId; i++)
                    {
                        chunk.bufferIndices[i] = *(int*)(ptr + position);
                        position += 4;
                    }

                    for (int i = 0; i < bufferCount; i++)
                    {
                        // Initialize buffer
                        chunk.InitializeBuffer(i);

                        // Get pointer to buffer header
                        var header = (BufferHeader*)(chunk.ptr + i * chunk.headerSize);

                        // Read serialized buffer metadata
                        int bufferLength = *(int*)(ptr + position);
                        position += 4;
                        int bufferCapacity = *(int*)(ptr + position);
                        position += 4;

                        // Handle buffer data if present
                        if (bufferLength > 0)
                        {
                            // Allocate memory for buffer data if needed
                            if (header->capacity < bufferLength)
                            {
                                if (header->pointer != null)
                                {
                                    UnsafeUtility.Free(header->pointer, Allocator.Persistent);
                                }

                                header->pointer = (byte*)UnsafeUtility.Malloc(
                                    bufferLength * elementSize,
                                    UnsafeUtility.AlignOf<byte>(),
                                    Allocator.Persistent);

                                header->capacity = bufferCapacity;
                            }

                            // Set buffer length
                            header->length = bufferLength;

                            // Copy buffer content
                            UnsafeUtility.MemCpy(
                                header->pointer,
                                ptr + position,
                                bufferLength * elementSize);

                            position += bufferLength * elementSize;
                        }
                        else
                        {
                            // Empty buffer, ensure length is 0
                            header->length = 0;
                        }
                    }

                    manager->bufferChunks.Add(bufferTypeIndex, chunk);
                }
            }
        }
    }
}