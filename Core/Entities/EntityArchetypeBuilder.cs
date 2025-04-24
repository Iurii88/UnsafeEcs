using System.Runtime.CompilerServices;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Core.Entities
{
    public partial struct EntityArchetypeBuilder
    {
        private ComponentBits m_componentBits;

        public static EntityArchetypeBuilder Create()
        {
            return new EntityArchetypeBuilder();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityArchetypeBuilder With<T>() where T : unmanaged, IComponent
        {
            m_componentBits.SetComponent(TypeManager.GetComponentTypeIndex<T>());
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityArchetypeBuilder With<T1, T2>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
        {
            m_componentBits.SetComponent(TypeManager.GetComponentTypeIndex<T1>());
            m_componentBits.SetComponent(TypeManager.GetComponentTypeIndex<T2>());
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityArchetypeBuilder With<T1, T2, T3>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
        {
            m_componentBits.SetComponent(TypeManager.GetComponentTypeIndex<T1>());
            m_componentBits.SetComponent(TypeManager.GetComponentTypeIndex<T2>());
            m_componentBits.SetComponent(TypeManager.GetComponentTypeIndex<T3>());
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityArchetypeBuilder With<T1, T2, T3, T4>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
        {
            m_componentBits.SetComponent(TypeManager.GetComponentTypeIndex<T1>());
            m_componentBits.SetComponent(TypeManager.GetComponentTypeIndex<T2>());
            m_componentBits.SetComponent(TypeManager.GetComponentTypeIndex<T3>());
            m_componentBits.SetComponent(TypeManager.GetComponentTypeIndex<T4>());
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityArchetypeBuilder With<T1, T2, T3, T4, T5>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
        {
            m_componentBits.SetComponent(TypeManager.GetComponentTypeIndex<T1>());
            m_componentBits.SetComponent(TypeManager.GetComponentTypeIndex<T2>());
            m_componentBits.SetComponent(TypeManager.GetComponentTypeIndex<T3>());
            m_componentBits.SetComponent(TypeManager.GetComponentTypeIndex<T4>());
            m_componentBits.SetComponent(TypeManager.GetComponentTypeIndex<T5>());
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityArchetypeBuilder WithBuffer<T>() where T : unmanaged, IBufferElement
        {
            m_componentBits.SetComponent(TypeManager.GetBufferTypeIndex<T>());
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityArchetypeBuilder WithBuffer<T1, T2>()
            where T1 : unmanaged, IBufferElement
            where T2 : unmanaged, IBufferElement
        {
            m_componentBits.SetComponent(TypeManager.GetBufferTypeIndex<T1>());
            m_componentBits.SetComponent(TypeManager.GetBufferTypeIndex<T2>());
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityArchetypeBuilder WithBuffer<T1, T2, T3>()
            where T1 : unmanaged, IBufferElement
            where T2 : unmanaged, IBufferElement
            where T3 : unmanaged, IBufferElement
        {
            m_componentBits.SetComponent(TypeManager.GetBufferTypeIndex<T1>());
            m_componentBits.SetComponent(TypeManager.GetBufferTypeIndex<T2>());
            m_componentBits.SetComponent(TypeManager.GetBufferTypeIndex<T3>());
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityArchetypeBuilder WithBuffer<T1, T2, T3, T4>()
            where T1 : unmanaged, IBufferElement
            where T2 : unmanaged, IBufferElement
            where T3 : unmanaged, IBufferElement
            where T4 : unmanaged, IBufferElement
        {
            m_componentBits.SetComponent(TypeManager.GetBufferTypeIndex<T1>());
            m_componentBits.SetComponent(TypeManager.GetBufferTypeIndex<T2>());
            m_componentBits.SetComponent(TypeManager.GetBufferTypeIndex<T3>());
            m_componentBits.SetComponent(TypeManager.GetBufferTypeIndex<T4>());
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityArchetypeBuilder WithBuffer<T1, T2, T3, T4, T5>()
            where T1 : unmanaged, IBufferElement
            where T2 : unmanaged, IBufferElement
            where T3 : unmanaged, IBufferElement
            where T4 : unmanaged, IBufferElement
            where T5 : unmanaged, IBufferElement
        {
            m_componentBits.SetComponent(TypeManager.GetBufferTypeIndex<T1>());
            m_componentBits.SetComponent(TypeManager.GetBufferTypeIndex<T2>());
            m_componentBits.SetComponent(TypeManager.GetBufferTypeIndex<T3>());
            m_componentBits.SetComponent(TypeManager.GetBufferTypeIndex<T4>());
            m_componentBits.SetComponent(TypeManager.GetBufferTypeIndex<T5>());
            return this;
        }

        public EntityArchetype Build()
        {
            return new EntityArchetype { componentBits = m_componentBits };
        }
    }
}