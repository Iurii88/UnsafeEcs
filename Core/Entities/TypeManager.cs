using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Core.Entities
{
    public static class TypeManager
    {
        public static readonly SharedStatic<int> TypeCount = SharedStatic<int>.GetOrCreate<TypeCountKey>();

        public static readonly SharedStatic<UnsafeParallelHashMap<long, int>> TypeToIndex =
            SharedStatic<UnsafeParallelHashMap<long, int>>.GetOrCreate<TypeToIndexKey>();

        public static readonly SharedStatic<UnsafeList<long>> TypeOrder =
            SharedStatic<UnsafeList<long>>.GetOrCreate<TypeOrderKey>();

        public static readonly SharedStatic<UnsafeList<int>> TypeSizes =
            SharedStatic<UnsafeList<int>>.GetOrCreate<TypeSizesKey>();

        public static readonly SharedStatic<UnsafeList<bool>> IsBufferList =
            SharedStatic<UnsafeList<bool>>.GetOrCreate<IsBufferListKey>();

        private struct TypeCountKey
        {
        }

        private struct TypeToIndexKey
        {
        }

        private struct TypeOrderKey
        {
        }

        private struct TypeSizesKey
        {
        }

        private struct IsBufferListKey
        {
        }

        public static void Initialize()
        {
            TypeToIndex.Data = new UnsafeParallelHashMap<long, int>(32, Allocator.Persistent);
            TypeOrder.Data = new UnsafeList<long>(32, Allocator.Persistent);
            TypeSizes.Data = new UnsafeList<int>(32, Allocator.Persistent);
            IsBufferList.Data = new UnsafeList<bool>(32, Allocator.Persistent);
            TypeCount.Data = 0;
        }

        public static void Dispose()
        {
            TypeToIndex.Data.Dispose();
            TypeOrder.Data.Dispose();
            TypeSizes.Data.Dispose();
            IsBufferList.Data.Dispose();
        }

        [BurstCompile]
        public static int GetComponentTypeIndex<T>() where T : unmanaged, IComponent
        {
            return RegisterType<T>();
        }

        [BurstCompile]
        public static int GetBufferTypeIndex<T>() where T : unmanaged, IBufferElement
        {
            return RegisterBufferType<T>();
        }

        [BurstCompile]
        public static int GetTypeIndexFromHash(long hash)
        {
            if (TypeToIndex.Data.TryGetValue(hash, out var index))
                return index;

            var newIndex = Interlocked.Increment(ref TypeCount.Data) - 1;
            TypeToIndex.Data.Add(hash, newIndex);
            TypeOrder.Data.Add(hash);

            // Ensure the buffer list has the same length as other lists
            while (IsBufferList.Data.Length <= newIndex)
                IsBufferList.Data.Add(false);

            return newIndex;
        }

        [BurstCompile]
        public static int RegisterType<T>() where T : unmanaged, IComponent
        {
            var hash = BurstRuntime.GetHashCode64<T>();
            if (TypeToIndex.Data.TryGetValue(hash, out var index))
                return index;

            var newIndex = Interlocked.Increment(ref TypeCount.Data) - 1;
            TypeToIndex.Data.Add(hash, newIndex);
            TypeOrder.Data.Add(hash);
            TypeSizes.Data.Add(UnsafeUtility.SizeOf<T>());
            IsBufferList.Data.Add(false); // Not a buffer type
            return newIndex;
        }

        [BurstCompile]
        public static int RegisterBufferType<T>() where T : unmanaged, IBufferElement
        {
            var hash = BurstRuntime.GetHashCode64<T>();
            if (TypeToIndex.Data.TryGetValue(hash, out var index))
                return index;

            var newIndex = Interlocked.Increment(ref TypeCount.Data) - 1;
            TypeToIndex.Data.Add(hash, newIndex);
            TypeOrder.Data.Add(hash);
            TypeSizes.Data.Add(UnsafeUtility.SizeOf<T>());
            IsBufferList.Data.Add(true); // Mark as buffer type
            return newIndex;
        }

        [BurstCompile]
        public static int GetTypeSizeByIndex(int index)
        {
            if (index < 0 || index >= TypeSizes.Data.Length)
                return 0;

            return TypeSizes.Data[index];
        }

        [BurstCompile]
        public static bool IsBufferType(int typeIndex)
        {
            if (typeIndex < 0 || typeIndex >= IsBufferList.Data.Length)
                return false;

            return IsBufferList.Data[typeIndex];
        }

        public static void Clear()
        {
            TypeToIndex.Data.Clear();
            TypeOrder.Data.Clear();
            TypeSizes.Data.Clear();
            IsBufferList.Data.Clear();
            TypeCount.Data = 0;
        }
    }
}