using System;
using System.Collections.Generic;
using System.Reflection;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.Components.Managed;

namespace UnsafeEcs.Editor
{
    public static class ComponentTypeRegistry
    {
        private static readonly Dictionary<int, Type> s_indexToType = new();
        private static readonly Dictionary<Type, Type> s_managedRefToInnerType = new();
        private static bool s_initialized;

        public static void Initialize()
        {
            if (s_initialized)
                return;

            s_initialized = true;
        }

        public static void Refresh()
        {
            s_indexToType.Clear();
            s_managedRefToInnerType.Clear();
        }

        public static Type GetTypeByIndex(int index)
        {
            if (s_indexToType.TryGetValue(index, out var type))
                return type;

            // Try to find type by looking up hash from TypeManager's runtime registry
            if (index >= 0 && index < TypeManager.TypeOrder.Data.Length)
            {
                var hash = TypeManager.TypeOrder.Data[index];
                type = TypeManager.GetTypeFromHash(hash);
                if (type != null)
                {
                    s_indexToType[index] = type;

                    // Cache ManagedRef inner type
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ManagedRef<>))
                    {
                        var innerType = type.GetGenericArguments()[0];
                        s_managedRefToInnerType[type] = innerType;
                    }

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

            // Try to extract it
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ManagedRef<>))
            {
                innerType = type.GetGenericArguments()[0];
                s_managedRefToInnerType[type] = innerType;
                return innerType;
            }

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
