using System.Runtime.CompilerServices;
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

        public static readonly SharedStatic<UnsafeList<int>> TypeSizes =
            SharedStatic<UnsafeList<int>>.GetOrCreate<TypeSizesKey>();

        public static readonly SharedStatic<UnsafeList<bool>> IsBufferList =
            SharedStatic<UnsafeList<bool>>.GetOrCreate<IsBufferListKey>();

        private static class ComponentTypeCache<T> where T : unmanaged, IComponent
        {
            public static readonly SharedStatic<int> TypeIndex = SharedStatic<int>.GetOrCreate<T>();
            public static readonly SharedStatic<bool> IsInitialized = SharedStatic<bool>.GetOrCreate<InitializedKey>();

            private struct InitializedKey
            {
            }
        }

        private static class BufferTypeCache<T> where T : unmanaged, IBufferElement
        {
            public static readonly SharedStatic<int> TypeIndex = SharedStatic<int>.GetOrCreate<T>();
            public static readonly SharedStatic<bool> IsInitialized = SharedStatic<bool>.GetOrCreate<InitializedKey>();

            private struct InitializedKey
            {
            }
        }

        private struct TypeCountKey
        {
        }

        private struct TypeToIndexKey
        {
        }

        private struct TypeSizesKey
        {
        }

        private struct IsBufferListKey
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Initialize()
        {
            if (!TypeToIndex.Data.IsCreated)
            {
                TypeToIndex.Data = new UnsafeParallelHashMap<long, int>(32, Allocator.Persistent);
            }

            if (!TypeSizes.Data.IsCreated)
            {
                TypeSizes.Data = new UnsafeList<int>(32, Allocator.Persistent);
            }

            if (!IsBufferList.Data.IsCreated)
            {
                IsBufferList.Data = new UnsafeList<bool>(32, Allocator.Persistent);
            }

            //TypeCount.Data = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Dispose()
        {
            //DisposePrivate();
        }

        private static void DisposePrivate()
        {
            TypeToIndex.Data.Dispose();
            TypeSizes.Data.Dispose();
            IsBufferList.Data.Dispose();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterDomainReloadCleanup()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= DisposePrivate;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += DisposePrivate;
        }
#endif

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetComponentTypeIndex<T>() where T : unmanaged, IComponent
        {
            var isInitialized = *(bool*)ComponentTypeCache<T>.IsInitialized.UnsafeDataPointer;
            if (isInitialized)
                return *(int*)ComponentTypeCache<T>.TypeIndex.UnsafeDataPointer;

            var index = RegisterType<T>();
            ComponentTypeCache<T>.TypeIndex.Data = index;
            ComponentTypeCache<T>.IsInitialized.Data = true;
            return index;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBufferTypeIndex<T>() where T : unmanaged, IBufferElement
        {
            if (BufferTypeCache<T>.IsInitialized.Data)
                return BufferTypeCache<T>.TypeIndex.Data;

            var index = RegisterBufferType<T>();
            BufferTypeCache<T>.TypeIndex.Data = index;
            BufferTypeCache<T>.IsInitialized.Data = true;
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

            EnsureCapacity(newIndex);

            TypeSizes.Data[newIndex] = 0;
            IsBufferList.Data[newIndex] = false;

            return newIndex;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int RegisterType<T>() where T : unmanaged, IComponent
        {
            var hash = BurstRuntime.GetHashCode64<T>();

            if (TypeToIndex.Data.TryGetValue(hash, out var index))
                return index;

            var newIndex = Interlocked.Increment(ref TypeCount.Data) - 1;
            TypeToIndex.Data.Add(hash, newIndex);

            EnsureCapacity(newIndex);

            TypeSizes.Data[newIndex] = UnsafeUtility.SizeOf<T>();
            IsBufferList.Data[newIndex] = false;

            return newIndex;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int RegisterBufferType<T>() where T : unmanaged, IBufferElement
        {
            var hash = BurstRuntime.GetHashCode64<T>();

            if (TypeToIndex.Data.TryGetValue(hash, out var index))
                return index;

            var newIndex = Interlocked.Increment(ref TypeCount.Data) - 1;
            TypeToIndex.Data.Add(hash, newIndex);

            EnsureCapacity(newIndex);

            TypeSizes.Data[newIndex] = UnsafeUtility.SizeOf<T>();
            IsBufferList.Data[newIndex] = true;

            return newIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureCapacity(int index)
        {
            if (TypeSizes.Data.Length <= index)
                TypeSizes.Data.Resize(index + 1, NativeArrayOptions.ClearMemory);

            if (IsBufferList.Data.Length <= index)
                IsBufferList.Data.Resize(index + 1, NativeArrayOptions.ClearMemory);
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTypeSizeByIndex(int index)
        {
            if (index < 0 || index >= TypeSizes.Data.Length)
                return 0;

            return TypeSizes.Data[index];
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetTypeHashByIndex(int index)
        {
            foreach (var kvp in TypeToIndex.Data)
            {
                if (kvp.Value == index)
                    return kvp.Key;
            }

            return 0;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBufferType(int typeIndex)
        {
            if (typeIndex < 0 || typeIndex >= IsBufferList.Data.Length)
                return false;

            return IsBufferList.Data[typeIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear()
        {
            TypeToIndex.Data.Clear();
            TypeSizes.Data.Clear();
            IsBufferList.Data.Clear();
            TypeCount.Data = 0;
        }
    }
}