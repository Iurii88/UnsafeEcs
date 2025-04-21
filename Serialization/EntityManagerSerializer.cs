using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;
using UnsafeEcs.Core.Entities;
using UnsafeEcs.Core.Utils;

namespace UnsafeEcs.Serialization
{
    public unsafe struct EntityManagerSerializer
    {
        // Magic number for format verification
        private const int SerializationMagic = 0xEC51;

        public static NativeArray<byte> Serialize(ReferenceWrapper<EntityManager> managerWrapper, Allocator allocator = Allocator.TempJob)
        {
            // Calculate total size needed
            var sizeCalculator = new UnsafeItem<int>(0, Allocator.TempJob);
            var sizeJob = new SizeCalculationJob
            {
                manager = managerWrapper.ptr,
                sizeCalculator = sizeCalculator
            }.Schedule();

            // Complete the size calculation job before allocating memory
            sizeJob.Complete();

            // Now we can safely access the calculated size
            var output = new NativeArray<byte>(sizeCalculator.Value, allocator, NativeArrayOptions.UninitializedMemory);

            // Schedule and run the serialization job
            var serializeJob = new SerializeJob
            {
                manager = managerWrapper.ptr,
                output = output
            }.Schedule();

            serializeJob.Complete();
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
                totalSize += 4; // deadEntities count (int) - NEW
                totalSize += 4; // componentChunks count (int)
                totalSize += 4; // bufferChunks count (int)

                // Free entities data
                totalSize += manager->freeEntities.Length * 8; // id is int (4 bytes), version is int (4 bytes)

                // For each archetype: entity (8 bytes) + 4 longs for ComponentBits (32 bytes)
                totalSize += manager->entityArchetypes.Length * (4 + 32);

                // Entity data
                totalSize += manager->entities.Length * 8; // Entity ID + version

                // Dead entities data (1 byte per entity)
                totalSize += manager->deadEntities.Length;

                // Component data
                foreach (var kv in manager->componentChunks)
                {
                    totalSize += 4; // Component type index
                    totalSize += 4; // length
                    totalSize += 4; // capacity
                    totalSize += 4; // componentSize

                    // For each component entry: entity id (4) + data size
                    totalSize += kv.Value.length * (4 + kv.Value.componentSize); // EntityId (4) + component data
                }

                // Buffer data
                foreach (var kv in manager->bufferChunks)
                {
                    totalSize += 4; // Buffer type index
                    totalSize += 4; // length
                    totalSize += 4; // capacity
                    totalSize += 4; // element size

                    for (var i = 0; i < kv.Value.length; i++)
                    {
                        // Get pointer to buffer header
                        var header = (BufferHeader*)(kv.Value.ptr + i * kv.Value.headerSize);

                        // Each buffer entry needs:
                        // Entity ID and version (4 bytes)
                        // Buffer length and capacity (8 bytes) 
                        // Actual buffer data (length * elementSize)
                        totalSize += 4 + 8 + header->length * kv.Value.elementSize;
                    }
                }

                sizeCalculator.Value = totalSize;
            }
        }

        [BurstCompile]
        private struct SerializeJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public EntityManager* manager;
            [NativeDisableUnsafePtrRestriction] public NativeArray<byte> output;

            private static long ComputeTypeInfoHash()
            {
                long hash = 0;
                foreach (var kv in ComponentTypeManager.TypeToIndex.Data)
                {
                    hash = hash * 31 + kv.Key;
                }

                return hash;
            }

            public void Execute()
            {
                byte* ptr = (byte*)output.GetUnsafePtr();
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

                // Dead entities count (4 bytes) - NEW
                *(int*)(ptr + position) = manager->deadEntities.Length;
                position += 4;

                // Component chunks count (4 bytes)
                *(int*)(ptr + position) = manager->componentChunks.Count;
                position += 4;

                // Buffer chunks count (4 bytes)
                *(int*)(ptr + position) = manager->bufferChunks.Count;
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

                // Write components
                foreach (var kv in manager->componentChunks)
                {
                    *(int*)(ptr + position) = kv.Key; // Component type index
                    position += 4;

                    *(int*)(ptr + position) = kv.Value.length; // Entity count
                    position += 4;

                    *(int*)(ptr + position) = kv.Value.capacity; // Capacity
                    position += 4;

                    *(int*)(ptr + position) = kv.Value.componentSize; // Component size
                    position += 4;

                    for (int i = 0; i < kv.Value.length; i++)
                    {
                        var entityId = kv.Value.indexToEntity[i];

                        // Write entity
                        *(int*)(ptr + position) = entityId;
                        position += 4;

                        // Write component data
                        UnsafeUtility.MemCpy(ptr + position, (byte*)kv.Value.ptr + i * kv.Value.componentSize, kv.Value.componentSize);
                        position += kv.Value.componentSize;
                    }
                }

                // Write buffers
                foreach (var kv in manager->bufferChunks)
                {
                    *(int*)(ptr + position) = kv.Key; // Buffer type index
                    position += 4;

                    *(int*)(ptr + position) = kv.Value.length; // Entity count
                    position += 4;

                    *(int*)(ptr + position) = kv.Value.capacity; // Capacity
                    position += 4;

                    *(int*)(ptr + position) = kv.Value.elementSize; // Element size
                    position += 4;

                    for (int i = 0; i < kv.Value.length; i++)
                    {
                        var entityId = kv.Value.indexToEntity[i];

                        // Write entity
                        *(int*)(ptr + position) = entityId;
                        position += 4;

                        // Get buffer header
                        var header = (BufferHeader*)(kv.Value.ptr + i * kv.Value.headerSize);

                        // Write buffer metadata
                        *(int*)(ptr + position) = header->length;
                        position += 4;
                        *(int*)(ptr + position) = header->capacity;
                        position += 4;

                        // Write buffer data if exists
                        if (header->length > 0 && header->pointer != null)
                        {
                            UnsafeUtility.MemCpy(ptr + position, header->pointer, header->length * kv.Value.elementSize);
                            position += header->length * kv.Value.elementSize;
                        }
                    }
                }
            }
        }
    }
}