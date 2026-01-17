using Unity.Burst;
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

        /// <summary>
        /// Creates EntityName from a managed string. NOT Burst-compatible.
        /// For Burst jobs, use the FixedString constructors instead.
        /// </summary>
        [BurstDiscard]
        public static EntityName FromString(string name)
        {
            var result = new EntityName();
            if (string.IsNullOrEmpty(name))
                return result;

            // Truncate if too long to avoid exception
            var truncated = name.Length > MaxLength ? name.Substring(0, MaxLength) : name;
            result.Value = new FixedString64Bytes(truncated);
            return result;
        }

        public EntityName(FixedString32Bytes name)
        {
            Value = default;
            Value.Append(name);
        }

        public EntityName(FixedString64Bytes name)
        {
            Value = name;
        }

        public EntityName(FixedString128Bytes name)
        {
            Value = default;
            // Truncate by copying only what fits
            foreach (var b in name)
            {
                if (Value.Length >= MaxLength) break;
                Value.Append(b);
            }
        }

        public override string ToString() => Value.ToString();

        [BurstDiscard]
        public static implicit operator EntityName(string name) => FromString(name);

        public static implicit operator string(EntityName name) => name.Value.ToString();
    }
}
