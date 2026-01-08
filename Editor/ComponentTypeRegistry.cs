using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.Components.Managed;

namespace UnsafeEcs.Editor
{
    public static class ComponentTypeRegistry
    {
        private static readonly Dictionary<int, Type> s_indexToType = new();
        private static readonly Dictionary<long, Type> s_hashToType = new();
        private static readonly Dictionary<Type, Type> s_managedRefToInnerType = new();
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

                        if (!type.IsValueType)
                            continue;

                        var isComponent = componentType.IsAssignableFrom(type);
                        var isBuffer = bufferType.IsAssignableFrom(type);

                        if (!isComponent && !isBuffer)
                            continue;

                        var hash = BurstRuntime.GetHashCode64(type);
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
            }

            return null;
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
