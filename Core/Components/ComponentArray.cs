using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Entities;

namespace UnsafeEcs.Core.Components
{
    public unsafe partial struct ComponentArray<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction] public void* ptr;
        public int length;
        [NativeDisableUnsafePtrRestriction] public int* componentIndices; // Maps entity ID to component index
        public int maxEntityId; // Highest entity ID in the mapping

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var itemPtr = (byte*)ptr + index * UnsafeUtility.SizeOf<T>();
                return ref ((T*)itemPtr)[0];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(Entity entity)
        {
            // Check bounds first to avoid memory access violation
            if (entity.id > maxEntityId)
                throw new InvalidOperationException($"Entity {entity.id} does not have this component");

            int index = componentIndices[entity.id];
            if (index < 0)
                throw new InvalidOperationException($"Entity {entity.id} does not have this component");

            return ref this[index];
        }

        public bool Has(Entity entity)
        {
            return entity.id <= maxEntityId && componentIndices[entity.id] >= 0;
        }

        public bool TryGet(Entity entity, out T component)
        {
            component = default;

            if (entity.id > maxEntityId)
                return false;

            int index = componentIndices[entity.id];
            if (index < 0)
                return false;

            component = this[index];
            return true;
        }
    }
}