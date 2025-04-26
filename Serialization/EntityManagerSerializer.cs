using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnsafeEcs.Core.DynamicBuffers;
using UnsafeEcs.Core.Entities;
using UnsafeEcs.Core.Utils;

namespace UnsafeEcs.Serialization
{
    public unsafe struct EntityManagerSerializer
    {
        // Magic number for format verification
        private const int SerializationMagic = 0xEC51;

        public static byte[] Serialize(ReferenceWrapper<EntityManager> managerWrapper)
        {
            // Calculate total size needed
            var sizeCalculator = new UnsafeItem<int>(0, Allocator.TempJob);
            new SizeCalculationJob
                {
                    manager = managerWrapper.ptr,
                    sizeCalculator = sizeCalculator
                }.Schedule()
                .Complete();

            var output = new byte[sizeCalculator.Value];

            fixed (byte* ptr = output)
            {
                new SerializeJob
                    {
                        manager = managerWrapper.ptr,
                        ptr = ptr
                    }.Schedule()
                    .Complete();
            }

            sizeCalculator.Dispose();

            return output;
        }

        [BurstCompile]
        private struct SizeCalculationJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public EntityManager* manager;

            public UnsafeItem<int> sizeCalculator;

            public void Execute()
            {
                // Header: 40 bytes total, broken down as:
                var totalSize = 0;

                totalSize += 4; // Magic number (int)
                totalSize += 8; // Type hash (long)
                totalSize += 4; // nextId (int)
                totalSize += 4; // freeEntities count (int)
                totalSize += 4; // entityArchetypes count (int)
                totalSize += 4; // entities count (int)
                totalSize += 4; // deadEntities count (int)
                totalSize += 4; // chunks count (int)

                // Free entities data
                totalSize += manager->freeEntities.Length * 8; // id is int (4 bytes), version is int (4 bytes)

                // For each archetype: entity (8 bytes) + 4 longs for ComponentBits (32 bytes)
                totalSize += manager->entityArchetypes.Length * (4 + 32);

                // Entity data
                totalSize += manager->entities.Length * 8; // Entity ID + version

                // Dead entities data (1 byte per entity)
                totalSize += manager->deadEntities.Length;

                // Chunks data
                for (int i = 0; i < manager->chunks.Length; i++)
                {
                    ref var chunkUnion = ref manager->chunks.Ptr[i];

                    totalSize += 4; // Type index (chunk array index)
                    totalSize += 1; // isBuffer flag (bool)

                    if (chunkUnion.isBuffer)
                    {
                        var bufferChunk = chunkUnion.AsBufferChunk();

                        totalSize += 4; // length
                        totalSize += 4; // capacity
                        totalSize += 4; // element size
                        totalSize += 4; // maxEntityId

                        // Entity ID mapping arrays
                        totalSize += bufferChunk->length * 4; // entityIds array (length-sized)
                        totalSize += (bufferChunk->maxEntityId + 1) * 4; // bufferIndices array (maxEntityId+1-sized)

                        for (var j = 0; j < bufferChunk->length; j++)
                        {
                            // Get pointer to buffer header
                            var header = (BufferHeader*)(bufferChunk->ptr + j * bufferChunk->headerSize);

                            // Each buffer entry needs:
                            // Buffer length and capacity (8 bytes) 
                            // Actual buffer data (length * elementSize)
                            totalSize += 8 + header->length * bufferChunk->elementSize;
                        }
                    }
                    else
                    {
                        var componentChunk = chunkUnion.AsComponentChunk();

                        totalSize += 4; // length
                        totalSize += 4; // capacity
                        totalSize += 4; // componentSize
                        totalSize += 4; // maxEntityId

                        // Entity ID mapping arrays
                        totalSize += componentChunk->length * 4; // entityIds array (length-sized)
                        totalSize += (componentChunk->maxEntityId + 1) * 4; // componentIndices array (maxEntityId+1-sized)

                        // For each component entry: component data
                        totalSize += componentChunk->length * componentChunk->componentSize;
                    }
                }

                sizeCalculator.Value = totalSize;
            }
        }

        [BurstCompile]
        private struct SerializeJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public EntityManager* manager;
            [NativeDisableUnsafePtrRestriction] public byte* ptr;

            private static long ComputeTypeInfoHash()
            {
                long hash = 0;
                foreach (var kv in TypeManager.TypeToIndex.Data)
                {
                    hash = hash * 31 + kv.Key;
                }

                return hash;
            }

