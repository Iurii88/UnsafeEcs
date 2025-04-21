using UnsafeEcs.Additions.Components;
using UnsafeEcs.Additions.Groups;
using UnsafeEcs.Core.Bootstrap.Attributes;
using UnsafeEcs.Core.Entities;
using UnsafeEcs.Core.Systems;

namespace UnsafeEcs.Additions.Systems
{
    [UpdateInGroup(typeof(CleanUpSystemGroup))]
    public class DestroySystem : SystemBase
    {
        private EntityQuery m_destroyQuery;

        public override void OnAwake()
        {
            base.OnAwake();
            m_destroyQuery = CreateQuery().With<Destroy>();
        }

        public override void OnUpdate()
        {
            m_destroyQuery.ForEach((ref Entity entity) =>
            {
                entity.Destroy();
            });
        }
    }
}