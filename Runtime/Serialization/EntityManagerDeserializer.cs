using System;
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
    public unsafe struct EntityManagerDeserializer
    {
        private const int SerializationMagic = 0xEC51;
        private const int SerializationMagicV2 = 0xEC52;

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

        public static void BuildRemapTable(byte* ptr, out NativeArray<int> remapTable,
            out int savedTypeCount, out int headerSize)
        {
            var position = 0;

            var magic = *(int*)(ptr + position);
            position += 4;

            if (magic != SerializationMagicV2)
                throw new ArgumentException("Invalid V2 serialization format");

            savedTypeCount = *(int*)(ptr + position);
            position += 4;

            remapTable = new NativeArray<int>(savedTypeCount, Allocator.TempJob);

            for (var i = 0; i < savedTypeCount; i++)
            {
                var hash = *(long*)(ptr + position);
                position += 8;
                var size = *(int*)(ptr + position);
                position += 4;
                position += 1; // isBuffer

                if (TypeManager.TypeToIndex.Data.TryGetValue(hash, out var currentIndex))
                {
                    var currentSize = TypeManager.TypeSizes.Data[currentIndex];
                    remapTable[i] = currentSize == size ? currentIndex : -1;
                }
                else
                {
                    remapTable[i] = -1;
                }
            }

            headerSize = position;
        }

        public static int DeserializeV2(MemoryRegion memoryRegion, ref EntityManager entityManager,
            NativeArray<int> remapTable, int savedTypeCount, int skippedChunks)
        {
            var skipped = new UnsafeItem<int>(skippedChunks, Allocator.TempJob);

            fixed (EntityManager* localPtr = &entityManager)
            {
                new DeserializeJobV2
                {
                    manager = localPtr,
                    ptr = memoryRegion.ptr,
                    remapTable = remapTable,
                    savedTypeCount = savedTypeCount,
                    skipped = skipped
                }.Schedule().Complete();
            }

            var result = skipped.Value;
            skipped.Dispose();
            return result;
        }

        [BurstCompile]
        private struct DeserializeJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public EntityManager* manager;
            [NativeDisableUnsafePtrRestriction] public byte* ptr;

            public void Execute()
            {
                var position = 0;

                var magic = *(int*)(ptr + position);
                if (magic != SerializationMagic) throw new ArgumentException("Invalid data format");
                position += 4;

                var typeInfoHash = *(long*)(ptr + position);
                position += 8;

                long currentHash = 0;
                foreach (var kv in TypeManager.TypeToIndex.Data)
                {
                    currentHash = currentHash * 31 + kv.Key;
                }

                if (typeInfoHash != currentHash)
                {
                    throw new InvalidOperationException(
                        "Type information mismatch between serialized data and current runtime");
                }

                var nextId = *(int*)(ptr + position);
                position += 4;
                manager->nextId.Value = nextId;

                var freeIdsCount = *(int*)(ptr + position);
                position += 4;

                var entityArchetypesCount = *(int*)(ptr + position);
                position += 4;

                var entityCount = *(int*)(ptr + position);
                position += 4;

                var deadEntitiesCount = *(int*)(ptr + position);
                position += 4;

                var chunksCount = *(int*)(ptr + position);
                position += 4;

                manager->freeEntities.Clear();
                for (var i = 0; i < freeIdsCount; i++)
                {
                    var freeId = *(int*)(ptr + position);
                    position += 4;
                    var freeVersion = *(uint*)(ptr + position);
                    position += 4;
                    manager->freeEntities.Add(new Entity { id = freeId, version = freeVersion });
                }

                manager->entityArchetypes.Clear();
                for (var i = 0; i < entityArchetypesCount; i++)
                {
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

                manager->entities.Clear();
                for (var i = 0; i < entityCount; i++)
                {
                    var id = *(int*)(ptr + position);
                    position += 4;
                    var version = *(uint*)(ptr + position);
                    position += 4;

                    var entity = new Entity { id = id, version = version, managerPtr = manager };
                    manager->entities.Add(entity);
                }

                manager->deadEntities.Clear();
                for (var i = 0; i < deadEntitiesCount; i++)
                {
                    var isDead = *(bool*)(ptr + position);
                    position += 1;
                    manager->deadEntities.Add(isDead);
                }

                for (var i = 0; i < manager->chunks.Length; i++)
                {
                    manager->chunks.Ptr[i].Dispose();
                }

                manager->chunks.Clear();

                if (manager->chunks.Capacity < chunksCount)
                {
                    manager->chunks.SetCapacity(chunksCount);
                }

                for (var chunkIdx = 0; chunkIdx < chunksCount; chunkIdx++)
                {
                    var typeIndex = *(int*)(ptr + position);
                    position += 4;

                    var isBuffer = *(bool*)(ptr + position);
                    position += 1;

                    while (manager->chunks.Length <= typeIndex)
                    {
                        manager->chunks.Add(new ChunkUnion { chunkPtr = null, isBuffer = false });
                    }

                    if (isBuffer)
                    {
                        var bufferCount = *(int*)(ptr + position);
                        position += 4;

                        var chunkCapacity = *(int*)(ptr + position);
                        position += 4;

                        var elementSize = *(int*)(ptr + position);
                        position += 4;

                        var maxEntityId = *(int*)(ptr + position);
                        position += 4;

                        var chunk = (BufferChunk*)UnsafeUtility.Malloc(
                            UnsafeUtility.SizeOf<BufferChunk>(),
                            UnsafeUtility.AlignOf<BufferChunk>(),
                            Allocator.Persistent);

                        *chunk = new BufferChunk(elementSize, chunkCapacity, maxEntityId, typeIndex, manager);
                        chunk->length = bufferCount;
                        chunk->maxEntityId = maxEntityId;

                        if (chunk->entityIds == null || chunk->capacity < bufferCount)
                        {
                            if (chunk->entityIds != null)
                            {
                                UnsafeUtility.Free(chunk->entityIds, Allocator.Persistent);
                            }

                            chunk->entityIds = (int*)UnsafeUtility.Malloc(
                                sizeof(int) * chunkCapacity,
                                UnsafeUtility.AlignOf<int>(),
                                Allocator.Persistent);
                        }

                        if (chunk->bufferIndices == null || maxEntityId >= chunk->maxEntityId)
                        {
                            if (chunk->bufferIndices != null)
                            {
                                UnsafeUtility.Free(chunk->bufferIndices, Allocator.Persistent);
                            }

                            var newSize = maxEntityId + 1;
                            chunk->bufferIndices = (int*)UnsafeUtility.Malloc(
                                sizeof(int) * newSize,
                                UnsafeUtility.AlignOf<int>(),
                                Allocator.Persistent);

                            UnsafeUtility.MemSet(chunk->bufferIndices, 0xFF, sizeof(int) * newSize);
                        }

                        for (var i = 0; i < bufferCount; i++)
                        {
                            chunk->entityIds[i] = *(int*)(ptr + position);
                            position += 4;
                        }

                        for (var i = 0; i <= maxEntityId; i++)
                        {
                            chunk->bufferIndices[i] = *(int*)(ptr + position);
                            position += 4;
                        }

                        for (var i = 0; i < bufferCount; i++)
                        {
                            chunk->InitializeBuffer(i);

                            var header = (BufferHeader*)(chunk->ptr + i * chunk->headerSize);

                            var bufferLength = *(int*)(ptr + position);
                            position += 4;
                            var bufferCapacity = *(int*)(ptr + position);
                            position += 4;

                            if (bufferLength > 0)
                            {
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

                                header->length = bufferLength;

                                UnsafeUtility.MemCpy(
                                    header->pointer,
                                    ptr + position,
                                    bufferLength * elementSize);

                                position += bufferLength * elementSize;
                            }
                            else
                            {
                                header->length = 0;
                            }
                        }

                        manager->chunks.Ptr[typeIndex] = ChunkUnion.FromBufferChunk(chunk);
                    }
                    else
                    {
                        var componentCount = *(int*)(ptr + position);
                        position += 4;

                        var componentCapacity = *(int*)(ptr + position);
                        position += 4;

                        var componentSize = *(int*)(ptr + position);
                        position += 4;

                        var maxEntityId = *(int*)(ptr + position);
                        position += 4;

                        var chunk = (ComponentChunk*)UnsafeUtility.Malloc(
                            UnsafeUtility.SizeOf<ComponentChunk>(),
                            UnsafeUtility.AlignOf<ComponentChunk>(),
                            Allocator.Persistent);

                        *chunk = new ComponentChunk(componentSize, componentCapacity, typeIndex, manager);
                        chunk->length = componentCount;
                        chunk->maxEntityId = maxEntityId;

                        if (chunk->entityIds == null || chunk->capacity < componentCount)
                        {
                            if (chunk->entityIds != null)
                            {
                                UnsafeUtility.Free(chunk->entityIds, Allocator.Persistent);
                            }

                            chunk->entityIds = (int*)UnsafeUtility.Malloc(
                                sizeof(int) * componentCapacity,
                                UnsafeUtility.AlignOf<int>(),
                                Allocator.Persistent);
                        }

                        if (chunk->componentIndices == null || maxEntityId >= chunk->maxEntityId)
                        {
                            if (chunk->componentIndices != null)
                            {
                                UnsafeUtility.Free(chunk->componentIndices, Allocator.Persistent);
                            }

                            var newSize = maxEntityId + 1;
                            chunk->componentIndices = (int*)UnsafeUtility.Malloc(
                                sizeof(int) * newSize,
                                UnsafeUtility.AlignOf<int>(),
                                Allocator.Persistent);

                            UnsafeUtility.MemSet(chunk->componentIndices, 0xFF, sizeof(int) * newSize);
                        }

                        for (var i = 0; i < componentCount; i++)
                        {
                            chunk->entityIds[i] = *(int*)(ptr + position);
                            position += 4;
                        }

                        for (var i = 0; i <= maxEntityId; i++)
                        {
                            chunk->componentIndices[i] = *(int*)(ptr + position);
                            position += 4;
                        }

                        for (var i = 0; i < componentCount; i++)
                        {
                            UnsafeUtility.MemCpy(
                                (byte*)chunk->ptr + i * componentSize,
                                ptr + position,
                                componentSize);
                            position += componentSize;
                        }

                        manager->chunks.Ptr[typeIndex] = ChunkUnion.FromComponentChunk(chunk);
                    }
                }
            }
        }

        [BurstCompile]
        private struct DeserializeJobV2 : IJob
        {
            [NativeDisableUnsafePtrRestriction] public EntityManager* manager;
            [NativeDisableUnsafePtrRestriction] public byte* ptr;
            [ReadOnly] public NativeArray<int> remapTable;
            public int savedTypeCount;
            public UnsafeItem<int> skipped;

            public void Execute()
            {
                var position = 0;

                position += 4; // skip magic

                var typeTableCount = *(int*)(ptr + position);
                position += 4;

                var typeTableStart = position;
                position += typeTableCount * 13;

                var nextId = *(int*)(ptr + position);
                position += 4;
                manager->nextId.Value = nextId;

                var freeIdsCount = *(int*)(ptr + position);
                position += 4;

                var entityArchetypesCount = *(int*)(ptr + position);
                position += 4;

                var entityCount = *(int*)(ptr + position);
                position += 4;

                var deadEntitiesCount = *(int*)(ptr + position);
                position += 4;

                var chunksCount = *(int*)(ptr + position);
                position += 4;

                manager->freeEntities.Clear();
                for (var i = 0; i < freeIdsCount; i++)
                {
                    var freeId = *(int*)(ptr + position);
                    position += 4;
                    var freeVersion = *(uint*)(ptr + position);
                    position += 4;
                    manager->freeEntities.Add(new Entity { id = freeId, version = freeVersion });
                }

                manager->entityArchetypes.Clear();
                for (var i = 0; i < entityArchetypesCount; i++)
                {
                    ComponentBits savedBits;
                    savedBits.part0 = *(ulong*)(ptr + position);
                    position += 8;
                    savedBits.part1 = *(ulong*)(ptr + position);
                    position += 8;
                    savedBits.part2 = *(ulong*)(ptr + position);
                    position += 8;
                    savedBits.part3 = *(ulong*)(ptr + position);
                    position += 8;

                    EntityArchetype archetype;
                    archetype.componentBits = RemapBits(savedBits);
                    manager->entityArchetypes.Add(archetype);
                }

                manager->entities.Clear();
                for (var i = 0; i < entityCount; i++)
                {
                    var id = *(int*)(ptr + position);
                    position += 4;
                    var version = *(uint*)(ptr + position);
                    position += 4;
                    manager->entities.Add(new Entity { id = id, version = version, managerPtr = manager });
                }

                manager->deadEntities.Clear();
                for (var i = 0; i < deadEntitiesCount; i++)
                {
                    var isDead = *(bool*)(ptr + position);
                    position += 1;
                    manager->deadEntities.Add(isDead);
                }

                for (var i = 0; i < manager->chunks.Length; i++)
                    manager->chunks.Ptr[i].Dispose();
                manager->chunks.Clear();

                var currentTypeCount = TypeManager.TypeCount.Data;
                if (manager->chunks.Capacity < currentTypeCount)
                    manager->chunks.SetCapacity(currentTypeCount);
                for (var i = 0; i < currentTypeCount; i++)
                    manager->chunks.Add(default);

                for (var chunkIdx = 0; chunkIdx < chunksCount; chunkIdx++)
                {
                    var typeHash = *(long*)(ptr + position);
                    position += 8;

                    var isBuffer = *(bool*)(ptr + position);
                    position += 1;

                    var savedIndex = FindSavedIndex(typeHash, typeTableStart, typeTableCount);
                    var currentIndex = -1;
                    if (savedIndex >= 0 && savedIndex < remapTable.Length)
                        currentIndex = remapTable[savedIndex];

                    if (currentIndex < 0)
                    {
                        SkipChunkData(ref position, isBuffer);
                        skipped.Value++;
                        continue;
                    }

                    while (manager->chunks.Length <= currentIndex)
                        manager->chunks.Add(default);

                    if (isBuffer)
                        ReadBufferChunk(ref position, currentIndex);
                    else
                        ReadComponentChunk(ref position, currentIndex);
                }
            }

            private int FindSavedIndex(long typeHash, int typeTableStart, int typeTableCount)
            {
                for (var i = 0; i < typeTableCount; i++)
                {
                    var hash = *(long*)(ptr + typeTableStart + i * 13);
                    if (hash == typeHash)
                        return i;
                }

                return -1;
            }

            private ComponentBits RemapBits(ComponentBits saved)
            {
                var remapped = new ComponentBits();
                for (var i = 0; i < savedTypeCount; i++)
                {
                    if (!saved.HasComponent(i)) continue;
                    var currentIndex = remapTable[i];
                    if (currentIndex >= 0)
                        remapped.SetComponent(currentIndex);
                }

                return remapped;
            }

            private void SkipChunkData(ref int position, bool isBuffer)
            {
                if (isBuffer)
                {
                    var bufferCount = *(int*)(ptr + position);
                    position += 4;
                    position += 4; // capacity
                    var elementSize = *(int*)(ptr + position);
                    position += 4;
                    var maxEntityId = *(int*)(ptr + position);
                    position += 4;

                    position += bufferCount * 4; // entityIds
                    position += (maxEntityId + 1) * 4; // bufferIndices

                    for (var j = 0; j < bufferCount; j++)
                    {
                        var bufLength = *(int*)(ptr + position);
                        position += 4;
                        position += 4; // capacity
                        if (bufLength > 0)
                            position += bufLength * elementSize;
                    }
                }
                else
                {
                    var componentCount = *(int*)(ptr + position);
                    position += 4;
                    position += 4; // capacity
                    var componentSize = *(int*)(ptr + position);
                    position += 4;
                    var maxEntityId = *(int*)(ptr + position);
                    position += 4;

                    position += componentCount * 4; // entityIds
                    position += (maxEntityId + 1) * 4; // componentIndices
                    position += componentCount * componentSize;
                }
            }

            private void ReadBufferChunk(ref int position, int currentIndex)
            {
                var bufferCount = *(int*)(ptr + position);
                position += 4;
                var chunkCapacity = *(int*)(ptr + position);
                position += 4;
                var elementSize = *(int*)(ptr + position);
                position += 4;
                var maxEntityId = *(int*)(ptr + position);
                position += 4;

                var chunk = (BufferChunk*)UnsafeUtility.Malloc(
                    UnsafeUtility.SizeOf<BufferChunk>(),
                    UnsafeUtility.AlignOf<BufferChunk>(),
                    Allocator.Persistent);

                *chunk = new BufferChunk(elementSize, chunkCapacity, maxEntityId, currentIndex, manager);
                chunk->length = bufferCount;
                chunk->maxEntityId = maxEntityId;

                if (chunk->entityIds == null || chunk->capacity < bufferCount)
                {
                    if (chunk->entityIds != null)
                        UnsafeUtility.Free(chunk->entityIds, Allocator.Persistent);

                    chunk->entityIds = (int*)UnsafeUtility.Malloc(
                        sizeof(int) * chunkCapacity,
                        UnsafeUtility.AlignOf<int>(),
                        Allocator.Persistent);
                }

                if (chunk->bufferIndices == null || maxEntityId >= chunk->maxEntityId)
                {
                    if (chunk->bufferIndices != null)
                        UnsafeUtility.Free(chunk->bufferIndices, Allocator.Persistent);

                    var newSize = maxEntityId + 1;
                    chunk->bufferIndices = (int*)UnsafeUtility.Malloc(
                        sizeof(int) * newSize,
                        UnsafeUtility.AlignOf<int>(),
                        Allocator.Persistent);
                    UnsafeUtility.MemSet(chunk->bufferIndices, 0xFF, sizeof(int) * newSize);
                }

                for (var i = 0; i < bufferCount; i++)
                {
                    chunk->entityIds[i] = *(int*)(ptr + position);
                    position += 4;
                }

                for (var i = 0; i <= maxEntityId; i++)
                {
                    chunk->bufferIndices[i] = *(int*)(ptr + position);
                    position += 4;
                }

                for (var i = 0; i < bufferCount; i++)
                {
                    chunk->InitializeBuffer(i);
                    var header = (BufferHeader*)(chunk->ptr + i * chunk->headerSize);

                    var bufferLength = *(int*)(ptr + position);
                    position += 4;
                    var bufferCapacity = *(int*)(ptr + position);
                    position += 4;

                    if (bufferLength > 0)
                    {
                        if (header->capacity < bufferLength)
                        {
                            if (header->pointer != null)
                                UnsafeUtility.Free(header->pointer, Allocator.Persistent);

                            header->pointer = (byte*)UnsafeUtility.Malloc(
                                bufferLength * elementSize,
                                UnsafeUtility.AlignOf<byte>(),
                                Allocator.Persistent);
                            header->capacity = bufferCapacity;
                        }

                        header->length = bufferLength;
                        UnsafeUtility.MemCpy(header->pointer, ptr + position, bufferLength * elementSize);
                        position += bufferLength * elementSize;
                    }
                    else
                    {
                        header->length = 0;
                    }
                }

                manager->chunks.Ptr[currentIndex] = ChunkUnion.FromBufferChunk(chunk);
            }

            private void ReadComponentChunk(ref int position, int currentIndex)
            {
                var componentCount = *(int*)(ptr + position);
                position += 4;
                var componentCapacity = *(int*)(ptr + position);
                position += 4;
                var componentSize = *(int*)(ptr + position);
                position += 4;
                var maxEntityId = *(int*)(ptr + position);
                position += 4;

                var chunk = (ComponentChunk*)UnsafeUtility.Malloc(
                    UnsafeUtility.SizeOf<ComponentChunk>(),
                    UnsafeUtility.AlignOf<ComponentChunk>(),
                    Allocator.Persistent);

                *chunk = new ComponentChunk(componentSize, componentCapacity, currentIndex, manager);
                chunk->length = componentCount;
                chunk->maxEntityId = maxEntityId;

                if (chunk->entityIds == null || chunk->capacity < componentCount)
                {
                    if (chunk->entityIds != null)
                        UnsafeUtility.Free(chunk->entityIds, Allocator.Persistent);

                    chunk->entityIds = (int*)UnsafeUtility.Malloc(
                        sizeof(int) * componentCapacity,
                        UnsafeUtility.AlignOf<int>(),
                        Allocator.Persistent);
                }

                if (chunk->componentIndices == null || maxEntityId >= chunk->maxEntityId)
                {
                    if (chunk->componentIndices != null)
                        UnsafeUtility.Free(chunk->componentIndices, Allocator.Persistent);

                    var newSize = maxEntityId + 1;
                    chunk->componentIndices = (int*)UnsafeUtility.Malloc(
                        sizeof(int) * newSize,
                        UnsafeUtility.AlignOf<int>(),
                        Allocator.Persistent);
                    UnsafeUtility.MemSet(chunk->componentIndices, 0xFF, sizeof(int) * newSize);
                }

                for (var i = 0; i < componentCount; i++)
                {
                    chunk->entityIds[i] = *(int*)(ptr + position);
                    position += 4;
                }

                for (var i = 0; i <= maxEntityId; i++)
                {
                    chunk->componentIndices[i] = *(int*)(ptr + position);
                    position += 4;
                }

                for (var i = 0; i < componentCount; i++)
                {
                    UnsafeUtility.MemCpy(
                        (byte*)chunk->ptr + i * componentSize,
                        ptr + position,
                        componentSize);
                    position += componentSize;
                }

                manager->chunks.Ptr[currentIndex] = ChunkUnion.FromComponentChunk(chunk);
            }
        }
    }
}
