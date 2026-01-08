using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.Components.Managed;
using UnsafeEcs.Core.DynamicBuffers;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Editor
{
    public class ComponentDataReader
    {
        public struct FieldData
        {
            public string Name;
            public string TypeName;
            public string Value;
            public bool IsNested;
            public List<FieldData> NestedFields;
        }

        public struct ManagedRefData
        {
            public string TypeName;
            public int ObjectId;
            public int Version;
            public int StorageId;
            public object ResolvedObject;
            public List<FieldData> ObjectFields;
        }

        public struct BufferData
        {
            public string ElementTypeName;
            public int Length;
            public int Capacity;
            public List<List<FieldData>> Elements;
        }

        private readonly World m_world;

        public World World => m_world;

        public ComponentDataReader(World world)
        {
            m_world = world;
        }

        public unsafe List<FieldData> ReadComponentData(int entityId, int componentIndex)
        {
            var result = new List<FieldData>();
            var type = ComponentTypeRegistry.GetTypeByIndex(componentIndex);

            if (type == null)
                return result;

            ref var entityManager = ref m_world.EntityManager;

            if (componentIndex >= entityManager.chunks.Length)
                return result;

            var chunkUnion = entityManager.chunks.Ptr[componentIndex];
            if (!chunkUnion.IsValid)
                return result;

            // Check if it's a buffer type
            if (TypeManager.IsBufferType(componentIndex))
            {
                return result; // Buffers handled separately
            }

            var chunk = chunkUnion.AsComponentChunk();
            if (chunk == null)
                return result;

            var componentPtr = chunk->GetComponentPtr(entityId);
            if (componentPtr == null)
                return result;

            ReadStructFields(componentPtr, type, result);
            return result;
        }

        public unsafe ManagedRefData? ReadManagedRefData(int entityId, int componentIndex)
        {
            var type = ComponentTypeRegistry.GetTypeByIndex(componentIndex);
            if (type == null || !type.IsGenericType)
                return null;

            if (type.GetGenericTypeDefinition() != typeof(ManagedRef<>))
                return null;

            ref var entityManager = ref m_world.EntityManager;

            if (componentIndex >= entityManager.chunks.Length)
                return null;

            var chunkUnion = entityManager.chunks.Ptr[componentIndex];
            if (!chunkUnion.IsValid)
                return null;

            var chunk = chunkUnion.AsComponentChunk();
            if (chunk == null)
                return null;

            var componentPtr = chunk->GetComponentPtr(entityId);
            if (componentPtr == null)
                return null;

            // Read ManagedRef fields (objectId, version, storageId)
            var innerType = type.GetGenericArguments()[0];

            // ManagedRef<T> has fields: objectId, version, storageId (all int)
            var objectIdField = type.GetField("objectId", BindingFlags.Public | BindingFlags.Instance);
            var versionField = type.GetField("version", BindingFlags.Public | BindingFlags.Instance);
            var storageIdField = type.GetField("storageId", BindingFlags.Public | BindingFlags.Instance);

            if (objectIdField == null || versionField == null || storageIdField == null)
                return null;

            var objectId = ReadFieldValue<int>(componentPtr, objectIdField);
            var version = ReadFieldValue<int>(componentPtr, versionField);
            var storageId = ReadFieldValue<int>(componentPtr, storageIdField);

            var refData = new ManagedRefData
            {
                TypeName = innerType.Name,
                ObjectId = objectId,
                Version = version,
                StorageId = storageId,
                ObjectFields = new List<FieldData>()
            };

            // Try to resolve the managed object using the World's managed storage
            try
            {
                var storage = m_world.managedStorage;
                if (storage != null)
                {
                    // Get the type index for the inner type
                    var typeId = ManagedTypeManager.GetTypeIndexFromType(innerType);
                    if (typeId >= 0)
                    {
                        var resolvedObj = storage.GetByTypeId(typeId, objectId, version);
                        if (resolvedObj != null)
                        {
                            refData.ResolvedObject = resolvedObj;
                            ReadManagedObjectFields(resolvedObj, refData.ObjectFields);
                        }
                    }
                }
            }
            catch
            {
                // Failed to resolve - that's okay, we still show the IDs
            }

            return refData;
        }

        public unsafe BufferData? ReadBufferData(int entityId, int componentIndex)
        {
            if (!TypeManager.IsBufferType(componentIndex))
                return null;

            var type = ComponentTypeRegistry.GetTypeByIndex(componentIndex);
            if (type == null)
                return null;

            ref var entityManager = ref m_world.EntityManager;

            if (componentIndex >= entityManager.chunks.Length)
                return null;

            var chunkUnion = entityManager.chunks.Ptr[componentIndex];
            if (!chunkUnion.IsValid)
                return null;

            var bufferChunk = chunkUnion.AsBufferChunk();
            if (bufferChunk == null)
                return null;

            if (!bufferChunk->TryGetBufferIndex(entityId, out var bufferIndex))
                return null;

            var headerPtr = (BufferHeader*)(bufferChunk->ptr + bufferIndex * bufferChunk->headerSize);

            var bufferData = new BufferData
            {
                ElementTypeName = type.Name,
                Length = headerPtr->length,
                Capacity = headerPtr->capacity,
                Elements = new List<List<FieldData>>()
            };

            // Read buffer elements (limit to first 20 for performance)
            var elementSize = bufferChunk->elementSize;
            var maxElements = Math.Min(headerPtr->length, 20);

            for (var i = 0; i < maxElements; i++)
            {
                var elementPtr = headerPtr->pointer + i * elementSize;
                var elementFields = new List<FieldData>();
                ReadStructFields(elementPtr, type, elementFields);
                bufferData.Elements.Add(elementFields);
            }

            return bufferData;
        }

        private unsafe void ReadStructFields(void* ptr, Type type, List<FieldData> result)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var fieldData = ReadField(ptr, field);
                result.Add(fieldData);
            }
        }

        private unsafe FieldData ReadField(void* structPtr, FieldInfo field)
        {
            var fieldData = new FieldData
            {
                Name = field.Name,
                TypeName = GetFriendlyTypeName(field.FieldType),
                IsNested = false,
                NestedFields = null
            };

            var fieldOffset = UnsafeUtility.GetFieldOffset(field);
            var fieldPtr = (byte*)structPtr + fieldOffset;

            fieldData.Value = ReadFieldValueAsString(fieldPtr, field.FieldType);

            // Handle nested structs (but not primitives or common types)
            if (field.FieldType.IsValueType && !field.FieldType.IsPrimitive && !field.FieldType.IsEnum &&
                field.FieldType != typeof(decimal) && !IsCommonMathType(field.FieldType))
            {
                fieldData.IsNested = true;
                fieldData.NestedFields = new List<FieldData>();
                ReadStructFields(fieldPtr, field.FieldType, fieldData.NestedFields);
            }

            return fieldData;
        }

        private unsafe string ReadFieldValueAsString(void* ptr, Type type)
        {
            try
            {
                if (type == typeof(int)) return (*(int*)ptr).ToString();
                if (type == typeof(uint)) return (*(uint*)ptr).ToString();
                if (type == typeof(float)) return (*(float*)ptr).ToString("F4");
                if (type == typeof(double)) return (*(double*)ptr).ToString("F6");
                if (type == typeof(bool)) return (*(bool*)ptr).ToString();
                if (type == typeof(byte)) return (*(byte*)ptr).ToString();
                if (type == typeof(sbyte)) return (*(sbyte*)ptr).ToString();
                if (type == typeof(short)) return (*(short*)ptr).ToString();
                if (type == typeof(ushort)) return (*(ushort*)ptr).ToString();
                if (type == typeof(long)) return (*(long*)ptr).ToString();
                if (type == typeof(ulong)) return (*(ulong*)ptr).ToString();
                if (type == typeof(char)) return (*(char*)ptr).ToString();

                // Unity math types
                if (type == typeof(float2))
                {
                    var v = *(float2*)ptr;
                    return $"({v.x:F2}, {v.y:F2})";
                }
                if (type == typeof(float3))
                {
                    var v = *(float3*)ptr;
                    return $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
                }
                if (type == typeof(float4))
                {
                    var v = *(float4*)ptr;
                    return $"({v.x:F2}, {v.y:F2}, {v.z:F2}, {v.w:F2})";
                }
                if (type == typeof(int2))
                {
                    var v = *(int2*)ptr;
                    return $"({v.x}, {v.y})";
                }
                if (type == typeof(int3))
                {
                    var v = *(int3*)ptr;
                    return $"({v.x}, {v.y}, {v.z})";
                }
                if (type == typeof(int4))
                {
                    var v = *(int4*)ptr;
                    return $"({v.x}, {v.y}, {v.z}, {v.w})";
                }
                if (type == typeof(quaternion))
                {
                    var q = *(quaternion*)ptr;
                    return $"({q.value.x:F2}, {q.value.y:F2}, {q.value.z:F2}, {q.value.w:F2})";
                }

                // Unity types
                if (type == typeof(Vector2))
                {
                    var v = *(Vector2*)ptr;
                    return $"({v.x:F2}, {v.y:F2})";
                }
                if (type == typeof(Vector3))
                {
                    var v = *(Vector3*)ptr;
                    return $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
                }
                if (type == typeof(Vector4))
                {
                    var v = *(Vector4*)ptr;
                    return $"({v.x:F2}, {v.y:F2}, {v.z:F2}, {v.w:F2})";
                }
                if (type == typeof(Quaternion))
                {
                    var q = *(Quaternion*)ptr;
                    return $"({q.x:F2}, {q.y:F2}, {q.z:F2}, {q.w:F2})";
                }
                if (type == typeof(Color))
                {
                    var c = *(Color*)ptr;
                    return $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";
                }
                if (type == typeof(Color32))
                {
                    var c = *(Color32*)ptr;
                    return $"RGBA({c.r}, {c.g}, {c.b}, {c.a})";
                }

                // Enums
                if (type.IsEnum)
                {
                    var underlyingType = Enum.GetUnderlyingType(type);
                    object enumValue;
                    if (underlyingType == typeof(int))
                        enumValue = *(int*)ptr;
                    else if (underlyingType == typeof(byte))
                        enumValue = *(byte*)ptr;
                    else if (underlyingType == typeof(short))
                        enumValue = *(short*)ptr;
                    else if (underlyingType == typeof(long))
                        enumValue = *(long*)ptr;
                    else
                        enumValue = *(int*)ptr;

                    return Enum.ToObject(type, enumValue).ToString();
                }

                // Nested struct - show type name
                if (type.IsValueType)
                    return $"[{type.Name}]";

                return "?";
            }
            catch
            {
                return "?";
            }
        }

        private unsafe T ReadFieldValue<T>(void* structPtr, FieldInfo field) where T : unmanaged
        {
            var offset = UnsafeUtility.GetFieldOffset(field);
            return *(T*)((byte*)structPtr + offset);
        }

        private void ReadManagedObjectFields(object obj, List<FieldData> result)
        {
            if (obj == null)
                return;

            var type = obj.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var fieldData = new FieldData
                {
                    Name = field.Name,
                    TypeName = GetFriendlyTypeName(field.FieldType),
                    IsNested = false
                };

                try
                {
                    var value = field.GetValue(obj);
                    fieldData.Value = value?.ToString() ?? "null";

                    // Limit string length
                    if (fieldData.Value.Length > 100)
                        fieldData.Value = fieldData.Value.Substring(0, 97) + "...";
                }
                catch
                {
                    fieldData.Value = "?";
                }

                result.Add(fieldData);
            }

            // Also read properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                    continue;

                var fieldData = new FieldData
                {
                    Name = prop.Name,
                    TypeName = GetFriendlyTypeName(prop.PropertyType),
                    IsNested = false
                };

                try
                {
                    var value = prop.GetValue(obj);
                    fieldData.Value = value?.ToString() ?? "null";

                    if (fieldData.Value.Length > 100)
                        fieldData.Value = fieldData.Value.Substring(0, 97) + "...";
                }
                catch
                {
                    fieldData.Value = "?";
                }

                result.Add(fieldData);
            }
        }

        private string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(uint)) return "uint";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(sbyte)) return "sbyte";
            if (type == typeof(short)) return "short";
            if (type == typeof(ushort)) return "ushort";
            if (type == typeof(long)) return "long";
            if (type == typeof(ulong)) return "ulong";
            if (type == typeof(char)) return "char";
            if (type == typeof(string)) return "string";
            if (type == typeof(object)) return "object";

            if (type.IsGenericType)
            {
                var baseName = type.Name;
                var tickIndex = baseName.IndexOf('`');
                if (tickIndex > 0)
                    baseName = baseName.Substring(0, tickIndex);

                var args = type.GetGenericArguments();
                return $"{baseName}<{string.Join(", ", Array.ConvertAll(args, GetFriendlyTypeName))}>";
            }

            return type.Name;
        }

        private bool IsCommonMathType(Type type)
        {
            return type == typeof(float2) || type == typeof(float3) || type == typeof(float4) ||
                   type == typeof(int2) || type == typeof(int3) || type == typeof(int4) ||
                   type == typeof(quaternion) ||
                   type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                   type == typeof(Quaternion) || type == typeof(Color) || type == typeof(Color32);
        }
    }
}
