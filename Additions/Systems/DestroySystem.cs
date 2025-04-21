using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
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
            var entitiesToDestroy = m_destroyQuery.Fetch();
            new DestroyJob
            {
                entitiesToDestroy = entitiesToDestroy
            }.Schedule().Complete();
        }
        
        [BurstCompile]
        private struct DestroyJob : IJob
        {
            public UnsafeList<Entity> entitiesToDestroy;
            
            public void Execute()
            {
                foreach (var entity in entitiesToDestroy)
                    entity.Destroy();
            }
        }
    }
}