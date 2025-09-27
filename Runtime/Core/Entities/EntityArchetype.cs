using System;
using System.Runtime.CompilerServices;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Core.Entities
{
    public struct EntityArchetype : IEquatable<EntityArchetype>
    {
        public ComponentBits componentBits;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent(int index)
        {
            componentBits.SetComponent(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(int index)
        {
            componentBits.RemoveComponent(index);
        }

        public bool Equals(EntityArchetype other)
        {
            return componentBits.Equals(other.componentBits);
        }

        public override bool Equals(object obj)
        {
            return obj is EntityArchetype other && Equals(other);
        }

        public override int GetHashCode()
        {
            return componentBits.GetHashCode();
        }

        public void Clear()
        {
            componentBits.Clear();
        }
    }
}