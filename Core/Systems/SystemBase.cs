using Unity.Jobs;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.Entities;
using UnsafeEcs.Core.Utils;
using UnsafeEcs.Core.Worlds;

namespace UnsafeEcs.Core.Systems
{
    public abstract class SystemBase
    {
        public World world;
        public JobHandle dependency;
        public ReferenceWrapper<EntityManager> entityManagerWrapper => world.entityManagerWrapper;
        
        public virtual void OnAwake() {}
        public virtual void OnUpdate() {}
        public virtual void OnFixedUpdate() {}
        public virtual void OnDestroy() {}

        protected EntityQuery CreateQuery()
        {
            return world.EntityManager.CreateQuery();
        }

        protected ComponentArray<T> GetComponentArray<T>() where T : unmanaged, IComponent
        {
            return world.EntityManager.GetComponentArray<T>();
        }
    }
}