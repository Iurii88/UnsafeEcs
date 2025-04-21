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
            m_componentBits.SetComponent(ComponentTypeManager.GetTypeIndex<T>());
            return this;
        }

        public EntityArchetype Build()
        {
            return new EntityArchetype { componentBits = m_componentBits };
        }
    }
}