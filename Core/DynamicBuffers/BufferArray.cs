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
        public UnsafeHashMap<int, int> entityToIndex;

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
        public DynamicBuffer<T> Get(Entity entity) => this[entityToIndex[entity.id]];

        public bool Has(Entity entity) => entityToIndex.ContainsKey(entity.id);

        public bool TryGet(Entity entity, out DynamicBuffer<T> buffer)
        {
            if (entityToIndex.TryGetValue(entity.id, out var index))
            {
                buffer = this[index];
                return true;
            }

            buffer = default;
            return false;
        }
    }
}