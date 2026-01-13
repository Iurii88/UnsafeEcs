using System;
using UnityEditor;
using UnityEngine;

namespace UnsafeEcs.Editor
{
    /// <summary>
    /// Utility for opening C# script files in the IDE from runtime Types.
    /// </summary>
    public static class EditorScriptUtility
    {
        /// <summary>
        /// Opens the C# script file for the given type in the IDE.
        /// </summary>
        /// <param name="type">The type whose source file should be opened.</param>
        /// <returns>True if the file was found and opened, false otherwise.</returns>
        public static bool OpenScriptForType(Type type)
        {
            if (type == null)
                return false;

            // Handle generic types - get the base type definition
            var targetType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            var typeName = targetType.Name;

            // Remove generic arity suffix (e.g., "ManagedRef`1" -> "ManagedRef")
            var tickIndex = typeName.IndexOf('`');
            if (tickIndex > 0)
                typeName = typeName.Substring(0, tickIndex);

            // Search for the script asset
            var guids = AssetDatabase.FindAssets($"{typeName} t:MonoScript");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

                if (script != null)
                {
                    var scriptType = script.GetClass();

                    // Check if this is the correct type
                    if (scriptType == targetType ||
                        (scriptType != null && scriptType.Name == typeName))
                    {
                        AssetDatabase.OpenAsset(script);
                        return true;
                    }

                    // For types that don't have a direct MonoScript class match,
                    // check the file name
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                    if (fileName == typeName)
                    {
                        AssetDatabase.OpenAsset(script);
                        return true;
                    }
                }
            }

            // Fallback: search by namespace + type name
            var fullTypeName = targetType.FullName;
            if (!string.IsNullOrEmpty(fullTypeName))
            {
                // Try searching with the full type name
                var fallbackGuids = AssetDatabase.FindAssets($"t:MonoScript");
                foreach (var guid in fallbackGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

                    if (script != null)
                    {
                        var scriptType = script.GetClass();
                        if (scriptType == targetType)
                        {
                            AssetDatabase.OpenAsset(script);
                            return true;
                        }
                    }
                }
            }

            Debug.LogWarning($"Could not find script file for type: {type.FullName}");
            return false;
        }

        /// <summary>
        /// Checks if the current event is a double-click within the given rect.
        /// </summary>
        public static bool IsDoubleClick(Rect rect)
        {
            return Event.current.type == EventType.MouseDown &&
                   Event.current.clickCount == 2 &&
                   rect.Contains(Event.current.mousePosition);
        }
    }
}
