using System;
using Unity.Burst;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Core
{
    /// <summary>
    /// Utility class for Burst-compatible debug logging and error throwing.
    /// Uses [BurstDiscard] to provide detailed error messages in managed context.
    /// In Burst context: these methods are stripped, so errors must be caught at a higher level.
    /// </summary>
    public static unsafe class EcsDebug
    {
        /// <summary>
        /// Throws an exception indicating entity doesn't have the specified component.
        /// This method is marked [BurstDiscard] - it will be skipped in Burst-compiled code.
        /// </summary>
        [BurstDiscard]
        public static void ThrowNoComponent<T>(int entityId, int typeIndex, EntityManager* managerPtr) where T : unmanaged
        {
#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
            var entityDebugString = managerPtr != null
                ? managerPtr->GetEntityDebugString(entityId)
                : $"Entity ({entityId})";
            throw new InvalidOperationException($"{entityDebugString} does not have component {typeof(T).Name}");
#else
            throw new InvalidOperationException($"Entity {entityId} does not have component (typeIndex={typeIndex})");
#endif
        }

        /// <summary>
        /// Throws an exception indicating entity doesn't have the specified buffer.
        /// This method is marked [BurstDiscard] - it will be skipped in Burst-compiled code.
        /// </summary>
        [BurstDiscard]
        public static void ThrowNoBuffer<T>(int entityId, int typeIndex, EntityManager* managerPtr) where T : unmanaged
        {
#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
            var entityDebugString = managerPtr != null
                ? managerPtr->GetEntityDebugString(entityId)
                : $"Entity ({entityId})";
            throw new InvalidOperationException($"{entityDebugString} does not have buffer {typeof(T).Name}");
#else
            throw new InvalidOperationException($"Entity {entityId} does not have buffer (typeIndex={typeIndex})");
#endif
        }

        /// <summary>
        /// Throws an exception indicating entity is not alive.
        /// This method is marked [BurstDiscard] - it will be skipped in Burst-compiled code.
        /// </summary>
        [BurstDiscard]
        public static void ThrowEntityNotAlive(int entityId, EntityManager* managerPtr)
        {
#if UNSAFE_ECS_VERBOSE_ERRORS || UNITY_EDITOR
            var entityDebugString = managerPtr != null
                ? managerPtr->GetEntityDebugString(entityId)
                : $"Entity ({entityId})";
            throw new InvalidOperationException($"{entityDebugString} is not alive");
#else
            throw new InvalidOperationException($"Entity {entityId} is not alive");
#endif
        }
    }
}