            public void Execute()
            {
                int position = 0;

                // Write header - separated fields for better clarity
                // Magic number (4 bytes)
                *(int*)(ptr + position) = SerializationMagic;
                position += 4;

                // Type hash (8 bytes)
                *(long*)(ptr + position) = ComputeTypeInfoHash();
                position += 8;

                // nextId (4 bytes)
                *(int*)(ptr + position) = manager->nextId.Value;
                position += 4;

                // Free entities count (4 bytes)
                *(int*)(ptr + position) = manager->freeEntities.Length;
                position += 4;

                // Entity archetypes count (4 bytes)
                *(int*)(ptr + position) = manager->entityArchetypes.Length;
                position += 4;

                // Entity count (4 bytes)
                *(int*)(ptr + position) = manager->entities.Length;
                position += 4;

                // Dead entities count (4 bytes)
                *(int*)(ptr + position) = manager->deadEntities.Length;
                position += 4;

                // Chunks count (4 bytes)
                *(int*)(ptr + position) = manager->chunks.Length;
                position += 4;

                // Write free entities
                for (var i = 0; i < manager->freeEntities.Length; i++)
                {
                    *(int*)(ptr + position) = manager->freeEntities.Ptr[i].id;
                    position += 4;

                    *(uint*)(ptr + position) = manager->freeEntities.Ptr[i].version;
                    position += 4;
                }

                // Write entityArchetypes
                for (var i = 0; i < manager->entityArchetypes.Length; i++)
                {
                    ref var entityArchetype = ref manager->entityArchetypes.Ptr[i];

                    // Write ComponentBits (4 ulongs = 32 bytes)
                    *(ulong*)(ptr + position) = entityArchetype.componentBits.part0;
                    position += 8;
                    *(ulong*)(ptr + position) = entityArchetype.componentBits.part1;
                    position += 8;
                    *(ulong*)(ptr + position) = entityArchetype.componentBits.part2;
                    position += 8;
                    *(ulong*)(ptr + position) = entityArchetype.componentBits.part3;
                    position += 8;
                }

                // Write entities
                for (int i = 0; i < manager->entities.Length; i++)
                {
                    var entity = manager->entities[i];
                    *(int*)(ptr + position) = entity.id;
                    position += 4;
                    *(uint*)(ptr + position) = entity.version;
                    position += 4;
                }

                // Write dead entities (1 byte per entity)
                for (int i = 0; i < manager->deadEntities.Length; i++)
                {
                    *(bool*)(ptr + position) = manager->deadEntities.Ptr[i];
                    position += 1;
                }

                // Write chunks data
                for (int i = 0; i < manager->chunks.Length; i++)
                {
                    ref var chunkUnion = ref manager->chunks.Ptr[i];

                    // Write chunk index (effectively the type index)
                    *(int*)(ptr + position) = i;
                    position += 4;

                    // Write isBuffer flag
                    *(bool*)(ptr + position) = chunkUnion.isBuffer;
                    position += 1;

                    if (chunkUnion.isBuffer)
                    {
                        var bufferChunk = chunkUnion.AsBufferChunk();

                        *(int*)(ptr + position) = bufferChunk->length; // Buffer count
                        position += 4;

                        *(int*)(ptr + position) = bufferChunk->capacity; // Capacity
                        position += 4;

                        *(int*)(ptr + position) = bufferChunk->elementSize; // Element size
                        position += 4;

                        *(int*)(ptr + position) = bufferChunk->maxEntityId; // Max entity ID
                        position += 4;

                        // Write entityIds array (length-sized)
                        for (int j = 0; j < bufferChunk->length; j++)
                        {
                            *(int*)(ptr + position) = bufferChunk->entityIds[j];
                            position += 4;
                        }

                        // Write bufferIndices array (maxEntityId+1-sized)
                        for (int j = 0; j <= bufferChunk->maxEntityId; j++)
                        {
                            *(int*)(ptr + position) = bufferChunk->bufferIndices[j];
                            position += 4;
                        }

                        // Write buffer data for each buffer
                        for (int j = 0; j < bufferChunk->length; j++)
                        {
                            // Get buffer header
                            var header = (BufferHeader*)(bufferChunk->ptr + j * bufferChunk->headerSize);

                            // Write buffer metadata
                            *(int*)(ptr + position) = header->length;
                            position += 4;
                            *(int*)(ptr + position) = header->capacity;
                            position += 4;

                            // Write buffer data if exists
                            if (header->length > 0 && header->pointer != null)
                            {
                                UnsafeUtility.MemCpy(
                                    ptr + position,
                                    header->pointer,
                                    header->length * bufferChunk->elementSize);
                                position += header->length * bufferChunk->elementSize;
                            }
                        }
                    }
                    else
                    {
                        var componentChunk = chunkUnion.AsComponentChunk();

                        *(int*)(ptr + position) = componentChunk->length; // Entity count
                        position += 4;

                        *(int*)(ptr + position) = componentChunk->capacity; // Capacity
                        position += 4;

                        *(int*)(ptr + position) = componentChunk->componentSize; // Component size
                        position += 4;

                        *(int*)(ptr + position) = componentChunk->maxEntityId; // Max entity ID
                        position += 4;

                        // Write entityIds array (length-sized)
                        for (int j = 0; j < componentChunk->length; j++)
                        {
                            *(int*)(ptr + position) = componentChunk->entityIds[j];
                            position += 4;
                        }

                        // Write componentIndices array (maxEntityId+1-sized)
                        for (int j = 0; j <= componentChunk->maxEntityId; j++)
                        {
                            *(int*)(ptr + position) = componentChunk->componentIndices[j];
                            position += 4;
                        }

                        // Write component data
                        for (int j = 0; j < componentChunk->length; j++)
                        {
                            UnsafeUtility.MemCpy(
                                ptr + position,
                                (byte*)componentChunk->ptr + (j * componentChunk->componentSize),
                                componentChunk->componentSize);
                            position += componentChunk->componentSize;
                        }
                    }
                }
            }
        }
    }
}