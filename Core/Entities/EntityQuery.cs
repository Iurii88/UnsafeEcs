using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Core.Entities
{
    public unsafe partial struct EntityQuery : IEquatable<EntityQuery>
    {
        public ComponentBits withMask;
        public ComponentBits withoutMask;
        public ComponentBits withAnyMask;
        public ComponentBits componentBits => withMask | withoutMask | withAnyMask;

        [NativeDisableUnsafePtrRestriction] private readonly EntityManager* m_manager;

        internal EntityQuery(EntityManager* managerPtr)
        {
            m_manager = managerPtr;
            withMask = new ComponentBits();
            withoutMask = new ComponentBits();
            withAnyMask = new ComponentBits();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MatchesQuery(in ComponentBits otherComponentBits)
        {
            return otherComponentBits.HasAll(withMask) &&
                   !otherComponentBits.HasAny(withoutMask) &&
                   (withAnyMask.IsEmpty || otherComponentBits.HasAny(withAnyMask));
        }

         [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery With<T>() where T : unmanaged, IComponent
        {
            var index = ComponentTypeManager.GetTypeIndex<T>();
            withMask.SetComponent(index);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery With<T1, T2>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
        {
            return With<T1>().With<T2>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery With<T1, T2, T3>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
        {
            return With<T1, T2>().With<T3>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery With<T1, T2, T3, T4>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
        {
            return With<T1, T2, T3>().With<T4>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery With<T1, T2, T3, T4, T5>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
        {
            return With<T1, T2, T3, T4>().With<T5>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery Without<T>() where T : unmanaged, IComponent
        {
            var index = ComponentTypeManager.GetTypeIndex<T>();
            withoutMask.SetComponent(index);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery Without<T1, T2>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
        {
            return Without<T1>().Without<T2>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery Without<T1, T2, T3>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
        {
            return Without<T1, T2>().Without<T3>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery Without<T1, T2, T3, T4>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
        {
            return Without<T1, T2, T3>().Without<T4>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery Without<T1, T2, T3, T4, T5>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
        {
            return Without<T1, T2, T3, T4>().Without<T5>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery WithAny<T>() where T : unmanaged, IComponent
        {
            var index = ComponentTypeManager.GetTypeIndex<T>();
            withAnyMask.SetComponent(index);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery WithAny<T1, T2>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
        {
            return WithAny<T1>().WithAny<T2>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery WithAny<T1, T2, T3>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
        {
            return WithAny<T1, T2>().WithAny<T3>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery WithAny<T1, T2, T3, T4>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
        {
            return WithAny<T1, T2, T3>().WithAny<T4>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityQuery WithAny<T1, T2, T3, T4, T5>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
        {
            return WithAny<T1, T2, T3, T4>().WithAny<T5>();
        }

        public ReadOnlySpan<Entity> FetchReadOnly()
        {
            return m_manager->QueryEntitiesReadOnly(ref this);
        }

        public UnsafeList<Entity> Fetch()
        {
            return m_manager->QueryEntities(ref this);
        }
        
        public override bool Equals(object obj)
        {
            return obj is EntityQuery other && Equals(other);
        }

        public bool Equals(EntityQuery other)
        {
            return withMask.Equals(other.withMask) &&
                   withoutMask.Equals(other.withoutMask) &&
                   withAnyMask.Equals(other.withAnyMask);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(withMask, withoutMask, withAnyMask);
        }
    }
}