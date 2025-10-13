using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnsafeEcs.Core.Components
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

        // Global cache version counter for cache invalidation
        public static readonly SharedStatic<int> CacheVersion = SharedStatic<int>.GetOrCreate<CacheVersionKey>();

        public static void Initialize()
        {
            TypeToIndex.Data = new UnsafeParallelHashMap<long, int>(32, Allocator.Persistent);
            TypeOrder.Data = new UnsafeList<long>(32, Allocator.Persistent);
            TypeSizes.Data = new UnsafeList<int>(32, Allocator.Persistent);
            IsBufferList.Data = new UnsafeList<bool>(32, Allocator.Persistent);
            TypeCount.Data = 0;

            // Increment cache version to invalidate all existing caches
            CacheVersion.Data++;
        }

        public static void Dispose()
        {
            TypeToIndex.Data.Dispose();
            TypeOrder.Data.Dispose();
            TypeSizes.Data.Dispose();
            IsBufferList.Data.Dispose();
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetComponentTypeIndex<T>() where T : unmanaged, IComponent
        {
            return GetTypeIndex<T>(false);
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBufferTypeIndex<T>() where T : unmanaged, IBufferElement
        {
            return GetTypeIndex<T>(true);
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int GetTypeIndex<T>(bool isBuffer) where T : unmanaged
        {
            var version = *(int*)Cache<T>.Version.UnsafeDataPointer;
            var cacheVersion = *(int*)CacheVersion.UnsafeDataPointer;

            if (version == cacheVersion)
                return *(int*)Cache<T>.TypeIndex.UnsafeDataPointer;

            var index = RegisterTypeInternal<T>(isBuffer);
            Cache<T>.TypeIndex.Data = index;
            Cache<T>.Version.Data = CacheVersion.Data;
            return index;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int RegisterTypeInternal<T>(bool isBuffer) where T : unmanaged
        {
            var hash = BurstRuntime.GetHashCode64<T>();
            if (TypeToIndex.Data.TryGetValue(hash, out var index))
                return index;

            var newIndex = Interlocked.Increment(ref TypeCount.Data) - 1;
            TypeToIndex.Data.Add(hash, newIndex);
            TypeOrder.Data.Add(hash);
            TypeSizes.Data.Add(UnsafeUtility.SizeOf<T>());
            IsBufferList.Data.Add(isBuffer);
            return newIndex;
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

            // Invalidate all caches by incrementing the global cache version
            CacheVersion.Data++;
        }

        // Type cache static storage
        private static class Cache<T> where T : unmanaged
        {
            // Static field to store component type index
            public static readonly SharedStatic<int> TypeIndex = SharedStatic<int>.GetOrCreate<CacheTypeIndexKey<T>>();

            // Cache version to detect invalidations
            public static readonly SharedStatic<int> Version = SharedStatic<int>.GetOrCreate<CacheVersionKey<T>>();
        }

        // Key types for SharedStatic storage
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

        private struct CacheVersionKey
        {
        }

        // Key types for Cache<T>
        private struct CacheTypeIndexKey<T> where T : unmanaged
        {
        }

        private struct CacheVersionKey<T> where T : unmanaged
        {
        }
    }
}