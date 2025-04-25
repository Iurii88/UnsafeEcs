using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Core.DynamicBuffers
{
    public unsafe partial struct BufferArray<T> where T : unmanaged, IBufferElement
    {
        [NativeDisableUnsafePtrRestriction] public byte* ptr;
        public int length;
        public int headerSize;
        [NativeDisableUnsafePtrRestriction] public int* bufferIndices; // Direct entity ID -> buffer index mapping
        public int maxEntityId; // Maximum entity ID in mapping

        public DynamicBuffer<T> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var headerPtr = ptr + index * headerSize;
                return new DynamicBuffer<T>((BufferHeader*)headerPtr);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DynamicBuffer<T> Get(Entity entity)
        {
            if (entity.id > maxEntityId)
                throw new InvalidOperationException($"Entity {entity.id} does not have this buffer component");

            int index = bufferIndices[entity.id];
            if (index < 0)
                throw new InvalidOperationException($"Entity {entity.id} does not have this buffer component");

            return this[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(Entity entity)
        {
            return entity.id <= maxEntityId && bufferIndices[entity.id] >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(Entity entity, out DynamicBuffer<T> buffer)
        {
            buffer = default;

            if (entity.id > maxEntityId)
                return false;

            int index = bufferIndices[entity.id];
            if (index < 0)
                return false;

            buffer = this[index];
            return true;
        }
    }
}