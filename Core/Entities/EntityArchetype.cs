using System;
using System.Runtime.CompilerServices;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Core.Entities
{
    public partial struct EntityArchetype : IEquatable<EntityArchetype>
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
        
        public bool Equals(EntityArchetype other) => componentBits.Equals(other.componentBits);

        public override bool Equals(object obj) =>
            obj is EntityArchetype other && Equals(other);

        public override int GetHashCode() => componentBits.GetHashCode();

        public void Clear()
        {
            componentBits.Clear();
        }
    }
}