using System;
using Unity.Burst;
using Unity.Collections;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Core
{
    /// <summary>
    /// Utility class for Burst-compatible debug logging and error throwing.
    /// In Burst context: throws simple exceptions with FixedString messages.
    /// In managed context: throws detailed exceptions with entity names and type info.
    /// </summary>
    public static unsafe class EcsDebug
    {
        /// <summary>
        /// Throws an exception indicating entity doesn't have the specified component.
        /// In Burst: throws simple message with entity id and type index.
        /// In managed: throws detailed message with entity debug string and type name.
        /// </summary>
        public static void ThrowNoComponent<T>(int entityId, int typeIndex, EntityManager* managerPtr) where T : unmanaged
        {
            // Try managed version first (will be skipped in Burst)
            ThrowNoComponentManaged<T>(entityId, managerPtr);
            // Burst fallback
            ThrowNoComponentBurst(entityId, typeIndex);
        }

        /// <summary>
        /// Throws an exception indicating entity doesn't have the specified buffer.
        /// In Burst: throws simple message with entity id and type index.
        /// In managed: throws detailed message with entity debug string and type name.
        /// </summary>
        public static void ThrowNoBuffer<T>(int entityId, int typeIndex, EntityManager* managerPtr) where T : unmanaged
        {
            // Try managed version first (will be skipped in Burst)
            ThrowNoBufferManaged<T>(entityId, managerPtr);
            // Burst fallback
            ThrowNoBufferBurst(entityId, typeIndex);
        }

        /// <summary>
        /// Throws an exception indicating entity is not alive.
        /// In Burst: throws simple message with entity id.
        /// In managed: throws detailed message with entity debug string.
        /// </summary>
        public static void ThrowEntityNotAlive(int entityId, EntityManager* managerPtr)
        {
            // Try managed version first (will be skipped in Burst)
            ThrowEntityNotAliveManaged(entityId, managerPtr);
            // Burst fallback
            ThrowEntityNotAliveBurst(entityId);
        }

        [BurstCompile]
        private static void ThrowNoComponentBurst(int entityId, int typeIndex)
        {
            // Burst-compatible: build message using Unicode.Rune for characters
            FixedString128Bytes msg = default;
            // "Entity "
            msg.Append((Unicode.Rune)'E');
            msg.Append((Unicode.Rune)'n');
            msg.Append((Unicode.Rune)'t');
            msg.Append((Unicode.Rune)'i');
            msg.Append((Unicode.Rune)'t');
            msg.Append((Unicode.Rune)'y');
            msg.Append((Unicode.Rune)' ');
            msg.Append(entityId);
            // " no component idx="
            msg.Append((Unicode.Rune)' ');
            msg.Append((Unicode.Rune)'n');
            msg.Append((Unicode.Rune)'o');
            msg.Append((Unicode.Rune)' ');
            msg.Append((Unicode.Rune)'c');
            msg.Append((Unicode.Rune)'o');
            msg.Append((Unicode.Rune)'m');
            msg.Append((Unicode.Rune)'p');
            msg.Append((Unicode.Rune)' ');
            msg.Append((Unicode.Rune)'i');
            msg.Append((Unicode.Rune)'d');
            msg.Append((Unicode.Rune)'x');
            msg.Append((Unicode.Rune)'=');
            msg.Append(typeIndex);

            throw new InvalidOperationException(msg.ToString());
        }

        [BurstCompile]
        private static void ThrowNoBufferBurst(int entityId, int typeIndex)
        {
            FixedString128Bytes msg = default;
            // "Entity "
            msg.Append((Unicode.Rune)'E');
            msg.Append((Unicode.Rune)'n');
            msg.Append((Unicode.Rune)'t');
            msg.Append((Unicode.Rune)'i');
            msg.Append((Unicode.Rune)'t');
            msg.Append((Unicode.Rune)'y');
            msg.Append((Unicode.Rune)' ');
            msg.Append(entityId);
            // " no buffer idx="
            msg.Append((Unicode.Rune)' ');
            msg.Append((Unicode.Rune)'n');
            msg.Append((Unicode.Rune)'o');
            msg.Append((Unicode.Rune)' ');
            msg.Append((Unicode.Rune)'b');
            msg.Append((Unicode.Rune)'u');
            msg.Append((Unicode.Rune)'f');
            msg.Append((Unicode.Rune)' ');
            msg.Append((Unicode.Rune)'i');
            msg.Append((Unicode.Rune)'d');
            msg.Append((Unicode.Rune)'x');
            msg.Append((Unicode.Rune)'=');
            msg.Append(typeIndex);

            throw new InvalidOperationException(msg.ToString());
        }

        [BurstCompile]
        private static void ThrowEntityNotAliveBurst(int entityId)
        {
            FixedString128Bytes msg = default;
            // "Entity "
            msg.Append((Unicode.Rune)'E');
            msg.Append((Unicode.Rune)'n');
            msg.Append((Unicode.Rune)'t');
            msg.Append((Unicode.Rune)'i');
            msg.Append((Unicode.Rune)'t');
            msg.Append((Unicode.Rune)'y');
            msg.Append((Unicode.Rune)' ');
            msg.Append(entityId);
            // " dead"
            msg.Append((Unicode.Rune)' ');
            msg.Append((Unicode.Rune)'d');
            msg.Append((Unicode.Rune)'e');
            msg.Append((Unicode.Rune)'a');
            msg.Append((Unicode.Rune)'d');

            throw new InvalidOperationException(msg.ToString());
        }

        [BurstDiscard]
        private static void ThrowNoComponentManaged<T>(int entityId, EntityManager* managerPtr) where T : unmanaged
        {
#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
            var entityDebugString = managerPtr != null
                ? managerPtr->GetEntityDebugString(entityId)
                : $"Entity ({entityId})";
            throw new InvalidOperationException($"{entityDebugString} does not have component {typeof(T).Name}");
#endif
        }

        [BurstDiscard]
        private static void ThrowNoBufferManaged<T>(int entityId, EntityManager* managerPtr) where T : unmanaged
        {
#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
            var entityDebugString = managerPtr != null
                ? managerPtr->GetEntityDebugString(entityId)
                : $"Entity ({entityId})";
            throw new InvalidOperationException($"{entityDebugString} does not have buffer {typeof(T).Name}");
#endif
        }

        [BurstDiscard]
        private static void ThrowEntityNotAliveManaged(int entityId, EntityManager* managerPtr)
        {
#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
            var entityDebugString = managerPtr != null
                ? managerPtr->GetEntityDebugString(entityId)
                : $"Entity ({entityId})";
            throw new InvalidOperationException($"{entityDebugString} is not alive");
#endif
        }
    }
}
