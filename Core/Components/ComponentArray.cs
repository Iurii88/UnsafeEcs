using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Core.Components
{
    public unsafe partial struct ComponentArray<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction] public void* ptr;
        public int length;
        public UnsafeHashMap<int, int> entityToIndex;

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var itemPtr = (byte*)ptr + index * UnsafeUtility.SizeOf<T>();
                return ref ((T*)itemPtr)[0];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(Entity entity) => ref this[entityToIndex[entity.id]];

        public bool Has(Entity entity) => entityToIndex.ContainsKey(entity.id);

        public bool TryGet(Entity entity, out T component)
        {
            if (entityToIndex.TryGetValue(entity.id, out var index))
            {
                component = this[index];
                return true;
            }

            component = default;
            return false;
        }
    }
}