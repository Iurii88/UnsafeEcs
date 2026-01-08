using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Burst;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.Components.Managed;

namespace UnsafeEcs.Editor
{
    public static class ComponentTypeRegistry
    {
        private static readonly Dictionary<int, Type> s_indexToType = new();
        private static readonly Dictionary<long, Type> s_hashToType = new();
        private static readonly Dictionary<Type, Type> s_managedRefToInnerType = new();
        private static readonly List<Type> s_allManagedTypes = new(); // All class types that could be used with ManagedRef<T>
        private static bool s_initialized;

        public static void Initialize()
        {
            if (s_initialized)
                return;

            s_initialized = true;
            ScanComponentTypes();
        }

        public static void Refresh()
        {
            s_indexToType.Clear();
            s_hashToType.Clear();
            s_managedRefToInnerType.Clear();
            s_allManagedTypes.Clear();
            ScanComponentTypes();
        }

        private static void ScanComponentTypes()
        {
            var componentType = typeof(IComponent);
            var bufferType = typeof(IBufferElement);
            var managedRefGenericType = typeof(ManagedRef<>);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                            continue;

                        // Collect class types for potential ManagedRef<T> lookups
                        if (type.IsClass && !type.IsAbstract)
                        {
                            s_allManagedTypes.Add(type);
                        }

                        if (!type.IsValueType)
                            continue;

                        var isComponent = componentType.IsAssignableFrom(type);
                        var isBuffer = bufferType.IsAssignableFrom(type);

                        if (!isComponent && !isBuffer)
                            continue;

                        // Use the generic hash method for consistency with TypeManager
                        var hash = GetGenericTypeHash(type);
                        s_hashToType[hash] = type;

                        // Check if this is a ManagedRef<T>
                        if (type.IsGenericType && type.GetGenericTypeDefinition() == managedRefGenericType)
                        {
                            var innerType = type.GetGenericArguments()[0];
                            s_managedRefToInnerType[type] = innerType;
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip assemblies that can't be loaded
                }
            }
        }

        public static Type GetTypeByIndex(int index)
        {
            if (s_indexToType.TryGetValue(index, out var type))
                return type;

            // Try to find type by looking up hash from TypeManager
            if (index >= 0 && index < TypeManager.TypeOrder.Data.Length)
            {
                var hash = TypeManager.TypeOrder.Data[index];
                if (s_hashToType.TryGetValue(hash, out type))
                {
                    s_indexToType[index] = type;
                    return type;
                }

                // If not found, try to find ManagedRef<T> types by iterating all known types
                // and checking their generic hash
                type = TryFindManagedRefType(hash);
                if (type != null)
                {
                    s_indexToType[index] = type;
                    s_hashToType[hash] = type;
                    return type;
                }
            }

            return null;
        }

        private static Type TryFindManagedRefType(long targetHash)
        {
            var managedRefGenericType = typeof(ManagedRef<>);

            foreach (var managedType in s_allManagedTypes)
            {
                try
                {
                    var managedRefType = managedRefGenericType.MakeGenericType(managedType);

                    // Use reflection to call the generic BurstRuntime.GetHashCode64<T>()
                    // since BurstRuntime.GetHashCode64(Type) may produce different results
                    var hash = GetGenericTypeHash(managedRefType);
                    if (hash == targetHash)
                    {
                        s_managedRefToInnerType[managedRefType] = managedType;
                        return managedRefType;
                    }
                }
                catch
                {
                    // Some types may not be valid for ManagedRef<T>
                }
            }

            return null;
        }

        private static long GetGenericTypeHash(Type type)
        {
            // Try using the generic method via reflection to get the same hash as Burst
            // We need to find the generic method GetHashCode64<T>() specifically
            foreach (var method in typeof(BurstRuntime).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != nameof(BurstRuntime.GetHashCode64))
                    continue;

                if (!method.IsGenericMethodDefinition)
                    continue;

                if (method.GetParameters().Length != 0)
                    continue;

                try
                {
                    var genericMethod = method.MakeGenericMethod(type);
                    return (long)genericMethod.Invoke(null, null);
                }
                catch
                {
                    // Fall back to type-based hash
                }
            }

            return BurstRuntime.GetHashCode64(type);
        }

        public static string GetTypeNameByIndex(int index)
        {
            var type = GetTypeByIndex(index);
            if (type == null)
                return $"Unknown({index})";

            // For ManagedRef<T>, show as "ManagedRef<T>"
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                var genericArgs = type.GetGenericArguments();
                var baseName = genericDef.Name;
                var tickIndex = baseName.IndexOf('`');
                if (tickIndex > 0)
                    baseName = baseName.Substring(0, tickIndex);

                return $"{baseName}<{string.Join(", ", Array.ConvertAll(genericArgs, t => t.Name))}>";
            }

            return type.Name;
        }

        public static string GetTypeFullNameByIndex(int index)
        {
            var type = GetTypeByIndex(index);
            return type?.FullName ?? $"Unknown({index})";
        }

        public static bool IsBufferType(int index)
        {
            return TypeManager.IsBufferType(index);
        }

        public static bool IsManagedRefType(int index)
        {
            var type = GetTypeByIndex(index);
            if (type == null)
                return false;

            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ManagedRef<>);
        }

        public static Type GetManagedRefInnerType(int index)
        {
            var type = GetTypeByIndex(index);
            if (type == null)
                return null;

            if (s_managedRefToInnerType.TryGetValue(type, out var innerType))
                return innerType;

            return null;
        }

        public static FieldInfo[] GetComponentFields(int index)
        {
            var type = GetTypeByIndex(index);
            if (type == null)
                return Array.Empty<FieldInfo>();

            return type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}
