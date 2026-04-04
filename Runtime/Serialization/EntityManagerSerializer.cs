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
        private const int SerializationMagic = 0xEC51;
        private const int SerializationMagicV2 = 0xEC52;

        private static byte[] s_cachedOutputV2;

        public static byte[] Serialize(ReferenceWrapper<EntityManager> managerWrapper)
        {
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

        public static byte[] SerializeV2(ReferenceWrapper<EntityManager> managerWrapper)
        {
            return SerializeV2(managerWrapper, out _);
        }

        public static byte[] SerializeV2(ReferenceWrapper<EntityManager> managerWrapper, out int dataLength)
        {
            var sizeCalculator = new UnsafeItem<int>(0, Allocator.TempJob);
            new SizeCalculationJobV2
                {
                    manager = managerWrapper.ptr,
                    sizeCalculator = sizeCalculator
                }.Schedule()
                .Complete();

            dataLength = sizeCalculator.Value;
            sizeCalculator.Dispose();

            if (s_cachedOutputV2 == null || s_cachedOutputV2.Length < dataLength)
                s_cachedOutputV2 = new byte[dataLength];

            fixed (byte* ptr = s_cachedOutputV2)
            {
                new SerializeJobV2
                    {
                        manager = managerWrapper.ptr,
                        ptr = ptr
                    }.Schedule()
                    .Complete();
            }

            return s_cachedOutputV2;
        }

        [BurstCompile]
        private struct SizeCalculationJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public EntityManager* manager;

            public UnsafeItem<int> sizeCalculator;

            public void Execute()
            {
                var totalSize = 0;

                totalSize += 4; // Magic number (int)
                totalSize += 8; // Type hash (long)
                totalSize += 4; // nextId (int)
                totalSize += 4; // freeEntities count (int)
                totalSize += 4; // entityArchetypes count (int)
                totalSize += 4; // entities count (int)
                totalSize += 4; // deadEntities count (int)
                totalSize += 4; // chunks count (int)

                totalSize += manager->freeEntities.Length * 8;
                totalSize += manager->entityArchetypes.Length * (4 + 32);
                totalSize += manager->entities.Length * 8;
                totalSize += manager->deadEntities.Length;

                for (var i = 0; i < manager->chunks.Length; i++)
                {
                    ref var chunkUnion = ref manager->chunks.Ptr[i];

                    totalSize += 4;
                    totalSize += 1;

                    if (chunkUnion.isBuffer)
                    {
                        var bufferChunk = chunkUnion.AsBufferChunk();

                        totalSize += 4; // length
                        totalSize += 4; // capacity
                        totalSize += 4; // element size
                        totalSize += 4; // maxEntityId

                        totalSize += bufferChunk->length * 4;
                        totalSize += (bufferChunk->maxEntityId + 1) * 4;

                        for (var j = 0; j < bufferChunk->length; j++)
                        {
                            var header = (BufferHeader*)(bufferChunk->ptr + j * bufferChunk->headerSize);
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

                        totalSize += componentChunk->length * 4;
                        totalSize += (componentChunk->maxEntityId + 1) * 4;
                        totalSize += componentChunk->length * componentChunk->componentSize;
                    }
                }

                sizeCalculator.Value = totalSize;
            }
        }

        [BurstCompile]
        private struct SizeCalculationJobV2 : IJob
        {
            [NativeDisableUnsafePtrRestriction] public EntityManager* manager;

            public UnsafeItem<int> sizeCalculator;

            public void Execute()
            {
                var totalSize = 0;

                totalSize += 4; // Magic V2
                totalSize += 4; // Type table count

                var typeCount = TypeManager.TypeCount.Data;
                totalSize += typeCount * 13; // Per type: hash(8) + size(4) + isBuffer(1)

                totalSize += 4; // nextId
                totalSize += 4; // freeEntities count
                totalSize += 4; // entityArchetypes count
                totalSize += 4; // entities count
                totalSize += 4; // deadEntities count
                totalSize += 4; // valid chunks count

                totalSize += manager->freeEntities.Length * 8;
                totalSize += manager->entityArchetypes.Length * 32;
                totalSize += manager->entities.Length * 8;
                totalSize += manager->deadEntities.Length;

                for (var i = 0; i < manager->chunks.Length; i++)
                {
                    ref var chunkUnion = ref manager->chunks.Ptr[i];
                    if (!chunkUnion.IsValid) continue;

                    totalSize += 8; // typeHash (long)
                    totalSize += 1; // isBuffer

                    if (chunkUnion.isBuffer)
                    {
                        var bufferChunk = chunkUnion.AsBufferChunk();

                        totalSize += 4; // length
                        totalSize += 4; // capacity
                        totalSize += 4; // element size
                        totalSize += 4; // maxEntityId

                        totalSize += bufferChunk->length * 4;
                        totalSize += (bufferChunk->maxEntityId + 1) * 4;

                        for (var j = 0; j < bufferChunk->length; j++)
                        {
                            var header = (BufferHeader*)(bufferChunk->ptr + j * bufferChunk->headerSize);
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

                        totalSize += componentChunk->length * 4;
                        totalSize += (componentChunk->maxEntityId + 1) * 4;
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
                foreach (var kv in TypeManager.TypeToIndex.Data) hash = hash * 31 + kv.Key;

                return hash;
            }

            public void Execute()
            {
                var position = 0;

                *(int*)(ptr + position) = SerializationMagic;
                position += 4;

                *(long*)(ptr + position) = ComputeTypeInfoHash();
                position += 8;

                *(int*)(ptr + position) = manager->nextId.Value;
                position += 4;

                *(int*)(ptr + position) = manager->freeEntities.Length;
                position += 4;

                *(int*)(ptr + position) = manager->entityArchetypes.Length;
                position += 4;

                *(int*)(ptr + position) = manager->entities.Length;
                position += 4;

                *(int*)(ptr + position) = manager->deadEntities.Length;
                position += 4;

                *(int*)(ptr + position) = manager->chunks.Length;
                position += 4;

                for (var i = 0; i < manager->freeEntities.Length; i++)
                {
                    *(int*)(ptr + position) = manager->freeEntities.Ptr[i].id;
                    position += 4;

                    *(uint*)(ptr + position) = manager->freeEntities.Ptr[i].version;
                    position += 4;
                }

                for (var i = 0; i < manager->entityArchetypes.Length; i++)
                {
                    ref var entityArchetype = ref manager->entityArchetypes.Ptr[i];

                    *(ulong*)(ptr + position) = entityArchetype.componentBits.part0;
                    position += 8;
                    *(ulong*)(ptr + position) = entityArchetype.componentBits.part1;
                    position += 8;
                    *(ulong*)(ptr + position) = entityArchetype.componentBits.part2;
                    position += 8;
                    *(ulong*)(ptr + position) = entityArchetype.componentBits.part3;
                    position += 8;
                }

                for (var i = 0; i < manager->entities.Length; i++)
                {
                    var entity = manager->entities[i];
                    *(int*)(ptr + position) = entity.id;
                    position += 4;
                    *(uint*)(ptr + position) = entity.version;
                    position += 4;
                }

                for (var i = 0; i < manager->deadEntities.Length; i++)
                {
                    *(bool*)(ptr + position) = manager->deadEntities.Ptr[i];
                    position += 1;
                }

                for (var i = 0; i < manager->chunks.Length; i++)
                {
                    ref var chunkUnion = ref manager->chunks.Ptr[i];

                    *(int*)(ptr + position) = i;
                    position += 4;

                    *(bool*)(ptr + position) = chunkUnion.isBuffer;
                    position += 1;

                    if (chunkUnion.isBuffer)
                    {
                        var bufferChunk = chunkUnion.AsBufferChunk();

                        *(int*)(ptr + position) = bufferChunk->length;
                        position += 4;

                        *(int*)(ptr + position) = bufferChunk->capacity;
                        position += 4;

                        *(int*)(ptr + position) = bufferChunk->elementSize;
                        position += 4;

                        *(int*)(ptr + position) = bufferChunk->maxEntityId;
                        position += 4;

                        for (var j = 0; j < bufferChunk->length; j++)
                        {
                            *(int*)(ptr + position) = bufferChunk->entityIds[j];
                            position += 4;
                        }

                        for (var j = 0; j <= bufferChunk->maxEntityId; j++)
                        {
                            *(int*)(ptr + position) = bufferChunk->bufferIndices[j];
                            position += 4;
                        }

                        for (var j = 0; j < bufferChunk->length; j++)
                        {
                            var header = (BufferHeader*)(bufferChunk->ptr + j * bufferChunk->headerSize);

                            *(int*)(ptr + position) = header->length;
                            position += 4;
                            *(int*)(ptr + position) = header->capacity;
                            position += 4;

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

                        *(int*)(ptr + position) = componentChunk->length;
                        position += 4;

                        *(int*)(ptr + position) = componentChunk->capacity;
                        position += 4;

                        *(int*)(ptr + position) = componentChunk->componentSize;
                        position += 4;

                        *(int*)(ptr + position) = componentChunk->maxEntityId;
                        position += 4;

                        for (var j = 0; j < componentChunk->length; j++)
                        {
                            *(int*)(ptr + position) = componentChunk->entityIds[j];
                            position += 4;
                        }

                        for (var j = 0; j <= componentChunk->maxEntityId; j++)
                        {
                            *(int*)(ptr + position) = componentChunk->componentIndices[j];
                            position += 4;
                        }

                        for (var j = 0; j < componentChunk->length; j++)
                        {
                            UnsafeUtility.MemCpy(
                                ptr + position,
                                (byte*)componentChunk->ptr + j * componentChunk->componentSize,
                                componentChunk->componentSize);
                            position += componentChunk->componentSize;
                        }
                    }
                }
            }
        }

        [BurstCompile]
        private struct SerializeJobV2 : IJob
        {
            [NativeDisableUnsafePtrRestriction] public EntityManager* manager;
            [NativeDisableUnsafePtrRestriction] public byte* ptr;

            public void Execute()
            {
                var position = 0;

                *(int*)(ptr + position) = SerializationMagicV2;
                position += 4;

                var typeCount = TypeManager.TypeCount.Data;
                *(int*)(ptr + position) = typeCount;
                position += 4;

                for (var i = 0; i < typeCount; i++)
                {
                    *(long*)(ptr + position) = TypeManager.TypeOrder.Data[i];
                    position += 8;
                    *(int*)(ptr + position) = TypeManager.TypeSizes.Data[i];
                    position += 4;
                    *(bool*)(ptr + position) = TypeManager.IsBufferList.Data[i];
                    position += 1;
                }

                *(int*)(ptr + position) = manager->nextId.Value;
                position += 4;

                *(int*)(ptr + position) = manager->freeEntities.Length;
                position += 4;

                *(int*)(ptr + position) = manager->entityArchetypes.Length;
                position += 4;

                *(int*)(ptr + position) = manager->entities.Length;
                position += 4;

                *(int*)(ptr + position) = manager->deadEntities.Length;
                position += 4;

                var validChunks = 0;
                for (var i = 0; i < manager->chunks.Length; i++)
                {
                    if (manager->chunks.Ptr[i].IsValid)
                        validChunks++;
                }

                *(int*)(ptr + position) = validChunks;
                position += 4;

                for (var i = 0; i < manager->freeEntities.Length; i++)
                {
                    *(int*)(ptr + position) = manager->freeEntities.Ptr[i].id;
                    position += 4;
                    *(uint*)(ptr + position) = manager->freeEntities.Ptr[i].version;
                    position += 4;
                }

                for (var i = 0; i < manager->entityArchetypes.Length; i++)
                {
                    ref var archetype = ref manager->entityArchetypes.Ptr[i];
                    *(ulong*)(ptr + position) = archetype.componentBits.part0;
                    position += 8;
                    *(ulong*)(ptr + position) = archetype.componentBits.part1;
                    position += 8;
                    *(ulong*)(ptr + position) = archetype.componentBits.part2;
                    position += 8;
                    *(ulong*)(ptr + position) = archetype.componentBits.part3;
                    position += 8;
                }

                for (var i = 0; i < manager->entities.Length; i++)
                {
                    var entity = manager->entities[i];
                    *(int*)(ptr + position) = entity.id;
                    position += 4;
                    *(uint*)(ptr + position) = entity.version;
                    position += 4;
                }

                for (var i = 0; i < manager->deadEntities.Length; i++)
                {
                    *(bool*)(ptr + position) = manager->deadEntities.Ptr[i];
                    position += 1;
                }

                for (var i = 0; i < manager->chunks.Length; i++)
                {
                    ref var chunkUnion = ref manager->chunks.Ptr[i];
                    if (!chunkUnion.IsValid) continue;

                    *(long*)(ptr + position) = TypeManager.TypeOrder.Data[i];
                    position += 8;

                    *(bool*)(ptr + position) = chunkUnion.isBuffer;
                    position += 1;

                    if (chunkUnion.isBuffer)
                    {
                        var bufferChunk = chunkUnion.AsBufferChunk();

                        *(int*)(ptr + position) = bufferChunk->length;
                        position += 4;
                        *(int*)(ptr + position) = bufferChunk->capacity;
                        position += 4;
                        *(int*)(ptr + position) = bufferChunk->elementSize;
                        position += 4;
                        *(int*)(ptr + position) = bufferChunk->maxEntityId;
                        position += 4;

                        for (var j = 0; j < bufferChunk->length; j++)
                        {
                            *(int*)(ptr + position) = bufferChunk->entityIds[j];
                            position += 4;
                        }

                        for (var j = 0; j <= bufferChunk->maxEntityId; j++)
                        {
                            *(int*)(ptr + position) = bufferChunk->bufferIndices[j];
                            position += 4;
                        }

                        for (var j = 0; j < bufferChunk->length; j++)
                        {
                            var header = (BufferHeader*)(bufferChunk->ptr + j * bufferChunk->headerSize);

                            *(int*)(ptr + position) = header->length;
                            position += 4;
                            *(int*)(ptr + position) = header->capacity;
                            position += 4;

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

                        *(int*)(ptr + position) = componentChunk->length;
                        position += 4;
                        *(int*)(ptr + position) = componentChunk->capacity;
                        position += 4;
                        *(int*)(ptr + position) = componentChunk->componentSize;
                        position += 4;
                        *(int*)(ptr + position) = componentChunk->maxEntityId;
                        position += 4;

                        for (var j = 0; j < componentChunk->length; j++)
                        {
                            *(int*)(ptr + position) = componentChunk->entityIds[j];
                            position += 4;
                        }

                        for (var j = 0; j <= componentChunk->maxEntityId; j++)
                        {
                            *(int*)(ptr + position) = componentChunk->componentIndices[j];
                            position += 4;
                        }

                        for (var j = 0; j < componentChunk->length; j++)
                        {
                            UnsafeUtility.MemCpy(
                                ptr + position,
                                (byte*)componentChunk->ptr + j * componentChunk->componentSize,
                                componentChunk->componentSize);
                            position += componentChunk->componentSize;
                        }
                    }
                }
            }
        }
    }
}
