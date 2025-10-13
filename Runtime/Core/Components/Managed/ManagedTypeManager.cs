using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnsafeEcs.Core.Components.Managed
{
    public static class ManagedTypeManager
    {
        public static readonly SharedStatic<int> TypeCount = SharedStatic<int>.GetOrCreate<TypeCountKey>();

        public static readonly SharedStatic<UnsafeParallelHashMap<long, int>> TypeToIndex =
            SharedStatic<UnsafeParallelHashMap<long, int>>.GetOrCreate<TypeToIndexKey>();

        public static readonly SharedStatic<UnsafeList<long>> TypeOrder =
            SharedStatic<UnsafeList<long>>.GetOrCreate<TypeOrderKey>();

        // Global cache version counter for cache invalidation
        public static readonly SharedStatic<int> CacheVersion = SharedStatic<int>.GetOrCreate<CacheVersionKey>();

        public static void Initialize()
        {
            TypeToIndex.Data = new UnsafeParallelHashMap<long, int>(32, Allocator.Persistent);
            TypeOrder.Data = new UnsafeList<long>(32, Allocator.Persistent);
            TypeCount.Data = 0;
            CacheVersion.Data++;
        }

        public static void Dispose()
        {
            if (TypeToIndex.Data.IsCreated)
                TypeToIndex.Data.Dispose();
            if (TypeOrder.Data.IsCreated)
                TypeOrder.Data.Dispose();
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTypeIndex<T>() where T : class
        {
            var version = Cache<T>.Version.Data;
            var cacheVersion = CacheVersion.Data;

            if (version == cacheVersion)
                return Cache<T>.TypeIndex.Data;

            var hash = BurstRuntime.GetHashCode64<TypeMarker<T>>();
            var index = GetTypeIndexFromHash(hash);

            Cache<T>.TypeIndex.Data = index;
            Cache<T>.Version.Data = cacheVersion;

            return index;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            CacheVersion.Data++;
        }

        // Type cache static storage
        private static class Cache<T> where T : class
        {
            public static readonly SharedStatic<int> TypeIndex = SharedStatic<int>.GetOrCreate<CacheTypeIndexKey>();
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

        private struct CacheVersionKey
        {
        }

        // Key types for Cache<T>
        private struct CacheTypeIndexKey
        {
        }

        private struct CacheVersionKey<T> where T : class
        {
        }
    }
}