using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnsafeEcs.Core.Components
{
    public static class ComponentTypeManager
    {
        public static readonly SharedStatic<int> TypeCount = SharedStatic<int>.GetOrCreate<TypeCountKey>();

        public static readonly SharedStatic<UnsafeParallelHashMap<long, int>> TypeToIndex =
            SharedStatic<UnsafeParallelHashMap<long, int>>.GetOrCreate<TypeToIndexKey>();

        public static readonly SharedStatic<UnsafeList<long>> TypeOrder =
            SharedStatic<UnsafeList<long>>.GetOrCreate<TypeOrderKey>();

        private struct TypeCountKey
        {
        }

        private struct TypeToIndexKey
        {
        }

        private struct TypeOrderKey
        {
        }

        public static void Initialize()
        {
            TypeToIndex.Data = new UnsafeParallelHashMap<long, int>(32, Allocator.Persistent);
            TypeOrder.Data = new UnsafeList<long>(32, Allocator.Persistent);
            TypeCount.Data = 0;
        }

        public static void Dispose()
        {
            TypeToIndex.Data.Dispose();
            TypeOrder.Data.Dispose();
        }

        [BurstCompile]
        public static int GetTypeIndex<T>() where T : unmanaged, IComponent
        {
            var hash = BurstRuntime.GetHashCode64<T>();
            return GetTypeIndexFromHash(hash);
        }

        [BurstCompile]
        public static int GetTypeIndexFromHash(long hash)
        {
            if (TypeToIndex.Data.TryGetValue(hash, out var index))
                return index;

            var newIndex = Interlocked.Increment(ref TypeCount.Data) - 1;
            TypeToIndex.Data.Add(hash, newIndex);
            TypeOrder.Data.Add(hash);
            return newIndex;
        }

        public static void Clear()
        {
            TypeToIndex.Data.Clear();
            TypeOrder.Data.Clear();
            TypeCount.Data = 0;
        }
    }
}