using System;
using Unity.Collections;

namespace UnsafeEcs.Core.Components
{
    /// <summary>
    /// Component that stores entity name for debugging and editor display.
    /// Maximum length is 61 UTF-8 bytes (FixedString64Bytes capacity).
    /// Longer names are automatically truncated.
    /// </summary>
    public struct EntityName : IComponent
    {
        public const int MaxLength = 61; // FixedString64Bytes has 3 bytes header

        public FixedString64Bytes Value;

        public EntityName(string name)
        {
            Value = default;
            if (string.IsNullOrEmpty(name))
                return;

            // Truncate if too long to avoid exception
            var truncated = name.Length > MaxLength ? name.Substring(0, MaxLength) : name;
            Value = new FixedString64Bytes(truncated);
        }

        public EntityName(FixedString64Bytes name)
        {
            Value = name;
        }

        public override string ToString() => Value.ToString();

        public static implicit operator EntityName(string name) => new(name);
        public static implicit operator string(EntityName name) => name.Value.ToString();
    }
}
